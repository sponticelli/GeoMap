namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Represents the result of a direction finding operation in a triangulation.
    /// </summary>
    /// <remarks>
    /// This enum is used by the <see cref="TriangularMesh.FindDirection"/> method to indicate the position
    /// of a point relative to a path in the triangulation. It helps determine how to navigate
    /// through the triangulation when inserting segments or locating points.
    /// </remarks>
    [System.Serializable]
    public enum PointPathOrientation
    {
        /// <summary>
        /// Indicates that the search point is within the triangulation and not collinear with any edge.
        /// </summary>
        Within,

        /// <summary>
        /// Indicates that the search point is collinear with an edge and lies to the left of the current position.
        /// </summary>
        Leftcollinear,

        /// <summary>
        /// Indicates that the search point is collinear with an edge and lies to the right of the current position.
        /// </summary>
        Rightcollinear,
    }
}