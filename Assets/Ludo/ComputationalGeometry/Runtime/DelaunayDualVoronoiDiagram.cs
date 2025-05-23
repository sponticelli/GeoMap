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
            _points = new Point[_triangularMesh.TriangleDictionary.Count + _triangularMesh.SubSegmentDictionary.Count * 5];
            _regions = new List<VoronoiRegion>(_triangularMesh.VertexDictionary.Count);
            ComputeCircumCenters();
            TagBlindTriangles();
            foreach (Vertex vertex in _triangularMesh.VertexDictionary.Values)
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
            double barycentric1 = 0.0;
            double barycentric2 = 0.0;
            foreach (Triangle triangle in _triangularMesh.TriangleDictionary.Values)
            {
                orientedTriangle.triangle = triangle;
                Point circumcenter = Primitives.FindCircumcenter(orientedTriangle.Origin(), orientedTriangle.Destination(), orientedTriangle.Apex(), ref barycentric1, ref barycentric2);
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
            OrientedTriangle neighborTriangle = new OrientedTriangle();
            OrientedSubSegment seg = new OrientedSubSegment();
            OrientedSubSegment neighborSegment = new OrientedSubSegment();
            foreach (Triangle triangle in _triangularMesh.TriangleDictionary.Values)
                triangle.infected = false;
            foreach (Segment segment in _triangularMesh.SubSegmentDictionary.Values)
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
                            orientedTriangle.SetAsSymmetricTriangle(ref neighborTriangle);
                            neighborTriangle.SegPivot(ref neighborSegment);
                            if (neighborTriangle.triangle != TriangularMesh.dummytri && !neighborTriangle.triangle.infected &&
                                neighborSegment.seg == TriangularMesh.dummysub)
                            {
                                triangleStack.Push(neighborTriangle.triangle);
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
            Vertex triangleVertex1 = tri.Origin();
            Vertex triangleVertex2 = tri.Destination();
            Vertex triangleVertex3 = tri.Apex();
            Vertex segmentVertex1 = seg.Origin();
            Vertex segmentVertex2 = seg.Destination();
            Point circumcenter = _points[tri.triangle.id];
            Point intersectionPoint;
            return SegmentsIntersect(segmentVertex1, segmentVertex2, circumcenter, triangleVertex1, out intersectionPoint, true) ||
                   SegmentsIntersect(segmentVertex1, segmentVertex2, circumcenter, triangleVertex2, out intersectionPoint, true) ||
                   SegmentsIntersect(segmentVertex1, segmentVertex2, circumcenter, triangleVertex3, out intersectionPoint, true);
        }

        private void ConstructBvdCell(Vertex vertex)
        {
            VoronoiRegion voronoiRegion = new VoronoiRegion(vertex);
            _regions.Add(voronoiRegion);
            OrientedTriangle currentTriangle = new OrientedTriangle();
            OrientedTriangle startTriangle = new OrientedTriangle();
            OrientedTriangle nextTriangle = new OrientedTriangle();
            OrientedSubSegment orientedSubSegment = new OrientedSubSegment();
            OrientedSubSegment nextTriangleSubSegment = new OrientedSubSegment();
            int count = _triangularMesh.TriangleDictionary.Count;
            List<Point> points = new List<Point>();
            vertex.triangle.Copy(ref startTriangle);
            if (startTriangle.Origin() != vertex)
            {
                throw new Exception("ConstructBvdCell: inconsistent topology.");
            }
            startTriangle.Copy(ref currentTriangle);
            startTriangle.Onext(ref nextTriangle);
            do
            {
                Point point1 = _points[currentTriangle.triangle.id];
                Point point2 = _points[nextTriangle.triangle.id];
                Point p;
                if (!currentTriangle.triangle.infected)
                {
                    points.Add(point1);
                    if (nextTriangle.triangle.infected)
                    {
                        nextTriangleSubSegment.seg = _subsegMap[nextTriangle.triangle.hash];
                        if (SegmentsIntersect(nextTriangleSubSegment.GetSegmentOrigin(), nextTriangleSubSegment.GetSegmentDestination(), point1, point2, out p, true))
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
                    orientedSubSegment.seg = _subsegMap[currentTriangle.triangle.hash];
                    if (!nextTriangle.triangle.infected)
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
                        nextTriangleSubSegment.seg = _subsegMap[nextTriangle.triangle.hash];
                        if (!orientedSubSegment.Equal(nextTriangleSubSegment))
                        {
                            if (SegmentsIntersect(orientedSubSegment.GetSegmentOrigin(), orientedSubSegment.GetSegmentDestination(), point1, point2, out p, true))
                            {
                                p.id = count + _segIndex;
                                _points[count + _segIndex] = p;
                                ++_segIndex;
                                points.Add(p);
                            }
                            if (SegmentsIntersect(nextTriangleSubSegment.GetSegmentOrigin(), nextTriangleSubSegment.GetSegmentDestination(), point1, point2, out p, true))
                            {
                                p.id = count + _segIndex;
                                _points[count + _segIndex] = p;
                                ++_segIndex;
                                points.Add(p);
                            }
                        }
                    }
                }
                nextTriangle.Copy(ref currentTriangle);
                nextTriangle.OnextSelf();
            }
            while (!currentTriangle.Equal(startTriangle));
            voronoiRegion.Add(points);
        }

        private void ConstructBoundaryBvdCell(Vertex vertex)
        {
            VoronoiRegion voronoiRegion = new VoronoiRegion(vertex);
            _regions.Add(voronoiRegion);
            OrientedTriangle currentTriangle = new OrientedTriangle();
            OrientedTriangle  startTriangle= new OrientedTriangle();
            OrientedTriangle nextTriangle = new OrientedTriangle();
            OrientedTriangle boundaryTriangle = new OrientedTriangle();
            OrientedSubSegment orientedSubSegment = new OrientedSubSegment();
            OrientedSubSegment nextTriangleSubSegment = new OrientedSubSegment();
            int count = _triangularMesh.TriangleDictionary.Count;
            List<Point> points = new List<Point>();
            vertex.triangle.Copy(ref startTriangle);
            if (startTriangle.Origin() != vertex)
                throw new Exception("ConstructBoundaryBvdCell: inconsistent topology.");
            startTriangle.Copy(ref currentTriangle);
            startTriangle.Onext(ref nextTriangle);
            startTriangle.Oprev(ref boundaryTriangle);
            if (boundaryTriangle.triangle != TriangularMesh.dummytri)
            {
                while (boundaryTriangle.triangle != TriangularMesh.dummytri &&
                   !boundaryTriangle.Equal(startTriangle))
                {
                    boundaryTriangle.Copy(ref currentTriangle);
                    boundaryTriangle.OprevSelf();
                }
                currentTriangle.Copy(ref startTriangle);
                currentTriangle.Onext(ref nextTriangle);
            }
            if (boundaryTriangle.triangle == TriangularMesh.dummytri)
            {
                Point point = new Point(vertex.x, vertex.y);
                point.id = count + _segIndex;
                _points[count + _segIndex] = point;
                ++_segIndex;
                points.Add(point);
            }
            Vertex vertex1 = currentTriangle.Origin();
            Vertex vertex2 = currentTriangle.Destination();
            Point p = new Point((vertex1.X + vertex2.X) / 2.0, (vertex1.Y + vertex2.Y) / 2.0);
            p.id = count + _segIndex;
            _points[count + _segIndex] = p;
            ++_segIndex;
            points.Add(p);
            do
            {
                Point point1 = _points[currentTriangle.triangle.id];
                if (nextTriangle.triangle == TriangularMesh.dummytri)
                {
                    if (!currentTriangle.triangle.infected)
                    {
                        points.Add(point1);
                    }
                    Vertex vertex3 = currentTriangle.Origin();
                    Vertex vertex4 = currentTriangle.Apex();
                    Point point2 = new Point((vertex3.X + vertex4.X) / 2.0, (vertex3.Y + vertex4.Y) / 2.0);
                    point2.id = count + _segIndex;
                    _points[count + _segIndex] = point2;
                    ++_segIndex;
                    points.Add(point2);
                    break;
                }
                Point point3 = _points[nextTriangle.triangle.id];
                if (!currentTriangle.triangle.infected)
                {
                    points.Add(point1);
                    if (nextTriangle.triangle.infected)
                    {
                        nextTriangleSubSegment.seg = _subsegMap[nextTriangle.triangle.hash];
                        if (SegmentsIntersect(nextTriangleSubSegment.GetSegmentOrigin(), nextTriangleSubSegment.GetSegmentDestination(), point1, point3, out p, true))
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
                    orientedSubSegment.seg = _subsegMap[currentTriangle.triangle.hash];
                    Vertex p1 = orientedSubSegment.GetSegmentOrigin();
                    Vertex p2 = orientedSubSegment.GetSegmentDestination();
                    if (!nextTriangle.triangle.infected)
                    {
                        vertex2 = currentTriangle.Destination();
                        Vertex vertex5 = currentTriangle.Apex();
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
                        nextTriangleSubSegment.seg = _subsegMap[nextTriangle.triangle.hash];
                        if (!orientedSubSegment.Equal(nextTriangleSubSegment))
                        {
                            if (SegmentsIntersect(p1, p2, point1, point3, out p, true))
                            {
                                p.id = count + _segIndex;
                                _points[count + _segIndex] = p;
                                ++_segIndex;
                                points.Add(p);
                            }
                            if (SegmentsIntersect(nextTriangleSubSegment.GetSegmentOrigin(), nextTriangleSubSegment.GetSegmentDestination(), point1, point3, out p, true))
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
                nextTriangle.Copy(ref currentTriangle);
                nextTriangle.OnextSelf();
            }
            while (!currentTriangle.Equal(startTriangle));
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
            double dx1 = x2 - x1;
            double dy1 = y2 - y1;
            double dx3 = x3 - x1;
            double dy3 = y3 - y1;
            double dx4 = x4 - x1;
            double dy4 = y4 - y1;
            double segmentLength = Math.Sqrt(dx1 * dx1 + dy1 * dy1);
            double cosAngle = dx1 / segmentLength;
            double sinAngle = dy1 / segmentLength;

            // Calculate the transformed coordinates of the second segment
            double x3Transformed = dx3 * cosAngle + dy3 * sinAngle;  // x3 in new coordinate system
            double y3Transformed = dy3 * cosAngle - dx3 * sinAngle;  // y3 in new coordinate system
            double x3Copy = x3Transformed;
            double x4Transformed = dx4 * cosAngle + dy4 * sinAngle;  // x4 in new coordinate system
            double y4Transformed = dy4 * cosAngle - dx4 * sinAngle;  // y4 in new coordinate system
            double x4Copy = x4Transformed;

            // Check if the second segment crosses the x-axis
            if (y3Transformed < 0.0 && y4Transformed < 0.0 || ((y3Transformed < 0.0 ? 0 : (y4Transformed >= 0.0 ? 1 : 0)) & (strictIntersect ? 1 : 0)) != 0)
                return false;

            // Calculate the intersection point
            double intersectionX = x4Copy + (x3Copy - x4Copy) * y4Transformed / (y4Transformed - y3Transformed);

            // Check if the intersection point is within the first segment
            if (intersectionX < 0.0 || intersectionX > segmentLength & strictIntersect)
                return false;

            // Calculate the intersection point in the original coordinate system
            p = new Point(x1 + intersectionX * cosAngle, y1 + intersectionX * sinAngle);
            return true;
        }
    }
}