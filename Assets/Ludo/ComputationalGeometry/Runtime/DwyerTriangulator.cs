using System;

namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Implements the Dwyer algorithm for Delaunay triangulation.
    /// </summary>
    /// <remarks>
    /// The Dwyer algorithm is a divide-and-conquer approach to Delaunay triangulation that offers
    /// improved performance over incremental algorithms for large point sets. It recursively divides
    /// the point set, triangulates the subsets, and then merges the triangulations.
    /// </remarks>
    [Serializable]
    public class DwyerTriangulator
    {
        /// <summary>
        /// Random number generator for the quicksort algorithm.
        /// </summary>
        private static Random _rand = new(DateTime.Now.Millisecond);

        /// <summary>
        /// Flag indicating whether to use Dwyer's divide-and-conquer algorithm.
        /// </summary>
        private bool _useDwyer = true;

        /// <summary>
        /// Array of vertices to be triangulated, sorted by coordinates.
        /// </summary>
        private Vertex[] _sortarray;

        /// <summary>
        /// Reference to the mesh being triangulated.
        /// </summary>
        private TriangularMesh _triangularMesh;

        /// <summary>
        /// Sorts vertices by their x and y coordinates using a quicksort algorithm.
        /// </summary>
        /// <param name="left">The left index of the range to sort.</param>
        /// <param name="right">The right index of the range to sort.</param>
        /// <remarks>
        /// For small arrays (less than 32 elements), an insertion sort is used for efficiency.
        /// For larger arrays, a quicksort algorithm with random pivot selection is used.
        /// Vertices are sorted primarily by x-coordinate, and secondarily by y-coordinate.
        /// </remarks>
        private void VertexSort(int left, int right)
        {
            int left1 = left;
            int right1 = right;
            if (right - left + 1 < 32 /*0x20*/)
            {
                for (int index1 = left + 1; index1 <= right; ++index1)
                {
                    Vertex vertex = _sortarray[index1];
                    int index2;
                    for (index2 = index1 - 1;
                         index2 >= left && (_sortarray[index2].x > vertex.x ||
                                            _sortarray[index2].x == vertex.x &&
                                            _sortarray[index2].y > vertex.y);
                         --index2)
                        _sortarray[index2 + 1] = _sortarray[index2];
                    _sortarray[index2 + 1] = vertex;
                }
            }
            else
            {
                int index = _rand.Next(left, right);
                double x = _sortarray[index].x;
                double y = _sortarray[index].y;
                --left;
                ++right;
                while (left < right)
                {
                    do
                    {
                        ++left;
                    } while (left <= right && (_sortarray[left].x < x ||
                                               _sortarray[left].x == x && _sortarray[left].y < y));

                    do
                    {
                        --right;
                    } while (left <= right && (_sortarray[right].x > x ||
                                               _sortarray[right].x == x && _sortarray[right].y > y));

                    if (left < right)
                    {
                        (_sortarray[left], _sortarray[right]) = (_sortarray[right], _sortarray[left]);
                    }
                }

                if (left > left1)
                    VertexSort(left1, left);
                if (right1 <= right + 1)
                    return;
                VertexSort(right + 1, right1);
            }
        }

        /// <summary>
        /// Partitions vertices to find the median vertex along a specified axis.
        /// </summary>
        /// <param name="left">The left index of the range to partition.</param>
        /// <param name="right">The right index of the range to partition.</param>
        /// <param name="median">The index where the median should be placed.</param>
        /// <param name="axis">The axis to use for comparison (0 for x-axis, 1 for y-axis).</param>
        /// <remarks>
        /// This method uses a quickselect algorithm to efficiently find the median vertex
        /// along the specified axis. It's used as part of the divide-and-conquer approach
        /// to partition the point set before triangulation.
        /// </remarks>
        private void VertexMedian(int left, int right, int median, int axis)
        {
            int rangeSize = right - left + 1;
            int originalLeft = left;
            int originalRight = right;
            if (rangeSize == 2)
            {
                if (_sortarray[left][axis] <= _sortarray[right][axis] &&
                    (_sortarray[left][axis] != _sortarray[right][axis] ||
                     _sortarray[left][1 - axis] <= _sortarray[right][1 - axis]))
                    return;
                (_sortarray[right], _sortarray[left]) = (_sortarray[left], _sortarray[right]);
            }
            else
            {
                int pivotIndex = _rand.Next(left, right);
                double pivotCoord = _sortarray[pivotIndex][axis];
                double pivotOtherCoord = _sortarray[pivotIndex][1 - axis];
                --left;
                ++right;
                while (left < right)
                {
                    do
                    {
                        ++left;
                    } while (left <= right && (_sortarray[left][axis] < pivotCoord ||
                                               _sortarray[left][axis] == pivotCoord &&
                                               _sortarray[left][1 - axis] < pivotOtherCoord));

                    do
                    {
                        --right;
                    } while (left <= right && (_sortarray[right][axis] > pivotCoord ||
                                               _sortarray[right][axis] == pivotCoord &&
                                               _sortarray[right][1 - axis] > pivotOtherCoord));

                    if (left < right)
                    {
                        (_sortarray[left], _sortarray[right]) = (_sortarray[right], _sortarray[left]);
                    }
                }

                if (left > median)
                {
                    VertexMedian(originalLeft, left - 1, median, axis);
                }

                if (right >= median - 1)
                {
                    return;
                }
                VertexMedian(right + 1, originalRight, median, axis);
            }
        }

        /// <summary>
        /// Recursively partitions vertices by alternating between x and y axes.
        /// </summary>
        /// <param name="left">The left index of the range to partition.</param>
        /// <param name="right">The right index of the range to partition.</param>
        /// <param name="axis">The starting axis to use for comparison (0 for x-axis, 1 for y-axis).</param>
        /// <remarks>
        /// This method implements a 2D k-d tree partitioning strategy, alternating between
        /// x and y axes at each level of recursion. This creates a balanced spatial partitioning
        /// of the vertices, which improves the efficiency of the divide-and-conquer triangulation.
        /// </remarks>
        private void AlternateAxes(int left, int right, int axis)
        {
            int rangeSize = right - left + 1;
            int halfSize = rangeSize >> 1;
            if (rangeSize <= 3)
            {
                axis = 0;
            }
            VertexMedian(left, right, left + halfSize, axis);
            if (rangeSize - halfSize < 2)
            {
                return;
            }

            if (halfSize >= 2)
            {
                AlternateAxes(left, left + halfSize - 1, 1 - axis);
            }
            AlternateAxes(left + halfSize, right, 1 - axis);
        }

        /// <summary>
        /// Merges two triangulated convex hulls into a single triangulation.
        /// </summary>
        /// <param name="farleft">The leftmost triangle of the left hull.</param>
        /// <param name="innerleft">The rightmost triangle of the left hull.</param>
        /// <param name="innerright">The leftmost triangle of the right hull.</param>
        /// <param name="farright">The rightmost triangle of the right hull.</param>
        /// <param name="axis">The axis used for the division (0 for x-axis, 1 for y-axis).</param>
        /// <remarks>
        /// This is a key part of the divide-and-conquer algorithm. It merges two separately
        /// triangulated convex hulls by creating triangles that connect them, ensuring the
        /// Delaunay property is maintained throughout the merged triangulation.
        /// </remarks>
        private void MergeHulls(
            ref OrientedTriangle farleft,
            ref OrientedTriangle innerleft,
            ref OrientedTriangle innerright,
            ref OrientedTriangle farright,
            int axis)
        {
            OrientedTriangle leftHullSym = new OrientedTriangle();
            OrientedTriangle rightHullSym = new OrientedTriangle();
            OrientedTriangle workingTriangle = new OrientedTriangle();
            OrientedTriangle adjacentTriangle = new OrientedTriangle();
            OrientedTriangle neighborTriangle = new OrientedTriangle();
            OrientedTriangle connectingTriangle = new OrientedTriangle();
            OrientedTriangle tempTriangle = new OrientedTriangle();
            OrientedTriangle mergeTriangle = new OrientedTriangle();
            Vertex vertex1 = innerleft.Destination();
            Vertex pb = innerleft.Apex();
            Vertex vertex2 = innerright.Origin();
            Vertex pa = innerright.Apex();
            if (_useDwyer && axis == 1)
            {
                Vertex vertex3 = farleft.Origin();
                Vertex vertex4 = farleft.Apex();
                Vertex vertex5 = farright.Destination();
                Vertex vertex6 = farright.Apex();
                for (; vertex4.y < vertex3.y; vertex4 = farleft.Apex())
                {
                    farleft.LnextSelf();
                    farleft.SetSelfAsSymmetricTriangle();
                    vertex3 = vertex4;
                }

                innerleft.SetAsSymmetricTriangle(ref tempTriangle);
                for (Vertex vertex7 = tempTriangle.Apex(); vertex7.y > vertex1.y; vertex7 = tempTriangle.Apex())
                {
                    tempTriangle.Lnext(ref innerleft);
                    pb = vertex1;
                    vertex1 = vertex7;
                    innerleft.SetAsSymmetricTriangle(ref tempTriangle);
                }

                for (; pa.y < vertex2.y; pa = innerright.Apex())
                {
                    innerright.LnextSelf();
                    innerright.SetSelfAsSymmetricTriangle();
                    vertex2 = pa;
                }

                farright.SetAsSymmetricTriangle(ref tempTriangle);
                for (Vertex vertex8 = tempTriangle.Apex(); vertex8.y > vertex5.y; vertex8 = tempTriangle.Apex())
                {
                    tempTriangle.Lnext(ref farright);
                    vertex6 = vertex5;
                    vertex5 = vertex8;
                    farright.SetAsSymmetricTriangle(ref tempTriangle);
                }
            }

            bool flag1;
            do
            {
                flag1 = false;
                if (Primitives.CounterClockwise(vertex1, pb, vertex2) > 0.0)
                {
                    innerleft.LprevSelf();
                    innerleft.SetSelfAsSymmetricTriangle();
                    vertex1 = pb;
                    pb = innerleft.Apex();
                    flag1 = true;
                }

                if (Primitives.CounterClockwise(pa, vertex2, vertex1) > 0.0)
                {
                    innerright.LnextSelf();
                    innerright.SetSelfAsSymmetricTriangle();
                    vertex2 = pa;
                    pa = innerright.Apex();
                    flag1 = true;
                }
            } while (flag1);

            innerleft.SetAsSymmetricTriangle(ref leftHullSym);
            innerright.SetAsSymmetricTriangle(ref rightHullSym);
            _triangularMesh.MakeTriangle(ref mergeTriangle);
            mergeTriangle.Bond(ref innerleft);
            mergeTriangle.LnextSelf();
            mergeTriangle.Bond(ref innerright);
            mergeTriangle.LnextSelf();
            mergeTriangle.SetOrigin(vertex2);
            mergeTriangle.SetDestination(vertex1);
            Vertex vertex9 = farleft.Origin();
            if (vertex1 == vertex9)
            {
                mergeTriangle.Lnext(ref farleft);
            }
            Vertex vertex10 = farright.Destination();
            if (vertex2 == vertex10)
            {
                mergeTriangle.Lprev(ref farright);
            }
            Vertex vertex11 = vertex1;
            Vertex vertex12 = vertex2;
            Vertex vertex13 = leftHullSym.Apex();
            Vertex vertex14 = rightHullSym.Apex();
            while (true)
            {
                bool flag2 = Primitives.CounterClockwise(vertex13, vertex11, vertex12) <= 0.0;
                bool flag3 = Primitives.CounterClockwise(vertex14, vertex11, vertex12) <= 0.0;
                if (!(flag2 & flag3))
                {
                    if (!flag2)
                    {
                        leftHullSym.Lprev(ref workingTriangle);
                        workingTriangle.SetSelfAsSymmetricTriangle();
                        Vertex vertex15 = workingTriangle.Apex();
                        if (vertex15 != null)
                        {
                            for (bool flag4 = Primitives.InCircle(vertex11, vertex12, vertex13,
                                     vertex15) > 0.0;
                                 flag4;
                                 flag4 = vertex15 != null && Primitives.InCircle(vertex11,
                                     vertex12, vertex13, vertex15) > 0.0)
                            {
                                workingTriangle.LnextSelf();
                                workingTriangle.SetAsSymmetricTriangle(ref neighborTriangle);
                                workingTriangle.LnextSelf();
                                workingTriangle.SetAsSymmetricTriangle(ref adjacentTriangle);
                                workingTriangle.Bond(ref neighborTriangle);
                                leftHullSym.Bond(ref adjacentTriangle);
                                leftHullSym.LnextSelf();
                                leftHullSym.SetAsSymmetricTriangle(ref connectingTriangle);
                                workingTriangle.LprevSelf();
                                workingTriangle.Bond(ref connectingTriangle);
                                leftHullSym.SetOrigin(vertex11);
                                leftHullSym.SetDestination(null);
                                leftHullSym.SetApex(vertex15);
                                workingTriangle.SetOrigin(null);
                                workingTriangle.SetDestination(vertex13);
                                workingTriangle.SetApex(vertex15);
                                vertex13 = vertex15;
                                adjacentTriangle.Copy(ref workingTriangle);
                                vertex15 = workingTriangle.Apex();
                            }
                        }
                    }

                    if (!flag3)
                    {
                        rightHullSym.Lnext(ref workingTriangle);
                        workingTriangle.SetSelfAsSymmetricTriangle();
                        Vertex vertex16 = workingTriangle.Apex();
                        if (vertex16 != null)
                        {
                            for (bool flag5 = Primitives.InCircle(vertex11, vertex12, vertex14,
                                     vertex16) > 0.0;
                                 flag5;
                                 flag5 = vertex16 != null && Primitives.InCircle(vertex11,
                                     vertex12, vertex14, vertex16) > 0.0)
                            {
                                workingTriangle.LprevSelf();
                                workingTriangle.SetAsSymmetricTriangle(ref neighborTriangle);
                                workingTriangle.LprevSelf();
                                workingTriangle.SetAsSymmetricTriangle(ref adjacentTriangle);
                                workingTriangle.Bond(ref neighborTriangle);
                                rightHullSym.Bond(ref adjacentTriangle);
                                rightHullSym.LprevSelf();
                                rightHullSym.SetAsSymmetricTriangle(ref connectingTriangle);
                                workingTriangle.LnextSelf();
                                workingTriangle.Bond(ref connectingTriangle);
                                rightHullSym.SetOrigin(null);
                                rightHullSym.SetDestination(vertex12);
                                rightHullSym.SetApex(vertex16);
                                workingTriangle.SetOrigin(vertex14);
                                workingTriangle.SetDestination(null);
                                workingTriangle.SetApex(vertex16);
                                vertex14 = vertex16;
                                adjacentTriangle.Copy(ref workingTriangle);
                                vertex16 = workingTriangle.Apex();
                            }
                        }
                    }

                    if (flag2 || !flag3 &&
                        Primitives.InCircle(vertex13, vertex11, vertex12, vertex14) > 0.0)
                    {
                        mergeTriangle.Bond(ref rightHullSym);
                        rightHullSym.Lprev(ref mergeTriangle);
                        mergeTriangle.SetDestination(vertex11);
                        vertex12 = vertex14;
                        mergeTriangle.SetAsSymmetricTriangle(ref rightHullSym);
                        vertex14 = rightHullSym.Apex();
                    }
                    else
                    {
                        mergeTriangle.Bond(ref leftHullSym);
                        leftHullSym.Lnext(ref mergeTriangle);
                        mergeTriangle.SetOrigin(vertex12);
                        vertex11 = vertex13;
                        mergeTriangle.SetAsSymmetricTriangle(ref leftHullSym);
                        vertex13 = leftHullSym.Apex();
                    }
                }
                else
                    break;
            }

            _triangularMesh.MakeTriangle(ref workingTriangle);
            workingTriangle.SetOrigin(vertex11);
            workingTriangle.SetDestination(vertex12);
            workingTriangle.Bond(ref mergeTriangle);
            workingTriangle.LnextSelf();
            workingTriangle.Bond(ref rightHullSym);
            workingTriangle.LnextSelf();
            workingTriangle.Bond(ref leftHullSym);
            if (!_useDwyer || axis != 1)
            {
                return;
            }
            Vertex vertex17 = farleft.Origin();
            Vertex vertex18 = farleft.Apex();
            Vertex vertex19 = farright.Destination();
            Vertex vertex20 = farright.Apex();
            farleft.SetAsSymmetricTriangle(ref tempTriangle);
            for (Vertex vertex21 = tempTriangle.Apex(); vertex21.x < vertex17.x; vertex21 = tempTriangle.Apex())
            {
                tempTriangle.Lprev(ref farleft);
                vertex18 = vertex17;
                vertex17 = vertex21;
                farleft.SetAsSymmetricTriangle(ref tempTriangle);
            }

            for (; vertex20.x > vertex19.x; vertex20 = farright.Apex())
            {
                farright.LprevSelf();
                farright.SetSelfAsSymmetricTriangle();
                vertex19 = vertex20;
            }
        }

        /// <summary>
        /// Recursively applies the divide-and-conquer algorithm to triangulate a subset of vertices.
        /// </summary>
        /// <param name="left">The left index of the range to triangulate.</param>
        /// <param name="right">The right index of the range to triangulate.</param>
        /// <param name="axis">The axis used for division (0 for x-axis, 1 for y-axis).</param>
        /// <param name="farleft">When the method returns, contains the leftmost triangle of the triangulation.</param>
        /// <param name="farright">When the method returns, contains the rightmost triangle of the triangulation.</param>
        /// <remarks>
        /// This method handles the recursive division of the point set, with special cases for small numbers
        /// of points (2 or 3), and calls MergeHulls to combine the triangulations of the divided subsets.
        /// </remarks>
        private void DivconqRecurse(int left, int right, int axis, ref OrientedTriangle farleft, ref OrientedTriangle farright)
        {
            OrientedTriangle newotri = new OrientedTriangle();
            OrientedTriangle otri1 = new OrientedTriangle();
            OrientedTriangle otri2 = new OrientedTriangle();
            OrientedTriangle otri3 = new OrientedTriangle();
            OrientedTriangle otri4 = new OrientedTriangle();
            OrientedTriangle otri5 = new OrientedTriangle();
            int rangeSize = right - left + 1;
            switch (rangeSize)
            {
                case 2:
                    _triangularMesh.MakeTriangle(ref farleft);
                    farleft.SetOrigin(_sortarray[left]);
                    farleft.SetDestination(_sortarray[left + 1]);
                    _triangularMesh.MakeTriangle(ref farright);
                    farright.SetOrigin(_sortarray[left + 1]);
                    farright.SetDestination(_sortarray[left]);
                    farleft.Bond(ref farright);
                    farleft.LprevSelf();
                    farright.LnextSelf();
                    farleft.Bond(ref farright);
                    farleft.LprevSelf();
                    farright.LnextSelf();
                    farleft.Bond(ref farright);
                    farright.Lprev(ref farleft);
                    break;
                case 3:
                    _triangularMesh.MakeTriangle(ref newotri);
                    _triangularMesh.MakeTriangle(ref otri1);
                    _triangularMesh.MakeTriangle(ref otri2);
                    _triangularMesh.MakeTriangle(ref otri3);
                    double orientation = Primitives.CounterClockwise(_sortarray[left],
                        _sortarray[left + 1], _sortarray[left + 2]);
                    if (orientation == 0.0)
                    {
                        newotri.SetOrigin(_sortarray[left]);
                        newotri.SetDestination(_sortarray[left + 1]);
                        otri1.SetOrigin(_sortarray[left + 1]);
                        otri1.SetDestination(_sortarray[left]);
                        otri2.SetOrigin(_sortarray[left + 2]);
                        otri2.SetDestination(_sortarray[left + 1]);
                        otri3.SetOrigin(_sortarray[left + 1]);
                        otri3.SetDestination(_sortarray[left + 2]);
                        newotri.Bond(ref otri1);
                        otri2.Bond(ref otri3);
                        newotri.LnextSelf();
                        otri1.LprevSelf();
                        otri2.LnextSelf();
                        otri3.LprevSelf();
                        newotri.Bond(ref otri3);
                        otri1.Bond(ref otri2);
                        newotri.LnextSelf();
                        otri1.LprevSelf();
                        otri2.LnextSelf();
                        otri3.LprevSelf();
                        newotri.Bond(ref otri1);
                        otri2.Bond(ref otri3);
                        otri1.Copy(ref farleft);
                        otri2.Copy(ref farright);
                        break;
                    }

                    newotri.SetOrigin(_sortarray[left]);
                    otri1.SetDestination(_sortarray[left]);
                    otri3.SetOrigin(_sortarray[left]);
                    if (orientation > 0.0)
                    {
                        newotri.SetDestination(_sortarray[left + 1]);
                        otri1.SetOrigin(_sortarray[left + 1]);
                        otri2.SetDestination(_sortarray[left + 1]);
                        newotri.SetApex(_sortarray[left + 2]);
                        otri2.SetOrigin(_sortarray[left + 2]);
                        otri3.SetDestination(_sortarray[left + 2]);
                    }
                    else
                    {
                        newotri.SetDestination(_sortarray[left + 2]);
                        otri1.SetOrigin(_sortarray[left + 2]);
                        otri2.SetDestination(_sortarray[left + 2]);
                        newotri.SetApex(_sortarray[left + 1]);
                        otri2.SetOrigin(_sortarray[left + 1]);
                        otri3.SetDestination(_sortarray[left + 1]);
                    }

                    newotri.Bond(ref otri1);
                    newotri.LnextSelf();
                    newotri.Bond(ref otri2);
                    newotri.LnextSelf();
                    newotri.Bond(ref otri3);
                    otri1.LprevSelf();
                    otri2.LnextSelf();
                    otri1.Bond(ref otri2);
                    otri1.LprevSelf();
                    otri3.LprevSelf();
                    otri1.Bond(ref otri3);
                    otri2.LnextSelf();
                    otri3.LprevSelf();
                    otri2.Bond(ref otri3);
                    otri1.Copy(ref farleft);
                    if (orientation > 0.0)
                    {
                        otri2.Copy(ref farright);
                        break;
                    }

                    farleft.Lnext(ref farright);
                    break;
                default:
                    int halfSize = rangeSize >> 1;
                    DivconqRecurse(left, left + halfSize - 1, 1 - axis, ref farleft, ref otri4);
                    DivconqRecurse(left + halfSize, right, 1 - axis, ref otri5, ref farright);
                    MergeHulls(ref farleft, ref otri4, ref otri5, ref farright, axis);
                    break;
            }
        }

        /// <summary>
        /// Removes ghost triangles from the triangulation.
        /// </summary>
        /// <param name="startghost">A reference to a ghost triangle to start the removal process.</param>
        /// <returns>The number of ghost triangles removed.</returns>
        /// <remarks>
        /// Ghost triangles are temporary triangles created during the triangulation process
        /// that extend to infinity. This method removes them to produce a proper triangulation
        /// of the convex hull of the input points.
        /// </remarks>
        private int RemoveGhosts(ref OrientedTriangle startghost)
        {
            OrientedTriangle adjacentTriangle = new OrientedTriangle();
            OrientedTriangle currentTriangle = new OrientedTriangle();
            OrientedTriangle nextTriangle = new OrientedTriangle();
            bool markVertices = !_triangularMesh.behavior.Poly;
            startghost.Lprev(ref adjacentTriangle);
            adjacentTriangle.SetSelfAsSymmetricTriangle();
            TriangularMesh.dummytri.neighbors[0] = adjacentTriangle;
            startghost.Copy(ref currentTriangle);
            int ghostCount = 0;
            do
            {
                ++ghostCount;
                currentTriangle.Lnext(ref nextTriangle);
                currentTriangle.LprevSelf();
                currentTriangle.SetSelfAsSymmetricTriangle();
                if (markVertices && currentTriangle.triangle != TriangularMesh.dummytri)
                {
                    Vertex vertex = currentTriangle.Origin();
                    if (vertex.mark == 0)
                        vertex.mark = 1;
                }

                currentTriangle.Dissolve();
                nextTriangle.SetAsSymmetricTriangle(ref currentTriangle);
                _triangularMesh.TriangleDealloc(nextTriangle.triangle);
            } while (!currentTriangle.Equal(startghost));

            return ghostCount;
        }

        /// <summary>
        /// Triangulates a mesh using the Dwyer algorithm.
        /// </summary>
        /// <param name="m">The mesh to triangulate.</param>
        /// <returns>The number of triangles on the convex hull of the triangulation.</returns>
        /// <remarks>
        /// This is the main entry point for the Dwyer triangulation algorithm. It sorts the vertices,
        /// removes duplicates, optionally applies the alternating axes partitioning, and then
        /// recursively triangulates the point set using the divide-and-conquer approach.
        /// </remarks>
        public int Triangulate(TriangularMesh m)
        {
            OrientedTriangle orientedTriangle = new OrientedTriangle();
            OrientedTriangle farright = new OrientedTriangle();
            _triangularMesh = m;
            _sortarray = new Vertex[m.invertices];
            int vertexCount = 0;
            foreach (Vertex vertex in m.VertexDictionary.Values)
            {
                _sortarray[vertexCount++] = vertex;
            }
            VertexSort(0, m.invertices - 1);
            int uniqueIndex = 0;
            for (int currentIndex = 1; currentIndex < m.invertices; ++currentIndex)
            {
                if (_sortarray[uniqueIndex].x == _sortarray[currentIndex].x &&
                    _sortarray[uniqueIndex].y == _sortarray[currentIndex].y)
                {
                    _sortarray[currentIndex].type = VertexType.UndeadVertex;
                    ++m.undeads;
                }
                else
                {
                    ++uniqueIndex;
                    _sortarray[uniqueIndex] = _sortarray[currentIndex];
                }
            }

            int uniqueVertexCount = uniqueIndex + 1;
            if (_useDwyer)
            {
                int midpoint = uniqueVertexCount >> 1;
                if (uniqueVertexCount - midpoint >= 2)
                {
                    if (midpoint >= 2)
                        AlternateAxes(0, midpoint - 1, 1);
                    AlternateAxes(midpoint, uniqueVertexCount - 1, 1);
                }
            }

            DivconqRecurse(0, uniqueVertexCount - 1, 0, ref orientedTriangle, ref farright);
            return RemoveGhosts(ref orientedTriangle);
        }
    }
}