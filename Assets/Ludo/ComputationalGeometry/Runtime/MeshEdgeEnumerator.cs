using System;
using System.Collections;
using System.Collections.Generic;

namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Provides an enumerator for iterating through all edges in a mesh.
    /// </summary>
    /// <remarks>
    /// This enumerator traverses all triangles in the mesh and extracts unique edges.
    /// It ensures each edge is enumerated exactly once by comparing triangle IDs and
    /// using the dummy triangle check to handle boundary edges.
    /// </remarks>
    [System.Serializable]
    public class MeshEdgeEnumerator : IEnumerator<MeshEdge>, IDisposable, IEnumerator
    {
        /// <summary>
        /// Enumerator for the triangles in the mesh.
        /// </summary>
        private IEnumerator<Triangle> triangles;

        /// <summary>
        /// The current oriented triangle being processed.
        /// </summary>
        private Otri tri;

        /// <summary>
        /// The neighboring triangle across the current edge.
        /// </summary>
        private Otri neighbor;

        /// <summary>
        /// The subsegment associated with the current edge, if any.
        /// </summary>
        private Osub sub;

        /// <summary>
        /// The current edge being enumerated.
        /// </summary>
        private MeshEdge current;

        /// <summary>
        /// The first vertex of the current edge.
        /// </summary>
        private Vertex p1;

        /// <summary>
        /// The second vertex of the current edge.
        /// </summary>
        private Vertex p2;

        /// <summary>
        /// Initializes a new instance of the <see cref="MeshEdgeEnumerator"/> class for the specified mesh.
        /// </summary>
        /// <param name="triangularMesh">The mesh whose edges will be enumerated.</param>
        /// <remarks>
        /// The constructor initializes the triangle enumerator and sets up the first triangle
        /// to begin the edge enumeration process.
        /// </remarks>
        public MeshEdgeEnumerator(TriangularMesh triangularMesh)
        {
            this.triangles = (IEnumerator<Triangle>) triangularMesh.triangles.Values.GetEnumerator();
            this.triangles.MoveNext();
            this.tri.triangle = this.triangles.Current;
            this.tri.orient = 0;
        }

        /// <summary>
        /// Gets the current edge in the enumeration.
        /// </summary>
        /// <value>The current edge.</value>
        public MeshEdge Current => this.current;

        /// <summary>
        /// Releases all resources used by the <see cref="MeshEdgeEnumerator"/>.
        /// </summary>
        public void Dispose() => this.triangles.Dispose();

        /// <summary>
        /// Gets the current edge in the enumeration as an object.
        /// </summary>
        /// <value>The current edge as an object.</value>
        object IEnumerator.Current => (object) this.current;

        /// <summary>
        /// Advances the enumerator to the next edge in the mesh.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the enumerator was successfully advanced to the next edge;
        /// <c>false</c> if the enumerator has passed the end of the collection.
        /// </returns>
        /// <remarks>
        /// This method iterates through the triangles and their orientations to find unique edges.
        /// An edge is considered unique if:
        /// 1. The current triangle ID is less than the neighboring triangle ID, or
        /// 2. The neighboring triangle is a dummy triangle (indicating a boundary edge).
        ///
        /// The method handles advancing to the next triangle when all orientations of the current
        /// triangle have been processed.
        /// </remarks>
        public bool MoveNext()
        {
            if (this.tri.triangle == null)
                return false;
            this.current = (MeshEdge) null;
            while (this.current == null)
            {
                if (this.tri.orient == 3)
                {
                    if (!this.triangles.MoveNext())
                        return false;
                    this.tri.triangle = this.triangles.Current;
                    this.tri.orient = 0;
                }
                this.tri.Sym(ref this.neighbor);
                if (this.tri.triangle.id < this.neighbor.triangle.id || this.neighbor.triangle == TriangularMesh.dummytri)
                {
                    this.p1 = this.tri.Org();
                    this.p2 = this.tri.Dest();
                    this.tri.SegPivot(ref this.sub);
                    this.current = new MeshEdge(this.p1.id, this.p2.id, this.sub.seg.boundary);
                }
                ++this.tri.orient;
            }
            return true;
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first edge in the collection.
        /// </summary>
        public void Reset() => this.triangles.Reset();
    }
}