using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Provides methods for enforcing quality constraints on a triangular mesh.
    /// </summary>
    /// <remarks>
    /// The Quality class implements algorithms for improving mesh quality by refining triangles
    /// and segments that violate quality constraints. It identifies poor-quality elements and
    /// strategically inserts new vertices to improve the overall mesh quality.
    ///
    /// Quality constraints can include minimum angle requirements, maximum area limitations,
    /// and conforming Delaunay properties. The class works closely with the Mesh and Behavior
    /// classes to enforce these constraints according to the specified behavior settings.
    /// </remarks>
    [Serializable]
    public class Quality
    {
        /// <summary>
        /// Queue of subsegments that violate quality constraints and need to be split.
        /// </summary>
        private Queue<NonconformingSubsegment> badsubsegs;

        /// <summary>
        /// Priority queue of triangles that violate quality constraints and need to be refined.
        /// </summary>
        private QualityViolatingTriangleQueue queue;

        /// <summary>
        /// Reference to the mesh being refined.
        /// </summary>
        private TriangularMesh _triangularMesh;

        /// <summary>
        /// Reference to the behavior settings that control quality constraints.
        /// </summary>
        private TriangulationSettings behavior;

        /// <summary>
        /// Helper class for finding optimal locations for new vertices.
        /// </summary>
        private VertexPositionOptimizer _vertexPositionOptimizer;

        /// <summary>
        /// Optional user-defined test function for custom quality constraints.
        /// </summary>
        /// <remarks>
        /// This delegate takes three points (the vertices of a triangle) and its area,
        /// and returns true if the triangle needs refinement.
        /// </remarks>
        private Func<Point, Point, Point, double, bool> userTest;

        /// <summary>
        /// Initializes a new instance of the <see cref="Quality"/> class.
        /// </summary>
        /// <param name="triangularMesh">The mesh to apply quality constraints to.</param>
        /// <remarks>
        /// The constructor initializes the quality improvement system with references to the mesh
        /// and its behavior settings. It creates the necessary data structures for tracking
        /// poor-quality elements during the refinement process.
        /// </remarks>
        public Quality(TriangularMesh triangularMesh)
        {
            badsubsegs = new Queue<NonconformingSubsegment>();
            queue = new QualityViolatingTriangleQueue();
            _triangularMesh = triangularMesh;
            behavior = triangularMesh.behavior;
            _vertexPositionOptimizer = new VertexPositionOptimizer(triangularMesh);
        }

        /// <summary>
        /// Adds a subsegment that violates quality constraints to the queue for refinement.
        /// </summary>
        /// <param name="badseg">The bad subsegment to add to the queue.</param>
        /// <remarks>
        /// This method is used to mark a subsegment for refinement during the mesh improvement process.
        /// </remarks>
        public void AddBadSubseg(NonconformingSubsegment badseg) => badsubsegs.Enqueue(badseg);

        /// <summary>
        /// Checks the mesh for topological consistency and correctness.
        /// </summary>
        /// <returns>True if the mesh is consistent and correct; otherwise, false.</returns>
        /// <remarks>
        /// This method performs a series of checks on the mesh to verify its topological integrity.
        /// It checks for:
        /// - Properly oriented triangles (counterclockwise vertices)
        /// - Consistent triangle-triangle bonds
        /// - Matching edge coordinates between adjacent triangles
        /// - Proper vertex connectivity
        ///
        /// Any inconsistencies found are reported through debug warnings.
        /// </remarks>
        public bool CheckMesh()
        {
            OrientedTriangle orientedTriangle = new OrientedTriangle();
            OrientedTriangle o2_1 = new OrientedTriangle();
            OrientedTriangle o2_2 = new OrientedTriangle();
            bool noExact = TriangulationSettings.NoExact;
            TriangulationSettings.NoExact = false;
            int num = 0;
            foreach (Triangle triangle in _triangularMesh.triangles.Values)
            {
                orientedTriangle.triangle = triangle;
                for (orientedTriangle.orient = 0; orientedTriangle.orient < 3; ++orientedTriangle.orient)
                {
                    Vertex pa = orientedTriangle.Org();
                    Vertex pb = orientedTriangle.Dest();
                    if (orientedTriangle.orient == 0)
                    {
                        Vertex pc = orientedTriangle.Apex();
                        if (Primitives.CounterClockwise(pa, pb, pc) <= 0.0)
                        {
                            ++num;
                        }
                    }

                    orientedTriangle.Sym(ref o2_1);
                    if (o2_1.triangle != TriangularMesh.dummytri)
                    {
                        o2_1.Sym(ref o2_2);
                        if (orientedTriangle.triangle != o2_2.triangle || orientedTriangle.orient != o2_2.orient)
                        {
                            if (orientedTriangle.triangle == o2_2.triangle)
                                Debug.LogWarning(
                                    "Asymmetric triangle-triangle bond: (Right triangle, wrong orientation)");
                            ++num;
                        }

                        Vertex vertex1 = o2_1.Org();
                        Vertex vertex2 = o2_1.Dest();
                        if (pa != vertex2 || pb != vertex1)
                        {
                            Debug.LogWarning("Mismatched edge coordinates between two triangles.");
                            ++num;
                        }
                    }
                }
            }

            _triangularMesh.MakeVertexMap();
            foreach (Vertex vertex in _triangularMesh.vertices.Values)
            {
                if (vertex.tri.triangle == null)
                    Debug.LogWarning(
                        $"Vertex (ID {(object)vertex.id}) not connected to mesh (duplicate input vertex?)");
            }

            TriangulationSettings.NoExact = noExact;
            return num == 0;
        }

        /// <summary>
        /// Checks if the mesh satisfies the Delaunay criterion.
        /// </summary>
        /// <returns>True if the mesh is Delaunay; otherwise, false.</returns>
        /// <remarks>
        /// This method verifies that the mesh satisfies the Delaunay criterion, which requires
        /// that the circumcircle of any triangle in the mesh contains no other vertices.
        ///
        /// It checks each triangle in the mesh against its neighbors to ensure that no
        /// vertex lies inside another triangle's circumcircle. Any violations are reported
        /// through debug warnings.
        /// </remarks>
        public bool CheckDelaunay()
        {
            OrientedTriangle orientedTriangle = new OrientedTriangle();
            OrientedTriangle o2 = new OrientedTriangle();
            OrientedSubSegment os = new OrientedSubSegment();
            bool noExact = TriangulationSettings.NoExact;
            TriangulationSettings.NoExact = false;
            int num = 0;
            foreach (Triangle triangle in _triangularMesh.triangles.Values)
            {
                orientedTriangle.triangle = triangle;
                for (orientedTriangle.orient = 0; orientedTriangle.orient < 3; ++orientedTriangle.orient)
                {
                    Vertex pa = orientedTriangle.Org();
                    Vertex pb = orientedTriangle.Dest();
                    Vertex pc = orientedTriangle.Apex();
                    orientedTriangle.Sym(ref o2);
                    Vertex pd = o2.Apex();
                    bool flag = o2.triangle != TriangularMesh.dummytri && !OrientedTriangle.IsDead(o2.triangle) &&
                                orientedTriangle.triangle.id < o2.triangle.id && pa != _triangularMesh.infvertex1 &&
                                pa != _triangularMesh.infvertex2 && pa != _triangularMesh.infvertex3 &&
                                pb != _triangularMesh.infvertex1 && pb != _triangularMesh.infvertex2 &&
                                pb != _triangularMesh.infvertex3 && pc != _triangularMesh.infvertex1 &&
                                pc != _triangularMesh.infvertex2 && pc != _triangularMesh.infvertex3 &&
                                pd != _triangularMesh.infvertex1 && pd != _triangularMesh.infvertex2 &&
                                pd != _triangularMesh.infvertex3;
                    if (_triangularMesh.checksegments & flag)
                    {
                        orientedTriangle.SegPivot(ref os);
                        if (os.seg != TriangularMesh.dummysub)
                            flag = false;
                    }

                    if (flag && Primitives.NonRegular(pa, pb, pc, pd) > 0.0)
                    {
                        Debug.LogWarning(
                            $"Non-regular pair of triangles found (IDs {orientedTriangle.triangle.id}/{o2.triangle.id}).");
                        ++num;
                    }
                }
            }

            TriangulationSettings.NoExact = noExact;
            return num == 0;
        }

        /// <summary>
        /// Checks if a subsegment is encroached upon by nearby vertices.
        /// </summary>
        /// <param name="testsubseg">The subsegment to check.</param>
        /// <returns>
        /// 0 if the subsegment is not encroached upon,
        /// 1 if it is encroached upon from one side,
        /// 3 if it is encroached upon from both sides.
        /// </returns>
        /// <remarks>
        /// A subsegment is considered encroached upon if a vertex lies inside or near its
        /// diametral circle (the circle whose diameter is the subsegment).
        ///
        /// If an encroachment is found, the subsegment is added to the queue of bad subsegments
        /// for later refinement.
        /// </remarks>
        public int CheckSeg4Encroach(ref OrientedSubSegment testsubseg)
        {
            OrientedTriangle ot = new OrientedTriangle();
            OrientedSubSegment o2 = new OrientedSubSegment();
            int num1 = 0;
            int num2 = 0;
            Vertex vertex1 = testsubseg.Origin();
            Vertex vertex2 = testsubseg.Destination();
            testsubseg.TriPivot(ref ot);
            if (ot.triangle != TriangularMesh.dummytri)
            {
                ++num2;
                Vertex vertex3 = ot.Apex();
                double num3 = (vertex1.x - vertex3.x) * (vertex2.x - vertex3.x) +
                              (vertex1.y - vertex3.y) * (vertex2.y - vertex3.y);
                if (num3 < 0.0 && (behavior.ConformingDelaunay || num3 * num3 >=
                        (2.0 * behavior.goodAngle - 1.0) * (2.0 * behavior.goodAngle - 1.0) *
                        ((vertex1.x - vertex3.x) * (vertex1.x - vertex3.x) +
                         (vertex1.y - vertex3.y) * (vertex1.y - vertex3.y)) *
                        ((vertex2.x - vertex3.x) * (vertex2.x - vertex3.x) +
                         (vertex2.y - vertex3.y) * (vertex2.y - vertex3.y))))
                    num1 = 1;
            }

            testsubseg.Sym(ref o2);
            o2.TriPivot(ref ot);
            if (ot.triangle != TriangularMesh.dummytri)
            {
                ++num2;
                Vertex vertex4 = ot.Apex();
                double num4 = (vertex1.x - vertex4.x) * (vertex2.x - vertex4.x) +
                              (vertex1.y - vertex4.y) * (vertex2.y - vertex4.y);
                if (num4 < 0.0 && (behavior.ConformingDelaunay || num4 * num4 >=
                        (2.0 * behavior.goodAngle - 1.0) * (2.0 * behavior.goodAngle - 1.0) *
                        ((vertex1.x - vertex4.x) * (vertex1.x - vertex4.x) +
                         (vertex1.y - vertex4.y) * (vertex1.y - vertex4.y)) *
                        ((vertex2.x - vertex4.x) * (vertex2.x - vertex4.x) +
                         (vertex2.y - vertex4.y) * (vertex2.y - vertex4.y))))
                    num1 += 2;
            }

            if (num1 > 0 && (behavior.NoBisect == 0 || behavior.NoBisect == 1 && num2 == 2))
            {
                NonconformingSubsegment nonconformingSubsegment = new NonconformingSubsegment();
                if (num1 == 1)
                {
                    nonconformingSubsegment.Encsubseg = testsubseg;
                    nonconformingSubsegment.subSegmentOrigin = vertex1;
                    nonconformingSubsegment.subSegmentDestination = vertex2;
                }
                else
                {
                    nonconformingSubsegment.Encsubseg = o2;
                    nonconformingSubsegment.subSegmentOrigin = vertex2;
                    nonconformingSubsegment.subSegmentDestination = vertex1;
                }

                badsubsegs.Enqueue(nonconformingSubsegment);
            }

            return num1;
        }

        /// <summary>
        /// Tests a triangle against quality constraints and adds it to the refinement queue if necessary.
        /// </summary>
        /// <param name="testtri">The triangle to test.</param>
        /// <remarks>
        /// This method evaluates a triangle against various quality criteria, including:
        /// - Minimum angle requirements
        /// - Maximum area constraints
        /// - Variable area constraints
        /// - User-defined quality tests
        ///
        /// If the triangle fails any of these tests, it is added to the priority queue
        /// for refinement during the mesh improvement process.
        /// </remarks>
        public void TestTriangle(ref OrientedTriangle testtri)
        {
            OrientedTriangle o2_1 = new OrientedTriangle();
            OrientedTriangle o2_2 = new OrientedTriangle();
            OrientedSubSegment os = new OrientedSubSegment();
            Vertex enqorg = testtri.Org();
            Vertex enqdest = testtri.Dest();
            Vertex enqapex = testtri.Apex();
            double num1 = enqorg.x - enqdest.x;
            double num2 = enqorg.y - enqdest.y;
            double num3 = enqdest.x - enqapex.x;
            double num4 = enqdest.y - enqapex.y;
            double num5 = enqapex.x - enqorg.x;
            double num6 = enqapex.y - enqorg.y;
            double num7 = num1 * num1;
            double num8 = num2 * num2;
            double num9 = num3 * num3;
            double num10 = num4 * num4;
            double num11 = num5 * num5;
            double num12 = num6 * num6;
            double num13 = num7 + num8;
            double num14 = num9 + num10;
            double num15 = num12;
            double num16 = num11 + num15;
            double minedge;
            double num17;
            Vertex vertex1;
            Vertex vertex2;
            if (num13 < num14 && num13 < num16)
            {
                minedge = num13;
                double num18 = num3 * num5 + num4 * num6;
                num17 = num18 * num18 / (num14 * num16);
                vertex1 = enqorg;
                vertex2 = enqdest;
                testtri.Copy(ref o2_1);
            }
            else if (num14 < num16)
            {
                minedge = num14;
                double num19 = num1 * num5 + num2 * num6;
                num17 = num19 * num19 / (num13 * num16);
                vertex1 = enqdest;
                vertex2 = enqapex;
                testtri.Lnext(ref o2_1);
            }
            else
            {
                minedge = num16;
                double num20 = num1 * num3 + num2 * num4;
                num17 = num20 * num20 / (num13 * num14);
                vertex1 = enqapex;
                vertex2 = enqorg;
                testtri.Lprev(ref o2_1);
            }

            if (behavior.VarArea || behavior.fixedArea || behavior.Usertest)
            {
                double num21 = 0.5 * (num1 * num4 - num2 * num3);
                if (behavior.fixedArea && num21 > behavior.MaxArea)
                {
                    queue.Enqueue(ref testtri, minedge, enqapex, enqorg, enqdest);
                    return;
                }

                if (behavior.VarArea && num21 > testtri.triangle.area && testtri.triangle.area > 0.0)
                {
                    queue.Enqueue(ref testtri, minedge, enqapex, enqorg, enqdest);
                    return;
                }

                if (behavior.Usertest && userTest != null &&
                    userTest(enqorg, enqdest, enqapex, num21))
                {
                    queue.Enqueue(ref testtri, minedge, enqapex, enqorg, enqdest);
                    return;
                }
            }

            double num22 = num13 <= num14 || num13 <= num16
                ? (num14 <= num16
                    ? (num13 + num14 - num16) / (2.0 * Math.Sqrt(num13 * num14))
                    : (num13 + num16 - num14) / (2.0 * Math.Sqrt(num13 * num16)))
                : (num14 + num16 - num13) / (2.0 * Math.Sqrt(num14 * num16));
            if (num17 <= behavior.goodAngle &&
                (num22 >= behavior.maxGoodAngle || behavior.MaxAngle == 0.0))
                return;
            if (vertex1.type == VertexType.SegmentVertex && vertex2.type == VertexType.SegmentVertex)
            {
                o2_1.SegPivot(ref os);
                if (os.seg == TriangularMesh.dummysub)
                {
                    o2_1.Copy(ref o2_2);
                    do
                    {
                        o2_1.OprevSelf();
                        o2_1.SegPivot(ref os);
                    } while (os.seg == TriangularMesh.dummysub);

                    Vertex vertex3 = os.GetSegmentOrigin();
                    Vertex vertex4 = os.GetSegmentDestination();
                    do
                    {
                        o2_2.DnextSelf();
                        o2_2.SegPivot(ref os);
                    } while (os.seg == TriangularMesh.dummysub);

                    Vertex vertex5 = os.GetSegmentOrigin();
                    Vertex vertex6 = os.GetSegmentDestination();
                    Vertex vertex7 = null;
                    if (vertex4.x == vertex5.x && vertex4.y == vertex5.y)
                        vertex7 = vertex4;
                    else if (vertex3.x == vertex6.x && vertex3.y == vertex6.y)
                        vertex7 = vertex3;
                    if (vertex7 != null)
                    {
                        double num23 = (vertex1.x - vertex7.x) * (vertex1.x - vertex7.x) +
                                       (vertex1.y - vertex7.y) * (vertex1.y - vertex7.y);
                        double num24 = (vertex2.x - vertex7.x) * (vertex2.x - vertex7.x) +
                                       (vertex2.y - vertex7.y) * (vertex2.y - vertex7.y);
                        if (num23 < 1001.0 / 1000.0 * num24 && num23 > 0.999 * num24)
                            return;
                    }
                }
            }

            queue.Enqueue(ref testtri, minedge, enqapex, enqorg, enqdest);
        }

        /// <summary>
        /// Identifies all encroached subsegments in the mesh.
        /// </summary>
        /// <remarks>
        /// This method examines each subsegment in the mesh and checks if it is encroached upon
        /// by nearby vertices. Encroached subsegments are added to the refinement queue.
        /// </remarks>
        private void TallyEncs()
        {
            OrientedSubSegment testsubseg = new OrientedSubSegment();
            testsubseg.orient = 0;
            foreach (Segment segment in _triangularMesh.subsegs.Values)
            {
                testsubseg.seg = segment;
                CheckSeg4Encroach(ref testsubseg);
            }
        }

        /// <summary>
        /// Splits encroached subsegments by inserting new vertices.
        /// </summary>
        /// <param name="triflaws">If true, checks for new triangle quality violations after splitting.</param>
        /// <remarks>
        /// This method processes the queue of encroached subsegments and splits each one by
        /// inserting a new vertex. The new vertex is typically placed at the midpoint of the
        /// subsegment, but its position may be adjusted to avoid creating new encroachments.
        ///
        /// After splitting a subsegment, the method checks if the resulting subsegments are
        /// still encroached upon and adds them to the queue if necessary.
        ///
        /// The process continues until either all encroached subsegments are resolved or
        /// the maximum number of Steiner points is reached.
        /// </remarks>
        private void SplitEncSegs(bool triflaws)
        {
            OrientedTriangle otri1 = new OrientedTriangle();
            OrientedTriangle otri2 = new OrientedTriangle();
            OrientedSubSegment os = new OrientedSubSegment();
            OrientedSubSegment orientedSubSegment = new OrientedSubSegment();
            while (badsubsegs.Count > 0 && _triangularMesh.steinerleft != 0)
            {
                NonconformingSubsegment nonconformingSubsegment = badsubsegs.Dequeue();
                OrientedSubSegment encsubseg = nonconformingSubsegment.Encsubseg;
                Vertex pa = encsubseg.Origin();
                Vertex pb = encsubseg.Destination();
                if (!OrientedSubSegment.IsDead(encsubseg.seg) && pa == nonconformingSubsegment.subSegmentOrigin &&
                    pb == nonconformingSubsegment.subSegmentDestination)
                {
                    encsubseg.TriPivot(ref otri1);
                    otri1.Lnext(ref otri2);
                    otri2.SegPivot(ref os);
                    bool flag1 = os.seg != TriangularMesh.dummysub;
                    otri2.LnextSelf();
                    otri2.SegPivot(ref os);
                    bool flag2 = os.seg != TriangularMesh.dummysub;
                    if (!behavior.ConformingDelaunay && !flag1 && !flag2)
                    {
                        Vertex vertex = otri1.Apex();
                        while (vertex.type == VertexType.FreeVertex &&
                               (pa.x - vertex.x) * (pb.x - vertex.x) + (pa.y - vertex.y) * (pb.y - vertex.y) < 0.0)
                        {
                            _triangularMesh.DeleteVertex(ref otri2);
                            encsubseg.TriPivot(ref otri1);
                            vertex = otri1.Apex();
                            otri1.Lprev(ref otri2);
                        }
                    }

                    otri1.Sym(ref otri2);
                    if (otri2.triangle != TriangularMesh.dummytri)
                    {
                        otri2.LnextSelf();
                        otri2.SegPivot(ref os);
                        bool flag3 = os.seg != TriangularMesh.dummysub;
                        flag2 |= flag3;
                        otri2.LnextSelf();
                        otri2.SegPivot(ref os);
                        bool flag4 = os.seg != TriangularMesh.dummysub;
                        flag1 |= flag4;
                        if (!behavior.ConformingDelaunay && !flag4 && !flag3)
                        {
                            Vertex vertex = otri2.Org();
                            while (vertex.type == VertexType.FreeVertex && (pa.x - vertex.x) * (pb.x - vertex.x) +
                                   (pa.y - vertex.y) * (pb.y - vertex.y) < 0.0)
                            {
                                _triangularMesh.DeleteVertex(ref otri2);
                                otri1.Sym(ref otri2);
                                vertex = otri2.Apex();
                                otri2.LprevSelf();
                            }
                        }
                    }

                    double num1;
                    if (flag1 | flag2)
                    {
                        double num2 = Math.Sqrt((pb.x - pa.x) * (pb.x - pa.x) + (pb.y - pa.y) * (pb.y - pa.y));
                        double num3 = 1.0;
                        while (num2 > 3.0 * num3)
                            num3 *= 2.0;
                        while (num2 < 1.5 * num3)
                            num3 *= 0.5;
                        num1 = num3 / num2;
                        if (flag2)
                            num1 = 1.0 - num1;
                    }
                    else
                        num1 = 0.5;

                    Vertex vertex1 = new Vertex(pa.x + num1 * (pb.x - pa.x), pa.y + num1 * (pb.y - pa.y),
                        encsubseg.Mark(), _triangularMesh.nextras);
                    vertex1.type = VertexType.SegmentVertex;
                    vertex1.hash = _triangularMesh.hash_vtx++;
                    vertex1.id = vertex1.hash;
                    _triangularMesh.vertices.Add(vertex1.hash, vertex1);
                    for (int index = 0; index < _triangularMesh.nextras; ++index)
                        vertex1.attributes[index] =
                            pa.attributes[index] + num1 * (pb.attributes[index] - pa.attributes[index]);
                    if (!TriangulationSettings.NoExact)
                    {
                        double num4 = Primitives.CounterClockwise(pa, pb, vertex1);
                        double num5 = (pa.x - pb.x) * (pa.x - pb.x) + (pa.y - pb.y) * (pa.y - pb.y);
                        if (num4 != 0.0 && num5 != 0.0)
                        {
                            double d = num4 / num5;
                            if (!double.IsNaN(d))
                            {
                                vertex1.x += d * (pb.y - pa.y);
                                vertex1.y += d * (pa.x - pb.x);
                            }
                        }
                    }

                    if (vertex1.x == pa.x && vertex1.y == pa.y || vertex1.x == pb.x && vertex1.y == pb.y)
                    {
                        throw new Exception("Ran out of precision");
                    }

                    switch (_triangularMesh.InsertVertex(vertex1, ref otri1, ref encsubseg, true, triflaws))
                    {
                        case VertexInsertionOutcome.Successful:
                        case VertexInsertionOutcome.Encroaching:
                            if (_triangularMesh.steinerleft > 0)
                                --_triangularMesh.steinerleft;
                            CheckSeg4Encroach(ref encsubseg);
                            encsubseg.NextSelf();
                            CheckSeg4Encroach(ref encsubseg);
                            break;
                        default:
                            throw new Exception("Failure to split a segment.");
                    }
                }

                nonconformingSubsegment.subSegmentOrigin = null;
            }
        }

        /// <summary>
        /// Identifies all poor-quality triangles in the mesh.
        /// </summary>
        /// <remarks>
        /// This method examines each triangle in the mesh and tests it against the quality
        /// constraints. Triangles that fail the quality tests are added to the refinement queue.
        /// </remarks>
        private void TallyFaces()
        {
            OrientedTriangle testtri = new OrientedTriangle();
            testtri.orient = 0;
            foreach (Triangle triangle in _triangularMesh.triangles.Values)
            {
                testtri.triangle = triangle;
                TestTriangle(ref testtri);
            }
        }

        /// <summary>
        /// Splits a poor-quality triangle by inserting a new vertex.
        /// </summary>
        /// <param name="badtri">The poor-quality triangle to split.</param>
        /// <remarks>
        /// This method refines a poor-quality triangle by inserting a new vertex at a strategic
        /// location. The location is typically near the circumcenter of the triangle, but may
        /// be adjusted based on quality constraints and to avoid creating new encroachments.
        ///
        /// If the insertion would cause a subsegment to be encroached upon, the operation may
        /// be canceled, and the encroached subsegment will be split instead.
        ///
        /// The method also handles attribute interpolation for the new vertex based on its
        /// barycentric coordinates within the original triangle.
        /// </remarks>
        private void SplitTriangle(QualityViolatingTriangle badtri)
        {
            OrientedTriangle orientedTriangle = new OrientedTriangle();
            double xi = 0.0;
            double eta = 0.0;
            OrientedTriangle poortri = badtri.poortri;
            Vertex torg = poortri.Org();
            Vertex tdest = poortri.Dest();
            Vertex tapex = poortri.Apex();
            if (OrientedTriangle.IsDead(poortri.triangle) || !(torg == badtri.triangorg) ||
                !(tdest == badtri.triangdest) || !(tapex == badtri.triangapex))
                return;
            Point point = behavior.fixedArea || behavior.VarArea
                ? Primitives.FindCircumcenter(torg, tdest, tapex, ref xi, ref eta,
                    behavior.offconstant)
                : _vertexPositionOptimizer.FindPosition(torg, tdest, tapex, ref xi, ref eta, true, poortri);
            if ((point.x == torg.x && point.y == torg.y) ||
                (point.x == tdest.x && point.y == tdest.y) ||
                (point.x == tapex.x && point.y == tapex.y)) return;

            Vertex newvertex = new Vertex(point.x, point.y, 0, _triangularMesh.nextras);
            newvertex.type = VertexType.FreeVertex;
            for (int index = 0; index < _triangularMesh.nextras; ++index)
                newvertex.attributes[index] = torg.attributes[index] +
                                              xi * (tdest.attributes[index] - torg.attributes[index]) +
                                              eta * (tapex.attributes[index] - torg.attributes[index]);
            if (eta < xi)
                poortri.LprevSelf();
            OrientedSubSegment splitseg = new OrientedSubSegment();
            switch (_triangularMesh.InsertVertex(newvertex, ref poortri, ref splitseg, true, true))
            {
                case VertexInsertionOutcome.Successful:
                    newvertex.hash = _triangularMesh.hash_vtx++;
                    newvertex.id = newvertex.hash;
                    _triangularMesh.vertices.Add(newvertex.hash, newvertex);
                    if (_triangularMesh.steinerleft > 0)
                    {
                        --_triangularMesh.steinerleft;
                    }

                    break;
                case VertexInsertionOutcome.Encroaching:
                    _triangularMesh.UndoVertex();
                    break;
                case VertexInsertionOutcome.Violating:
                    break;
            }
        }

        /// <summary>
        /// Enforces quality constraints on the mesh by refining poor-quality elements.
        /// </summary>
        /// <remarks>
        /// This is the main method for improving mesh quality. It performs the following steps:
        ///
        /// 1. Identifies encroached subsegments and adds them to the refinement queue.
        /// 2. Splits encroached subsegments by inserting new vertices.
        /// 3. If quality constraints are specified (minimum angle, area constraints, etc.):
        ///    a. Identifies poor-quality triangles and adds them to the refinement queue.
        ///    b. Refines poor-quality triangles by inserting new vertices at strategic locations.
        ///    c. Handles any new encroachments that occur during triangle refinement.
        ///
        /// The refinement process continues until either all quality constraints are satisfied
        /// or the maximum number of Steiner points (additional vertices) is reached.
        /// </remarks>
        public void EnforceQuality()
        {
            TallyEncs();
            SplitEncSegs(false);
            if (behavior.MinAngle > 0.0 || behavior.VarArea || behavior.fixedArea ||
                behavior.Usertest)
            {
                TallyFaces();
                _triangularMesh.checkquality = true;
                while (queue.Count > 0 && _triangularMesh.steinerleft != 0)
                {
                    QualityViolatingTriangle badtri = queue.Dequeue();
                    SplitTriangle(badtri);
                    if (badsubsegs.Count > 0)
                    {
                        queue.Enqueue(badtri);
                        SplitEncSegs(true);
                    }
                }
            }

            if ( !behavior.ConformingDelaunay || badsubsegs.Count <= 0 ||
                _triangularMesh.steinerleft != 0)
                return;
            Debug.LogWarning(
                "I ran out of Steiner points, but the mesh has encroached subsegments, and therefore might not be truly Delaunay. If the Delaunay property is important to you, try increasing the number of Steiner points.");
        }
    }
}