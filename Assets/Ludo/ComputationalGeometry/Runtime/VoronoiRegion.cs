using System;
using System.Collections.Generic;

namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Represents a region in a Voronoi diagram, defined by a generator point and a collection of boundary vertices.
    /// </summary>
    [Serializable]
    public class VoronoiRegion
    {
        private int id;
        private Point generator;
        private List<Point> vertices;
        private bool bounded;

        /// <summary>
        /// Gets the unique identifier of the region.
        /// </summary>
        public int ID => id;

        /// <summary>
        /// Gets the generator point of the region.
        /// This is the point for which all locations in the region are closer to it than to any other generator.
        /// </summary>
        public Point Generator => generator;

        /// <summary>
        /// Gets the collection of vertices that form the boundary of the region.
        /// </summary>
        public ICollection<Point> Vertices => vertices;

        /// <summary>
        /// Gets or sets a value indicating whether the region is bounded.
        /// Unbounded regions extend infinitely in some direction.
        /// </summary>
        public bool Bounded
        {
            get => bounded;
            set => bounded = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VoronoiRegion"/> class with the specified generator vertex.
        /// </summary>
        /// <param name="generator">The generator vertex of the region.</param>
        public VoronoiRegion(Vertex generator)
        {
            id = generator.id;
            this.generator = generator;
            vertices = new List<Point>();
            bounded = true;
        }

        /// <summary>
        /// Adds a vertex to the boundary of the region.
        /// </summary>
        /// <param name="point">The point to add as a vertex.</param>
        public void Add(Point point) => vertices.Add(point);

        /// <summary>
        /// Adds multiple vertices to the boundary of the region.
        /// </summary>
        /// <param name="points">The list of points to add as vertices.</param>
        public void Add(List<Point> points) => vertices.AddRange(points);

        /// <summary>
        /// Returns a string representation of the current region.
        /// </summary>
        /// <returns>A string representation of the current region in the format "R-ID {id}".</returns>
        public override string ToString() => $"R-ID {id}";
    }
}