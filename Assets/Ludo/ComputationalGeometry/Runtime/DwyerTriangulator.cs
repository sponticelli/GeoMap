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
    [System.Serializable]
    public class DwyerTriangulator
    {
        /// <summary>
        /// Random number generator for the quicksort algorithm.
        /// </summary>
        private static Random rand = new Random(DateTime.Now.Millisecond);

        /// <summary>
        /// Flag indicating whether to use Dwyer's divide-and-conquer algorithm.
        /// </summary>
        private bool useDwyer = true;

        /// <summary>
        /// Array of vertices to be triangulated, sorted by coordinates.
        /// </summary>
        private Vertex[] sortarray;

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
                    Vertex vertex = this.sortarray[index1];
                    int index2;
                    for (index2 = index1 - 1;
                         index2 >= left && (this.sortarray[index2].x > vertex.x ||
                                            this.sortarray[index2].x == vertex.x &&
                                            this.sortarray[index2].y > vertex.y);
                         --index2)
                        this.sortarray[index2 + 1] = this.sortarray[index2];
                    this.sortarray[index2 + 1] = vertex;
                }
            }
            else
            {
                int index = DwyerTriangulator.rand.Next(left, right);
                double x = this.sortarray[index].x;
                double y = this.sortarray[index].y;
                --left;
                ++right;
                while (left < right)
                {
                    do
                    {
                        ++left;
                    } while (left <= right && (this.sortarray[left].x < x ||
                                               this.sortarray[left].x == x && this.sortarray[left].y < y));

                    do
                    {
                        --right;
                    } while (left <= right && (this.sortarray[right].x > x ||
                                               this.sortarray[right].x == x && this.sortarray[right].y > y));

                    if (left < right)
                    {
                        Vertex vertex = this.sortarray[left];
                        this.sortarray[left] = this.sortarray[right];
                        this.sortarray[right] = vertex;
                    }
                }

                if (left > left1)
                    this.VertexSort(left1, left);
                if (right1 <= right + 1)
                    return;
                this.VertexSort(right + 1, right1);
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
            int num1 = right - left + 1;
            int left1 = left;
            int right1 = right;
            if (num1 == 2)
            {
                if (this.sortarray[left][axis] <= this.sortarray[right][axis] &&
                    (this.sortarray[left][axis] != this.sortarray[right][axis] ||
                     this.sortarray[left][1 - axis] <= this.sortarray[right][1 - axis]))
                    return;
                Vertex vertex = this.sortarray[right];
                this.sortarray[right] = this.sortarray[left];
                this.sortarray[left] = vertex;
            }
            else
            {
                int index = DwyerTriangulator.rand.Next(left, right);
                double num2 = this.sortarray[index][axis];
                double num3 = this.sortarray[index][1 - axis];
                --left;
                ++right;
                while (left < right)
                {
                    do
                    {
                        ++left;
                    } while (left <= right && (this.sortarray[left][axis] < num2 ||
                                               this.sortarray[left][axis] == num2 &&
                                               this.sortarray[left][1 - axis] < num3));

                    do
                    {
                        --right;
                    } while (left <= right && (this.sortarray[right][axis] > num2 ||
                                               this.sortarray[right][axis] == num2 &&
                                               this.sortarray[right][1 - axis] > num3));

                    if (left < right)
                    {
                        Vertex vertex = this.sortarray[left];
                        this.sortarray[left] = this.sortarray[right];
                        this.sortarray[right] = vertex;
                    }
                }

                if (left > median)
                    this.VertexMedian(left1, left - 1, median, axis);
                if (right >= median - 1)
                    return;
                this.VertexMedian(right + 1, right1, median, axis);
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
            int num1 = right - left + 1;
            int num2 = num1 >> 1;
            if (num1 <= 3)
                axis = 0;
            this.VertexMedian(left, right, left + num2, axis);
            if (num1 - num2 < 2)
                return;
            if (num2 >= 2)
                this.AlternateAxes(left, left + num2 - 1, 1 - axis);
            this.AlternateAxes(left + num2, right, 1 - axis);
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
            ref Otri farleft,
            ref Otri innerleft,
            ref Otri innerright,
            ref Otri farright,
            int axis)
        {
            Otri o2_1 = new Otri();
            Otri o2_2 = new Otri();
            Otri otri1 = new Otri();
            Otri o2_3 = new Otri();
            Otri o2_4 = new Otri();
            Otri o2_5 = new Otri();
            Otri o2_6 = new Otri();
            Otri otri2 = new Otri();
            Vertex vertex1 = innerleft.Dest();
            Vertex pb = innerleft.Apex();
            Vertex vertex2 = innerright.Org();
            Vertex pa = innerright.Apex();
            if (this.useDwyer && axis == 1)
            {
                Vertex vertex3 = farleft.Org();
                Vertex vertex4 = farleft.Apex();
                Vertex vertex5 = farright.Dest();
                Vertex vertex6 = farright.Apex();
                for (; vertex4.y < vertex3.y; vertex4 = farleft.Apex())
                {
                    farleft.LnextSelf();
                    farleft.SymSelf();
                    vertex3 = vertex4;
                }

                innerleft.Sym(ref o2_6);
                for (Vertex vertex7 = o2_6.Apex(); vertex7.y > vertex1.y; vertex7 = o2_6.Apex())
                {
                    o2_6.Lnext(ref innerleft);
                    pb = vertex1;
                    vertex1 = vertex7;
                    innerleft.Sym(ref o2_6);
                }

                for (; pa.y < vertex2.y; pa = innerright.Apex())
                {
                    innerright.LnextSelf();
                    innerright.SymSelf();
                    vertex2 = pa;
                }

                farright.Sym(ref o2_6);
                for (Vertex vertex8 = o2_6.Apex(); vertex8.y > vertex5.y; vertex8 = o2_6.Apex())
                {
                    o2_6.Lnext(ref farright);
                    vertex6 = vertex5;
                    vertex5 = vertex8;
                    farright.Sym(ref o2_6);
                }
            }

            bool flag1;
            do
            {
                flag1 = false;
                if (Primitives.CounterClockwise((Point)vertex1, (Point)pb, (Point)vertex2) > 0.0)
                {
                    innerleft.LprevSelf();
                    innerleft.SymSelf();
                    vertex1 = pb;
                    pb = innerleft.Apex();
                    flag1 = true;
                }

                if (Primitives.CounterClockwise((Point)pa, (Point)vertex2, (Point)vertex1) > 0.0)
                {
                    innerright.LnextSelf();
                    innerright.SymSelf();
                    vertex2 = pa;
                    pa = innerright.Apex();
                    flag1 = true;
                }
            } while (flag1);

            innerleft.Sym(ref o2_1);
            innerright.Sym(ref o2_2);
            this._triangularMesh.MakeTriangle(ref otri2);
            otri2.Bond(ref innerleft);
            otri2.LnextSelf();
            otri2.Bond(ref innerright);
            otri2.LnextSelf();
            otri2.SetOrg(vertex2);
            otri2.SetDest(vertex1);
            Vertex vertex9 = farleft.Org();
            if ((Point)vertex1 == (Point)vertex9)
                otri2.Lnext(ref farleft);
            Vertex vertex10 = farright.Dest();
            if ((Point)vertex2 == (Point)vertex10)
                otri2.Lprev(ref farright);
            Vertex vertex11 = vertex1;
            Vertex vertex12 = vertex2;
            Vertex vertex13 = o2_1.Apex();
            Vertex vertex14 = o2_2.Apex();
            while (true)
            {
                bool flag2 = Primitives.CounterClockwise((Point)vertex13, (Point)vertex11, (Point)vertex12) <= 0.0;
                bool flag3 = Primitives.CounterClockwise((Point)vertex14, (Point)vertex11, (Point)vertex12) <= 0.0;
                if (!(flag2 & flag3))
                {
                    if (!flag2)
                    {
                        o2_1.Lprev(ref otri1);
                        otri1.SymSelf();
                        Vertex vertex15 = otri1.Apex();
                        if ((Point)vertex15 != (Point)null)
                        {
                            for (bool flag4 = Primitives.InCircle((Point)vertex11, (Point)vertex12, (Point)vertex13,
                                     (Point)vertex15) > 0.0;
                                 flag4;
                                 flag4 = (Point)vertex15 != (Point)null && Primitives.InCircle((Point)vertex11,
                                     (Point)vertex12, (Point)vertex13, (Point)vertex15) > 0.0)
                            {
                                otri1.LnextSelf();
                                otri1.Sym(ref o2_4);
                                otri1.LnextSelf();
                                otri1.Sym(ref o2_3);
                                otri1.Bond(ref o2_4);
                                o2_1.Bond(ref o2_3);
                                o2_1.LnextSelf();
                                o2_1.Sym(ref o2_5);
                                otri1.LprevSelf();
                                otri1.Bond(ref o2_5);
                                o2_1.SetOrg(vertex11);
                                o2_1.SetDest((Vertex)null);
                                o2_1.SetApex(vertex15);
                                otri1.SetOrg((Vertex)null);
                                otri1.SetDest(vertex13);
                                otri1.SetApex(vertex15);
                                vertex13 = vertex15;
                                o2_3.Copy(ref otri1);
                                vertex15 = otri1.Apex();
                            }
                        }
                    }

                    if (!flag3)
                    {
                        o2_2.Lnext(ref otri1);
                        otri1.SymSelf();
                        Vertex vertex16 = otri1.Apex();
                        if ((Point)vertex16 != (Point)null)
                        {
                            for (bool flag5 = Primitives.InCircle((Point)vertex11, (Point)vertex12, (Point)vertex14,
                                     (Point)vertex16) > 0.0;
                                 flag5;
                                 flag5 = (Point)vertex16 != (Point)null && Primitives.InCircle((Point)vertex11,
                                     (Point)vertex12, (Point)vertex14, (Point)vertex16) > 0.0)
                            {
                                otri1.LprevSelf();
                                otri1.Sym(ref o2_4);
                                otri1.LprevSelf();
                                otri1.Sym(ref o2_3);
                                otri1.Bond(ref o2_4);
                                o2_2.Bond(ref o2_3);
                                o2_2.LprevSelf();
                                o2_2.Sym(ref o2_5);
                                otri1.LnextSelf();
                                otri1.Bond(ref o2_5);
                                o2_2.SetOrg((Vertex)null);
                                o2_2.SetDest(vertex12);
                                o2_2.SetApex(vertex16);
                                otri1.SetOrg(vertex14);
                                otri1.SetDest((Vertex)null);
                                otri1.SetApex(vertex16);
                                vertex14 = vertex16;
                                o2_3.Copy(ref otri1);
                                vertex16 = otri1.Apex();
                            }
                        }
                    }

                    if (flag2 || !flag3 &&
                        Primitives.InCircle((Point)vertex13, (Point)vertex11, (Point)vertex12, (Point)vertex14) > 0.0)
                    {
                        otri2.Bond(ref o2_2);
                        o2_2.Lprev(ref otri2);
                        otri2.SetDest(vertex11);
                        vertex12 = vertex14;
                        otri2.Sym(ref o2_2);
                        vertex14 = o2_2.Apex();
                    }
                    else
                    {
                        otri2.Bond(ref o2_1);
                        o2_1.Lnext(ref otri2);
                        otri2.SetOrg(vertex12);
                        vertex11 = vertex13;
                        otri2.Sym(ref o2_1);
                        vertex13 = o2_1.Apex();
                    }
                }
                else
                    break;
            }

            this._triangularMesh.MakeTriangle(ref otri1);
            otri1.SetOrg(vertex11);
            otri1.SetDest(vertex12);
            otri1.Bond(ref otri2);
            otri1.LnextSelf();
            otri1.Bond(ref o2_2);
            otri1.LnextSelf();
            otri1.Bond(ref o2_1);
            if (!this.useDwyer || axis != 1)
                return;
            Vertex vertex17 = farleft.Org();
            Vertex vertex18 = farleft.Apex();
            Vertex vertex19 = farright.Dest();
            Vertex vertex20 = farright.Apex();
            farleft.Sym(ref o2_6);
            for (Vertex vertex21 = o2_6.Apex(); vertex21.x < vertex17.x; vertex21 = o2_6.Apex())
            {
                o2_6.Lprev(ref farleft);
                vertex18 = vertex17;
                vertex17 = vertex21;
                farleft.Sym(ref o2_6);
            }

            for (; vertex20.x > vertex19.x; vertex20 = farright.Apex())
            {
                farright.LprevSelf();
                farright.SymSelf();
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
        private void DivconqRecurse(int left, int right, int axis, ref Otri farleft, ref Otri farright)
        {
            Otri newotri = new Otri();
            Otri otri1 = new Otri();
            Otri otri2 = new Otri();
            Otri otri3 = new Otri();
            Otri otri4 = new Otri();
            Otri otri5 = new Otri();
            int num1 = right - left + 1;
            switch (num1)
            {
                case 2:
                    this._triangularMesh.MakeTriangle(ref farleft);
                    farleft.SetOrg(this.sortarray[left]);
                    farleft.SetDest(this.sortarray[left + 1]);
                    this._triangularMesh.MakeTriangle(ref farright);
                    farright.SetOrg(this.sortarray[left + 1]);
                    farright.SetDest(this.sortarray[left]);
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
                    this._triangularMesh.MakeTriangle(ref newotri);
                    this._triangularMesh.MakeTriangle(ref otri1);
                    this._triangularMesh.MakeTriangle(ref otri2);
                    this._triangularMesh.MakeTriangle(ref otri3);
                    double num2 = Primitives.CounterClockwise((Point)this.sortarray[left],
                        (Point)this.sortarray[left + 1], (Point)this.sortarray[left + 2]);
                    if (num2 == 0.0)
                    {
                        newotri.SetOrg(this.sortarray[left]);
                        newotri.SetDest(this.sortarray[left + 1]);
                        otri1.SetOrg(this.sortarray[left + 1]);
                        otri1.SetDest(this.sortarray[left]);
                        otri2.SetOrg(this.sortarray[left + 2]);
                        otri2.SetDest(this.sortarray[left + 1]);
                        otri3.SetOrg(this.sortarray[left + 1]);
                        otri3.SetDest(this.sortarray[left + 2]);
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

                    newotri.SetOrg(this.sortarray[left]);
                    otri1.SetDest(this.sortarray[left]);
                    otri3.SetOrg(this.sortarray[left]);
                    if (num2 > 0.0)
                    {
                        newotri.SetDest(this.sortarray[left + 1]);
                        otri1.SetOrg(this.sortarray[left + 1]);
                        otri2.SetDest(this.sortarray[left + 1]);
                        newotri.SetApex(this.sortarray[left + 2]);
                        otri2.SetOrg(this.sortarray[left + 2]);
                        otri3.SetDest(this.sortarray[left + 2]);
                    }
                    else
                    {
                        newotri.SetDest(this.sortarray[left + 2]);
                        otri1.SetOrg(this.sortarray[left + 2]);
                        otri2.SetDest(this.sortarray[left + 2]);
                        newotri.SetApex(this.sortarray[left + 1]);
                        otri2.SetOrg(this.sortarray[left + 1]);
                        otri3.SetDest(this.sortarray[left + 1]);
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
                    if (num2 > 0.0)
                    {
                        otri2.Copy(ref farright);
                        break;
                    }

                    farleft.Lnext(ref farright);
                    break;
                default:
                    int num3 = num1 >> 1;
                    this.DivconqRecurse(left, left + num3 - 1, 1 - axis, ref farleft, ref otri4);
                    this.DivconqRecurse(left + num3, right, 1 - axis, ref otri5, ref farright);
                    this.MergeHulls(ref farleft, ref otri4, ref otri5, ref farright, axis);
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
        private int RemoveGhosts(ref Otri startghost)
        {
            Otri o2_1 = new Otri();
            Otri o2_2 = new Otri();
            Otri o2_3 = new Otri();
            bool flag = !this._triangularMesh.behavior.Poly;
            startghost.Lprev(ref o2_1);
            o2_1.SymSelf();
            TriangularMesh.dummytri.neighbors[0] = o2_1;
            startghost.Copy(ref o2_2);
            int num = 0;
            do
            {
                ++num;
                o2_2.Lnext(ref o2_3);
                o2_2.LprevSelf();
                o2_2.SymSelf();
                if (flag && o2_2.triangle != TriangularMesh.dummytri)
                {
                    Vertex vertex = o2_2.Org();
                    if (vertex.mark == 0)
                        vertex.mark = 1;
                }

                o2_2.Dissolve();
                o2_3.Sym(ref o2_2);
                this._triangularMesh.TriangleDealloc(o2_3.triangle);
            } while (!o2_2.Equal(startghost));

            return num;
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
            Otri otri = new Otri();
            Otri farright = new Otri();
            this._triangularMesh = m;
            this.sortarray = new Vertex[m.invertices];
            int num1 = 0;
            foreach (Vertex vertex in m.vertices.Values)
                this.sortarray[num1++] = vertex;
            this.VertexSort(0, m.invertices - 1);
            int index1 = 0;
            for (int index2 = 1; index2 < m.invertices; ++index2)
            {
                if (this.sortarray[index1].x == this.sortarray[index2].x &&
                    this.sortarray[index1].y == this.sortarray[index2].y)
                {
                    this.sortarray[index2].type = VertexType.UndeadVertex;
                    ++m.undeads;
                }
                else
                {
                    ++index1;
                    this.sortarray[index1] = this.sortarray[index2];
                }
            }

            int num2 = index1 + 1;
            if (this.useDwyer)
            {
                int left = num2 >> 1;
                if (num2 - left >= 2)
                {
                    if (left >= 2)
                        this.AlternateAxes(0, left - 1, 1);
                    this.AlternateAxes(left, num2 - 1, 1);
                }
            }

            this.DivconqRecurse(0, num2 - 1, 0, ref otri, ref farright);
            return this.RemoveGhosts(ref otri);
        }
    }
}