using System;

namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Implements the incremental algorithm for Delaunay triangulation.
    /// </summary>
    /// <remarks>
    /// The incremental algorithm builds a triangulation by inserting vertices one at a time
    /// into an existing triangulation. It starts with a large bounding triangle that contains
    /// all input vertices, then inserts each vertex and updates the triangulation to maintain
    /// the Delaunay property. After all vertices are inserted, the bounding triangle is removed.
    /// </remarks>
    [Serializable]
    public class IncrementalDelaunayTriangulator
    {
        /// <summary>
        /// Reference to the mesh being triangulated.
        /// </summary>
        private TriangularMesh _triangularMesh;

        /// <summary>
        /// Creates a bounding triangle that contains all vertices of the mesh.
        /// </summary>
        /// <remarks>
        /// This method creates a large triangle that surrounds all input vertices.
        /// The triangle is formed by three artificial vertices placed far outside the
        /// bounding box of the input vertices. This initial triangulation serves as
        /// the starting point for the incremental algorithm.
        /// </remarks>
        private void GetBoundingBox()
        {
            OrientedTriangle boundingTriangle = new OrientedTriangle();
            AxisAlignedBoundingBox2D bounds = _triangularMesh.bounds;
            double maxDimension = bounds.Width;
            if (bounds.Height > maxDimension)
                maxDimension = bounds.Height;
            if (maxDimension == 0.0)
                maxDimension = 1.0;
            _triangularMesh.infvertex1 = new Vertex(bounds.Xmin - 50.0 * maxDimension, bounds.Ymin - 40.0 * maxDimension);
            _triangularMesh.infvertex2 = new Vertex(bounds.Xmax + 50.0 * maxDimension, bounds.Ymin - 40.0 * maxDimension);
            _triangularMesh.infvertex3 = new Vertex(0.5 * (bounds.Xmin + bounds.Xmax), bounds.Ymax + 60.0 * maxDimension);
            _triangularMesh.MakeTriangle(ref boundingTriangle);
            boundingTriangle.SetOrigin(_triangularMesh.infvertex1);
            boundingTriangle.SetDestination(_triangularMesh.infvertex2);
            boundingTriangle.SetApex(_triangularMesh.infvertex3);
            TriangularMesh.dummytri.neighbors[0] = boundingTriangle;
        }

        /// <summary>
        /// Removes the bounding triangle and any associated triangles from the mesh.
        /// </summary>
        /// <returns>The number of triangles on the convex hull of the triangulation.</returns>
        /// <remarks>
        /// After all vertices have been inserted, this method removes the artificial bounding
        /// triangle and any triangles connected to its vertices. This leaves only the triangulation
        /// of the input vertices. The method returns the number of triangles on the convex hull,
        /// which is needed for various mesh operations.
        /// </remarks>
        private int RemoveBox()
        {
            OrientedTriangle nextTriangle = new OrientedTriangle();
            OrientedTriangle startTriangle = new OrientedTriangle();
            OrientedTriangle adjacentTriangle = new OrientedTriangle();
            OrientedTriangle currentTriangle = new OrientedTriangle();
            OrientedTriangle endTriangle = new OrientedTriangle();
            OrientedTriangle neighborTriangle = new OrientedTriangle();
            bool markVertices = !_triangularMesh.behavior.Poly;
            currentTriangle.triangle = TriangularMesh.dummytri;
            currentTriangle.orient = 0;
            currentTriangle.SetSelfAsSymmetricTriangle();
            currentTriangle.Lprev(ref endTriangle);
            currentTriangle.LnextSelf();
            currentTriangle.SetSelfAsSymmetricTriangle();
            currentTriangle.Lprev(ref startTriangle);
            startTriangle.SetSelfAsSymmetricTriangle();
            currentTriangle.Lnext(ref adjacentTriangle);
            adjacentTriangle.SetSelfAsSymmetricTriangle();
            if (adjacentTriangle.triangle == TriangularMesh.dummytri)
            {
                startTriangle.LprevSelf();
                startTriangle.SetSelfAsSymmetricTriangle();
            }

            TriangularMesh.dummytri.neighbors[0] = startTriangle;
            int hullSize = -2;
            while (!currentTriangle.Equal(endTriangle))
            {
                ++hullSize;
                currentTriangle.Lprev(ref neighborTriangle);
                neighborTriangle.SetSelfAsSymmetricTriangle();
                if (markVertices && neighborTriangle.triangle != TriangularMesh.dummytri)
                {
                    Vertex vertex = neighborTriangle.Origin();
                    if (vertex.mark == 0)
                        vertex.mark = 1;
                }

                neighborTriangle.Dissolve();
                currentTriangle.Lnext(ref nextTriangle);
                nextTriangle.SetAsSymmetricTriangle(ref currentTriangle);
                _triangularMesh.TriangleDealloc(nextTriangle.triangle);
                if (currentTriangle.triangle == TriangularMesh.dummytri)
                    neighborTriangle.Copy(ref currentTriangle);
            }

            _triangularMesh.TriangleDealloc(endTriangle.triangle);
            return hullSize;
        }

        /// <summary>
        /// Triangulates a mesh using the incremental algorithm.
        /// </summary>
        /// <param name="triangularMesh">The mesh to triangulate.</param>
        /// <returns>The number of triangles on the convex hull of the triangulation.</returns>
        /// <remarks>
        /// This is the main entry point for the incremental triangulation algorithm. It:
        /// 1. Creates a bounding triangle that contains all input vertices
        /// 2. Inserts each vertex one by one into the triangulation
        /// 3. Removes the bounding triangle and returns the number of triangles on the convex hull
        ///
        /// The algorithm handles duplicate vertices by marking them as "undead" vertices,
        /// which are ignored in subsequent mesh operations.
        /// </remarks>
        public int Triangulate(TriangularMesh triangularMesh)
        {
            _triangularMesh = triangularMesh;
            OrientedTriangle searchTriangle = new OrientedTriangle();
            GetBoundingBox();
            foreach (Vertex vertexToInsert in triangularMesh.VertexDictionary.Values)
            {
                searchTriangle.triangle = TriangularMesh.dummytri;
                OrientedSubSegment splitSegment = new OrientedSubSegment();
                if (triangularMesh.InsertVertex(vertexToInsert, ref searchTriangle, ref splitSegment, false, false) ==
                    VertexInsertionOutcome.Duplicate)
                {
                    vertexToInsert.type = VertexType.UndeadVertex;
                    ++triangularMesh.undeads;
                }
            }

            return RemoveBox();
        }
    }
}