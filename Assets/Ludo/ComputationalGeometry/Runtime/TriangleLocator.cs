using System;

namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Provides efficient point location in a triangular mesh.
    /// </summary>
    /// <remarks>
    /// The TriangleLocator class implements algorithms for quickly finding the triangle
    /// that contains a given point in a mesh. This is a fundamental operation in many
    /// geometric algorithms, including point insertion, interpolation, and ray tracing.
    ///
    /// The class uses a combination of strategies to optimize point location:
    /// 1. Caching the most recently used triangle for spatial locality
    /// 2. Sampling representative triangles from the mesh for good starting points
    /// 3. Walking through the mesh from a starting triangle to the target location
    ///
    /// These strategies together provide efficient point location even in large meshes.
    /// </remarks>
    [Serializable]
    public class TriangleLocator
    {
        /// <summary>
        /// StratifiedTriangleSampler for selecting representative triangles from the mesh.
        /// </summary>
        private StratifiedTriangleSampler _stratifiedTriangleSampler;

        /// <summary>
        /// Reference to the mesh being searched.
        /// </summary>
        private TriangularMesh _triangularMesh;

        /// <summary>
        /// The most recently located triangle, used as a starting point for subsequent searches.
        /// </summary>
        internal OrientedTriangle recenttri;

        /// <summary>
        /// Initializes a new instance of the <see cref="TriangleLocator"/> class.
        /// </summary>
        /// <param name="triangularMesh">The mesh to search for point locations.</param>
        /// <remarks>
        /// The constructor initializes the locator with a reference to the mesh
        /// and creates a _stratifiedTriangleSampler for selecting representative triangles.
        /// </remarks>
        public TriangleLocator(TriangularMesh triangularMesh)
        {
            _triangularMesh = triangularMesh;
            _stratifiedTriangleSampler = new StratifiedTriangleSampler();
        }

        /// <summary>
        /// Updates the most recently used triangle.
        /// </summary>
        /// <param name="orientedTriangle">The triangle to set as the most recently used.</param>
        /// <remarks>
        /// This method is used to update the cached triangle after a successful location
        /// or when the mesh has been modified in a way that makes a specific triangle
        /// likely to be a good starting point for future searches.
        /// </remarks>
        public void Update(ref OrientedTriangle orientedTriangle) => orientedTriangle.Copy(ref recenttri);

        /// <summary>
        /// Resets the locator by clearing the most recently used triangle.
        /// </summary>
        /// <remarks>
        /// This method should be called when the mesh has been significantly modified,
        /// such as after a retriangulation, to prevent using an invalid triangle as
        /// a starting point for future searches.
        /// </remarks>
        public void Reset() => recenttri.triangle = null;

        /// <summary>
        /// Precisely locates a point in the mesh, starting from a given triangle.
        /// </summary>
        /// <param name="searchpoint">The point to locate.</param>
        /// <param name="searchtri">The starting triangle, which is updated to the triangle containing the point.</param>
        /// <param name="stopatsubsegment">If true, stops the search at subsegment boundaries.</param>
        /// <returns>The result of the location operation, indicating whether the point is inside a triangle, on an edge, on a vertex, or outside the mesh.</returns>
        /// <remarks>
        /// This method implements a robust point location algorithm that walks through the mesh
        /// from the starting triangle to the triangle containing the search point. It uses
        /// geometric predicates to determine the direction of movement at each step.
        ///
        /// The algorithm is guaranteed to find the correct triangle if the mesh is valid and
        /// the starting triangle is properly initialized. If stopatsubsegment is true, the
        /// search will stop at subsegment boundaries, which is useful for constrained triangulations.
        /// </remarks>
        public PointLocationResult PreciseLocate(Point searchpoint, ref OrientedTriangle searchtri, bool stopatsubsegment)
        {
            OrientedTriangle o2 = new OrientedTriangle();
            OrientedSubSegment os = new OrientedSubSegment();
            Vertex pa = searchtri.Org();
            Vertex pb = searchtri.Dest();
            for (Vertex vertex = searchtri.Apex();
                 vertex.x != searchpoint.X || vertex.y != searchpoint.Y;
                 vertex = searchtri.Apex())
            {
                double num1 = Primitives.CounterClockwise(pa, vertex, searchpoint);
                double num2 = Primitives.CounterClockwise(vertex, pb, searchpoint);
                bool flag;
                if (num1 > 0.0)
                    flag = num2 <= 0.0 || (vertex.x - searchpoint.X) * (pb.x - pa.x) +
                        (vertex.y - searchpoint.Y) * (pb.y - pa.y) > 0.0;
                else if (num2 > 0.0)
                {
                    flag = false;
                }
                else
                {
                    if (num1 == 0.0)
                    {
                        searchtri.LprevSelf();
                        return PointLocationResult.OnEdge;
                    }

                    if (num2 != 0.0)
                        return PointLocationResult.InTriangle;
                    searchtri.LnextSelf();
                    return PointLocationResult.OnEdge;
                }

                if (flag)
                {
                    searchtri.Lprev(ref o2);
                    pb = vertex;
                }
                else
                {
                    searchtri.Lnext(ref o2);
                    pa = vertex;
                }

                o2.Sym(ref searchtri);
                if (_triangularMesh.checksegments & stopatsubsegment)
                {
                    o2.SegPivot(ref os);
                    if (os.seg != TriangularMesh.dummysub)
                    {
                        o2.Copy(ref searchtri);
                        return PointLocationResult.Outside;
                    }
                }

                if (searchtri.triangle == TriangularMesh.dummytri)
                {
                    o2.Copy(ref searchtri);
                    return PointLocationResult.Outside;
                }
            }

            searchtri.LprevSelf();
            return PointLocationResult.OnVertex;
        }

        /// <summary>
        /// Locates a point in the mesh, using various strategies for efficiency.
        /// </summary>
        /// <param name="searchpoint">The point to locate.</param>
        /// <param name="searchtri">A triangle that will be updated to the triangle containing the point.</param>
        /// <returns>The result of the location operation, indicating whether the point is inside a triangle, on an edge, on a vertex, or outside the mesh.</returns>
        /// <remarks>
        /// This method implements an efficient point location algorithm that combines several strategies:
        /// 1. First, it checks if the point is in or near the most recently used triangle
        /// 2. If not, it samples representative triangles from the mesh to find a good starting point
        /// 3. Finally, it calls PreciseLocate to walk from the starting triangle to the exact location
        ///
        /// This approach is typically much faster than a simple linear search through all triangles,
        /// especially for large meshes or when there is spatial locality in the search points.
        /// </remarks>
        public PointLocationResult Locate(Point searchpoint, ref OrientedTriangle searchtri)
        {
            OrientedTriangle orientedTriangle = new OrientedTriangle();
            Vertex vertex1 = searchtri.Org();
            double num1 = (searchpoint.X - vertex1.x) * (searchpoint.X - vertex1.x) +
                          (searchpoint.Y - vertex1.y) * (searchpoint.Y - vertex1.y);
            if (recenttri.triangle != null && !OrientedTriangle.IsDead(recenttri.triangle))
            {
                Vertex vertex2 = recenttri.Org();
                if (vertex2.x == searchpoint.X && vertex2.y == searchpoint.Y)
                {
                    recenttri.Copy(ref searchtri);
                    return PointLocationResult.OnVertex;
                }

                double num2 = (searchpoint.X - vertex2.x) * (searchpoint.X - vertex2.x) +
                              (searchpoint.Y - vertex2.y) * (searchpoint.Y - vertex2.y);
                if (num2 < num1)
                {
                    recenttri.Copy(ref searchtri);
                    num1 = num2;
                }
            }

            _stratifiedTriangleSampler.Update(_triangularMesh);
            foreach (int sample in _stratifiedTriangleSampler.GetSamples(_triangularMesh))
            {
                orientedTriangle.triangle = _triangularMesh.triangles[sample];
                if (!OrientedTriangle.IsDead(orientedTriangle.triangle))
                {
                    Vertex vertex3 = orientedTriangle.Org();
                    double num3 = (searchpoint.X - vertex3.x) * (searchpoint.X - vertex3.x) +
                                  (searchpoint.Y - vertex3.y) * (searchpoint.Y - vertex3.y);
                    if (num3 < num1)
                    {
                        orientedTriangle.Copy(ref searchtri);
                        num1 = num3;
                    }
                }
            }

            Vertex pa = searchtri.Org();
            Vertex pb = searchtri.Dest();
            if (pa.x == searchpoint.X && pa.y == searchpoint.Y)
                return PointLocationResult.OnVertex;
            if (pb.x == searchpoint.X && pb.y == searchpoint.Y)
            {
                searchtri.LnextSelf();
                return PointLocationResult.OnVertex;
            }

            double num4 = Primitives.CounterClockwise(pa, pb, searchpoint);
            if (num4 < 0.0)
                searchtri.SymSelf();
            else if (num4 == 0.0 && pa.x < searchpoint.X == searchpoint.X < pb.x &&
                     pa.y < searchpoint.Y == searchpoint.Y < pb.y)
                return PointLocationResult.OnEdge;
            return PreciseLocate(searchpoint, ref searchtri, false);
        }
    }
}