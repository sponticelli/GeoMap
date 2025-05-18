using System;
using System.Collections.Generic;

namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Represents the input geometry for mesh generation, containing points, segments, holes, and regions.
    /// </summary>
    [Serializable]
    public class MeshInputData
    {
        public List<Vertex> points;
        public List<MeshEdge> segments;
        public List<Point> holes;
        public List<RegionPointer> regions;
        private AxisAlignedBoundingBox2D _bounds;
        private int _pointAttributes = -1;

        /// <summary>
        /// Initializes a new instance of the <see cref="MeshInputData"/> class with default capacity.
        /// </summary>
        public MeshInputData()
            : this(3)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MeshInputData"/> class with specified capacity.
        /// </summary>
        /// <param name="capacity">The initial capacity for the points collection.</param>
        public MeshInputData(int capacity)
        {
            points = new List<Vertex>(capacity);
            segments = new List<MeshEdge>();
            holes = new List<Point>();
            regions = new List<RegionPointer>();
            _bounds = new AxisAlignedBoundingBox2D();
            _pointAttributes = -1;
        }

        /// <summary>
        /// Gets the bounding box of the input geometry.
        /// </summary>
        public AxisAlignedBoundingBox2D Bounds => _bounds;

        /// <summary>
        /// Gets a value indicating whether the input geometry has any segments.
        /// </summary>
        public bool HasSegments => segments.Count > 0;

        /// <summary>
        /// Gets the number of points in the input geometry.
        /// </summary>
        public int Count => points.Count;

        /// <summary>
        /// Gets the collection of points in the input geometry.
        /// </summary>
        public IEnumerable<Point> Points => points;

        /// <summary>
        /// Gets the collection of segments in the input geometry.
        /// </summary>
        public ICollection<MeshEdge> Segments => segments;

        /// <summary>
        /// Gets the collection of holes in the input geometry.
        /// </summary>
        public ICollection<Point> Holes => holes;

        /// <summary>
        /// Gets the collection of region pointers in the input geometry.
        /// </summary>
        public ICollection<RegionPointer> Regions => regions;

        /// <summary>
        /// Clears all points, segments, holes, and regions from the input geometry.
        /// </summary>
        public void Clear()
        {
            points.Clear();
            segments.Clear();
            holes.Clear();
            regions.Clear();
            _pointAttributes = -1;
        }

        /// <summary>
        /// Adds a point with the specified coordinates and default boundary mark 0.
        /// </summary>
        /// <param name="x">The x-coordinate of the point.</param>
        /// <param name="y">The y-coordinate of the point.</param>
        public void AddPoint(double x, double y) => AddPoint(x, y, 0);

        /// <summary>
        /// Adds a point with the specified coordinates and boundary mark.
        /// </summary>
        /// <param name="x">The x-coordinate of the point.</param>
        /// <param name="y">The y-coordinate of the point.</param>
        /// <param name="boundary">The boundary mark of the point.</param>
        public void AddPoint(double x, double y, int boundary)
        {
            points.Add(new Vertex(x, y, boundary));
            _bounds.Update(x, y);
        }

        /// <summary>
        /// Adds a point with the specified coordinates, boundary mark, and a single attribute.
        /// </summary>
        /// <param name="x">The x-coordinate of the point.</param>
        /// <param name="y">The y-coordinate of the point.</param>
        /// <param name="boundary">The boundary mark of the point.</param>
        /// <param name="attribute">The attribute value to associate with the point.</param>
        public void AddPoint(double x, double y, int boundary, double attribute)
        {
            AddPoint(x, y, 0, new double[1] { attribute });
        }

        /// <summary>
        /// Adds a point with the specified coordinates, boundary mark, and attributes.
        /// </summary>
        /// <param name="x">The x-coordinate of the point.</param>
        /// <param name="y">The y-coordinate of the point.</param>
        /// <param name="boundary">The boundary mark of the point.</param>
        /// <param name="attribs">The array of attribute values to associate with the point.</param>
        /// <exception cref="ArgumentException">Thrown when the attributes are inconsistent with previously added points.</exception>
        public void AddPoint(double x, double y, int boundary, double[] attribs)
        {
            if (_pointAttributes < 0)
            {
                _pointAttributes = attribs == null ? 0 : attribs.Length;
            }
            else
            {
                if (attribs == null && _pointAttributes > 0)
                    throw new ArgumentException("Inconsitent use of point attributes.");
                if (attribs != null && _pointAttributes != attribs.Length)
                    throw new ArgumentException("Inconsitent use of point attributes.");
            }

            List<Vertex> points = this.points;
            Vertex vertex = new Vertex(x, y, boundary);
            vertex.attributes = attribs;
            points.Add(vertex);
            _bounds.Update(x, y);
        }

        /// <summary>
        /// Adds a hole at the specified coordinates.
        /// </summary>
        /// <param name="x">The x-coordinate of the hole.</param>
        /// <param name="y">The y-coordinate of the hole.</param>
        public void AddHole(double x, double y) => holes.Add(new Point(x, y));

        /// <summary>
        /// Adds a region pointer at the specified coordinates with the given identifier.
        /// </summary>
        /// <param name="x">The x-coordinate of the region pointer.</param>
        /// <param name="y">The y-coordinate of the region pointer.</param>
        /// <param name="id">The identifier of the region.</param>
        public void AddRegion(double x, double y, int id)
        {
            regions.Add(new RegionPointer(x, y, id));
        }

        /// <summary>
        /// Adds a segment connecting the points with the specified indices and default boundary mark 0.
        /// </summary>
        /// <param name="p0">The index of the first endpoint.</param>
        /// <param name="p1">The index of the second endpoint.</param>
        public void AddSegment(int p0, int p1) => AddSegment(p0, p1, 0);

        /// <summary>
        /// Adds a segment connecting the points with the specified indices and boundary mark.
        /// </summary>
        /// <param name="p0">The index of the first endpoint.</param>
        /// <param name="p1">The index of the second endpoint.</param>
        /// <param name="boundary">The boundary mark of the segment.</param>
        /// <exception cref="NotSupportedException">Thrown when the endpoints are invalid.</exception>
        public void AddSegment(int p0, int p1, int boundary)
        {
            if (p0 == p1 || p0 < 0 || p1 < 0)
                throw new NotSupportedException("Invalid endpoints.");
            segments.Add(new MeshEdge(p0, p1, boundary));
        }
    }
}