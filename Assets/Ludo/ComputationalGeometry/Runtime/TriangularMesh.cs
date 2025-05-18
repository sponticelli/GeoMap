using System;
using System.Collections.Generic;

namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Represents a triangular mesh, providing methods for triangulation, refinement, and manipulation.
    /// </summary>
    [Serializable]
    public class TriangularMesh
    {
        private Quality quality;
        private Stack<OrientedTriangle> flipstack;
        internal Dictionary<int, Triangle> triangles;
        internal Dictionary<int, Segment> subsegs;
        internal Dictionary<int, Vertex> vertices;
        internal int hash_vtx;
        internal int hash_seg;
        internal int hash_tri;
        internal List<Point> holes;
        internal List<RegionPointer> regions;
        internal AxisAlignedBoundingBox2D bounds;
        internal int invertices;
        internal int inelements;
        internal int insegments;
        internal int undeads;
        internal int edges;
        internal int mesh_dim;
        internal int nextras;
        internal int hullsize;
        internal int steinerleft;
        internal bool checksegments;
        internal bool checkquality;
        internal Vertex infvertex1;
        internal Vertex infvertex2;
        internal Vertex infvertex3;
        internal static Triangle dummytri;
        internal static Segment dummysub;
        internal TriangleLocator locator;
        internal TriangulationSettings behavior;
        internal VertexNumbering numbering;

        /// <summary>
        /// Gets the behavior settings for the mesh.
        /// </summary>
        public TriangulationSettings Behavior => behavior;

        /// <summary>
        /// Gets the bounding box of the mesh.
        /// </summary>
        public AxisAlignedBoundingBox2D Bounds => bounds;

        /// <summary>
        /// Gets the collection of vertices in the mesh.
        /// </summary>
        public ICollection<Vertex> Vertices => vertices.Values;

        /// <summary>
        /// Gets the list of holes in the mesh.
        /// </summary>
        public IList<Point> Holes => holes;

        /// <summary>
        /// Gets the collection of triangles in the mesh.
        /// </summary>
        public ICollection<Triangle> Triangles => triangles.Values;

        /// <summary>
        /// Gets the collection of segments in the mesh.
        /// </summary>
        public ICollection<Segment> Segments => subsegs.Values;

        /// <summary>
        /// Gets the collection of edges in the mesh.
        /// </summary>
        public IEnumerable<MeshEdge> Edges
        {
            get
            {
                MeshEdgeEnumerator e = new MeshEdgeEnumerator(this);
                while (e.MoveNext())
                    yield return e.Current;
            }
        }

        /// <summary>
        /// Gets the number of input points in the mesh.
        /// </summary>
        public int NumberOfInputPoints => invertices;

        /// <summary>
        /// Gets the number of edges in the mesh.
        /// </summary>
        public int NumberOfEdges => edges;

        /// <summary>
        /// Gets a value indicating whether the mesh represents a polygon.
        /// </summary>
        public bool IsPolygon => insegments > 0;

        /// <summary>
        /// Gets the current node numbering scheme of the mesh.
        /// </summary>
        public VertexNumbering CurrentNumbering => numbering;

        /// <summary>
        /// Initializes a new instance of the <see cref="TriangularMesh"/> class with default behavior settings.
        /// </summary>
        public TriangularMesh()
            : this(new TriangulationSettings())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TriangularMesh"/> class with the specified behavior settings.
        /// </summary>
        /// <param name="behavior">The behavior settings for the mesh.</param>
        public TriangularMesh(TriangulationSettings behavior)
        {
            this.behavior = behavior;
            behavior = new TriangulationSettings();
            vertices = new Dictionary<int, Vertex>();
            triangles = new Dictionary<int, Triangle>();
            subsegs = new Dictionary<int, Segment>();
            flipstack = new Stack<OrientedTriangle>();
            holes = new List<Point>();
            regions = new List<RegionPointer>();
            quality = new Quality(this);
            locator = new TriangleLocator(this);
            Primitives.ExactInit();
            if (dummytri != null)
                return;
            DummyInit();
        }

        /// <summary>
        /// Triangulates the specified meshInput geometry to create a mesh.
        /// </summary>
        /// <param name="meshInput">The meshInput geometry to triangulate.</param>
        public void Triangulate(MeshInputData meshInput)
        {
            ResetData();
            behavior.Poly = meshInput.HasSegments;
            if (!behavior.Poly)
            {
                behavior.VarArea = false;
                behavior.useRegions = false;
            }
            behavior.useRegions = meshInput.Regions.Count > 0;
            steinerleft = behavior.SteinerPoints;
            TransferNodes(meshInput);
            hullsize = Delaunay();
            infvertex1 = null;
            infvertex2 = null;
            infvertex3 = null;
            if (behavior.useSegments)
            {
                checksegments = true;
                FormSkeleton(meshInput);
            }
            if (behavior.Poly && triangles.Count > 0)
            {
                foreach (Point hole in meshInput.holes)
                    holes.Add(hole);
                foreach (RegionPointer region in meshInput.regions)
                    regions.Add(region);
                new MeshRegionProcessor(this).CarveHoles();
            }
            else
            {
                holes.Clear();
                regions.Clear();
            }
            if (behavior.Quality && triangles.Count > 0)
                quality.EnforceQuality();
            edges = (3 * triangles.Count + hullsize) / 2;
        }

        /// <summary>
        /// Refines the mesh by either halving the area of the largest triangle or using the default refinement.
        /// </summary>
        /// <param name="halfArea">If true, halves the area of the largest triangle; otherwise, uses the default refinement.</param>
        public void Refine(bool halfArea)
        {
            if (halfArea)
            {
                double num1 = 0.0;
                foreach (Triangle triangle in triangles.Values)
                {
                    double num2 = Math.Abs((triangle.vertices[2].x - triangle.vertices[0].x) * (triangle.vertices[1].y - triangle.vertices[0].y) - (triangle.vertices[1].x - triangle.vertices[0].x) * (triangle.vertices[2].y - triangle.vertices[0].y)) / 2.0;
                    if (num2 > num1)
                        num1 = num2;
                }
                Refine(num1 / 2.0);
            }
            else
                Refine();
        }

        /// <summary>
        /// Refines the mesh using the specified area constraint.
        /// </summary>
        /// <param name="areaConstraint">The maximum area constraint for triangles in the mesh.</param>
        public void Refine(double areaConstraint)
        {
            behavior.fixedArea = true;
            behavior.MaxArea = areaConstraint;
            Refine();
            behavior.fixedArea = false;
            behavior.MaxArea = -1.0;
        }

        /// <summary>
        /// Refines the mesh using the current behavior settings.
        /// </summary>
        public void Refine()
        {
            inelements = triangles.Count;
            invertices = vertices.Count;
            if (behavior.Poly)
                insegments = !behavior.useSegments ? hullsize : subsegs.Count;
            Reset();
            steinerleft = behavior.SteinerPoints;
            infvertex1 = null;
            infvertex2 = null;
            infvertex3 = null;
            if (behavior.useSegments)
                checksegments = true;
            if (triangles.Count > 0)
                quality.EnforceQuality();
            edges = (3 * triangles.Count + hullsize) / 2;
        }

        /// <summary>
        /// Smooths the mesh to improve triangle quality.
        /// </summary>
        public void Smooth()
        {
            numbering = VertexNumbering.None;
            new SimpleMeshSmoother(this).Smooth();
        }

        /// <summary>
        /// Renumbers the mesh nodes using the linear numbering scheme.
        /// </summary>
        public void Renumber() => Renumber(VertexNumbering.Linear);

        /// <summary>
        /// Renumbers the mesh nodes using the specified numbering scheme.
        /// </summary>
        /// <param name="num">The node numbering scheme to use.</param>
        public void Renumber(VertexNumbering num)
        {
            if (num == numbering)
                return;
            switch (num)
            {
                case VertexNumbering.Linear:
                    int num1 = 0;
                    using (Dictionary<int, Vertex>.ValueCollection.Enumerator enumerator = vertices.Values.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                            enumerator.Current.id = num1++;
                        break;
                    }
                case VertexNumbering.CuthillMcKee:
                    int[] numArray = new MeshBandwidthOptimizer().Renumber(this);
                    using (Dictionary<int, Vertex>.ValueCollection.Enumerator enumerator = vertices.Values.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            Vertex current = enumerator.Current;
                            current.id = numArray[current.id];
                        }
                        break;
                    }
            }
            numbering = num;
            int num2 = 0;
            foreach (Triangle triangle in triangles.Values)
                triangle.id = num2++;
        }

        /// <summary>
        /// Checks the mesh for consistency and Delaunay properties.
        /// </summary>
        /// <param name="isConsistent">When this method returns, contains a value indicating whether the mesh is consistent.</param>
        /// <param name="isDelaunay">When this method returns, contains a value indicating whether the mesh satisfies the Delaunay property.</param>
        public void Check(out bool isConsistent, out bool isDelaunay)
        {
            isConsistent = quality.CheckMesh();
            isDelaunay = quality.CheckDelaunay();
        }

        /// <summary>
        /// Performs Delaunay triangulation using the algorithm specified in the behavior settings.
        /// </summary>
        /// <returns>The number of triangles on the convex hull.</returns>
        private int Delaunay()
        {
            int num = behavior.Algorithm != TriangulationAlgorithm.Dwyer ?
                (behavior.Algorithm != TriangulationAlgorithm.SweepLine ?
                    new IncrementalDelaunayTriangulator().Triangulate(this) :
                    new SweepLine().Triangulate(this)) : new DwyerTriangulator().Triangulate(this);
            return triangles.Count != 0 ? num : 0;
        }

        /// <summary>
        /// Resets all data structures in the mesh.
        /// </summary>
        private void ResetData()
        {
            vertices.Clear();
            triangles.Clear();
            subsegs.Clear();
            holes.Clear();
            regions.Clear();
            hash_vtx = 0;
            hash_seg = 0;
            hash_tri = 0;
            flipstack.Clear();
            hullsize = 0;
            edges = 0;
            Reset();
            locator.Reset();
        }

        /// <summary>
        /// Resets the mesh state without clearing the data structures.
        /// </summary>
        private void Reset()
        {
            numbering = VertexNumbering.None;
            undeads = 0;
            checksegments = false;
            checkquality = false;
            Statistic.InCircleCount = 0L;
            Statistic.CounterClockwiseCount = 0L;
            Statistic.InCircleCountDecimal = 0L;
            Statistic.CounterClockwiseCountDecimal = 0L;
            Statistic.Orient3dCount = 0L;
            Statistic.HyperbolaCount = 0L;
            Statistic.CircleTopCount = 0L;
            Statistic.CircumcenterCount = 0L;
        }

        /// <summary>
        /// Initializes the dummy triangle and segment used as sentinels in the mesh.
        /// </summary>
        private void DummyInit()
        {
            dummytri = new Triangle();
            dummytri.hash = -1;
            dummytri.id = -1;
            dummytri.neighbors[0].triangle = dummytri;
            dummytri.neighbors[1].triangle = dummytri;
            dummytri.neighbors[2].triangle = dummytri;
            if (!behavior.useSegments)
                return;
            dummysub = new Segment();
            dummysub.hash = -1;
            dummysub.subsegs[0].seg = dummysub;
            dummysub.subsegs[1].seg = dummysub;
            dummytri.subsegs[0].seg = dummysub;
            dummytri.subsegs[1].seg = dummysub;
            dummytri.subsegs[2].seg = dummysub;
        }

        /// <summary>
        /// Transfers nodes from the input geometry to the mesh.
        /// </summary>
        /// <param name="data">The input geometry containing the nodes to transfer.</param>
        /// <exception cref="Exception">Thrown when the input has fewer than three vertices.</exception>
        private void TransferNodes(MeshInputData data)
        {
            List<Vertex> points = data.points;
            invertices = points.Count;
            mesh_dim = 2;
            if (invertices < 3)
            {
                throw new Exception("Input must have at least three input vertices.");
            }
            nextras = points[0].attributes == null ? 0 : points[0].attributes.Length;
            foreach (Vertex vertex in points)
            {
                vertex.hash = hash_vtx++;
                vertex.id = vertex.hash;
                vertices.Add(vertex.hash, vertex);
            }
            bounds = data.Bounds;
        }

        /// <summary>
        /// Creates a mapping from vertices to triangles in the mesh.
        /// </summary>
        internal void MakeVertexMap()
        {
            OrientedTriangle orientedTriangle = new OrientedTriangle();
            foreach (Triangle triangle in triangles.Values)
            {
                orientedTriangle.triangle = triangle;
                for (orientedTriangle.orient = 0; orientedTriangle.orient < 3; ++orientedTriangle.orient)
                    orientedTriangle.Org().tri = orientedTriangle;
            }
        }

        /// <summary>
        /// Creates a new triangle and adds it to the mesh.
        /// </summary>
        /// <param name="newotri">When this method returns, contains the newly created triangle.</param>
        internal void MakeTriangle(ref OrientedTriangle newotri)
        {
            Triangle triangle = new Triangle
            {
                hash = hash_tri++
            };
            triangle.id = triangle.hash;
            newotri.triangle = triangle;
            newotri.orient = 0;
            triangles.Add(triangle.hash, triangle);
        }

        /// <summary>
        /// Creates a new segment and adds it to the mesh.
        /// </summary>
        /// <param name="newsubseg">When this method returns, contains the newly created segment.</param>
        internal void MakeSegment(ref OrientedSubSegment newsubseg)
        {
            Segment segment = new Segment();
            segment.hash = hash_seg++;
            newsubseg.seg = segment;
            newsubseg.orient = 0;
            subsegs.Add(segment.hash, segment);
        }

        internal VertexInsertionOutcome InsertVertex(
            Vertex newvertex,
            ref OrientedTriangle searchtri,
            ref OrientedSubSegment splitseg,
            bool segmentflaws,
            bool triflaws)
        {
            OrientedTriangle otri1 = new OrientedTriangle();
            OrientedTriangle o2_1 = new OrientedTriangle();
            OrientedTriangle o2_2 = new OrientedTriangle();
            OrientedTriangle o2_3 = new OrientedTriangle();
            OrientedTriangle o2_4 = new OrientedTriangle();
            OrientedTriangle o2_5 = new OrientedTriangle();
            OrientedTriangle otri2 = new OrientedTriangle();
            OrientedTriangle otri3 = new OrientedTriangle();
            OrientedTriangle newotri = new OrientedTriangle();
            OrientedTriangle o2_6 = new OrientedTriangle();
            OrientedTriangle o2_7 = new OrientedTriangle();
            OrientedTriangle o2_8 = new OrientedTriangle();
            OrientedTriangle o2_9 = new OrientedTriangle();
            OrientedTriangle o2_10 = new OrientedTriangle();
            OrientedSubSegment os1 = new OrientedSubSegment();
            OrientedSubSegment os2 = new OrientedSubSegment();
            OrientedSubSegment os3 = new OrientedSubSegment();
            OrientedSubSegment os4 = new OrientedSubSegment();
            OrientedSubSegment os5 = new OrientedSubSegment();
            OrientedSubSegment osub1 = new OrientedSubSegment();
            OrientedSubSegment o2_11 = new OrientedSubSegment();
            OrientedSubSegment osub2 = new OrientedSubSegment();
            PointLocationResult pointLocationResult;
            if (splitseg.seg == null)
            {
                if (searchtri.triangle == dummytri)
                {
                    otri1.triangle = dummytri;
                    otri1.orient = 0;
                    otri1.SymSelf();
                    pointLocationResult = locator.Locate(newvertex, ref otri1);
                }
                else
                {
                    searchtri.Copy(ref otri1);
                    pointLocationResult = locator.PreciseLocate(newvertex, ref otri1, true);
                }
            }
            else
            {
                searchtri.Copy(ref otri1);
                pointLocationResult = PointLocationResult.OnEdge;
            }
            switch (pointLocationResult)
            {
                case PointLocationResult.OnEdge:
                case PointLocationResult.Outside:
                    if (checksegments && splitseg.seg == null)
                    {
                        otri1.SegPivot(ref os5);
                        if (os5.seg != dummysub)
                        {
                            if (segmentflaws)
                            {
                                bool flag = behavior.NoBisect != 2;
                                if (flag && behavior.NoBisect == 1)
                                {
                                    otri1.Sym(ref o2_10);
                                    flag = o2_10.triangle != dummytri;
                                }
                                if (flag)
                                    quality.AddBadSubseg(new NonconformingSubsegment
                                    {
                                        Encsubseg = os5,
                                        subSegmentOrigin = os5.Origin(),
                                        subSegmentDestination = os5.Destination()
                                    });
                            }
                            otri1.Copy(ref searchtri);
                            locator.Update(ref otri1);
                            return VertexInsertionOutcome.Violating;
                        }
                    }
                    otri1.Lprev(ref o2_3);
                    o2_3.Sym(ref o2_7);
                    otri1.Sym(ref o2_5);
                    bool flag1 = o2_5.triangle != dummytri;
                    if (flag1)
                    {
                        o2_5.LnextSelf();
                        o2_5.Sym(ref o2_9);
                        MakeTriangle(ref newotri);
                    }
                    else
                        ++hullsize;
                    MakeTriangle(ref otri3);
                    Vertex ptr1 = otri1.Org();
                    otri1.Dest();
                    Vertex ptr2 = otri1.Apex();
                    otri3.SetOrg(ptr2);
                    otri3.SetDest(ptr1);
                    otri3.SetApex(newvertex);
                    otri1.SetOrg(newvertex);
                    otri3.triangle.region = o2_3.triangle.region;
                    if (behavior.VarArea)
                        otri3.triangle.area = o2_3.triangle.area;
                    if (flag1)
                    {
                        Vertex ptr3 = o2_5.Dest();
                        newotri.SetOrg(ptr1);
                        newotri.SetDest(ptr3);
                        newotri.SetApex(newvertex);
                        o2_5.SetOrg(newvertex);
                        newotri.triangle.region = o2_5.triangle.region;
                        if (behavior.VarArea)
                            newotri.triangle.area = o2_5.triangle.area;
                    }
                    if (checksegments)
                    {
                        o2_3.SegPivot(ref os2);
                        if (os2.seg != dummysub)
                        {
                            o2_3.SegDissolve();
                            otri3.SegBond(ref os2);
                        }
                        if (flag1)
                        {
                            o2_5.SegPivot(ref os4);
                            if (os4.seg != dummysub)
                            {
                                o2_5.SegDissolve();
                                newotri.SegBond(ref os4);
                            }
                        }
                    }
                    otri3.Bond(ref o2_7);
                    otri3.LprevSelf();
                    otri3.Bond(ref o2_3);
                    otri3.LprevSelf();
                    if (flag1)
                    {
                        newotri.Bond(ref o2_9);
                        newotri.LnextSelf();
                        newotri.Bond(ref o2_5);
                        newotri.LnextSelf();
                        newotri.Bond(ref otri3);
                    }
                    if (splitseg.seg != null)
                    {
                        splitseg.SetDestination(newvertex);
                        Vertex ptr4 = splitseg.GetSegmentOrigin();
                        Vertex ptr5 = splitseg.GetSegmentDestination();
                        splitseg.SymSelf();
                        splitseg.Pivot(ref o2_11);
                        InsertSubseg(ref otri3, splitseg.seg.boundary);
                        otri3.SegPivot(ref osub2);
                        osub2.SetSegOrg(ptr4);
                        osub2.SetSegDest(ptr5);
                        splitseg.Bond(ref osub2);
                        osub2.SymSelf();
                        osub2.Bond(ref o2_11);
                        splitseg.SymSelf();
                        if (newvertex.mark == 0)
                            newvertex.mark = splitseg.seg.boundary;
                    }
                    if (checkquality)
                    {
                        flipstack.Clear();
                        flipstack.Push(new OrientedTriangle());
                        flipstack.Push(otri1);
                    }
                    otri1.LnextSelf();
                    break;
                case PointLocationResult.OnVertex:
                    otri1.Copy(ref searchtri);
                    locator.Update(ref otri1);
                    return VertexInsertionOutcome.Duplicate;
                default:
                    otri1.Lnext(ref o2_2);
                    otri1.Lprev(ref o2_3);
                    o2_2.Sym(ref o2_6);
                    o2_3.Sym(ref o2_7);
                    MakeTriangle(ref otri2);
                    MakeTriangle(ref otri3);
                    Vertex ptr6 = otri1.Org();
                    Vertex ptr7 = otri1.Dest();
                    Vertex ptr8 = otri1.Apex();
                    otri2.SetOrg(ptr7);
                    otri2.SetDest(ptr8);
                    otri2.SetApex(newvertex);
                    otri3.SetOrg(ptr8);
                    otri3.SetDest(ptr6);
                    otri3.SetApex(newvertex);
                    otri1.SetApex(newvertex);
                    otri2.triangle.region = otri1.triangle.region;
                    otri3.triangle.region = otri1.triangle.region;
                    if (behavior.VarArea)
                    {
                        double area = otri1.triangle.area;
                        otri2.triangle.area = area;
                        otri3.triangle.area = area;
                    }
                    if (checksegments)
                    {
                        o2_2.SegPivot(ref os1);
                        if (os1.seg != dummysub)
                        {
                            o2_2.SegDissolve();
                            otri2.SegBond(ref os1);
                        }
                        o2_3.SegPivot(ref os2);
                        if (os2.seg != dummysub)
                        {
                            o2_3.SegDissolve();
                            otri3.SegBond(ref os2);
                        }
                    }
                    otri2.Bond(ref o2_6);
                    otri3.Bond(ref o2_7);
                    otri2.LnextSelf();
                    otri3.LprevSelf();
                    otri2.Bond(ref otri3);
                    otri2.LnextSelf();
                    o2_2.Bond(ref otri2);
                    otri3.LprevSelf();
                    o2_3.Bond(ref otri3);
                    if (checkquality)
                    {
                        flipstack.Clear();
                        flipstack.Push(otri1);
                    }
                    break;
            }
            VertexInsertionOutcome vertexInsertionOutcome = VertexInsertionOutcome.Successful;
            Vertex vertex1 = otri1.Org();
            Vertex vertex2 = vertex1;
            Vertex vertex3 = otri1.Dest();
            while (true)
            {
                bool flag2;
                do
                {
                    flag2 = true;
                    if (checksegments)
                    {
                        otri1.SegPivot(ref osub1);
                        if (osub1.seg != dummysub)
                        {
                            flag2 = false;
                            if (segmentflaws && quality.CheckSeg4Encroach(ref osub1) > 0)
                                vertexInsertionOutcome = VertexInsertionOutcome.Encroaching;
                        }
                    }
                    if (flag2)
                    {
                        otri1.Sym(ref o2_1);
                        if (o2_1.triangle == dummytri)
                        {
                            flag2 = false;
                        }
                        else
                        {
                            Vertex vertex4 = o2_1.Apex();
                            flag2 = vertex3 == infvertex1 || vertex3 == infvertex2 || vertex3 == infvertex3 ? Primitives.CounterClockwise(newvertex, vertex2, vertex4) > 0.0 : (vertex2 == infvertex1 || vertex2 == infvertex2 || vertex2 == infvertex3 ? Primitives.CounterClockwise(vertex4, vertex3, newvertex) > 0.0 : !(vertex4 == infvertex1) && !(vertex4 == infvertex2) && !(vertex4 == infvertex3) && Primitives.InCircle(vertex3, newvertex, vertex2, vertex4) > 0.0);
                            if (flag2)
                            {
                                o2_1.Lprev(ref o2_4);
                                o2_4.Sym(ref o2_8);
                                o2_1.Lnext(ref o2_5);
                                o2_5.Sym(ref o2_9);
                                otri1.Lnext(ref o2_2);
                                o2_2.Sym(ref o2_6);
                                otri1.Lprev(ref o2_3);
                                o2_3.Sym(ref o2_7);
                                o2_4.Bond(ref o2_6);
                                o2_2.Bond(ref o2_7);
                                o2_3.Bond(ref o2_9);
                                o2_5.Bond(ref o2_8);
                                if (checksegments)
                                {
                                    o2_4.SegPivot(ref os3);
                                    o2_2.SegPivot(ref os1);
                                    o2_3.SegPivot(ref os2);
                                    o2_5.SegPivot(ref os4);
                                    if (os3.seg == dummysub)
                                        o2_5.SegDissolve();
                                    else
                                        o2_5.SegBond(ref os3);
                                    if (os1.seg == dummysub)
                                        o2_4.SegDissolve();
                                    else
                                        o2_4.SegBond(ref os1);
                                    if (os2.seg == dummysub)
                                        o2_2.SegDissolve();
                                    else
                                        o2_2.SegBond(ref os2);
                                    if (os4.seg == dummysub)
                                        o2_3.SegDissolve();
                                    else
                                        o2_3.SegBond(ref os4);
                                }
                                otri1.SetOrg(vertex4);
                                otri1.SetDest(newvertex);
                                otri1.SetApex(vertex2);
                                o2_1.SetOrg(newvertex);
                                o2_1.SetDest(vertex4);
                                o2_1.SetApex(vertex3);
                                int num1 = Math.Min(o2_1.triangle.region, otri1.triangle.region);
                                o2_1.triangle.region = num1;
                                otri1.triangle.region = num1;
                                if (behavior.VarArea)
                                {
                                    double num2 = o2_1.triangle.area <= 0.0 || otri1.triangle.area <= 0.0 ? -1.0 : 0.5 * (o2_1.triangle.area + otri1.triangle.area);
                                    o2_1.triangle.area = num2;
                                    otri1.triangle.area = num2;
                                }
                                if (checkquality)
                                    flipstack.Push(otri1);
                                otri1.LprevSelf();
                                vertex3 = vertex4;
                            }
                        }
                    }
                }
                while (flag2);
                if (triflaws)
                    quality.TestTriangle(ref otri1);
                otri1.LnextSelf();
                otri1.Sym(ref o2_10);
                if (!(vertex3 == vertex1) && o2_10.triangle != dummytri)
                {
                    o2_10.Lnext(ref otri1);
                    vertex2 = vertex3;
                    vertex3 = otri1.Dest();
                }
                else
                    break;
            }
            otri1.Lnext(ref searchtri);
            OrientedTriangle otri4 = new OrientedTriangle();
            otri1.Lnext(ref otri4);
            locator.Update(ref otri4);
            return vertexInsertionOutcome;
        }

        internal void InsertSubseg(ref OrientedTriangle tri, int subsegmark)
        {
            OrientedTriangle o2 = new OrientedTriangle();
            OrientedSubSegment orientedSubSegment = new OrientedSubSegment();
            Vertex ptr1 = tri.Org();
            Vertex ptr2 = tri.Dest();
            if (ptr1.mark == 0)
                ptr1.mark = subsegmark;
            if (ptr2.mark == 0)
                ptr2.mark = subsegmark;
            tri.SegPivot(ref orientedSubSegment);
            if (orientedSubSegment.seg == dummysub)
            {
                MakeSegment(ref orientedSubSegment);
                orientedSubSegment.SetOrigin(ptr2);
                orientedSubSegment.SetDestination(ptr1);
                orientedSubSegment.SetSegOrg(ptr2);
                orientedSubSegment.SetSegDest(ptr1);
                tri.SegBond(ref orientedSubSegment);
                tri.Sym(ref o2);
                orientedSubSegment.SymSelf();
                o2.SegBond(ref orientedSubSegment);
                orientedSubSegment.seg.boundary = subsegmark;
            }
            else
            {
                if (orientedSubSegment.seg.boundary != 0)
                    return;
                orientedSubSegment.seg.boundary = subsegmark;
            }
        }

        internal void Flip(ref OrientedTriangle flipedge)
        {
            OrientedTriangle o2_1 = new OrientedTriangle();
            OrientedTriangle o2_2 = new OrientedTriangle();
            OrientedTriangle o2_3 = new OrientedTriangle();
            OrientedTriangle o2_4 = new OrientedTriangle();
            OrientedTriangle o2_5 = new OrientedTriangle();
            OrientedTriangle o2_6 = new OrientedTriangle();
            OrientedTriangle o2_7 = new OrientedTriangle();
            OrientedTriangle o2_8 = new OrientedTriangle();
            OrientedTriangle o2_9 = new OrientedTriangle();
            OrientedSubSegment os1 = new OrientedSubSegment();
            OrientedSubSegment os2 = new OrientedSubSegment();
            OrientedSubSegment os3 = new OrientedSubSegment();
            OrientedSubSegment os4 = new OrientedSubSegment();
            Vertex ptr1 = flipedge.Org();
            Vertex ptr2 = flipedge.Dest();
            Vertex ptr3 = flipedge.Apex();
            flipedge.Sym(ref o2_5);
            Vertex ptr4 = o2_5.Apex();
            o2_5.Lprev(ref o2_3);
            o2_3.Sym(ref o2_8);
            o2_5.Lnext(ref o2_4);
            o2_4.Sym(ref o2_9);
            flipedge.Lnext(ref o2_1);
            o2_1.Sym(ref o2_6);
            flipedge.Lprev(ref o2_2);
            o2_2.Sym(ref o2_7);
            o2_3.Bond(ref o2_6);
            o2_1.Bond(ref o2_7);
            o2_2.Bond(ref o2_9);
            o2_4.Bond(ref o2_8);
            if (checksegments)
            {
                o2_3.SegPivot(ref os3);
                o2_1.SegPivot(ref os1);
                o2_2.SegPivot(ref os2);
                o2_4.SegPivot(ref os4);
                if (os3.seg == dummysub)
                    o2_4.SegDissolve();
                else
                    o2_4.SegBond(ref os3);
                if (os1.seg == dummysub)
                    o2_3.SegDissolve();
                else
                    o2_3.SegBond(ref os1);
                if (os2.seg == dummysub)
                    o2_1.SegDissolve();
                else
                    o2_1.SegBond(ref os2);
                if (os4.seg == dummysub)
                    o2_2.SegDissolve();
                else
                    o2_2.SegBond(ref os4);
            }
            flipedge.SetOrg(ptr4);
            flipedge.SetDest(ptr3);
            flipedge.SetApex(ptr1);
            o2_5.SetOrg(ptr3);
            o2_5.SetDest(ptr4);
            o2_5.SetApex(ptr2);
        }

        internal void Unflip(ref OrientedTriangle flipedge)
        {
            OrientedTriangle o2_1 = new OrientedTriangle();
            OrientedTriangle o2_2 = new OrientedTriangle();
            OrientedTriangle o2_3 = new OrientedTriangle();
            OrientedTriangle o2_4 = new OrientedTriangle();
            OrientedTriangle o2_5 = new OrientedTriangle();
            OrientedTriangle o2_6 = new OrientedTriangle();
            OrientedTriangle o2_7 = new OrientedTriangle();
            OrientedTriangle o2_8 = new OrientedTriangle();
            OrientedTriangle o2_9 = new OrientedTriangle();
            OrientedSubSegment os1 = new OrientedSubSegment();
            OrientedSubSegment os2 = new OrientedSubSegment();
            OrientedSubSegment os3 = new OrientedSubSegment();
            OrientedSubSegment os4 = new OrientedSubSegment();
            Vertex ptr1 = flipedge.Org();
            Vertex ptr2 = flipedge.Dest();
            Vertex ptr3 = flipedge.Apex();
            flipedge.Sym(ref o2_5);
            Vertex ptr4 = o2_5.Apex();
            o2_5.Lprev(ref o2_3);
            o2_3.Sym(ref o2_8);
            o2_5.Lnext(ref o2_4);
            o2_4.Sym(ref o2_9);
            flipedge.Lnext(ref o2_1);
            o2_1.Sym(ref o2_6);
            flipedge.Lprev(ref o2_2);
            o2_2.Sym(ref o2_7);
            o2_3.Bond(ref o2_9);
            o2_1.Bond(ref o2_8);
            o2_2.Bond(ref o2_6);
            o2_4.Bond(ref o2_7);
            if (checksegments)
            {
                o2_3.SegPivot(ref os3);
                o2_1.SegPivot(ref os1);
                o2_2.SegPivot(ref os2);
                o2_4.SegPivot(ref os4);
                if (os3.seg == dummysub)
                    o2_1.SegDissolve();
                else
                    o2_1.SegBond(ref os3);
                if (os1.seg == dummysub)
                    o2_2.SegDissolve();
                else
                    o2_2.SegBond(ref os1);
                if (os2.seg == dummysub)
                    o2_4.SegDissolve();
                else
                    o2_4.SegBond(ref os2);
                if (os4.seg == dummysub)
                    o2_3.SegDissolve();
                else
                    o2_3.SegBond(ref os4);
            }
            flipedge.SetOrg(ptr3);
            flipedge.SetDest(ptr4);
            flipedge.SetApex(ptr2);
            o2_5.SetOrg(ptr4);
            o2_5.SetDest(ptr3);
            o2_5.SetApex(ptr1);
        }

        private void TriangulatePolygon(
            OrientedTriangle firstedge,
            OrientedTriangle lastedge,
            int edgecount,
            bool doflip,
            bool triflaws)
        {
            OrientedTriangle orientedTriangle = new OrientedTriangle();
            OrientedTriangle firstedge1 = new OrientedTriangle();
            OrientedTriangle o2 = new OrientedTriangle();
            int num = 1;
            Vertex pa = lastedge.Apex();
            Vertex pb = firstedge.Dest();
            firstedge.Onext(ref firstedge1);
            Vertex pc = firstedge1.Dest();
            firstedge1.Copy(ref orientedTriangle);
            for (int index = 2; index <= edgecount - 2; ++index)
            {
                orientedTriangle.OnextSelf();
                Vertex pd = orientedTriangle.Dest();
                if (Primitives.InCircle(pa, pb, pc, pd) > 0.0)
                {
                    orientedTriangle.Copy(ref firstedge1);
                    pc = pd;
                    num = index;
                }
            }
            if (num > 1)
            {
                firstedge1.Oprev(ref o2);
                TriangulatePolygon(firstedge, o2, num + 1, true, triflaws);
            }
            if (num < edgecount - 2)
            {
                firstedge1.Sym(ref o2);
                TriangulatePolygon(firstedge1, lastedge, edgecount - num, true, triflaws);
                o2.Sym(ref firstedge1);
            }
            if (doflip)
            {
                Flip(ref firstedge1);
                if (triflaws)
                {
                    firstedge1.Sym(ref orientedTriangle);
                    quality.TestTriangle(ref orientedTriangle);
                }
            }
            firstedge1.Copy(ref lastedge);
        }

        internal void DeleteVertex(ref OrientedTriangle deltri)
        {
            OrientedTriangle o2_1 = new OrientedTriangle();
            OrientedTriangle o2_2 = new OrientedTriangle();
            OrientedTriangle o2_3 = new OrientedTriangle();
            OrientedTriangle o2_4 = new OrientedTriangle();
            OrientedTriangle o2_5 = new OrientedTriangle();
            OrientedTriangle o2_6 = new OrientedTriangle();
            OrientedTriangle o2_7 = new OrientedTriangle();
            OrientedTriangle o2_8 = new OrientedTriangle();
            OrientedSubSegment os1 = new OrientedSubSegment();
            OrientedSubSegment os2 = new OrientedSubSegment();
            VertexDealloc(deltri.Org());
            deltri.Onext(ref o2_1);
            int edgecount = 1;
            while (!deltri.Equal(o2_1))
            {
                ++edgecount;
                o2_1.OnextSelf();
            }
            if (edgecount > 3)
            {
                deltri.Onext(ref o2_2);
                deltri.Oprev(ref o2_3);
                TriangulatePolygon(o2_2, o2_3, edgecount, false, behavior.NoBisect == 0);
            }
            deltri.Lprev(ref o2_4);
            deltri.Dnext(ref o2_5);
            o2_5.Sym(ref o2_7);
            o2_4.Oprev(ref o2_6);
            o2_6.Sym(ref o2_8);
            deltri.Bond(ref o2_7);
            o2_4.Bond(ref o2_8);
            o2_5.SegPivot(ref os1);
            if (os1.seg != dummysub)
                deltri.SegBond(ref os1);
            o2_6.SegPivot(ref os2);
            if (os2.seg != dummysub)
                o2_4.SegBond(ref os2);
            Vertex ptr = o2_5.Org();
            deltri.SetOrg(ptr);
            if (behavior.NoBisect == 0)
                quality.TestTriangle(ref deltri);
            TriangleDealloc(o2_5.triangle);
            TriangleDealloc(o2_6.triangle);
        }

        internal void UndoVertex()
        {
            OrientedTriangle o2_1 = new OrientedTriangle();
            OrientedTriangle o2_2 = new OrientedTriangle();
            OrientedTriangle o2_3 = new OrientedTriangle();
            OrientedTriangle o2_4 = new OrientedTriangle();
            OrientedTriangle o2_5 = new OrientedTriangle();
            OrientedTriangle o2_6 = new OrientedTriangle();
            OrientedTriangle o2_7 = new OrientedTriangle();
            OrientedSubSegment os1 = new OrientedSubSegment();
            OrientedSubSegment os2 = new OrientedSubSegment();
            OrientedSubSegment os3 = new OrientedSubSegment();
            while (flipstack.Count > 0)
            {
                OrientedTriangle flipedge = flipstack.Pop();
                if (flipstack.Count == 0)
                {
                    flipedge.Dprev(ref o2_1);
                    o2_1.LnextSelf();
                    flipedge.Onext(ref o2_2);
                    o2_2.LprevSelf();
                    o2_1.Sym(ref o2_4);
                    o2_2.Sym(ref o2_5);
                    Vertex ptr = o2_1.Dest();
                    flipedge.SetApex(ptr);
                    flipedge.LnextSelf();
                    flipedge.Bond(ref o2_4);
                    o2_1.SegPivot(ref os1);
                    flipedge.SegBond(ref os1);
                    flipedge.LnextSelf();
                    flipedge.Bond(ref o2_5);
                    o2_2.SegPivot(ref os2);
                    flipedge.SegBond(ref os2);
                    TriangleDealloc(o2_1.triangle);
                    TriangleDealloc(o2_2.triangle);
                }
                else if (flipstack.Peek().triangle == null)
                {
                    flipedge.Lprev(ref o2_7);
                    o2_7.Sym(ref o2_2);
                    o2_2.LnextSelf();
                    o2_2.Sym(ref o2_5);
                    Vertex ptr = o2_2.Dest();
                    flipedge.SetOrg(ptr);
                    o2_7.Bond(ref o2_5);
                    o2_2.SegPivot(ref os2);
                    o2_7.SegBond(ref os2);
                    TriangleDealloc(o2_2.triangle);
                    flipedge.Sym(ref o2_7);
                    if (o2_7.triangle != dummytri)
                    {
                        o2_7.LnextSelf();
                        o2_7.Dnext(ref o2_3);
                        o2_3.Sym(ref o2_6);
                        o2_7.SetOrg(ptr);
                        o2_7.Bond(ref o2_6);
                        o2_3.SegPivot(ref os3);
                        o2_7.SegBond(ref os3);
                        TriangleDealloc(o2_3.triangle);
                    }
                    flipstack.Clear();
                }
                else
                    Unflip(ref flipedge);
            }
        }

        private PointPathOrientation FindDirection(ref OrientedTriangle searchtri, Vertex searchpoint)
        {
            OrientedTriangle o2 = new OrientedTriangle();
            Vertex vertex = searchtri.Org();
            Vertex pc1 = searchtri.Dest();
            Vertex pc2 = searchtri.Apex();
            double num1 = Primitives.CounterClockwise(searchpoint, vertex, pc2);
            bool flag1 = num1 > 0.0;
            double num2 = Primitives.CounterClockwise(vertex, searchpoint, pc1);
            bool flag2 = num2 > 0.0;
            if (flag1 & flag2)
            {
                searchtri.Onext(ref o2);
                if (o2.triangle == dummytri)
                    flag1 = false;
                else
                    flag2 = false;
            }
            for (; flag1; flag1 = num1 > 0.0)
            {
                searchtri.OnextSelf();
                if (searchtri.triangle == dummytri)
                {
                    throw new Exception("Unable to find a triangle on path.");
                }
                Vertex pc3 = searchtri.Apex();
                num2 = num1;
                num1 = Primitives.CounterClockwise(searchpoint, vertex, pc3);
            }
            for (; flag2; flag2 = num2 > 0.0)
            {
                searchtri.OprevSelf();
                if (searchtri.triangle == dummytri)
                {
                    throw new Exception("Unable to find a triangle on path.");
                }
                Vertex pc4 = searchtri.Dest();
                num1 = num2;
                num2 = Primitives.CounterClockwise(vertex, searchpoint, pc4);
            }
            if (num1 == 0.0)
                return PointPathOrientation.Leftcollinear;
            return num2 == 0.0 ? PointPathOrientation.Rightcollinear : PointPathOrientation.Within;
        }

        private void SegmentIntersection(ref OrientedTriangle splittri, ref OrientedSubSegment splitsubseg, Vertex endpoint2)
        {
            OrientedSubSegment o2 = new OrientedSubSegment();
            Vertex searchpoint = splittri.Apex();
            Vertex vertex1 = splittri.Org();
            Vertex vertex2 = splittri.Dest();
            double num1 = vertex2.x - vertex1.x;
            double num2 = vertex2.y - vertex1.y;
            double num3 = endpoint2.x - searchpoint.x;
            double num4 = endpoint2.y - searchpoint.y;
            double num5 = vertex1.x - endpoint2.x;
            double num6 = vertex1.y - endpoint2.y;
            double num7 = num3;
            double num8 = num2 * num7 - num1 * num4;
            if (num8 == 0.0)
            {
                throw new Exception("Attempt to find intersection of parallel segments.");
            }
            double num9 = (num4 * num5 - num3 * num6) / num8;
            Vertex vertex3 = new Vertex(vertex1.x + num9 * (vertex2.x - vertex1.x), vertex1.y + num9 * (vertex2.y - vertex1.y), splitsubseg.seg.boundary, nextras);
            vertex3.hash = hash_vtx++;
            vertex3.id = vertex3.hash;
            for (int index = 0; index < nextras; ++index)
                vertex3.attributes[index] = vertex1.attributes[index] + num9 * (vertex2.attributes[index] - vertex1.attributes[index]);
            vertices.Add(vertex3.hash, vertex3);
            if (InsertVertex(vertex3, ref splittri, ref splitsubseg, false, false) != VertexInsertionOutcome.Successful)
            {
                throw new Exception("Failure to split a segment.");
            }
            vertex3.tri = splittri;
            if (steinerleft > 0)
                --steinerleft;
            splitsubseg.SymSelf();
            splitsubseg.Pivot(ref o2);
            splitsubseg.Dissolve();
            o2.Dissolve();
            do
            {
                splitsubseg.SetSegOrg(vertex3);
                splitsubseg.NextSelf();
            }
            while (splitsubseg.seg != dummysub);
            do
            {
                o2.SetSegOrg(vertex3);
                o2.NextSelf();
            }
            while (o2.seg != dummysub);
            int direction = (int) FindDirection(ref splittri, searchpoint);
            Vertex vertex4 = splittri.Dest();
            Vertex vertex5 = splittri.Apex();
            if (vertex5.x == searchpoint.x && vertex5.y == searchpoint.y)
                splittri.OnextSelf();
            else if (vertex4.x != searchpoint.x || vertex4.y != searchpoint.y)
            {
                throw new Exception("Topological inconsistency after splitting a segment.");
            }
        }

        private bool ScoutSegment(ref OrientedTriangle searchtri, Vertex endpoint2, int newmark)
        {
            OrientedTriangle orientedTriangle = new OrientedTriangle();
            OrientedSubSegment orientedSubSegment = new OrientedSubSegment();
            PointPathOrientation direction = FindDirection(ref searchtri, endpoint2);
            Vertex vertex1 = searchtri.Dest();
            Vertex vertex2 = searchtri.Apex();
            if (vertex2.x == endpoint2.x && vertex2.y == endpoint2.y || vertex1.x == endpoint2.x && vertex1.y == endpoint2.y)
            {
                if (vertex2.x == endpoint2.x && vertex2.y == endpoint2.y)
                    searchtri.LprevSelf();
                InsertSubseg(ref searchtri, newmark);
                return true;
            }
            if (direction == PointPathOrientation.Leftcollinear)
            {
                searchtri.LprevSelf();
                InsertSubseg(ref searchtri, newmark);
                return ScoutSegment(ref searchtri, endpoint2, newmark);
            }
            if (direction == PointPathOrientation.Rightcollinear)
            {
                InsertSubseg(ref searchtri, newmark);
                searchtri.LnextSelf();
                return ScoutSegment(ref searchtri, endpoint2, newmark);
            }
            searchtri.Lnext(ref orientedTriangle);
            orientedTriangle.SegPivot(ref orientedSubSegment);
            if (orientedSubSegment.seg == dummysub)
                return false;
            SegmentIntersection(ref orientedTriangle, ref orientedSubSegment, endpoint2);
            orientedTriangle.Copy(ref searchtri);
            InsertSubseg(ref searchtri, newmark);
            return ScoutSegment(ref searchtri, endpoint2, newmark);
        }

        private void DelaunayFixup(ref OrientedTriangle fixuptri, bool leftside)
        {
            OrientedTriangle otri1 = new OrientedTriangle();
            OrientedTriangle otri2 = new OrientedTriangle();
            OrientedSubSegment os = new OrientedSubSegment();
            fixuptri.Lnext(ref otri1);
            otri1.Sym(ref otri2);
            if (otri2.triangle == dummytri)
                return;
            otri1.SegPivot(ref os);
            if (os.seg != dummysub)
                return;
            Vertex vertex1 = otri1.Apex();
            Vertex vertex2 = otri1.Org();
            Vertex vertex3 = otri1.Dest();
            Vertex vertex4 = otri2.Apex();
            if (leftside)
            {
                if (Primitives.CounterClockwise(vertex1, vertex2, vertex4) <= 0.0)
                    return;
            }
            else if (Primitives.CounterClockwise(vertex4, vertex3, vertex1) <= 0.0)
                return;
            if (Primitives.CounterClockwise(vertex3, vertex2, vertex4) > 0.0 && Primitives.InCircle(vertex2, vertex4, vertex3, vertex1) <= 0.0)
                return;
            Flip(ref otri1);
            fixuptri.LprevSelf();
            DelaunayFixup(ref fixuptri, leftside);
            DelaunayFixup(ref otri2, leftside);
        }

        private void ConstrainedEdge(ref OrientedTriangle starttri, Vertex endpoint2, int newmark)
        {
            OrientedTriangle otri1 = new OrientedTriangle();
            OrientedTriangle otri2 = new OrientedTriangle();
            OrientedSubSegment orientedSubSegment = new OrientedSubSegment();
            Vertex pa = starttri.Org();
            starttri.Lnext(ref otri1);
            Flip(ref otri1);
            bool flag1 = false;
            bool flag2 = false;
            do
            {
                Vertex pc = otri1.Org();
                if (pc.x == endpoint2.x && pc.y == endpoint2.y)
                {
                    otri1.Oprev(ref otri2);
                    DelaunayFixup(ref otri1, false);
                    DelaunayFixup(ref otri2, true);
                    flag2 = true;
                }
                else
                {
                    double num = Primitives.CounterClockwise(pa, endpoint2, pc);
                    if (num == 0.0)
                    {
                        flag1 = true;
                        otri1.Oprev(ref otri2);
                        DelaunayFixup(ref otri1, false);
                        DelaunayFixup(ref otri2, true);
                        flag2 = true;
                    }
                    else
                    {
                        if (num > 0.0)
                        {
                            otri1.Oprev(ref otri2);
                            DelaunayFixup(ref otri2, true);
                            otri1.LprevSelf();
                        }
                        else
                        {
                            DelaunayFixup(ref otri1, false);
                            otri1.OprevSelf();
                        }
                        otri1.SegPivot(ref orientedSubSegment);
                        if (orientedSubSegment.seg == dummysub)
                        {
                            Flip(ref otri1);
                        }
                        else
                        {
                            flag1 = true;
                            SegmentIntersection(ref otri1, ref orientedSubSegment, endpoint2);
                            flag2 = true;
                        }
                    }
                }
            }
            while (!flag2);
            InsertSubseg(ref otri1, newmark);
            if (!flag1 || ScoutSegment(ref otri1, endpoint2, newmark))
                return;
            ConstrainedEdge(ref otri1, endpoint2, newmark);
        }

        private void InsertSegment(Vertex endpoint1, Vertex endpoint2, int newmark)
        {
            OrientedTriangle otri1 = new OrientedTriangle();
            OrientedTriangle otri2 = new OrientedTriangle();
            Vertex vertex1 = null;
            OrientedTriangle tri1 = endpoint1.tri;
            if (tri1.triangle != null)
                vertex1 = tri1.Org();
            if (vertex1 != endpoint1)
            {
                tri1.triangle = dummytri;
                tri1.orient = 0;
                tri1.SymSelf();
                if (locator.Locate(endpoint1, ref tri1) != PointLocationResult.OnVertex)
                {
                    throw new Exception("Unable to locate PSLG vertex in triangulation.");
                }
            }
            locator.Update(ref tri1);
            if (ScoutSegment(ref tri1, endpoint2, newmark))
                return;
            endpoint1 = tri1.Org();
            Vertex vertex2 = null;
            OrientedTriangle tri2 = endpoint2.tri;
            if (tri2.triangle != null)
                vertex2 = tri2.Org();
            if (vertex2 != endpoint2)
            {
                tri2.triangle = dummytri;
                tri2.orient = 0;
                tri2.SymSelf();
                if (locator.Locate(endpoint2, ref tri2) != PointLocationResult.OnVertex)
                {
                    throw new Exception("Unable to locate PSLG vertex in triangulation.");
                }
            }
            locator.Update(ref tri2);
            if (ScoutSegment(ref tri2, endpoint1, newmark))
                return;
            endpoint2 = tri2.Org();
            ConstrainedEdge(ref tri1, endpoint2, newmark);
        }

        private void MarkHull()
        {
            OrientedTriangle orientedTriangle = new OrientedTriangle();
            OrientedTriangle o2_1 = new OrientedTriangle();
            OrientedTriangle o2_2 = new OrientedTriangle();
            orientedTriangle.triangle = dummytri;
            orientedTriangle.orient = 0;
            orientedTriangle.SymSelf();
            orientedTriangle.Copy(ref o2_2);
            do
            {
                InsertSubseg(ref orientedTriangle, 1);
                orientedTriangle.LnextSelf();
                orientedTriangle.Oprev(ref o2_1);
                while (o2_1.triangle != dummytri)
                {
                    o2_1.Copy(ref orientedTriangle);
                    orientedTriangle.Oprev(ref o2_1);
                }
            }
            while (!orientedTriangle.Equal(o2_2));
        }

        private void FormSkeleton(MeshInputData meshInput)
        {
            insegments = 0;
            if (behavior.Poly)
            {
                if (triangles.Count == 0)
                    return;
                if (meshInput.HasSegments)
                    MakeVertexMap();
                foreach (MeshEdge segment in meshInput.segments)
                {
                    ++insegments;
                    int p0 = segment.P0;
                    int p1 = segment.P1;
                    int boundary = segment.Boundary;
                    if (p0 < 0 || p0 >= invertices)
                    {

                    }
                    else if (p1 < 0 || p1 >= invertices)
                    {

                    }
                    else
                    {
                        Vertex vertex1 = vertices[p0];
                        Vertex vertex2 = vertices[p1];
                        if (vertex1.x == vertex2.x && vertex1.y == vertex2.y)
                        {

                        }
                        else
                            InsertSegment(vertex1, vertex2, boundary);
                    }
                }
            }
            if (!behavior.Convex && behavior.Poly)
                return;
            MarkHull();
        }

        internal void TriangleDealloc(Triangle dyingtriangle)
        {
            OrientedTriangle.Kill(dyingtriangle);
            triangles.Remove(dyingtriangle.hash);
        }

        internal void VertexDealloc(Vertex dyingvertex)
        {
            dyingvertex.type = VertexType.DeadVertex;
            vertices.Remove(dyingvertex.hash);
        }

        internal void SubsegDealloc(Segment dyingsubseg)
        {
            OrientedSubSegment.Kill(dyingsubseg);
            subsegs.Remove(dyingsubseg.hash);
        }
    }
}