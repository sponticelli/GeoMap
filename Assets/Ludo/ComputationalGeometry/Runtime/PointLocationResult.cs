using System;

namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Represents the result of a point location operation in a triangulation.
    /// </summary>
    /// <remarks>
    /// This enum is used by the <see cref="TriangleLocator"/> class to indicate the position
    /// of a point relative to the triangulation. It helps determine how to handle different
    /// scenarios when inserting vertices, checking constraints, or performing other operations
    /// that depend on a point's location.
    /// </remarks>
    [Serializable]
    public enum PointLocationResult
    {
        /// <summary>
        /// Indicates that the point is strictly inside a triangle.
        /// </summary>
        /// <remarks>
        /// When a point is inside a triangle, it is not on any edge or vertex of the triangle.
        /// This is the most common case for points within the triangulation.
        /// </remarks>
        InTriangle,

        /// <summary>
        /// Indicates that the point lies exactly on an edge of a triangle.
        /// </summary>
        /// <remarks>
        /// When a point is on an edge, it is collinear with and between the two vertices that form the edge.
        /// This case requires special handling during operations like vertex insertion.
        /// </remarks>
        OnEdge,

        /// <summary>
        /// Indicates that the point coincides with a vertex of a triangle.
        /// </summary>
        /// <remarks>
        /// When a point is on a vertex, it has the exact same coordinates as an existing vertex
        /// in the triangulation. This typically indicates a duplicate point.
        /// </remarks>
        OnVertex,

        /// <summary>
        /// Indicates that the point is outside the triangulation.
        /// </summary>
        /// <remarks>
        /// When a point is outside the triangulation, it is not contained within any triangle
        /// of the mesh. This can occur when the point is outside the convex hull of the triangulation
        /// or when it is inside a hole in the triangulation.
        /// </remarks>
        Outside,
    }
}