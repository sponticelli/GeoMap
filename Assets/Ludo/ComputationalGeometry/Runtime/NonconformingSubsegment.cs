namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Represents a subsegment that violates quality constraints and needs to be refined.
    /// Used during mesh quality improvement to track segments that need to be split.
    /// </summary>
    [System.Serializable]
    public class NonconformingSubsegment
    {
        /// <summary>
        /// Static counter used to generate unique hash values for each instance.
        /// </summary>
        private static int hashSeed;

        /// <summary>
        /// The unique hash value for this instance.
        /// </summary>
        internal int Hash;

        /// <summary>
        /// The oriented subsegment that violates quality constraints.
        /// </summary>
        public Osub encsubseg;

        /// <summary>
        /// The origin vertex of the subsegment.
        /// </summary>
        public Vertex subsegorg;

        /// <summary>
        /// The destination vertex of the subsegment.
        /// </summary>
        public Vertex subsegdest;

        /// <summary>
        /// Initializes a new instance of the <see cref="NonconformingSubsegment"/> class.
        /// Assigns a unique hash value to the instance.
        /// </summary>
        public NonconformingSubsegment() => this.Hash = NonconformingSubsegment.hashSeed++;

        /// <summary>
        /// Returns a hash code for the current bad subsegment.
        /// </summary>
        /// <returns>A hash code for the current bad subsegment.</returns>
        public override int GetHashCode() => this.Hash;

        /// <summary>
        /// Returns a string representation of the current bad subsegment.
        /// </summary>
        /// <returns>A string representation of the current bad subsegment in the format "B-SID {hash}".</returns>
        public override string ToString() => $"B-SID {this.encsubseg.seg.hash}";
    }
}