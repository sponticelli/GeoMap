using System;

namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Represents an axis-aligned bounding box in 2D space.
    /// </summary>
    [Serializable]
    public class AxisAlignedBoundingBox2D
    {
        private double _xmin;
        private double _ymin;
        private double _xmax;
        private double _ymax;

        /// <summary>
        /// Initializes a new instance of the <see cref="AxisAlignedBoundingBox2D"/> class with default values.
        /// The box is initialized with minimum values set to positive infinity and maximum values set to negative infinity.
        /// </summary>
        public AxisAlignedBoundingBox2D()
        {
            _xmin = double.MaxValue;
            _ymin = double.MaxValue;
            _xmax = double.MinValue;
            _ymax = double.MinValue;
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
            _xmin = xmin;
            _ymin = ymin;
            _xmax = xmax;
            _ymax = ymax;
        }

        /// <summary>
        /// Gets the minimum x-coordinate of the bounding box.
        /// </summary>
        public double Xmin => _xmin;

        /// <summary>
        /// Gets the minimum y-coordinate of the bounding box.
        /// </summary>
        public double Ymin => _ymin;

        /// <summary>
        /// Gets the maximum x-coordinate of the bounding box.
        /// </summary>
        public double Xmax => _xmax;

        /// <summary>
        /// Gets the maximum y-coordinate of the bounding box.
        /// </summary>
        public double Ymax => _ymax;

        /// <summary>
        /// Gets the width of the bounding box.
        /// </summary>
        public double Width => _xmax - _xmin;

        /// <summary>
        /// Gets the height of the bounding box.
        /// </summary>
        public double Height => _ymax - _ymin;

        /// <summary>
        /// Updates the bounding box to include the specified point.
        /// </summary>
        /// <param name="x">The x-coordinate of the point.</param>
        /// <param name="y">The y-coordinate of the point.</param>
        public void Update(double x, double y)
        {
            _xmin = Math.Min(_xmin, x);
            _ymin = Math.Min(_ymin, y);
            _xmax = Math.Max(_xmax, x);
            _ymax = Math.Max(_ymax, y);
        }

        /// <summary>
        /// Scales the bounding box by the specified amounts in the x and y directions.
        /// </summary>
        /// <param name="dx">The amount to scale in the x direction.</param>
        /// <param name="dy">The amount to scale in the y direction.</param>
        public void Scale(double dx, double dy)
        {
            _xmin -= dx;
            _xmax += dx;
            _ymin -= dy;
            _ymax += dy;
        }

        /// <summary>
        /// Determines whether the bounding box contains the specified point.
        /// </summary>
        /// <param name="pt">The point to check.</param>
        /// <returns>True if the bounding box contains the point; otherwise, false.</returns>
        public bool Contains(Point pt)
        {
            return pt.x >= _xmin && pt.x <= _xmax && pt.y >= _ymin && pt.y <= _ymax;
        }
    }
}