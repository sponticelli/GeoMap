namespace GeoMap.Geometry
{
    public class Triangle : ITriangle
    {
        internal int hash;
        internal int id;
        internal Otri[] neighbors;
        internal Vertex[] vertices;
        internal Osub[] subsegs;
        internal int region;
        internal double area;
        internal bool infected;

        public Triangle()
        {
            this.neighbors = new Otri[3];
            this.neighbors[0].triangle = Mesh.dummytri;
            this.neighbors[1].triangle = Mesh.dummytri;
            this.neighbors[2].triangle = Mesh.dummytri;
            this.vertices = new Vertex[3];
            this.subsegs = new Osub[3];
            this.subsegs[0].seg = Mesh.dummysub;
            this.subsegs[1].seg = Mesh.dummysub;
            this.subsegs[2].seg = Mesh.dummysub;
        }

        public int ID => this.id;

        public int P0 => !((Point) this.vertices[0] == (Point) null) ? this.vertices[0].id : -1;

        public int P1 => !((Point) this.vertices[1] == (Point) null) ? this.vertices[1].id : -1;

        public Vertex GetVertex(int index) => this.vertices[index];

        public int P2 => !((Point) this.vertices[2] == (Point) null) ? this.vertices[2].id : -1;

        public bool SupportsNeighbors => true;

        public int N0 => this.neighbors[0].triangle.id;

        public int N1 => this.neighbors[1].triangle.id;

        public int N2 => this.neighbors[2].triangle.id;

        public ITriangle GetNeighbor(int index)
        {
            return this.neighbors[index].triangle != Mesh.dummytri ? (ITriangle) this.neighbors[index].triangle : (ITriangle) null;
        }

        public ISegment GetSegment(int index)
        {
            return this.subsegs[index].seg != Mesh.dummysub ? (ISegment) this.subsegs[index].seg : (ISegment) null;
        }

        public double Area
        {
            get => this.area;
            set => this.area = value;
        }

        public int Region => this.region;

        public override int GetHashCode() => this.hash;

        public override string ToString() => $"TID {this.hash}";
    }
}