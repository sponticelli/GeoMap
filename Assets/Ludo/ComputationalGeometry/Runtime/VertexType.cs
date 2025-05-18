namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Defines the types of vertices in a geometric mesh.
    /// </summary>
    public enum VertexType
    {
        /// <summary>
        /// A vertex that was part of the input geometry.
        /// </summary>
        InputVertex,

        /// <summary>
        /// A vertex that was created as part of a segment.
        /// </summary>
        SegmentVertex,

        /// <summary>
        /// A vertex that was created during triangulation and is not constrained.
        /// </summary>
        FreeVertex,

        /// <summary>
        /// A vertex that has been marked as dead and should be ignored.
        /// </summary>
        DeadVertex,

        /// <summary>
        /// A vertex that was previously marked as dead but has been reactivated.
        /// </summary>
        UndeadVertex,
    }
}