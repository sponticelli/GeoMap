using System;

namespace GeoMap.Geometry
{
    public class Vertex : Point
    {
        internal int hash;
        internal VertexType type;
        internal Otri tri;

        public Vertex()
            : this(0.0, 0.0, 0, 0)
        {
        }

        public Vertex(double x, double y)
            : this(x, y, 0, 0)
        {
        }

        public Vertex(double x, double y, int mark)
            : this(x, y, mark, 0)
        {
        }

        public Vertex(double x, double y, int mark, int attribs)
            : base(x, y, mark)
        {
            this.type = VertexType.InputVertex;
            if (attribs <= 0)
                return;
            this.attributes = new double[attribs];
        }

        public VertexType Type => this.type;

        public double this[int i]
        {
            get
            {
                if (i == 0)
                    return this.x;
                if (i == 1)
                    return this.y;
                throw new ArgumentOutOfRangeException("Index must be 0 or 1.");
            }
        }

        public override int GetHashCode() => this.hash;
    }
}