namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Represents a triangle in a geometric mesh, implementing the <see cref="ITriangle"/> interface.
    /// </summary>
    [System.Serializable]
    public class Triangle : ITriangle
    {
        internal int hash;
        internal int id;
        internal Otri[] neighbors;
        internal Vertex[] vertices;
        internal Osub[] subsegs;
        internal int region;
        internal double area;
        internal bool infected;

        /// <summary>
        /// Initializes a new instance of the <see cref="Triangle"/> class.
        /// </summary>
        public Triangle()
        {
            this.neighbors = new Otri[3];
            this.neighbors[0].triangle = TriangularMesh.dummytri;
            this.neighbors[1].triangle = TriangularMesh.dummytri;
            this.neighbors[2].triangle = TriangularMesh.dummytri;
            this.vertices = new Vertex[3];
            this.subsegs = new Osub[3];
            this.subsegs[0].seg = TriangularMesh.dummysub;
            this.subsegs[1].seg = TriangularMesh.dummysub;
            this.subsegs[2].seg = TriangularMesh.dummysub;
        }

        /// <summary>
        /// Gets the unique identifier of the triangle.
        /// </summary>
        public int ID => this.id;

        /// <summary>
        /// Gets the identifier of the first vertex of the triangle.
        /// </summary>
        public int P0 => !((Point) this.vertices[0] == (Point) null) ? this.vertices[0].id : -1;

        /// <summary>
        /// Gets the identifier of the second vertex of the triangle.
        /// </summary>
        public int P1 => !((Point) this.vertices[1] == (Point) null) ? this.vertices[1].id : -1;

        /// <summary>
        /// Gets the vertex at the specified index.
        /// </summary>
        /// <param name="index">The index of the vertex (0, 1, or 2).</param>
        /// <returns>The vertex at the specified index.</returns>
        public Vertex GetVertex(int index) => this.vertices[index];

        /// <summary>
        /// Gets the identifier of the third vertex of the triangle.
        /// </summary>
        public int P2 => !((Point) this.vertices[2] == (Point) null) ? this.vertices[2].id : -1;

        /// <summary>
        /// Gets a value indicating whether the triangle supports neighbor relationships.
        /// </summary>
        public bool SupportsNeighbors => true;

        /// <summary>
        /// Gets the identifier of the first neighboring triangle.
        /// </summary>
        public int N0 => this.neighbors[0].triangle.id;

        /// <summary>
        /// Gets the identifier of the second neighboring triangle.
        /// </summary>
        public int N1 => this.neighbors[1].triangle.id;

        /// <summary>
        /// Gets the identifier of the third neighboring triangle.
        /// </summary>
        public int N2 => this.neighbors[2].triangle.id;

        /// <summary>
        /// Gets the neighboring triangle at the specified index.
        /// </summary>
        /// <param name="index">The index of the neighbor (0, 1, or 2).</param>
        /// <returns>The neighboring triangle at the specified index, or null if no neighbor exists.</returns>
        public ITriangle GetNeighbor(int index)
        {
            return this.neighbors[index].triangle != TriangularMesh.dummytri ? (ITriangle) this.neighbors[index].triangle : (ITriangle) null;
        }

        /// <summary>
        /// Gets the segment at the specified index.
        /// </summary>
        /// <param name="index">The index of the segment (0, 1, or 2).</param>
        /// <returns>The segment at the specified index, or null if no segment exists.</returns>
        public ISegment GetSegment(int index)
        {
            return this.subsegs[index].seg != TriangularMesh.dummysub ? (ISegment) this.subsegs[index].seg : (ISegment) null;
        }

        /// <summary>
        /// Gets or sets the area of the triangle.
        /// </summary>
        public double Area
        {
            get => this.area;
            set => this.area = value;
        }

        /// <summary>
        /// Gets the region identifier of the triangle.
        /// </summary>
        public int Region => this.region;

        /// <summary>
        /// Returns a hash code for the current triangle.
        /// </summary>
        /// <returns>A hash code for the current triangle.</returns>
        public override int GetHashCode() => this.hash;

        /// <summary>
        /// Returns a string representation of the current triangle.
        /// </summary>
        /// <returns>A string representation of the current triangle in the format "TID {hash}".</returns>
        public override string ToString() => $"TID {this.hash}";
    }
}