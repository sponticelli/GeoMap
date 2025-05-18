namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Defines the node numbering schemes available for mesh vertices.
    /// </summary>
    /// <remarks>
    /// Node numbering affects the ordering of vertices in a mesh, which can impact
    /// the performance of numerical algorithms and the bandwidth of resulting matrices.
    /// Different numbering schemes optimize for different computational characteristics.
    /// </remarks>
    [System.Serializable]
    public enum NodeNumbering
    {
        /// <summary>
        /// Indicates that no specific numbering scheme has been applied to the mesh.
        /// </summary>
        /// <remarks>
        /// This is typically the default state of a mesh before any renumbering operation
        /// or after operations that invalidate the current numbering scheme.
        /// </remarks>
        None,

        /// <summary>
        /// A simple sequential numbering scheme that assigns consecutive IDs to vertices.
        /// </summary>
        /// <remarks>
        /// Linear numbering assigns vertex IDs in the order they appear in the mesh's vertex collection.
        /// This is the simplest numbering scheme but may not optimize matrix bandwidth.
        /// </remarks>
        Linear,

        /// <summary>
        /// The Cuthill-McKee algorithm for reducing matrix bandwidth.
        /// </summary>
        /// <remarks>
        /// The Cuthill-McKee algorithm is a graph algorithm that reorders vertices to minimize
        /// the bandwidth of the resulting system matrices. This can significantly improve the
        /// performance of numerical solvers by reducing fill-in during matrix factorization.
        /// </remarks>
        CuthillMcKee,
    }
}