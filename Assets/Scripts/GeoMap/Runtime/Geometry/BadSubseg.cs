namespace GeoMap.Geometry
{
    public class BadSubseg
    {
        private static int hashSeed;
        internal int Hash;
        public Osub encsubseg;
        public Vertex subsegorg;
        public Vertex subsegdest;

        public BadSubseg() => this.Hash = BadSubseg.hashSeed++;

        public override int GetHashCode() => this.Hash;

        public override string ToString() => $"B-SID {this.encsubseg.seg.hash}";
    }
}