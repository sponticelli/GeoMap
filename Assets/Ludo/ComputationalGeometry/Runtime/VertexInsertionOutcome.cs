using System;

namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Represents the result of a vertex insertion operation in a triangulation.
    /// </summary>
    /// <remarks>
    /// This enum is used by the <see cref="TriangularMesh.InsertVertex"/> method to indicate the outcome
    /// of attempting to insert a vertex into the triangulation. It helps determine how to
    /// handle different insertion scenarios, such as when a vertex encroaches on a segment
    /// or violates quality constraints.
    /// </remarks>
    [Serializable]
    public enum VertexInsertionOutcome
    {
        /// <summary>
        /// Indicates that the vertex was successfully inserted into the triangulation.
        /// </summary>
        Successful,

        /// <summary>
        /// Indicates that the vertex encroaches upon a subsegment and was not inserted.
        /// This typically occurs during constrained Delaunay triangulation when a vertex
        /// would violate a constraint.
        /// </summary>
        Encroaching,

        /// <summary>
        /// Indicates that the vertex violates a quality constraint and was not inserted.
        /// This can occur when enforcing minimum angle or maximum area constraints.
        /// </summary>
        Violating,

        /// <summary>
        /// Indicates that the vertex is a duplicate of an existing vertex and was not inserted.
        /// In this case, the vertex is typically marked as an "undead" vertex and ignored.
        /// </summary>
        Duplicate,
    }
}