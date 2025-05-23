using System;

namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Represents a pointer to a region in a geometric mesh, defined by a point and an identifier.
    /// </summary>
    [Serializable]
    public class RegionPointer
    {
        public Point point;
        public int id;

        /// <summary>
        /// Initializes a new instance of the <see cref="RegionPointer"/> class with specified coordinates and identifier.
        /// </summary>
        /// <param name="x">The x-coordinate of the point.</param>
        /// <param name="y">The y-coordinate of the point.</param>
        /// <param name="id">The identifier of the region.</param>
        public RegionPointer(double x, double y, int id)
        {
            point = new Point(x, y);
            this.id = id;
        }
    }
}