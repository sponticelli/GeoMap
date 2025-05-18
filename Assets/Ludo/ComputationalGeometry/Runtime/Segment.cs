namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Represents a segment in a geometric mesh, implementing the <see cref="ISegment"/> interface.
    /// </summary>
    [System.Serializable]
    public class Segment : ISegment
    {
        internal int hash;
        internal Osub[] subsegs;
        internal Vertex[] vertices;
        internal Otri[] triangles;
        internal int boundary;

        /// <summary>
        /// Initializes a new instance of the <see cref="Segment"/> class.
        /// </summary>
        public Segment()
        {
            this.subsegs = new Osub[2];
            this.subsegs[0].seg = TriangularMesh.dummysub;
            this.subsegs[1].seg = TriangularMesh.dummysub;
            this.vertices = new Vertex[4];
            this.triangles = new Otri[2];
            this.triangles[0].triangle = TriangularMesh.dummytri;
            this.triangles[1].triangle = TriangularMesh.dummytri;
            this.boundary = 0;
        }

        /// <summary>
        /// Gets the identifier of the first endpoint of the segment.
        /// </summary>
        public int P0 => this.vertices[0].id;

        /// <summary>
        /// Gets the identifier of the second endpoint of the segment.
        /// </summary>
        public int P1 => this.vertices[1].id;

        /// <summary>
        /// Gets the boundary mark of the segment.
        /// </summary>
        public int Boundary => this.boundary;

        /// <summary>
        /// Gets the vertex at the specified index.
        /// </summary>
        /// <param name="index">The index of the vertex.</param>
        /// <returns>The vertex at the specified index.</returns>
        public Vertex GetVertex(int index) => this.vertices[index];

        /// <summary>
        /// Gets the triangle at the specified index.
        /// </summary>
        /// <param name="index">The index of the triangle.</param>
        /// <returns>The triangle at the specified index, or null if no triangle exists.</returns>
        public ITriangle GetTriangle(int index)
        {
            return this.triangles[index].triangle != TriangularMesh.dummytri ? (ITriangle) this.triangles[index].triangle : (ITriangle) null;
        }

        /// <summary>
        /// Returns a hash code for the current segment.
        /// </summary>
        /// <returns>A hash code for the current segment.</returns>
        public override int GetHashCode() => this.hash;

        /// <summary>
        /// Returns a string representation of the current segment.
        /// </summary>
        /// <returns>A string representation of the current segment in the format "SID {hash}".</returns>
        public override string ToString() => $"SID {this.hash}";
    }
}