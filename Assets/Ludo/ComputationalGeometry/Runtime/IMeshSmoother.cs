namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Defines the interface for a mesh smoothing algorithm.
    /// </summary>
    /// <remarks>
    /// Mesh smoothing algorithms improve the quality of a triangulation by adjusting vertex positions
    /// while preserving the overall shape and topology of the mesh. This can improve numerical stability
    /// in simulations and produce more visually appealing results. Implementations of this interface
    /// provide different smoothing strategies, such as Laplacian smoothing or optimization-based approaches.
    /// </remarks>
    public interface IMeshSmoother
    {
        /// <summary>
        /// Smooths the mesh by adjusting vertex positions to improve triangle quality.
        /// </summary>
        /// <remarks>
        /// This method applies the smoothing algorithm to the mesh, potentially through multiple iterations.
        /// It may modify vertex positions while preserving the overall topology of the mesh.
        /// The specific behavior depends on the implementation of the interface.
        /// </remarks>
        void Smooth();
    }
}