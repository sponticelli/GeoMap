using System;

namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Represents a 2D point with x and y coordinates.
    /// </summary>
    [System.Serializable]
    public class Point : IComparable<Point>, IEquatable<Point>
    {
        internal int id;
        internal double x;
        internal double y;
        internal int mark;
        internal double[] attributes;

        /// <summary>
        /// Initializes a new instance of the <see cref="Point"/> class with default coordinates (0,0) and boundary mark 0.
        /// </summary>
        public Point()
            : this(0.0, 0.0, 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Point"/> class with specified coordinates and default boundary mark 0.
        /// </summary>
        /// <param name="x">The x-coordinate.</param>
        /// <param name="y">The y-coordinate.</param>
        public Point(double x, double y)
            : this(x, y, 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Point"/> class with specified coordinates and boundary mark.
        /// </summary>
        /// <param name="x">The x-coordinate.</param>
        /// <param name="y">The y-coordinate.</param>
        /// <param name="mark">The boundary mark.</param>
        public Point(double x, double y, int mark)
        {
            this.x = x;
            this.y = y;
            this.mark = mark;
        }

        /// <summary>
        /// Gets the unique identifier of the point.
        /// </summary>
        public int ID => this.id;

        /// <summary>
        /// Gets the x-coordinate of the point.
        /// </summary>
        public double X => this.x;

        /// <summary>
        /// Gets the y-coordinate of the point.
        /// </summary>
        public double Y => this.y;

        /// <summary>
        /// Gets the boundary mark of the point.
        /// </summary>
        public int Boundary => this.mark;

        /// <summary>
        /// Gets the additional attributes associated with the point.
        /// </summary>
        public double[] Attributes => this.attributes;

        /// <summary>
        /// Determines whether two points are equal.
        /// </summary>
        /// <param name="a">The first point to compare.</param>
        /// <param name="b">The second point to compare.</param>
        /// <returns>True if the points are equal; otherwise, false.</returns>
        public static bool operator ==(Point a, Point b)
        {
            if ((object) a == (object) b)
                return true;
            return (object) a != null && (object) b != null && a.Equals(b);
        }

        /// <summary>
        /// Determines whether two points are not equal.
        /// </summary>
        /// <param name="a">The first point to compare.</param>
        /// <param name="b">The second point to compare.</param>
        /// <returns>True if the points are not equal; otherwise, false.</returns>
        public static bool operator !=(Point a, Point b) => !(a == b);

        /// <summary>
        /// Determines whether the specified object is equal to the current point.
        /// </summary>
        /// <param name="obj">The object to compare with the current point.</param>
        /// <returns>True if the specified object is equal to the current point; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            Point point = obj as Point;
            return (object) point != null && this.x == point.x && this.y == point.y;
        }

        /// <summary>
        /// Determines whether the specified point is equal to the current point.
        /// </summary>
        /// <param name="p">The point to compare with the current point.</param>
        /// <returns>True if the specified point is equal to the current point; otherwise, false.</returns>
        public bool Equals(Point p) => (object) p != null && this.x == p.x && this.y == p.y;

        /// <summary>
        /// Compares the current point with another point.
        /// </summary>
        /// <param name="other">The point to compare with the current point.</param>
        /// <returns>
        /// A value that indicates the relative order of the points being compared.
        /// Less than zero: This point is less than the other point.
        /// Zero: This point is equal to the other point.
        /// Greater than zero: This point is greater than the other point.
        /// </returns>
        public int CompareTo(Point other)
        {
            if (this.x == other.x && this.y == other.y)
                return 0;
            return this.x >= other.x && (this.x != other.x || this.y >= other.y) ? 1 : -1;
        }

        /// <summary>
        /// Returns a hash code for the current point.
        /// </summary>
        /// <returns>A hash code for the current point.</returns>
        public override int GetHashCode() => this.x.GetHashCode() ^ this.y.GetHashCode();

        /// <summary>
        /// Returns a string representation of the current point.
        /// </summary>
        /// <returns>A string representation of the current point in the format [x,y].</returns>
        public override string ToString() => $"[{this.x},{this.y}]";
    }
}