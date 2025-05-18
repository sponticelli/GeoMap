namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Represents an edge connecting two points in a geometric mesh.
    /// </summary>
    [System.Serializable]
    public class MeshEdge
    {
        /// <summary>
        /// Gets the identifier of the first endpoint of the edge.
        /// </summary>
        public int P0 { get; private set; }

        /// <summary>
        /// Gets the identifier of the second endpoint of the edge.
        /// </summary>
        public int P1 { get; private set; }

        /// <summary>
        /// Gets the boundary mark of the edge.
        /// </summary>
        public int Boundary { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MeshEdge"/> class with specified endpoints and default boundary mark 0.
        /// </summary>
        /// <param name="p0">The identifier of the first endpoint.</param>
        /// <param name="p1">The identifier of the second endpoint.</param>
        public MeshEdge(int p0, int p1)
            : this(p0, p1, 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MeshEdge"/> class with specified endpoints and boundary mark.
        /// </summary>
        /// <param name="p0">The identifier of the first endpoint.</param>
        /// <param name="p1">The identifier of the second endpoint.</param>
        /// <param name="boundary">The boundary mark of the edge.</param>
        public MeshEdge(int p0, int p1, int boundary)
        {
            this.P0 = p0;
            this.P1 = p1;
            this.Boundary = boundary;
        }
    }
}