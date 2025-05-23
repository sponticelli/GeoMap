namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Represents an oriented subsegment in a geometric mesh.
    /// </summary>
    public struct OrientedSubSegment
    {
        /// <summary>
        /// The segment associated with this oriented subsegment.
        /// </summary>
        public Segment seg;

        /// <summary>
        /// The orientation of this subsegment (0 or 1).
        /// </summary>
        public int orient;

        /// <summary>
        /// Returns a string representation of the current oriented subsegment.
        /// </summary>
        /// <returns>A string representation of the current oriented subsegment.</returns>
        public override string ToString() => seg == null ? "O-TID [null]" : $"O-SID {seg.hash}";

        /// <summary>
        /// Creates a symmetric version of this oriented subsegment.
        /// </summary>
        /// <param name="o2">When this method returns, contains the symmetric oriented subsegment.</param>
        public void Sym(ref OrientedSubSegment o2)
        {
            o2.seg = seg;
            o2.orient = 1 - orient;
        }

        /// <summary>
        /// Transforms this oriented subsegment into its symmetric version.
        /// </summary>
        public void SymSelf() => orient = 1 - orient;

        /// <summary>
        /// Gets the subsegment connected to this oriented subsegment.
        /// </summary>
        /// <param name="o2">When this method returns, contains the connected subsegment.</param>
        public void Pivot(ref OrientedSubSegment o2) => o2 = seg.OrientedSubSegments[orient];

        /// <summary>
        /// Transforms this oriented subsegment into the subsegment connected to it.
        /// </summary>
        public void PivotSelf() => this = seg.OrientedSubSegments[orient];

        /// <summary>
        /// Gets the next subsegment in the sequence.
        /// </summary>
        /// <param name="o2">When this method returns, contains the next subsegment.</param>
        public void Next(ref OrientedSubSegment o2) => o2 = seg.OrientedSubSegments[1 - orient];

        /// <summary>
        /// Transforms this oriented subsegment into the next subsegment in the sequence.
        /// </summary>
        public void NextSelf() => this = seg.OrientedSubSegments[1 - orient];

        /// <summary>
        /// Gets the origin vertex of this oriented subsegment.
        /// </summary>
        /// <returns>The origin vertex.</returns>
        public Vertex Origin() => seg.vertices[orient];

        /// <summary>
        /// Gets the destination vertex of this oriented subsegment.
        /// </summary>
        /// <returns>The destination vertex.</returns>
        public Vertex Destination() => seg.vertices[1 - orient];

        /// <summary>
        /// Sets the origin vertex of this oriented subsegment.
        /// </summary>
        /// <param name="ptr">The vertex to set as the origin.</param>
        public void SetOrigin(Vertex ptr) => seg.vertices[orient] = ptr;

        /// <summary>
        /// Sets the destination vertex of this oriented subsegment.
        /// </summary>
        /// <param name="ptr">The vertex to set as the destination.</param>
        public void SetDestination(Vertex ptr) => seg.vertices[1 - orient] = ptr;

        /// <summary>
        /// Gets the segment origin vertex of this oriented subsegment.
        /// </summary>
        /// <returns>The segment origin vertex.</returns>
        public Vertex GetSegmentOrigin() => seg.vertices[2 + orient];

        /// <summary>
        /// Gets the segment destination vertex of this oriented subsegment.
        /// </summary>
        /// <returns>The segment destination vertex.</returns>
        public Vertex GetSegmentDestination() => seg.vertices[3 - orient];

        /// <summary>
        /// Sets the segment origin vertex of this oriented subsegment.
        /// </summary>
        /// <param name="ptr">The vertex to set as the segment origin.</param>
        public void SetSegOrg(Vertex ptr) => seg.vertices[2 + orient] = ptr;

        /// <summary>
        /// Sets the segment destination vertex of this oriented subsegment.
        /// </summary>
        /// <param name="ptr">The vertex to set as the segment destination.</param>
        public void SetSegDest(Vertex ptr) => seg.vertices[3 - orient] = ptr;

        /// <summary>
        /// Gets the boundary mark of this oriented subsegment.
        /// </summary>
        /// <returns>The boundary mark.</returns>
        public int Mark() => seg.boundary;

        /// <summary>
        /// Sets the boundary mark of this oriented subsegment.
        /// </summary>
        /// <param name="value">The boundary mark to set.</param>
        public void SetMark(int value) => seg.boundary = value;

        /// <summary>
        /// Bonds this oriented subsegment to another oriented subsegment.
        /// </summary>
        /// <param name="o2">The oriented subsegment to bond with.</param>
        public void Bond(ref OrientedSubSegment o2)
        {
            seg.OrientedSubSegments[orient] = o2;
            o2.seg.OrientedSubSegments[o2.orient] = this;
        }

        /// <summary>
        /// Dissolves the bond between this oriented subsegment and any connected subsegment.
        /// </summary>
        public void Dissolve() => seg.OrientedSubSegments[orient].seg = TriangularMesh.dummysub;

        /// <summary>
        /// Copies this oriented subsegment to another oriented subsegment.
        /// </summary>
        /// <param name="o2">When this method returns, contains a copy of this oriented subsegment.</param>
        public void Copy(ref OrientedSubSegment o2)
        {
            o2.seg = seg;
            o2.orient = orient;
        }

        /// <summary>
        /// Determines whether this oriented subsegment is equal to another oriented subsegment.
        /// </summary>
        /// <param name="o2">The oriented subsegment to compare with.</param>
        /// <returns>True if the oriented subsegments are equal; otherwise, false.</returns>
        public bool Equal(OrientedSubSegment o2) => seg == o2.seg && orient == o2.orient;

        /// <summary>
        /// Determines whether the specified segment is dead.
        /// </summary>
        /// <param name="sub">The segment to check.</param>
        /// <returns>True if the segment is dead; otherwise, false.</returns>
        public static bool IsDead(Segment sub) => sub.OrientedSubSegments[0].seg == null;

        /// <summary>
        /// Kills the specified segment by setting its subsegments to null.
        /// </summary>
        /// <param name="sub">The segment to kill.</param>
        public static void Kill(Segment sub)
        {
            sub.OrientedSubSegments[0].seg = null;
            sub.OrientedSubSegments[1].seg = null;
        }

        /// <summary>
        /// Gets the triangle connected to this oriented subsegment.
        /// </summary>
        /// <param name="ot">When this method returns, contains the connected triangle.</param>
        public void TriPivot(ref OrientedTriangle ot) => ot = seg.triangles[orient];

        /// <summary>
        /// Dissolves the bond between this oriented subsegment and any connected triangle.
        /// </summary>
        public void TriDissolve() => seg.triangles[orient].triangle = TriangularMesh.dummytri;
    }
}