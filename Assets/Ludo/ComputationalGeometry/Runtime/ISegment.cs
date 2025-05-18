namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Defines the interface for a segment in a geometric mesh.
    /// </summary>
    public interface ISegment
    {
        /// <summary>
        /// Gets the identifier of the first endpoint of the segment.
        /// </summary>
        int P0 { get; }

        /// <summary>
        /// Gets the identifier of the second endpoint of the segment.
        /// </summary>
        int P1 { get; }

        /// <summary>
        /// Gets the boundary mark of the segment.
        /// </summary>
        int Boundary { get; }

        /// <summary>
        /// Gets the vertex at the specified index.
        /// </summary>
        /// <param name="index">The index of the vertex.</param>
        /// <returns>The vertex at the specified index.</returns>
        Vertex GetVertex(int index);

        /// <summary>
        /// Gets the triangle at the specified index.
        /// </summary>
        /// <param name="index">The index of the triangle.</param>
        /// <returns>The triangle at the specified index, or null if no triangle exists.</returns>
        ITriangle GetTriangle(int index);
    }
}