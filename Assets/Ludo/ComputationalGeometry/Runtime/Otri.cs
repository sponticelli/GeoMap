namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Represents an oriented triangle in a triangular mesh.
    /// </summary>
    /// <remarks>
    /// An oriented triangle consists of a triangle and an orientation (0, 1, or 2),
    /// which specifies one of the three edges of the triangle. This structure provides
    /// methods for navigating through the mesh by moving from one triangle to adjacent
    /// triangles, and for accessing and modifying the vertices and neighboring triangles.
    ///
    /// The orientation is used to identify which edge or vertex of the triangle is being
    /// referenced. For example, orientation 0 refers to the edge opposite vertex 0,
    /// orientation 1 refers to the edge opposite vertex 1, and orientation 2 refers to
    /// the edge opposite vertex 2.
    /// </remarks>
    [System.Serializable]
    public struct Otri
    {
        /// <summary>
        /// The triangle that this oriented triangle refers to.
        /// </summary>
        public Triangle triangle;

        /// <summary>
        /// The orientation of the triangle (0, 1, or 2).
        /// </summary>
        public int orient;

        /// <summary>
        /// Lookup table for adding 1 modulo 3 to an index.
        /// </summary>
        /// <remarks>
        /// This is used to efficiently cycle through the three orientations of a triangle
        /// in a clockwise direction.
        /// </remarks>
        private static readonly int[] plus1Mod3 = new int[3]
        {
            1,
            2,
            0
        };

        /// <summary>
        /// Lookup table for subtracting 1 modulo 3 from an index.
        /// </summary>
        /// <remarks>
        /// This is used to efficiently cycle through the three orientations of a triangle
        /// in a counterclockwise direction.
        /// </remarks>
        private static readonly int[] minus1Mod3 = new int[3]
        {
            2,
            0,
            1
        };

        /// <summary>
        /// Returns a string representation of the oriented triangle.
        /// </summary>
        /// <returns>A string in the format "O-TID {hash}" or "O-TID [null]" if the triangle is null.</returns>
        public override string ToString()
        {
            return this.triangle == null ? "O-TID [null]" : $"O-TID {this.triangle.hash}";
        }

        /// <summary>
        /// Sets o2 to be the symmetric (opposite) triangle of this triangle.
        /// </summary>
        /// <param name="o2">When this method returns, contains the symmetric triangle.</param>
        /// <remarks>
        /// The symmetric triangle is the one that shares the current edge but is on the opposite side.
        /// This operation is equivalent to finding the triangle adjacent to the current edge.
        /// </remarks>
        public void Sym(ref Otri o2)
        {
            o2.triangle = this.triangle.neighbors[this.orient].triangle;
            o2.orient = this.triangle.neighbors[this.orient].orient;
        }

        /// <summary>
        /// Transforms this triangle into its symmetric (opposite) triangle.
        /// </summary>
        /// <remarks>
        /// This method modifies the current triangle to become its symmetric triangle.
        /// It is equivalent to finding the triangle adjacent to the current edge and making
        /// that the current triangle.
        /// </remarks>
        public void SymSelf()
        {
            int orient = this.orient;
            this.orient = this.triangle.neighbors[orient].orient;
            this.triangle = this.triangle.neighbors[orient].triangle;
        }

        /// <summary>
        /// Sets o2 to the next edge (in counterclockwise order) of the same triangle.
        /// </summary>
        /// <param name="o2">When this method returns, contains the next edge of the same triangle.</param>
        /// <remarks>
        /// This operation rotates counterclockwise around the triangle to the next edge.
        /// </remarks>
        public void Lnext(ref Otri o2)
        {
            o2.triangle = this.triangle;
            o2.orient = Otri.plus1Mod3[this.orient];
        }

        /// <summary>
        /// Transforms this triangle to the next edge (in counterclockwise order) of the same triangle.
        /// </summary>
        /// <remarks>
        /// This method modifies the current triangle to rotate counterclockwise to the next edge.
        /// </remarks>
        public void LnextSelf() => this.orient = Otri.plus1Mod3[this.orient];

        /// <summary>
        /// Sets o2 to the previous edge (in clockwise order) of the same triangle.
        /// </summary>
        /// <param name="o2">When this method returns, contains the previous edge of the same triangle.</param>
        /// <remarks>
        /// This operation rotates clockwise around the triangle to the previous edge.
        /// </remarks>
        public void Lprev(ref Otri o2)
        {
            o2.triangle = this.triangle;
            o2.orient = Otri.minus1Mod3[this.orient];
        }

        /// <summary>
        /// Transforms this triangle to the previous edge (in clockwise order) of the same triangle.
        /// </summary>
        /// <remarks>
        /// This method modifies the current triangle to rotate clockwise to the previous edge.
        /// </remarks>
        public void LprevSelf() => this.orient = Otri.minus1Mod3[this.orient];

        /// <summary>
        /// Sets o2 to the next triangle counterclockwise around the origin of this triangle.
        /// </summary>
        /// <param name="o2">When this method returns, contains the next triangle around the origin.</param>
        /// <remarks>
        /// This operation finds the triangle that shares the origin vertex with this triangle
        /// and is the next one counterclockwise around that vertex.
        /// </remarks>
        public void Onext(ref Otri o2)
        {
            o2.triangle = this.triangle;
            o2.orient = Otri.minus1Mod3[this.orient];
            int orient = o2.orient;
            o2.orient = o2.triangle.neighbors[orient].orient;
            o2.triangle = o2.triangle.neighbors[orient].triangle;
        }

        public void OnextSelf()
        {
            this.orient = Otri.minus1Mod3[this.orient];
            int orient = this.orient;
            this.orient = this.triangle.neighbors[orient].orient;
            this.triangle = this.triangle.neighbors[orient].triangle;
        }

        public void Oprev(ref Otri o2)
        {
            o2.triangle = this.triangle.neighbors[this.orient].triangle;
            o2.orient = this.triangle.neighbors[this.orient].orient;
            o2.orient = Otri.plus1Mod3[o2.orient];
        }

        public void OprevSelf()
        {
            int orient = this.orient;
            this.orient = this.triangle.neighbors[orient].orient;
            this.triangle = this.triangle.neighbors[orient].triangle;
            this.orient = Otri.plus1Mod3[this.orient];
        }

        public void Dnext(ref Otri o2)
        {
            o2.triangle = this.triangle.neighbors[this.orient].triangle;
            o2.orient = this.triangle.neighbors[this.orient].orient;
            o2.orient = Otri.minus1Mod3[o2.orient];
        }

        public void DnextSelf()
        {
            int orient = this.orient;
            this.orient = this.triangle.neighbors[orient].orient;
            this.triangle = this.triangle.neighbors[orient].triangle;
            this.orient = Otri.minus1Mod3[this.orient];
        }

        public void Dprev(ref Otri o2)
        {
            o2.triangle = this.triangle;
            o2.orient = Otri.plus1Mod3[this.orient];
            int orient = o2.orient;
            o2.orient = o2.triangle.neighbors[orient].orient;
            o2.triangle = o2.triangle.neighbors[orient].triangle;
        }

        public void DprevSelf()
        {
            this.orient = Otri.plus1Mod3[this.orient];
            int orient = this.orient;
            this.orient = this.triangle.neighbors[orient].orient;
            this.triangle = this.triangle.neighbors[orient].triangle;
        }

        public void Rnext(ref Otri o2)
        {
            o2.triangle = this.triangle.neighbors[this.orient].triangle;
            o2.orient = this.triangle.neighbors[this.orient].orient;
            o2.orient = Otri.plus1Mod3[o2.orient];
            int orient = o2.orient;
            o2.orient = o2.triangle.neighbors[orient].orient;
            o2.triangle = o2.triangle.neighbors[orient].triangle;
        }

        public void RnextSelf()
        {
            int orient1 = this.orient;
            this.orient = this.triangle.neighbors[orient1].orient;
            this.triangle = this.triangle.neighbors[orient1].triangle;
            this.orient = Otri.plus1Mod3[this.orient];
            int orient2 = this.orient;
            this.orient = this.triangle.neighbors[orient2].orient;
            this.triangle = this.triangle.neighbors[orient2].triangle;
        }

        public void Rprev(ref Otri o2)
        {
            o2.triangle = this.triangle.neighbors[this.orient].triangle;
            o2.orient = this.triangle.neighbors[this.orient].orient;
            o2.orient = Otri.minus1Mod3[o2.orient];
            int orient = o2.orient;
            o2.orient = o2.triangle.neighbors[orient].orient;
            o2.triangle = o2.triangle.neighbors[orient].triangle;
        }

        public void RprevSelf()
        {
            int orient1 = this.orient;
            this.orient = this.triangle.neighbors[orient1].orient;
            this.triangle = this.triangle.neighbors[orient1].triangle;
            this.orient = Otri.minus1Mod3[this.orient];
            int orient2 = this.orient;
            this.orient = this.triangle.neighbors[orient2].orient;
            this.triangle = this.triangle.neighbors[orient2].triangle;
        }

        /// <summary>
        /// Gets the origin vertex of the oriented edge.
        /// </summary>
        /// <returns>The origin vertex of the current edge.</returns>
        /// <remarks>
        /// The origin vertex is the starting point of the current edge when traversing counterclockwise.
        /// </remarks>
        public Vertex Org() => this.triangle.vertices[Otri.plus1Mod3[this.orient]];

        /// <summary>
        /// Gets the destination vertex of the oriented edge.
        /// </summary>
        /// <returns>The destination vertex of the current edge.</returns>
        /// <remarks>
        /// The destination vertex is the ending point of the current edge when traversing counterclockwise.
        /// </remarks>
        public Vertex Dest() => this.triangle.vertices[Otri.minus1Mod3[this.orient]];

        /// <summary>
        /// Gets the apex vertex of the oriented triangle.
        /// </summary>
        /// <returns>The vertex opposite to the current edge.</returns>
        /// <remarks>
        /// The apex vertex is the vertex that is opposite to the current edge.
        /// </remarks>
        public Vertex Apex() => this.triangle.vertices[this.orient];

        /// <summary>
        /// Sets the origin vertex of the oriented edge.
        /// </summary>
        /// <param name="ptr">The vertex to set as the origin.</param>
        public void SetOrg(Vertex ptr) => this.triangle.vertices[Otri.plus1Mod3[this.orient]] = ptr;

        /// <summary>
        /// Sets the destination vertex of the oriented edge.
        /// </summary>
        /// <param name="ptr">The vertex to set as the destination.</param>
        public void SetDest(Vertex ptr) => this.triangle.vertices[Otri.minus1Mod3[this.orient]] = ptr;

        /// <summary>
        /// Sets the apex vertex of the oriented triangle.
        /// </summary>
        /// <param name="ptr">The vertex to set as the apex.</param>
        public void SetApex(Vertex ptr) => this.triangle.vertices[this.orient] = ptr;

        /// <summary>
        /// Creates a bond between this triangle and another triangle.
        /// </summary>
        /// <param name="o2">The triangle to bond with.</param>
        /// <remarks>
        /// This operation creates a two-way connection between the current edge of this triangle
        /// and the current edge of the other triangle, establishing them as neighbors.
        /// </remarks>
        public void Bond(ref Otri o2)
        {
            this.triangle.neighbors[this.orient].triangle = o2.triangle;
            this.triangle.neighbors[this.orient].orient = o2.orient;
            o2.triangle.neighbors[o2.orient].triangle = this.triangle;
            o2.triangle.neighbors[o2.orient].orient = this.orient;
        }

        /// <summary>
        /// Dissolves the bond between this triangle and its neighbor across the current edge.
        /// </summary>
        /// <remarks>
        /// This operation breaks the connection between the current edge of this triangle
        /// and any neighboring triangle, setting the neighbor to a dummy triangle.
        /// </remarks>
        public void Dissolve()
        {
            this.triangle.neighbors[this.orient].triangle = TriangularMesh.dummytri;
            this.triangle.neighbors[this.orient].orient = 0;
        }

        /// <summary>
        /// Copies this oriented triangle to another oriented triangle.
        /// </summary>
        /// <param name="o2">The oriented triangle to copy to.</param>
        /// <remarks>
        /// This operation makes o2 a copy of the current oriented triangle,
        /// with the same triangle and orientation.
        /// </remarks>
        public void Copy(ref Otri o2)
        {
            o2.triangle = this.triangle;
            o2.orient = this.orient;
        }

        /// <summary>
        /// Determines whether this oriented triangle is equal to another oriented triangle.
        /// </summary>
        /// <param name="o2">The oriented triangle to compare with.</param>
        /// <returns>True if both oriented triangles refer to the same triangle with the same orientation; otherwise, false.</returns>
        public bool Equal(Otri o2) => this.triangle == o2.triangle && this.orient == o2.orient;

        /// <summary>
        /// Marks this triangle as infected.
        /// </summary>
        /// <remarks>
        /// Infection is used in various algorithms to mark triangles for processing.
        /// </remarks>
        public void Infect() => this.triangle.infected = true;

        /// <summary>
        /// Marks this triangle as uninfected.
        /// </summary>
        /// <remarks>
        /// Clears the infection status of the triangle.
        /// </remarks>
        public void Uninfect() => this.triangle.infected = false;

        /// <summary>
        /// Determines whether this triangle is infected.
        /// </summary>
        /// <returns>True if the triangle is infected; otherwise, false.</returns>
        public bool IsInfected() => this.triangle.infected;

        /// <summary>
        /// Determines whether a triangle is dead (deallocated).
        /// </summary>
        /// <param name="tria">The triangle to check.</param>
        /// <returns>True if the triangle is dead; otherwise, false.</returns>
        /// <remarks>
        /// A triangle is considered dead if its first neighbor reference is null.
        /// </remarks>
        public static bool IsDead(Triangle tria) => tria.neighbors[0].triangle == null;

        /// <summary>
        /// Kills (deallocates) a triangle.
        /// </summary>
        /// <param name="tria">The triangle to kill.</param>
        /// <remarks>
        /// This operation marks a triangle as dead by setting its first and third neighbor references to null.
        /// </remarks>
        public static void Kill(Triangle tria)
        {
            tria.neighbors[0].triangle = (Triangle) null;
            tria.neighbors[2].triangle = (Triangle) null;
        }

        /// <summary>
        /// Gets the subsegment associated with the current edge of this triangle.
        /// </summary>
        /// <param name="os">When this method returns, contains the subsegment associated with the current edge.</param>
        /// <remarks>
        /// This operation retrieves the subsegment (if any) that lies on the current edge of the triangle.
        /// </remarks>
        public void SegPivot(ref Osub os) => os = this.triangle.subsegs[this.orient];

        /// <summary>
        /// Creates a bond between this triangle and a subsegment.
        /// </summary>
        /// <param name="os">The subsegment to bond with.</param>
        /// <remarks>
        /// This operation creates a two-way connection between the current edge of this triangle
        /// and the specified subsegment, establishing them as associated with each other.
        /// </remarks>
        public void SegBond(ref Osub os)
        {
            this.triangle.subsegs[this.orient] = os;
            os.seg.triangles[os.orient] = this;
        }

        /// <summary>
        /// Dissolves the bond between this triangle and any subsegment on the current edge.
        /// </summary>
        /// <remarks>
        /// This operation breaks the connection between the current edge of this triangle
        /// and any associated subsegment, setting the subsegment to a dummy subsegment.
        /// </remarks>
        public void SegDissolve() => this.triangle.subsegs[this.orient].seg = TriangularMesh.dummysub;
    }
}