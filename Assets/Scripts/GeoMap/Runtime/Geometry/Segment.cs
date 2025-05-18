namespace GeoMap.Geometry
{
    public class Segment : ISegment
    {
        internal int hash;
        internal Osub[] subsegs;
        internal Vertex[] vertices;
        internal Otri[] triangles;
        internal int boundary;

        public Segment()
        {
            this.subsegs = new Osub[2];
            this.subsegs[0].seg = Mesh.dummysub;
            this.subsegs[1].seg = Mesh.dummysub;
            this.vertices = new Vertex[4];
            this.triangles = new Otri[2];
            this.triangles[0].triangle = Mesh.dummytri;
            this.triangles[1].triangle = Mesh.dummytri;
            this.boundary = 0;
        }

        public int P0 => this.vertices[0].id;

        public int P1 => this.vertices[1].id;

        public int Boundary => this.boundary;

        public Vertex GetVertex(int index) => this.vertices[index];

        public ITriangle GetTriangle(int index)
        {
            return this.triangles[index].triangle != Mesh.dummytri ? (ITriangle) this.triangles[index].triangle : (ITriangle) null;
        }

        public override int GetHashCode() => this.hash;

        public override string ToString() => $"SID {this.hash}";
    }
}