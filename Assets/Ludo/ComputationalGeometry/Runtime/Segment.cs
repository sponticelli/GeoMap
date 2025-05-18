using System;

namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Represents a segment in a geometric mesh, implementing the <see cref="ISegment"/> interface.
    /// </summary>
    [Serializable]
    public class Segment : ISegment
    {
        internal int hash;
        internal OrientedSubSegment[] subsegs;
        internal Vertex[] vertices;
        internal OrientedTriangle[] triangles;
        internal int boundary;

        /// <summary>
        /// Initializes a new instance of the <see cref="Segment"/> class.
        /// </summary>
        public Segment()
        {
            subsegs = new OrientedSubSegment[2];
            subsegs[0].seg = TriangularMesh.dummysub;
            subsegs[1].seg = TriangularMesh.dummysub;
            vertices = new Vertex[4];
            triangles = new OrientedTriangle[2];
            triangles[0].triangle = TriangularMesh.dummytri;
            triangles[1].triangle = TriangularMesh.dummytri;
            boundary = 0;
        }

        /// <summary>
        /// Gets the identifier of the first endpoint of the segment.
        /// </summary>
        public int P0 => vertices[0].id;

        /// <summary>
        /// Gets the identifier of the second endpoint of the segment.
        /// </summary>
        public int P1 => vertices[1].id;

        /// <summary>
        /// Gets the boundary mark of the segment.
        /// </summary>
        public int Boundary => boundary;

        /// <summary>
        /// Gets the vertex at the specified index.
        /// </summary>
        /// <param name="index">The index of the vertex.</param>
        /// <returns>The vertex at the specified index.</returns>
        public Vertex GetVertex(int index) => vertices[index];

        /// <summary>
        /// Gets the triangle at the specified index.
        /// </summary>
        /// <param name="index">The index of the triangle.</param>
        /// <returns>The triangle at the specified index, or null if no triangle exists.</returns>
        public ITriangle GetTriangle(int index)
        {
            return triangles[index].triangle != TriangularMesh.dummytri ? triangles[index].triangle : (ITriangle) null;
        }

        /// <summary>
        /// Returns a hash code for the current segment.
        /// </summary>
        /// <returns>A hash code for the current segment.</returns>
        public override int GetHashCode() => hash;

        /// <summary>
        /// Returns a string representation of the current segment.
        /// </summary>
        /// <returns>A string representation of the current segment in the format "SID {hash}".</returns>
        public override string ToString() => $"SID {hash}";
    }
}