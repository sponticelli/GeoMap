namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Defines the interface for a triangle in a geometric mesh.
    /// </summary>
    public interface ITriangle
    {
        /// <summary>
        /// Gets the unique identifier of the triangle.
        /// </summary>
        int ID { get; }

        /// <summary>
        /// Gets the identifier of the first vertex of the triangle.
        /// </summary>
        int P0 { get; }

        /// <summary>
        /// Gets the identifier of the second vertex of the triangle.
        /// </summary>
        int P1 { get; }

        /// <summary>
        /// Gets the identifier of the third vertex of the triangle.
        /// </summary>
        int P2 { get; }

        /// <summary>
        /// Gets the vertex at the specified index.
        /// </summary>
        /// <param name="index">The index of the vertex (0, 1, or 2).</param>
        /// <returns>The vertex at the specified index.</returns>
        Vertex GetVertex(int index);

        /// <summary>
        /// Gets a value indicating whether the triangle supports neighbor relationships.
        /// </summary>
        bool SupportsNeighbors { get; }

        /// <summary>
        /// Gets the identifier of the first neighboring triangle.
        /// </summary>
        int N0 { get; }

        /// <summary>
        /// Gets the identifier of the second neighboring triangle.
        /// </summary>
        int N1 { get; }

        /// <summary>
        /// Gets the identifier of the third neighboring triangle.
        /// </summary>
        int N2 { get; }

        /// <summary>
        /// Gets the neighboring triangle at the specified index.
        /// </summary>
        /// <param name="index">The index of the neighbor (0, 1, or 2).</param>
        /// <returns>The neighboring triangle at the specified index.</returns>
        ITriangle GetNeighbor(int index);

        /// <summary>
        /// Gets the segment at the specified index.
        /// </summary>
        /// <param name="index">The index of the segment (0, 1, or 2).</param>
        /// <returns>The segment at the specified index.</returns>
        ISegment GetSegment(int index);

        /// <summary>
        /// Gets or sets the area of the triangle.
        /// </summary>
        double Area { get; set; }

        /// <summary>
        /// Gets the region identifier of the triangle.
        /// </summary>
        int Region { get; }
    }
}