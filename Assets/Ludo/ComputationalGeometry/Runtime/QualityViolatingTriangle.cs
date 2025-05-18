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
        public static int OTID;

        /// <summary>
        /// The unique identifier for this instance.
        /// </summary>
        public int ID;

        /// <summary>
        /// The oriented triangle that violates quality constraints.
        /// </summary>
        public OrientedTriangle poortri;

        /// <summary>
        /// The quality measure of the triangle, used for prioritizing refinement.
        /// Typically represents the shortest edge length or another quality metric.
        /// </summary>
        public double key;

        /// <summary>
        /// The origin vertex of the triangle.
        /// </summary>
        public Vertex triangorg;

        /// <summary>
        /// The destination vertex of the triangle.
        /// </summary>
        public Vertex triangdest;

        /// <summary>
        /// The apex vertex of the triangle.
        /// </summary>
        public Vertex triangapex;

        /// <summary>
        /// Reference to the next bad triangle in a linked list structure.
        /// Used by the QualityViolatingTriangleQueue to maintain a priority queue of bad triangles.
        /// </summary>
        public QualityViolatingTriangle nexttriang;

        /// <summary>
        /// Initializes a new instance of the <see cref="QualityViolatingTriangle"/> class.
        /// Assigns a unique ID to the instance.
        /// </summary>
        public QualityViolatingTriangle() => ID = OTID++;

        /// <summary>
        /// Returns a string representation of the current bad triangle.
        /// </summary>
        /// <returns>A string representation of the current bad triangle in the format "B-TID {hash}".</returns>
        public override string ToString() => $"B-TID {poortri.triangle.hash}";
    }
}