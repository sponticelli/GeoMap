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
            OrientedTriangle newotri = new OrientedTriangle();
            AxisAlignedBoundingBox2D bounds = _triangularMesh.bounds;
            double num = bounds.Width;
            if (bounds.Height > num)
                num = bounds.Height;
            if (num == 0.0)
                num = 1.0;
            _triangularMesh.infvertex1 = new Vertex(bounds.Xmin - 50.0 * num, bounds.Ymin - 40.0 * num);
            _triangularMesh.infvertex2 = new Vertex(bounds.Xmax + 50.0 * num, bounds.Ymin - 40.0 * num);
            _triangularMesh.infvertex3 = new Vertex(0.5 * (bounds.Xmin + bounds.Xmax), bounds.Ymax + 60.0 * num);
            _triangularMesh.MakeTriangle(ref newotri);
            newotri.SetOrg(_triangularMesh.infvertex1);
            newotri.SetDest(_triangularMesh.infvertex2);
            newotri.SetApex(_triangularMesh.infvertex3);
            TriangularMesh.dummytri.neighbors[0] = newotri;
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
            OrientedTriangle o2_1 = new OrientedTriangle();
            OrientedTriangle o2_2 = new OrientedTriangle();
            OrientedTriangle o2_3 = new OrientedTriangle();
            OrientedTriangle o2_4 = new OrientedTriangle();
            OrientedTriangle o2_5 = new OrientedTriangle();
            OrientedTriangle o2_6 = new OrientedTriangle();
            bool flag = !_triangularMesh.behavior.Poly;
            o2_4.triangle = TriangularMesh.dummytri;
            o2_4.orient = 0;
            o2_4.SymSelf();
            o2_4.Lprev(ref o2_5);
            o2_4.LnextSelf();
            o2_4.SymSelf();
            o2_4.Lprev(ref o2_2);
            o2_2.SymSelf();
            o2_4.Lnext(ref o2_3);
            o2_3.SymSelf();
            if (o2_3.triangle == TriangularMesh.dummytri)
            {
                o2_2.LprevSelf();
                o2_2.SymSelf();
            }

            TriangularMesh.dummytri.neighbors[0] = o2_2;
            int num = -2;
            while (!o2_4.Equal(o2_5))
            {
                ++num;
                o2_4.Lprev(ref o2_6);
                o2_6.SymSelf();
                if (flag && o2_6.triangle != TriangularMesh.dummytri)
                {
                    Vertex vertex = o2_6.Org();
                    if (vertex.mark == 0)
                        vertex.mark = 1;
                }

                o2_6.Dissolve();
                o2_4.Lnext(ref o2_1);
                o2_1.Sym(ref o2_4);
                _triangularMesh.TriangleDealloc(o2_1.triangle);
                if (o2_4.triangle == TriangularMesh.dummytri)
                    o2_6.Copy(ref o2_4);
            }

            _triangularMesh.TriangleDealloc(o2_5.triangle);
            return num;
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
            OrientedTriangle searchtri = new OrientedTriangle();
            GetBoundingBox();
            foreach (Vertex newvertex in triangularMesh.vertices.Values)
            {
                searchtri.triangle = TriangularMesh.dummytri;
                OrientedSubSegment splitseg = new OrientedSubSegment();
                if (triangularMesh.InsertVertex(newvertex, ref searchtri, ref splitseg, false, false) ==
                    VertexInsertionOutcome.Duplicate)
                {
                    newvertex.type = VertexType.UndeadVertex;
                    ++triangularMesh.undeads;
                }
            }

            return RemoveBox();
        }
    }
}