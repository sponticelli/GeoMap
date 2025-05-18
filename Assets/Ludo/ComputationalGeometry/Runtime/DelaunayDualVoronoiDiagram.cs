using System;
using System.Collections.Generic;

namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Implements a bounded Voronoi diagram generator based on a triangular mesh.
    /// This class creates a Voronoi diagram where regions are bounded by the convex hull of the input points.
    /// </summary>
    [Serializable]
    public class DelaunayDualVoronoiDiagram : IVoronoiDiagram
    {
        private TriangularMesh _triangularMesh;
        private Point[] _points;
        private List<VoronoiRegion> _regions;
        private int _segIndex;
        private Dictionary<int, Segment> _subsegMap;
        private bool _includeBoundary = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="DelaunayDualVoronoiDiagram"/> class with the specified mesh.
        /// Boundary vertices are included in the Voronoi diagram by default.
        /// </summary>
        /// <param name="triangularMesh">The triangular mesh to generate the Voronoi diagram from.</param>
        public DelaunayDualVoronoiDiagram(TriangularMesh triangularMesh)
            : this(triangularMesh, true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DelaunayDualVoronoiDiagram"/> class with the specified mesh and boundary inclusion option.
        /// </summary>
        /// <param name="triangularMesh">The triangular mesh to generate the Voronoi diagram from.</param>
        /// <param name="includeBoundary">If true, includes boundary vertices in the Voronoi diagram; otherwise, excludes them.</param>
        public DelaunayDualVoronoiDiagram(TriangularMesh triangularMesh, bool includeBoundary)
        {
            _triangularMesh = triangularMesh;
            _includeBoundary = includeBoundary;
            Generate();
        }

        /// <summary>
        /// Gets the array of points in the Voronoi diagram.
        /// These typically represent the circumcenters of triangles in the dual Delaunay triangulation.
        /// </summary>
        public Point[] Points => _points;

        /// <summary>
        /// Gets the list of Voronoi regions in the diagram.
        /// Each region corresponds to a generator point and contains the vertices that form the region's boundary.
        /// </summary>
        public List<VoronoiRegion> Regions => _regions;

        /// <summary>
        /// Generates the Voronoi diagram from the mesh.
        /// </summary>
        private void Generate()
        {
            _triangularMesh.Renumber();
            _triangularMesh.MakeVertexMap();
            _points = new Point[_triangularMesh.triangles.Count + _triangularMesh.subsegs.Count * 5];
            _regions = new List<VoronoiRegion>(_triangularMesh.vertices.Count);
            ComputeCircumCenters();
            TagBlindTriangles();
            foreach (Vertex vertex in _triangularMesh.vertices.Values)
            {
                if (vertex.type == VertexType.FreeVertex || vertex.Boundary == 0)
                {
                    ConstructBvdCell(vertex);
                }
                else if (_includeBoundary)
                {
                    ConstructBoundaryBvdCell(vertex);
                }
            }
        }

        /// <summary>
        /// Computes the circumcenters of all triangles in the mesh.
        /// These circumcenters become the vertices of the Voronoi diagram.
        /// </summary>
        private void ComputeCircumCenters()
        {
            OrientedTriangle orientedTriangle = new OrientedTriangle();
            double xi = 0.0;
            double eta = 0.0;
            foreach (Triangle triangle in _triangularMesh.triangles.Values)
            {
                orientedTriangle.triangle = triangle;
                Point circumcenter = Primitives.FindCircumcenter(orientedTriangle.Org(), orientedTriangle.Dest(), orientedTriangle.Apex(), ref xi, ref eta);
                circumcenter.id = triangle.id;
                _points[triangle.id] = circumcenter;
            }
        }

        /// <summary>
        /// Identifies triangles that are "blinded" by segments in the mesh.
        /// A triangle is considered blinded if a segment intersects any line from the triangle's circumcenter to its vertices.
        /// </summary>
        private void TagBlindTriangles()
        {
            int num = 0;
            _subsegMap = new Dictionary<int, Segment>();
            OrientedTriangle orientedTriangle = new OrientedTriangle();
            OrientedTriangle o2 = new OrientedTriangle();
            OrientedSubSegment seg = new OrientedSubSegment();
            OrientedSubSegment os = new OrientedSubSegment();
            foreach (Triangle triangle in _triangularMesh.triangles.Values)
                triangle.infected = false;
            foreach (Segment segment in _triangularMesh.subsegs.Values)
            {
                Stack<Triangle> triangleStack = new Stack<Triangle>();
                seg.seg = segment;
                seg.orient = 0;
                seg.TriPivot(ref orientedTriangle);
                if (orientedTriangle.triangle != TriangularMesh.dummytri && !orientedTriangle.triangle.infected)
                    triangleStack.Push(orientedTriangle.triangle);
                seg.SymSelf();
                seg.TriPivot(ref orientedTriangle);
                if (orientedTriangle.triangle != TriangularMesh.dummytri && !orientedTriangle.triangle.infected)
                {
                    triangleStack.Push(orientedTriangle.triangle);
                }
                while (triangleStack.Count > 0)
                {
                    orientedTriangle.triangle = triangleStack.Pop();
                    orientedTriangle.orient = 0;
                    if (TriangleIsBlinded(ref orientedTriangle, ref seg))
                    {
                        orientedTriangle.triangle.infected = true;
                        ++num;
                        _subsegMap.Add(orientedTriangle.triangle.hash, seg.seg);
                        for (orientedTriangle.orient = 0; orientedTriangle.orient < 3; ++orientedTriangle.orient)
                        {
                            orientedTriangle.Sym(ref o2);
                            o2.SegPivot(ref os);
                            if (o2.triangle != TriangularMesh.dummytri && !o2.triangle.infected &&
                                os.seg == TriangularMesh.dummysub)
                            {
                                triangleStack.Push(o2.triangle);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether a triangle is blinded by a segment.
        /// A triangle is blinded if a segment intersects any line from the triangle's circumcenter to its vertices.
        /// </summary>
        /// <param name="tri">The oriented triangle to check.</param>
        /// <param name="seg">The oriented segment to check against.</param>
        /// <returns>True if the triangle is blinded by the segment; otherwise, false.</returns>
        private bool TriangleIsBlinded(ref OrientedTriangle tri, ref OrientedSubSegment seg)
        {
            Vertex p41 = tri.Org();
            Vertex p42 = tri.Dest();
            Vertex p43 = tri.Apex();
            Vertex p1 = seg.Origin();
            Vertex p2 = seg.Destination();
            Point point = _points[tri.triangle.id];
            Point p;
            return SegmentsIntersect(p1, p2, point, p41, out p, true) || SegmentsIntersect(p1, p2, point, p42, out p, true) || SegmentsIntersect(p1, p2, point, p43, out p, true);
        }

        private void ConstructBvdCell(Vertex vertex)
        {
            VoronoiRegion voronoiRegion = new VoronoiRegion(vertex);
            _regions.Add(voronoiRegion);
            OrientedTriangle o21 = new OrientedTriangle();
            OrientedTriangle o22 = new OrientedTriangle();
            OrientedTriangle o23 = new OrientedTriangle();
            OrientedSubSegment orientedSubSegment = new OrientedSubSegment();
            OrientedSubSegment o24 = new OrientedSubSegment();
            int count = _triangularMesh.triangles.Count;
            List<Point> points = new List<Point>();
            vertex.tri.Copy(ref o22);
            if (o22.Org() != vertex)
            {
                throw new Exception("ConstructBvdCell: inconsistent topology.");
            }
            o22.Copy(ref o21);
            o22.Onext(ref o23);
            do
            {
                Point point1 = _points[o21.triangle.id];
                Point point2 = _points[o23.triangle.id];
                Point p;
                if (!o21.triangle.infected)
                {
                    points.Add(point1);
                    if (o23.triangle.infected)
                    {
                        o24.seg = _subsegMap[o23.triangle.hash];
                        if (SegmentsIntersect(o24.GetSegmentOrigin(), o24.GetSegmentDestination(), point1, point2, out p, true))
                        {
                            p.id = count + _segIndex;
                            _points[count + _segIndex] = p;
                            ++_segIndex;
                            points.Add(p);
                        }
                    }
                }
                else
                {
                    orientedSubSegment.seg = _subsegMap[o21.triangle.hash];
                    if (!o23.triangle.infected)
                    {
                        if (SegmentsIntersect(orientedSubSegment.GetSegmentOrigin(), orientedSubSegment.GetSegmentDestination(), point1, point2, out p, true))
                        {
                            p.id = count + _segIndex;
                            _points[count + _segIndex] = p;
                            ++_segIndex;
                            points.Add(p);
                        }
                    }
                    else
                    {
                        o24.seg = _subsegMap[o23.triangle.hash];
                        if (!orientedSubSegment.Equal(o24))
                        {
                            if (SegmentsIntersect(orientedSubSegment.GetSegmentOrigin(), orientedSubSegment.GetSegmentDestination(), point1, point2, out p, true))
                            {
                                p.id = count + _segIndex;
                                _points[count + _segIndex] = p;
                                ++_segIndex;
                                points.Add(p);
                            }
                            if (SegmentsIntersect(o24.GetSegmentOrigin(), o24.GetSegmentDestination(), point1, point2, out p, true))
                            {
                                p.id = count + _segIndex;
                                _points[count + _segIndex] = p;
                                ++_segIndex;
                                points.Add(p);
                            }
                        }
                    }
                }
                o23.Copy(ref o21);
                o23.OnextSelf();
            }
            while (!o21.Equal(o22));
            voronoiRegion.Add(points);
        }

        private void ConstructBoundaryBvdCell(Vertex vertex)
        {
            VoronoiRegion voronoiRegion = new VoronoiRegion(vertex);
            _regions.Add(voronoiRegion);
            OrientedTriangle o21 = new OrientedTriangle();
            OrientedTriangle o22 = new OrientedTriangle();
            OrientedTriangle o23 = new OrientedTriangle();
            OrientedTriangle o24 = new OrientedTriangle();
            OrientedSubSegment orientedSubSegment = new OrientedSubSegment();
            OrientedSubSegment o25 = new OrientedSubSegment();
            int count = _triangularMesh.triangles.Count;
            List<Point> points = new List<Point>();
            vertex.tri.Copy(ref o22);
            if (o22.Org() != vertex)
                throw new Exception("ConstructBoundaryBvdCell: inconsistent topology.");
            o22.Copy(ref o21);
            o22.Onext(ref o23);
            o22.Oprev(ref o24);
            if (o24.triangle != TriangularMesh.dummytri)
            {
                while (o24.triangle != TriangularMesh.dummytri && !o24.Equal(o22))
                {
                    o24.Copy(ref o21);
                    o24.OprevSelf();
                }
                o21.Copy(ref o22);
                o21.Onext(ref o23);
            }
            if (o24.triangle == TriangularMesh.dummytri)
            {
                Point point = new Point(vertex.x, vertex.y);
                point.id = count + _segIndex;
                _points[count + _segIndex] = point;
                ++_segIndex;
                points.Add(point);
            }
            Vertex vertex1 = o21.Org();
            Vertex vertex2 = o21.Dest();
            Point p = new Point((vertex1.X + vertex2.X) / 2.0, (vertex1.Y + vertex2.Y) / 2.0);
            p.id = count + _segIndex;
            _points[count + _segIndex] = p;
            ++_segIndex;
            points.Add(p);
            do
            {
                Point point1 = _points[o21.triangle.id];
                if (o23.triangle == TriangularMesh.dummytri)
                {
                    if (!o21.triangle.infected)
                    {
                        points.Add(point1);
                    }
                    Vertex vertex3 = o21.Org();
                    Vertex vertex4 = o21.Apex();
                    Point point2 = new Point((vertex3.X + vertex4.X) / 2.0, (vertex3.Y + vertex4.Y) / 2.0);
                    point2.id = count + _segIndex;
                    _points[count + _segIndex] = point2;
                    ++_segIndex;
                    points.Add(point2);
                    break;
                }
                Point point3 = _points[o23.triangle.id];
                if (!o21.triangle.infected)
                {
                    points.Add(point1);
                    if (o23.triangle.infected)
                    {
                        o25.seg = _subsegMap[o23.triangle.hash];
                        if (SegmentsIntersect(o25.GetSegmentOrigin(), o25.GetSegmentDestination(), point1, point3, out p, true))
                        {
                            p.id = count + _segIndex;
                            _points[count + _segIndex] = p;
                            ++_segIndex;
                            points.Add(p);
                        }
                    }
                }
                else
                {
                    orientedSubSegment.seg = _subsegMap[o21.triangle.hash];
                    Vertex p1 = orientedSubSegment.GetSegmentOrigin();
                    Vertex p2 = orientedSubSegment.GetSegmentDestination();
                    if (!o23.triangle.infected)
                    {
                        vertex2 = o21.Dest();
                        Vertex vertex5 = o21.Apex();
                        Point p3 = new Point((vertex2.X + vertex5.X) / 2.0, (vertex2.Y + vertex5.Y) / 2.0);
                        if (SegmentsIntersect(p1, p2, p3, point1, out p, false))
                        {
                            p.id = count + _segIndex;
                            _points[count + _segIndex] = p;
                            ++_segIndex;
                            points.Add(p);
                        }
                        if (SegmentsIntersect(p1, p2, point1, point3, out p, true))
                        {
                            p.id = count + _segIndex;
                            _points[count + _segIndex] = p;
                            ++_segIndex;
                            points.Add(p);
                        }
                    }
                    else
                    {
                        o25.seg = _subsegMap[o23.triangle.hash];
                        if (!orientedSubSegment.Equal(o25))
                        {
                            if (SegmentsIntersect(p1, p2, point1, point3, out p, true))
                            {
                                p.id = count + _segIndex;
                                _points[count + _segIndex] = p;
                                ++_segIndex;
                                points.Add(p);
                            }
                            if (SegmentsIntersect(o25.GetSegmentOrigin(), o25.GetSegmentDestination(), point1, point3, out p, true))
                            {
                                p.id = count + _segIndex;
                                _points[count + _segIndex] = p;
                                ++_segIndex;
                                points.Add(p);
                            }
                        }
                        else
                        {
                            Point p3 = new Point((vertex1.X + vertex2.X) / 2.0, (vertex1.Y + vertex2.Y) / 2.0);
                            if (SegmentsIntersect(p1, p2, p3, point3, out p, false))
                            {
                                p.id = count + _segIndex;
                                _points[count + _segIndex] = p;
                                ++_segIndex;
                                points.Add(p);
                            }
                        }
                    }
                }
                o23.Copy(ref o21);
                o23.OnextSelf();
            }
            while (!o21.Equal(o22));
            voronoiRegion.Add(points);
        }

        /// <summary>
        /// Determines whether two line segments intersect and calculates the intersection point if they do.
        /// </summary>
        /// <param name="p1">The first endpoint of the first segment.</param>
        /// <param name="p2">The second endpoint of the first segment.</param>
        /// <param name="p3">The first endpoint of the second segment.</param>
        /// <param name="p4">The second endpoint of the second segment.</param>
        /// <param name="p">When this method returns, contains the intersection point if the segments intersect; otherwise, null.</param>
        /// <param name="strictIntersect">If true, only considers proper intersections (not endpoint touches); otherwise, includes endpoint touches.</param>
        /// <returns>True if the segments intersect; otherwise, false.</returns>
        private bool SegmentsIntersect(
            Point p1,
            Point p2,
            Point p3,
            Point p4,
            out Point p,
            bool strictIntersect)
        {
            p = null;
            double x1 = p1.X;
            double y1 = p1.Y;
            double x2 = p2.X;
            double y2 = p2.Y;
            double x3 = p3.X;
            double y3 = p3.Y;
            double x4 = p4.X;
            double y4 = p4.Y;

            // Check for degenerate cases or coincident endpoints
            if (x1 == x2 && y1 == y2 || x3 == x4 && y3 == y4 || x1 == x3 && y1 == y3 || x2 == x3 && y2 == y3 || x1 == x4 && y1 == y4 || x2 == x4 && y2 == y4)
                return false;

            // Transform to a coordinate system where the first segment is along the x-axis
            double num1 = x2 - x1;
            double num2 = y2 - y1;
            double num3 = x3 - x1;
            double num4 = y3 - y1;
            double num5 = x4 - x1;
            double num6 = y4 - y1;
            double num7 = Math.Sqrt(num1 * num1 + num2 * num2);
            double num8 = num1 / num7;
            double num9 = num2 / num7;

            // Calculate the transformed coordinates of the second segment
            double num10 = num3 * num8 + num4 * num9;  // x3 in new coordinate system
            double num11 = num4 * num8 - num3 * num9;  // y3 in new coordinate system
            double num12 = num10;
            double num13 = num5 * num8 + num6 * num9;  // x4 in new coordinate system
            double num14 = num6 * num8 - num5 * num9;  // y4 in new coordinate system
            double num15 = num13;

            // Check if the second segment crosses the x-axis
            if (num11 < 0.0 && num14 < 0.0 || ((num11 < 0.0 ? 0 : (num14 >= 0.0 ? 1 : 0)) & (strictIntersect ? 1 : 0)) != 0)
                return false;

            // Calculate the intersection point
            double num16 = num15 + (num12 - num15) * num14 / (num14 - num11);

            // Check if the intersection point is within the first segment
            if (num16 < 0.0 || num16 > num7 & strictIntersect)
                return false;

            // Calculate the intersection point in the original coordinate system
            p = new Point(x1 + num16 * num8, y1 + num16 * num9);
            return true;
        }
    }
}