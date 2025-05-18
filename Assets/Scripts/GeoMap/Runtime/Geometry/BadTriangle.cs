namespace GeoMap.Geometry
{
    public class BadTriangle
    {
        public static int OTID;
        public int ID;
        public Otri poortri;
        public double key;
        public Vertex triangorg;
        public Vertex triangdest;
        public Vertex triangapex;
        public BadTriangle nexttriang;

        public BadTriangle() => this.ID = BadTriangle.OTID++;

        public override string ToString() => $"B-TID {this.poortri.triangle.hash}";
    }
}