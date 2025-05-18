namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Represents an oriented subsegment in a geometric mesh.
    /// </summary>
    public struct Osub
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
        public override string ToString() => this.seg == null ? "O-TID [null]" : $"O-SID {this.seg.hash}";

        /// <summary>
        /// Creates a symmetric version of this oriented subsegment.
        /// </summary>
        /// <param name="o2">When this method returns, contains the symmetric oriented subsegment.</param>
        public void Sym(ref Osub o2)
        {
            o2.seg = this.seg;
            o2.orient = 1 - this.orient;
        }

        /// <summary>
        /// Transforms this oriented subsegment into its symmetric version.
        /// </summary>
        public void SymSelf() => this.orient = 1 - this.orient;

        /// <summary>
        /// Gets the subsegment connected to this oriented subsegment.
        /// </summary>
        /// <param name="o2">When this method returns, contains the connected subsegment.</param>
        public void Pivot(ref Osub o2) => o2 = this.seg.subsegs[this.orient];

        /// <summary>
        /// Transforms this oriented subsegment into the subsegment connected to it.
        /// </summary>
        public void PivotSelf() => this = this.seg.subsegs[this.orient];

        /// <summary>
        /// Gets the next subsegment in the sequence.
        /// </summary>
        /// <param name="o2">When this method returns, contains the next subsegment.</param>
        public void Next(ref Osub o2) => o2 = this.seg.subsegs[1 - this.orient];

        /// <summary>
        /// Transforms this oriented subsegment into the next subsegment in the sequence.
        /// </summary>
        public void NextSelf() => this = this.seg.subsegs[1 - this.orient];

        /// <summary>
        /// Gets the origin vertex of this oriented subsegment.
        /// </summary>
        /// <returns>The origin vertex.</returns>
        public Vertex Org() => this.seg.vertices[this.orient];

        /// <summary>
        /// Gets the destination vertex of this oriented subsegment.
        /// </summary>
        /// <returns>The destination vertex.</returns>
        public Vertex Dest() => this.seg.vertices[1 - this.orient];

        /// <summary>
        /// Sets the origin vertex of this oriented subsegment.
        /// </summary>
        /// <param name="ptr">The vertex to set as the origin.</param>
        public void SetOrg(Vertex ptr) => this.seg.vertices[this.orient] = ptr;

        /// <summary>
        /// Sets the destination vertex of this oriented subsegment.
        /// </summary>
        /// <param name="ptr">The vertex to set as the destination.</param>
        public void SetDest(Vertex ptr) => this.seg.vertices[1 - this.orient] = ptr;

        /// <summary>
        /// Gets the segment origin vertex of this oriented subsegment.
        /// </summary>
        /// <returns>The segment origin vertex.</returns>
        public Vertex SegOrg() => this.seg.vertices[2 + this.orient];

        /// <summary>
        /// Gets the segment destination vertex of this oriented subsegment.
        /// </summary>
        /// <returns>The segment destination vertex.</returns>
        public Vertex SegDest() => this.seg.vertices[3 - this.orient];

        /// <summary>
        /// Sets the segment origin vertex of this oriented subsegment.
        /// </summary>
        /// <param name="ptr">The vertex to set as the segment origin.</param>
        public void SetSegOrg(Vertex ptr) => this.seg.vertices[2 + this.orient] = ptr;

        /// <summary>
        /// Sets the segment destination vertex of this oriented subsegment.
        /// </summary>
        /// <param name="ptr">The vertex to set as the segment destination.</param>
        public void SetSegDest(Vertex ptr) => this.seg.vertices[3 - this.orient] = ptr;

        /// <summary>
        /// Gets the boundary mark of this oriented subsegment.
        /// </summary>
        /// <returns>The boundary mark.</returns>
        public int Mark() => this.seg.boundary;

        /// <summary>
        /// Sets the boundary mark of this oriented subsegment.
        /// </summary>
        /// <param name="value">The boundary mark to set.</param>
        public void SetMark(int value) => this.seg.boundary = value;

        /// <summary>
        /// Bonds this oriented subsegment to another oriented subsegment.
        /// </summary>
        /// <param name="o2">The oriented subsegment to bond with.</param>
        public void Bond(ref Osub o2)
        {
            this.seg.subsegs[this.orient] = o2;
            o2.seg.subsegs[o2.orient] = this;
        }

        /// <summary>
        /// Dissolves the bond between this oriented subsegment and any connected subsegment.
        /// </summary>
        public void Dissolve() => this.seg.subsegs[this.orient].seg = TriangularMesh.dummysub;

        /// <summary>
        /// Copies this oriented subsegment to another oriented subsegment.
        /// </summary>
        /// <param name="o2">When this method returns, contains a copy of this oriented subsegment.</param>
        public void Copy(ref Osub o2)
        {
            o2.seg = this.seg;
            o2.orient = this.orient;
        }

        /// <summary>
        /// Determines whether this oriented subsegment is equal to another oriented subsegment.
        /// </summary>
        /// <param name="o2">The oriented subsegment to compare with.</param>
        /// <returns>True if the oriented subsegments are equal; otherwise, false.</returns>
        public bool Equal(Osub o2) => this.seg == o2.seg && this.orient == o2.orient;

        /// <summary>
        /// Determines whether the specified segment is dead.
        /// </summary>
        /// <param name="sub">The segment to check.</param>
        /// <returns>True if the segment is dead; otherwise, false.</returns>
        public static bool IsDead(Segment sub) => sub.subsegs[0].seg == null;

        /// <summary>
        /// Kills the specified segment by setting its subsegments to null.
        /// </summary>
        /// <param name="sub">The segment to kill.</param>
        public static void Kill(Segment sub)
        {
            sub.subsegs[0].seg = (Segment) null;
            sub.subsegs[1].seg = (Segment) null;
        }

        /// <summary>
        /// Gets the triangle connected to this oriented subsegment.
        /// </summary>
        /// <param name="ot">When this method returns, contains the connected triangle.</param>
        public void TriPivot(ref Otri ot) => ot = this.seg.triangles[this.orient];

        /// <summary>
        /// Dissolves the bond between this oriented subsegment and any connected triangle.
        /// </summary>
        public void TriDissolve() => this.seg.triangles[this.orient].triangle = TriangularMesh.dummytri;
    }
}