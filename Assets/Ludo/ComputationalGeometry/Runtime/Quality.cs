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
    [System.Serializable]
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
        private NewLocation newLocation;

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
            this.badsubsegs = new Queue<NonconformingSubsegment>();
            this.queue = new QualityViolatingTriangleQueue();
            this._triangularMesh = triangularMesh;
            this.behavior = triangularMesh.behavior;
            this.newLocation = new NewLocation(triangularMesh);
        }

        /// <summary>
        /// Adds a subsegment that violates quality constraints to the queue for refinement.
        /// </summary>
        /// <param name="badseg">The bad subsegment to add to the queue.</param>
        /// <remarks>
        /// This method is used to mark a subsegment for refinement during the mesh improvement process.
        /// </remarks>
        public void AddBadSubseg(NonconformingSubsegment badseg) => this.badsubsegs.Enqueue(badseg);

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
            Otri otri = new Otri();
            Otri o2_1 = new Otri();
            Otri o2_2 = new Otri();
            bool noExact = TriangulationSettings.NoExact;
            TriangulationSettings.NoExact = false;
            int num = 0;
            foreach (Triangle triangle in this._triangularMesh.triangles.Values)
            {
                otri.triangle = triangle;
                for (otri.orient = 0; otri.orient < 3; ++otri.orient)
                {
                    Vertex pa = otri.Org();
                    Vertex pb = otri.Dest();
                    if (otri.orient == 0)
                    {
                        Vertex pc = otri.Apex();
                        if (Primitives.CounterClockwise((Point)pa, (Point)pb, (Point)pc) <= 0.0)
                        {
                            ++num;
                        }
                    }

                    otri.Sym(ref o2_1);
                    if (o2_1.triangle != TriangularMesh.dummytri)
                    {
                        o2_1.Sym(ref o2_2);
                        if (otri.triangle != o2_2.triangle || otri.orient != o2_2.orient)
                        {
                            if (otri.triangle == o2_2.triangle)
                                Debug.LogWarning(
                                    "Asymmetric triangle-triangle bond: (Right triangle, wrong orientation)");
                            ++num;
                        }

                        Vertex vertex1 = o2_1.Org();
                        Vertex vertex2 = o2_1.Dest();
                        if ((Point)pa != (Point)vertex2 || (Point)pb != (Point)vertex1)
                        {
                            Debug.LogWarning("Mismatched edge coordinates between two triangles.");
                            ++num;
                        }
                    }
                }
            }

            this._triangularMesh.MakeVertexMap();
            foreach (Vertex vertex in this._triangularMesh.vertices.Values)
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
            Otri otri = new Otri();
            Otri o2 = new Otri();
            Osub os = new Osub();
            bool noExact = TriangulationSettings.NoExact;
            TriangulationSettings.NoExact = false;
            int num = 0;
            foreach (Triangle triangle in this._triangularMesh.triangles.Values)
            {
                otri.triangle = triangle;
                for (otri.orient = 0; otri.orient < 3; ++otri.orient)
                {
                    Vertex pa = otri.Org();
                    Vertex pb = otri.Dest();
                    Vertex pc = otri.Apex();
                    otri.Sym(ref o2);
                    Vertex pd = o2.Apex();
                    bool flag = o2.triangle != TriangularMesh.dummytri && !Otri.IsDead(o2.triangle) &&
                                otri.triangle.id < o2.triangle.id && (Point)pa != (Point)this._triangularMesh.infvertex1 &&
                                (Point)pa != (Point)this._triangularMesh.infvertex2 && (Point)pa != (Point)this._triangularMesh.infvertex3 &&
                                (Point)pb != (Point)this._triangularMesh.infvertex1 && (Point)pb != (Point)this._triangularMesh.infvertex2 &&
                                (Point)pb != (Point)this._triangularMesh.infvertex3 && (Point)pc != (Point)this._triangularMesh.infvertex1 &&
                                (Point)pc != (Point)this._triangularMesh.infvertex2 && (Point)pc != (Point)this._triangularMesh.infvertex3 &&
                                (Point)pd != (Point)this._triangularMesh.infvertex1 && (Point)pd != (Point)this._triangularMesh.infvertex2 &&
                                (Point)pd != (Point)this._triangularMesh.infvertex3;
                    if (this._triangularMesh.checksegments & flag)
                    {
                        otri.SegPivot(ref os);
                        if (os.seg != TriangularMesh.dummysub)
                            flag = false;
                    }

                    if (flag && Primitives.NonRegular((Point)pa, (Point)pb, (Point)pc, (Point)pd) > 0.0)
                    {
                        Debug.LogWarning(
                            $"Non-regular pair of triangles found (IDs {otri.triangle.id}/{o2.triangle.id}).");
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
        public int CheckSeg4Encroach(ref Osub testsubseg)
        {
            Otri ot = new Otri();
            Osub o2 = new Osub();
            int num1 = 0;
            int num2 = 0;
            Vertex vertex1 = testsubseg.Org();
            Vertex vertex2 = testsubseg.Dest();
            testsubseg.TriPivot(ref ot);
            if (ot.triangle != TriangularMesh.dummytri)
            {
                ++num2;
                Vertex vertex3 = ot.Apex();
                double num3 = (vertex1.x - vertex3.x) * (vertex2.x - vertex3.x) +
                              (vertex1.y - vertex3.y) * (vertex2.y - vertex3.y);
                if (num3 < 0.0 && (this.behavior.ConformingDelaunay || num3 * num3 >=
                        (2.0 * this.behavior.goodAngle - 1.0) * (2.0 * this.behavior.goodAngle - 1.0) *
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
                if (num4 < 0.0 && (this.behavior.ConformingDelaunay || num4 * num4 >=
                        (2.0 * this.behavior.goodAngle - 1.0) * (2.0 * this.behavior.goodAngle - 1.0) *
                        ((vertex1.x - vertex4.x) * (vertex1.x - vertex4.x) +
                         (vertex1.y - vertex4.y) * (vertex1.y - vertex4.y)) *
                        ((vertex2.x - vertex4.x) * (vertex2.x - vertex4.x) +
                         (vertex2.y - vertex4.y) * (vertex2.y - vertex4.y))))
                    num1 += 2;
            }

            if (num1 > 0 && (this.behavior.NoBisect == 0 || this.behavior.NoBisect == 1 && num2 == 2))
            {
                NonconformingSubsegment nonconformingSubsegment = new NonconformingSubsegment();
                if (num1 == 1)
                {
                    nonconformingSubsegment.encsubseg = testsubseg;
                    nonconformingSubsegment.subsegorg = vertex1;
                    nonconformingSubsegment.subsegdest = vertex2;
                }
                else
                {
                    nonconformingSubsegment.encsubseg = o2;
                    nonconformingSubsegment.subsegorg = vertex2;
                    nonconformingSubsegment.subsegdest = vertex1;
                }

                this.badsubsegs.Enqueue(nonconformingSubsegment);
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
        public void TestTriangle(ref Otri testtri)
        {
            Otri o2_1 = new Otri();
            Otri o2_2 = new Otri();
            Osub os = new Osub();
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

            if (this.behavior.VarArea || this.behavior.fixedArea || this.behavior.Usertest)
            {
                double num21 = 0.5 * (num1 * num4 - num2 * num3);
                if (this.behavior.fixedArea && num21 > this.behavior.MaxArea)
                {
                    this.queue.Enqueue(ref testtri, minedge, enqapex, enqorg, enqdest);
                    return;
                }

                if (this.behavior.VarArea && num21 > testtri.triangle.area && testtri.triangle.area > 0.0)
                {
                    this.queue.Enqueue(ref testtri, minedge, enqapex, enqorg, enqdest);
                    return;
                }

                if (this.behavior.Usertest && this.userTest != null &&
                    this.userTest((Point)enqorg, (Point)enqdest, (Point)enqapex, num21))
                {
                    this.queue.Enqueue(ref testtri, minedge, enqapex, enqorg, enqdest);
                    return;
                }
            }

            double num22 = num13 <= num14 || num13 <= num16
                ? (num14 <= num16
                    ? (num13 + num14 - num16) / (2.0 * Math.Sqrt(num13 * num14))
                    : (num13 + num16 - num14) / (2.0 * Math.Sqrt(num13 * num16)))
                : (num14 + num16 - num13) / (2.0 * Math.Sqrt(num14 * num16));
            if (num17 <= this.behavior.goodAngle &&
                (num22 >= this.behavior.maxGoodAngle || this.behavior.MaxAngle == 0.0))
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

                    Vertex vertex3 = os.SegOrg();
                    Vertex vertex4 = os.SegDest();
                    do
                    {
                        o2_2.DnextSelf();
                        o2_2.SegPivot(ref os);
                    } while (os.seg == TriangularMesh.dummysub);

                    Vertex vertex5 = os.SegOrg();
                    Vertex vertex6 = os.SegDest();
                    Vertex vertex7 = (Vertex)null;
                    if (vertex4.x == vertex5.x && vertex4.y == vertex5.y)
                        vertex7 = vertex4;
                    else if (vertex3.x == vertex6.x && vertex3.y == vertex6.y)
                        vertex7 = vertex3;
                    if ((Point)vertex7 != (Point)null)
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

            this.queue.Enqueue(ref testtri, minedge, enqapex, enqorg, enqdest);
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
            Osub testsubseg = new Osub();
            testsubseg.orient = 0;
            foreach (Segment segment in this._triangularMesh.subsegs.Values)
            {
                testsubseg.seg = segment;
                this.CheckSeg4Encroach(ref testsubseg);
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
            Otri otri1 = new Otri();
            Otri otri2 = new Otri();
            Osub os = new Osub();
            Osub osub = new Osub();
            while (this.badsubsegs.Count > 0 && this._triangularMesh.steinerleft != 0)
            {
                NonconformingSubsegment nonconformingSubsegment = this.badsubsegs.Dequeue();
                Osub encsubseg = nonconformingSubsegment.encsubseg;
                Vertex pa = encsubseg.Org();
                Vertex pb = encsubseg.Dest();
                if (!Osub.IsDead(encsubseg.seg) && (Point)pa == (Point)nonconformingSubsegment.subsegorg &&
                    (Point)pb == (Point)nonconformingSubsegment.subsegdest)
                {
                    encsubseg.TriPivot(ref otri1);
                    otri1.Lnext(ref otri2);
                    otri2.SegPivot(ref os);
                    bool flag1 = os.seg != TriangularMesh.dummysub;
                    otri2.LnextSelf();
                    otri2.SegPivot(ref os);
                    bool flag2 = os.seg != TriangularMesh.dummysub;
                    if (!this.behavior.ConformingDelaunay && !flag1 && !flag2)
                    {
                        Vertex vertex = otri1.Apex();
                        while (vertex.type == VertexType.FreeVertex &&
                               (pa.x - vertex.x) * (pb.x - vertex.x) + (pa.y - vertex.y) * (pb.y - vertex.y) < 0.0)
                        {
                            this._triangularMesh.DeleteVertex(ref otri2);
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
                        if (!this.behavior.ConformingDelaunay && !flag4 && !flag3)
                        {
                            Vertex vertex = otri2.Org();
                            while (vertex.type == VertexType.FreeVertex && (pa.x - vertex.x) * (pb.x - vertex.x) +
                                   (pa.y - vertex.y) * (pb.y - vertex.y) < 0.0)
                            {
                                this._triangularMesh.DeleteVertex(ref otri2);
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
                        encsubseg.Mark(), this._triangularMesh.nextras);
                    vertex1.type = VertexType.SegmentVertex;
                    vertex1.hash = this._triangularMesh.hash_vtx++;
                    vertex1.id = vertex1.hash;
                    this._triangularMesh.vertices.Add(vertex1.hash, vertex1);
                    for (int index = 0; index < this._triangularMesh.nextras; ++index)
                        vertex1.attributes[index] =
                            pa.attributes[index] + num1 * (pb.attributes[index] - pa.attributes[index]);
                    if (!TriangulationSettings.NoExact)
                    {
                        double num4 = Primitives.CounterClockwise((Point)pa, (Point)pb, (Point)vertex1);
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

                    switch (this._triangularMesh.InsertVertex(vertex1, ref otri1, ref encsubseg, true, triflaws))
                    {
                        case VertexInsertionOutcome.Successful:
                        case VertexInsertionOutcome.Encroaching:
                            if (this._triangularMesh.steinerleft > 0)
                                --this._triangularMesh.steinerleft;
                            this.CheckSeg4Encroach(ref encsubseg);
                            encsubseg.NextSelf();
                            this.CheckSeg4Encroach(ref encsubseg);
                            break;
                        default:
                            throw new Exception("Failure to split a segment.");
                    }
                }

                nonconformingSubsegment.subsegorg = (Vertex)null;
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
            Otri testtri = new Otri();
            testtri.orient = 0;
            foreach (Triangle triangle in this._triangularMesh.triangles.Values)
            {
                testtri.triangle = triangle;
                this.TestTriangle(ref testtri);
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
            Otri otri = new Otri();
            double xi = 0.0;
            double eta = 0.0;
            Otri poortri = badtri.poortri;
            Vertex torg = poortri.Org();
            Vertex tdest = poortri.Dest();
            Vertex tapex = poortri.Apex();
            if (Otri.IsDead(poortri.triangle) || !((Point)torg == (Point)badtri.triangorg) ||
                !((Point)tdest == (Point)badtri.triangdest) || !((Point)tapex == (Point)badtri.triangapex))
                return;
            Point point = this.behavior.fixedArea || this.behavior.VarArea
                ? Primitives.FindCircumcenter((Point)torg, (Point)tdest, (Point)tapex, ref xi, ref eta,
                    this.behavior.offconstant)
                : this.newLocation.FindLocation(torg, tdest, tapex, ref xi, ref eta, true, poortri);
            if ((point.x == torg.x && point.y == torg.y) ||
                (point.x == tdest.x && point.y == tdest.y) ||
                (point.x == tapex.x && point.y == tapex.y)) return;

            Vertex newvertex = new Vertex(point.x, point.y, 0, this._triangularMesh.nextras);
            newvertex.type = VertexType.FreeVertex;
            for (int index = 0; index < this._triangularMesh.nextras; ++index)
                newvertex.attributes[index] = torg.attributes[index] +
                                              xi * (tdest.attributes[index] - torg.attributes[index]) +
                                              eta * (tapex.attributes[index] - torg.attributes[index]);
            if (eta < xi)
                poortri.LprevSelf();
            Osub splitseg = new Osub();
            switch (this._triangularMesh.InsertVertex(newvertex, ref poortri, ref splitseg, true, true))
            {
                case VertexInsertionOutcome.Successful:
                    newvertex.hash = this._triangularMesh.hash_vtx++;
                    newvertex.id = newvertex.hash;
                    this._triangularMesh.vertices.Add(newvertex.hash, newvertex);
                    if (this._triangularMesh.steinerleft > 0)
                    {
                        --this._triangularMesh.steinerleft;
                        break;
                    }

                    break;
                case VertexInsertionOutcome.Encroaching:
                    this._triangularMesh.UndoVertex();
                    break;
                case VertexInsertionOutcome.Violating:
                    break;
                default:
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
            this.TallyEncs();
            this.SplitEncSegs(false);
            if (this.behavior.MinAngle > 0.0 || this.behavior.VarArea || this.behavior.fixedArea ||
                this.behavior.Usertest)
            {
                this.TallyFaces();
                this._triangularMesh.checkquality = true;
                while (this.queue.Count > 0 && this._triangularMesh.steinerleft != 0)
                {
                    QualityViolatingTriangle badtri = this.queue.Dequeue();
                    this.SplitTriangle(badtri);
                    if (this.badsubsegs.Count > 0)
                    {
                        this.queue.Enqueue(badtri);
                        this.SplitEncSegs(true);
                    }
                }
            }

            if ( !this.behavior.ConformingDelaunay || this.badsubsegs.Count <= 0 ||
                this._triangularMesh.steinerleft != 0)
                return;
            Debug.LogWarning(
                "I ran out of Steiner points, but the mesh has encroached subsegments, and therefore might not be truly Delaunay. If the Delaunay property is important to you, try increasing the number of Steiner points.");
        }
    }
}