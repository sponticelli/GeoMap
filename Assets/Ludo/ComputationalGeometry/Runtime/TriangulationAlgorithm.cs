namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Defines the available algorithms for Delaunay triangulation.
    /// </summary>
    /// <remarks>
    /// Each algorithm has different performance characteristics and trade-offs.
    /// The choice of algorithm can significantly impact the speed and memory usage
    /// of the triangulation process, especially for large point sets.
    /// </remarks>
    [System.Serializable]
    public enum TriangulationAlgorithm
    {
        /// <summary>
        /// Dwyer's divide-and-conquer algorithm for Delaunay triangulation.
        /// </summary>
        /// <remarks>
        /// This algorithm has O(n log n) expected time complexity and is generally
        /// the fastest option for large point sets. It recursively divides the point set,
        /// triangulates each subset, and then merges the results.
        /// </remarks>
        Dwyer,

        /// <summary>
        /// Incremental algorithm for Delaunay triangulation.
        /// </summary>
        /// <remarks>
        /// This algorithm inserts points one by one into an existing triangulation,
        /// maintaining the Delaunay property after each insertion. It has O(n²) worst-case
        /// time complexity but can be efficient for small point sets or when points are
        /// inserted in a good order.
        /// </remarks>
        Incremental,

        /// <summary>
        /// Fortune's sweep line algorithm for Delaunay triangulation.
        /// </summary>
        /// <remarks>
        /// This algorithm uses a horizontal sweep line that moves from top to bottom,
        /// processing events as it encounters them. It has O(n log n) worst-case time
        /// complexity and is particularly efficient for certain types of point distributions.
        /// </remarks>
        SweepLine,
    }
}