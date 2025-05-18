using System.Collections.Generic;

namespace GeoMap.Geometry
{
    public class VoronoiRegion
    {
        private int id;
        private Point generator;
        private List<Point> vertices;
        private bool bounded;

        public int ID => this.id;

        public Point Generator => this.generator;

        public ICollection<Point> Vertices => (ICollection<Point>) this.vertices;

        public bool Bounded
        {
            get => this.bounded;
            set => this.bounded = value;
        }

        public VoronoiRegion(Vertex generator)
        {
            this.id = generator.id;
            this.generator = (Point) generator;
            this.vertices = new List<Point>();
            this.bounded = true;
        }

        public void Add(Point point) => this.vertices.Add(point);

        public void Add(List<Point> points) => this.vertices.AddRange((IEnumerable<Point>) points);

        public override string ToString() => $"R-ID {this.id}";
    }
}