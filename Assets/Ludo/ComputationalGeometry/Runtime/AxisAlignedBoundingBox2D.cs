using System;

namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Represents an axis-aligned bounding box in 2D space.
    /// </summary>
    [System.Serializable]
    public class AxisAlignedBoundingBox2D
    {
        private double xmin;
        private double ymin;
        private double xmax;
        private double ymax;

        /// <summary>
        /// Initializes a new instance of the <see cref="AxisAlignedBoundingBox2D"/> class with default values.
        /// The box is initialized with minimum values set to positive infinity and maximum values set to negative infinity.
        /// </summary>
        public AxisAlignedBoundingBox2D()
        {
            this.xmin = double.MaxValue;
            this.ymin = double.MaxValue;
            this.xmax = double.MinValue;
            this.ymax = double.MinValue;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AxisAlignedBoundingBox2D"/> class with specified minimum and maximum coordinates.
        /// </summary>
        /// <param name="xmin">The minimum x-coordinate.</param>
        /// <param name="ymin">The minimum y-coordinate.</param>
        /// <param name="xmax">The maximum x-coordinate.</param>
        /// <param name="ymax">The maximum y-coordinate.</param>
        public AxisAlignedBoundingBox2D(double xmin, double ymin, double xmax, double ymax)
        {
            this.xmin = xmin;
            this.ymin = ymin;
            this.xmax = xmax;
            this.ymax = ymax;
        }

        /// <summary>
        /// Gets the minimum x-coordinate of the bounding box.
        /// </summary>
        public double Xmin => this.xmin;

        /// <summary>
        /// Gets the minimum y-coordinate of the bounding box.
        /// </summary>
        public double Ymin => this.ymin;

        /// <summary>
        /// Gets the maximum x-coordinate of the bounding box.
        /// </summary>
        public double Xmax => this.xmax;

        /// <summary>
        /// Gets the maximum y-coordinate of the bounding box.
        /// </summary>
        public double Ymax => this.ymax;

        /// <summary>
        /// Gets the width of the bounding box.
        /// </summary>
        public double Width => this.xmax - this.xmin;

        /// <summary>
        /// Gets the height of the bounding box.
        /// </summary>
        public double Height => this.ymax - this.ymin;

        /// <summary>
        /// Updates the bounding box to include the specified point.
        /// </summary>
        /// <param name="x">The x-coordinate of the point.</param>
        /// <param name="y">The y-coordinate of the point.</param>
        public void Update(double x, double y)
        {
            this.xmin = Math.Min(this.xmin, x);
            this.ymin = Math.Min(this.ymin, y);
            this.xmax = Math.Max(this.xmax, x);
            this.ymax = Math.Max(this.ymax, y);
        }

        /// <summary>
        /// Scales the bounding box by the specified amounts in the x and y directions.
        /// </summary>
        /// <param name="dx">The amount to scale in the x direction.</param>
        /// <param name="dy">The amount to scale in the y direction.</param>
        public void Scale(double dx, double dy)
        {
            this.xmin -= dx;
            this.xmax += dx;
            this.ymin -= dy;
            this.ymax += dy;
        }

        /// <summary>
        /// Determines whether the bounding box contains the specified point.
        /// </summary>
        /// <param name="pt">The point to check.</param>
        /// <returns>True if the bounding box contains the point; otherwise, false.</returns>
        public bool Contains(Point pt)
        {
            return pt.x >= this.xmin && pt.x <= this.xmax && pt.y >= this.ymin && pt.y <= this.ymax;
        }
    }
}