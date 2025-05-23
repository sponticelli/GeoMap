using System;

namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Represents a triangle that violates quality constraints and needs to be refined.
    /// Used during mesh quality improvement to track triangles that need to be split.
    /// </summary>
    [Serializable]
    public class QualityViolatingTriangle
    {
        /// <summary>
        /// Static counter used to generate unique IDs for each instance.
        /// </summary>
        public static int IDCounter;

        /// <summary>
        /// The unique identifier for this instance.
        /// </summary>
        public int id;

        /// <summary>
        /// The oriented triangle that violates quality constraints.
        /// </summary>
        public OrientedTriangle nonConformingTriangle;

        /// <summary>
        /// The quality measure of the triangle, used for prioritizing refinement.
        /// Typically represents the shortest edge length or another quality metric.
        /// </summary>
        public double qualityMetric;

        /// <summary>
        /// The origin vertex of the triangle.
        /// </summary>
        public Vertex vertexOrigin;

        /// <summary>
        /// The destination vertex of the triangle.
        /// </summary>
        public Vertex vertexDestination;

        /// <summary>
        /// The apex vertex of the triangle.
        /// </summary>
        public Vertex vertexApex;

        /// <summary>
        /// Reference to the next bad triangle in a linked list structure.
        /// Used by the QualityViolatingTriangleQueue to maintain a priority queue of bad triangles.
        /// </summary>
        public QualityViolatingTriangle nextNonComformingTriangle;

        /// <summary>
        /// Initializes a new instance of the <see cref="QualityViolatingTriangle"/> class.
        /// Assigns a unique ID to the instance.
        /// </summary>
        public QualityViolatingTriangle() => id = IDCounter++;

        /// <summary>
        /// Returns a string representation of the current bad triangle.
        /// </summary>
        /// <returns>A string representation of the current bad triangle in the format "B-TID {hash}".</returns>
        public override string ToString() => $"B-TID {nonConformingTriangle.triangle.hash}";
    }
}