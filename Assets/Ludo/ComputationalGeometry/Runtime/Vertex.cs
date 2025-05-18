using System;

namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Represents a vertex in a geometric mesh, extending the Point class with additional properties.
    /// </summary>
    [Serializable]
    public class Vertex : Point
    {
        internal int hash;
        internal VertexType type;
        internal OrientedTriangle tri;

        /// <summary>
        /// Initializes a new instance of the <see cref="Vertex"/> class with default coordinates (0,0), boundary mark 0, and no attributes.
        /// </summary>
        public Vertex()
            : this(0.0, 0.0, 0, 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Vertex"/> class with specified coordinates, default boundary mark 0, and no attributes.
        /// </summary>
        /// <param name="x">The x-coordinate.</param>
        /// <param name="y">The y-coordinate.</param>
        public Vertex(double x, double y)
            : this(x, y, 0, 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Vertex"/> class with specified coordinates, boundary mark, and no attributes.
        /// </summary>
        /// <param name="x">The x-coordinate.</param>
        /// <param name="y">The y-coordinate.</param>
        /// <param name="mark">The boundary mark.</param>
        public Vertex(double x, double y, int mark)
            : this(x, y, mark, 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Vertex"/> class with specified coordinates, boundary mark, and attribute count.
        /// </summary>
        /// <param name="x">The x-coordinate.</param>
        /// <param name="y">The y-coordinate.</param>
        /// <param name="mark">The boundary mark.</param>
        /// <param name="attribs">The number of attributes to allocate for this vertex.</param>
        public Vertex(double x, double y, int mark, int attribs)
            : base(x, y, mark)
        {
            type = VertexType.InputVertex;
            if (attribs <= 0)
                return;
            attributes = new double[attribs];
        }

        /// <summary>
        /// Gets the type of the vertex.
        /// </summary>
        public VertexType Type => type;

        /// <summary>
        /// Gets the coordinate value at the specified index (0 for x, 1 for y).
        /// </summary>
        /// <param name="i">The index of the coordinate (0 for x, 1 for y).</param>
        /// <returns>The coordinate value at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the index is not 0 or 1.</exception>
        public double this[int i]
        {
            get
            {
                if (i == 0)
                    return x;
                if (i == 1)
                    return y;
                throw new ArgumentOutOfRangeException("Index must be 0 or 1.");
            }
        }

        /// <summary>
        /// Returns a hash code for the current vertex.
        /// </summary>
        /// <returns>A hash code for the current vertex.</returns>
        public override int GetHashCode() => hash;
    }
}