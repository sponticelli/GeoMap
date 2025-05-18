using System;

namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Provides statistical information about a triangular mesh and tracks performance metrics.
    /// </summary>
    /// <remarks>
    /// The Statistic class collects and computes various metrics about a mesh, including:
    /// - Geometric properties (edge lengths, angles, areas, aspect ratios)
    /// - Mesh complexity (number of vertices, triangles, edges)
    /// - Performance counters for geometric predicates
    ///
    /// This information is useful for analyzing mesh quality, diagnosing issues,
    /// and evaluating the performance of the triangulation algorithms.
    /// </remarks>
    [System.Serializable]
    public class Statistic
    {
        /// <summary>
        /// Counter for the number of in-circle tests performed using double precision.
        /// </summary>
        public static long InCircleCount = 0;

        /// <summary>
        /// Counter for the number of in-circle tests performed using decimal precision.
        /// </summary>
        public static long InCircleCountDecimal = 0;

        /// <summary>
        /// Counter for the number of counterclockwise orientation tests performed using double precision.
        /// </summary>
        public static long CounterClockwiseCount = 0;

        /// <summary>
        /// Counter for the number of counterclockwise orientation tests performed using decimal precision.
        /// </summary>
        public static long CounterClockwiseCountDecimal = 0;

        /// <summary>
        /// Counter for the number of 3D orientation tests performed.
        /// </summary>
        public static long Orient3dCount = 0;

        /// <summary>
        /// Counter for the number of hyperbola intersection calculations performed.
        /// </summary>
        public static long HyperbolaCount = 0;

        /// <summary>
        /// Counter for the number of circumcenter calculations performed.
        /// </summary>
        public static long CircumcenterCount = 0;

        /// <summary>
        /// Counter for the number of circle top calculations performed.
        /// </summary>
        public static long CircleTopCount = 0;

        /// <summary>
        /// Counter for the number of vertex relocations performed.
        /// </summary>
        public static long RelocationCount = 0;
        /// <summary>
        /// The length of the shortest edge in the mesh.
        /// </summary>
        private double minEdge;

        /// <summary>
        /// The length of the longest edge in the mesh.
        /// </summary>
        private double maxEdge;

        /// <summary>
        /// The shortest altitude (height) of any triangle in the mesh.
        /// </summary>
        private double minAspect;

        /// <summary>
        /// The largest aspect ratio of any triangle in the mesh.
        /// </summary>
        private double maxAspect;

        /// <summary>
        /// The area of the smallest triangle in the mesh.
        /// </summary>
        private double minArea;

        /// <summary>
        /// The area of the largest triangle in the mesh.
        /// </summary>
        private double maxArea;

        /// <summary>
        /// The smallest angle (in degrees) of any triangle in the mesh.
        /// </summary>
        private double minAngle;

        /// <summary>
        /// The largest angle (in degrees) of any triangle in the mesh.
        /// </summary>
        private double maxAngle;

        /// <summary>
        /// The number of vertices in the input geometry.
        /// </summary>
        private int inVetrices;

        /// <summary>
        /// The number of triangles in the input geometry.
        /// </summary>
        private int inTriangles;

        /// <summary>
        /// The number of segments in the input geometry.
        /// </summary>
        private int inSegments;

        /// <summary>
        /// The number of holes in the input geometry.
        /// </summary>
        private int inHoles;

        /// <summary>
        /// The number of vertices in the output mesh.
        /// </summary>
        private int outVertices;

        /// <summary>
        /// The number of triangles in the output mesh.
        /// </summary>
        private int outTriangles;

        /// <summary>
        /// The number of edges in the output mesh.
        /// </summary>
        private int outEdges;

        /// <summary>
        /// The number of boundary edges in the output mesh.
        /// </summary>
        private int boundaryEdges;

        /// <summary>
        /// The number of interior boundary edges in the output mesh.
        /// </summary>
        private int intBoundaryEdges;

        /// <summary>
        /// The number of constrained edges in the output mesh.
        /// </summary>
        private int constrainedEdges;

        /// <summary>
        /// Histogram of angles in the mesh.
        /// </summary>
        private int[] angleTable;

        /// <summary>
        /// Histogram of minimum angles per triangle.
        /// </summary>
        private int[] minAngles;

        /// <summary>
        /// Histogram of maximum angles per triangle.
        /// </summary>
        private int[] maxAngles;

        /// <summary>
        /// Lookup table for adding 1 modulo 3 to an index.
        /// </summary>
        /// <remarks>
        /// This is used to efficiently cycle through the three vertices of a triangle
        /// in a clockwise direction.
        /// </remarks>
        private static readonly int[] plus1Mod3 = new int[3]
        {
            1,
            2,
            0
        };

        /// <summary>
        /// Lookup table for subtracting 1 modulo 3 from an index.
        /// </summary>
        /// <remarks>
        /// This is used to efficiently cycle through the three vertices of a triangle
        /// in a counterclockwise direction.
        /// </remarks>
        private static readonly int[] minus1Mod3 = new int[3]
        {
            2,
            0,
            1
        };

        /// <summary>
        /// Gets the length of the shortest edge in the mesh.
        /// </summary>
        public double ShortestEdge => this.minEdge;

        /// <summary>
        /// Gets the length of the longest edge in the mesh.
        /// </summary>
        public double LongestEdge => this.maxEdge;

        /// <summary>
        /// Gets the shortest altitude (height) of any triangle in the mesh.
        /// </summary>
        public double ShortestAltitude => this.minAspect;

        /// <summary>
        /// Gets the largest aspect ratio of any triangle in the mesh.
        /// </summary>
        public double LargestAspectRatio => this.maxAspect;

        /// <summary>
        /// Gets the area of the smallest triangle in the mesh.
        /// </summary>
        public double SmallestArea => this.minArea;

        /// <summary>
        /// Gets the area of the largest triangle in the mesh.
        /// </summary>
        public double LargestArea => this.maxArea;

        /// <summary>
        /// Gets the smallest angle (in degrees) of any triangle in the mesh.
        /// </summary>
        public double SmallestAngle => this.minAngle;

        /// <summary>
        /// Gets the largest angle (in degrees) of any triangle in the mesh.
        /// </summary>
        public double LargestAngle => this.maxAngle;

        /// <summary>
        /// Gets the number of vertices in the input geometry.
        /// </summary>
        public int InputVertices => this.inVetrices;

        /// <summary>
        /// Gets the number of triangles in the input geometry.
        /// </summary>
        public int InputTriangles => this.inTriangles;

        /// <summary>
        /// Gets the number of segments in the input geometry.
        /// </summary>
        public int InputSegments => this.inSegments;

        /// <summary>
        /// Gets the number of holes in the input geometry.
        /// </summary>
        public int InputHoles => this.inHoles;

        /// <summary>
        /// Gets the number of vertices in the output mesh.
        /// </summary>
        public int Vertices => this.outVertices;

        /// <summary>
        /// Gets the number of triangles in the output mesh.
        /// </summary>
        public int Triangles => this.outTriangles;

        /// <summary>
        /// Gets the number of edges in the output mesh.
        /// </summary>
        public int Edges => this.outEdges;

        /// <summary>
        /// Gets the number of boundary edges in the output mesh.
        /// </summary>
        public int BoundaryEdges => this.boundaryEdges;

        /// <summary>
        /// Gets the number of interior boundary edges in the output mesh.
        /// </summary>
        public int InteriorBoundaryEdges => this.intBoundaryEdges;

        /// <summary>
        /// Gets the number of constrained edges in the output mesh.
        /// </summary>
        public int ConstrainedEdges => this.constrainedEdges;

        /// <summary>
        /// Gets the histogram of angles in the mesh.
        /// </summary>
        public int[] AngleHistogram => this.angleTable;

        /// <summary>
        /// Gets the histogram of minimum angles per triangle.
        /// </summary>
        public int[] MinAngleHistogram => this.minAngles;

        /// <summary>
        /// Gets the histogram of maximum angles per triangle.
        /// </summary>
        public int[] MaxAngleHistogram => this.maxAngles;

        /// <summary>
        /// Computes a histogram of aspect ratios for the triangles in the mesh.
        /// </summary>
        /// <param name="triangularMesh">The mesh to analyze.</param>
        /// <remarks>
        /// This method calculates the aspect ratio of each triangle in the mesh and
        /// categorizes them into bins based on predefined thresholds. The aspect ratio
        /// is defined as the ratio of the longest edge squared to the triangle area.
        ///
        /// The resulting histogram can be used to analyze the distribution of triangle
        /// shapes in the mesh, which is an important quality metric.
        /// </remarks>
        private void GetAspectHistogram(TriangularMesh triangularMesh)
        {
            int[] numArray1 = new int[16 /*0x10*/];
            double[] numArray2 = new double[16 /*0x10*/]
            {
                1.5,
                2.0,
                2.5,
                3.0,
                4.0,
                6.0,
                10.0,
                15.0,
                25.0,
                50.0,
                100.0,
                300.0,
                1000.0,
                10000.0,
                100000.0,
                0.0
            };
            Otri otri = new Otri();
            Vertex[] vertexArray = new Vertex[3];
            double[] numArray3 = new double[3];
            double[] numArray4 = new double[3];
            double[] numArray5 = new double[3];
            otri.orient = 0;
            foreach (Triangle triangle in triangularMesh.triangles.Values)
            {
                otri.triangle = triangle;
                vertexArray[0] = otri.Org();
                vertexArray[1] = otri.Dest();
                vertexArray[2] = otri.Apex();
                double num1 = 0.0;
                for (int index1 = 0; index1 < 3; ++index1)
                {
                    int index2 = Statistic.plus1Mod3[index1];
                    int index3 = Statistic.minus1Mod3[index1];
                    numArray3[index1] = vertexArray[index2].x - vertexArray[index3].x;
                    numArray4[index1] = vertexArray[index2].y - vertexArray[index3].y;
                    numArray5[index1] = numArray3[index1] * numArray3[index1] + numArray4[index1] * numArray4[index1];
                    if (numArray5[index1] > num1)
                        num1 = numArray5[index1];
                }

                double num2 = Math.Abs((vertexArray[2].x - vertexArray[0].x) * (vertexArray[1].y - vertexArray[0].y) -
                                       (vertexArray[1].x - vertexArray[0].x) * (vertexArray[2].y - vertexArray[0].y)) /
                              2.0;
                double num3 = num2 * num2 / num1;
                double num4 = num1 / num3;
                int index = 0;
                while (num4 > numArray2[index] * numArray2[index] && index < 15)
                    ++index;
                ++numArray1[index];
            }
        }

        /// <summary>
        /// Updates the statistical information based on the current state of the mesh.
        /// </summary>
        /// <param name="triangularMesh">The mesh to analyze.</param>
        /// <param name="sampleDegrees">The number of bins to use for angle histograms.</param>
        /// <remarks>
        /// This method computes various statistical measures about the mesh, including:
        /// - Edge lengths (min/max)
        /// - Triangle areas (min/max)
        /// - Triangle angles (min/max and histograms)
        /// - Aspect ratios (min/max)
        /// - Counts of various mesh elements
        ///
        /// The sampleDegrees parameter determines the resolution of the angle histograms.
        /// A higher value provides more detailed information but requires more memory.
        ///
        /// Note that this method overrides the sampleDegrees parameter with a fixed value of 60,
        /// which provides a reasonable balance between detail and efficiency.
        /// </remarks>
        public void Update(TriangularMesh triangularMesh, int sampleDegrees)
        {
            this.inVetrices = triangularMesh.invertices;
            this.inTriangles = triangularMesh.inelements;
            this.inSegments = triangularMesh.insegments;
            this.inHoles = triangularMesh.holes.Count;
            this.outVertices = triangularMesh.vertices.Count - triangularMesh.undeads;
            this.outTriangles = triangularMesh.triangles.Count;
            this.outEdges = triangularMesh.edges;
            this.boundaryEdges = triangularMesh.hullsize;
            this.intBoundaryEdges = triangularMesh.subsegs.Count - triangularMesh.hullsize;
            this.constrainedEdges = triangularMesh.subsegs.Count;
            Point[] pointArray = new Point[3];
            sampleDegrees = 60;
            double[] numArray1 = new double[sampleDegrees / 2 - 1];
            double[] numArray2 = new double[3];
            double[] numArray3 = new double[3];
            double[] numArray4 = new double[3];
            double num1 = Math.PI / (double)sampleDegrees;
            double num2 = 180.0 / Math.PI;
            this.angleTable = new int[sampleDegrees];
            this.minAngles = new int[sampleDegrees];
            this.maxAngles = new int[sampleDegrees];
            for (int index = 0; index < sampleDegrees / 2 - 1; ++index)
            {
                numArray1[index] = Math.Cos(num1 * (double)(index + 1));
                numArray1[index] = numArray1[index] * numArray1[index];
            }

            for (int index = 0; index < sampleDegrees; ++index)
                this.angleTable[index] = 0;
            this.minAspect = triangularMesh.bounds.Width + triangularMesh.bounds.Height;
            this.minAspect *= this.minAspect;
            this.maxAspect = 0.0;
            this.minEdge = this.minAspect;
            this.maxEdge = 0.0;
            this.minArea = this.minAspect;
            this.maxArea = 0.0;
            this.minAngle = 0.0;
            this.maxAngle = 2.0;
            bool flag1 = true;
            bool flag2 = true;
            foreach (Triangle triangle in triangularMesh.triangles.Values)
            {
                double num3 = 0.0;
                double num4 = 1.0;
                pointArray[0] = (Point)triangle.vertices[0];
                pointArray[1] = (Point)triangle.vertices[1];
                pointArray[2] = (Point)triangle.vertices[2];
                double num5 = 0.0;
                for (int index1 = 0; index1 < 3; ++index1)
                {
                    int index2 = Statistic.plus1Mod3[index1];
                    int index3 = Statistic.minus1Mod3[index1];
                    numArray2[index1] = pointArray[index2].X - pointArray[index3].X;
                    numArray3[index1] = pointArray[index2].Y - pointArray[index3].Y;
                    numArray4[index1] = numArray2[index1] * numArray2[index1] + numArray3[index1] * numArray3[index1];
                    if (numArray4[index1] > num5)
                        num5 = numArray4[index1];
                    if (numArray4[index1] > this.maxEdge)
                        this.maxEdge = numArray4[index1];
                    if (numArray4[index1] < this.minEdge)
                        this.minEdge = numArray4[index1];
                }

                double num6 = Math.Abs((pointArray[2].X - pointArray[0].X) * (pointArray[1].Y - pointArray[0].Y) -
                                       (pointArray[1].X - pointArray[0].X) * (pointArray[2].Y - pointArray[0].Y));
                if (num6 < this.minArea)
                    this.minArea = num6;
                if (num6 > this.maxArea)
                    this.maxArea = num6;
                double num7 = num6 * num6 / num5;
                if (num7 < this.minAspect)
                    this.minAspect = num7;
                double num8 = num5 / num7;
                if (num8 > this.maxAspect)
                    this.maxAspect = num8;
                for (int index4 = 0; index4 < 3; ++index4)
                {
                    int index5 = Statistic.plus1Mod3[index4];
                    int index6 = Statistic.minus1Mod3[index4];
                    double num9 = numArray2[index5] * numArray2[index6] + numArray3[index5] * numArray3[index6];
                    double num10 = num9 * num9 / (numArray4[index5] * numArray4[index6]);
                    int index7 = sampleDegrees / 2 - 1;
                    for (int index8 = index7 - 1; index8 >= 0; --index8)
                    {
                        if (num10 > numArray1[index8])
                            index7 = index8;
                    }

                    if (num9 <= 0.0)
                    {
                        ++this.angleTable[index7];
                        if (num10 > this.minAngle)
                            this.minAngle = num10;
                        if (flag1 && num10 < this.maxAngle)
                            this.maxAngle = num10;
                        if (num10 > num3)
                            num3 = num10;
                        if (flag2 && num10 < num4)
                            num4 = num10;
                    }
                    else
                    {
                        ++this.angleTable[sampleDegrees - index7 - 1];
                        if (flag1 || num10 > this.maxAngle)
                        {
                            this.maxAngle = num10;
                            flag1 = false;
                        }

                        if (flag2 || num10 > num4)
                        {
                            num4 = num10;
                            flag2 = false;
                        }
                    }
                }

                int index9 = sampleDegrees / 2 - 1;
                for (int index10 = index9 - 1; index10 >= 0; --index10)
                {
                    if (num3 > numArray1[index10])
                        index9 = index10;
                }

                ++this.minAngles[index9];
                int index11 = sampleDegrees / 2 - 1;
                for (int index12 = index11 - 1; index12 >= 0; --index12)
                {
                    if (num4 > numArray1[index12])
                        index11 = index12;
                }

                if (flag2)
                    ++this.maxAngles[index11];
                else
                    ++this.maxAngles[sampleDegrees - index11 - 1];
                flag2 = true;
            }

            this.minEdge = Math.Sqrt(this.minEdge);
            this.maxEdge = Math.Sqrt(this.maxEdge);
            this.minAspect = Math.Sqrt(this.minAspect);
            this.maxAspect = Math.Sqrt(this.maxAspect);
            this.minArea *= 0.5;
            this.maxArea *= 0.5;
            this.minAngle = this.minAngle < 1.0 ? num2 * Math.Acos(Math.Sqrt(this.minAngle)) : 0.0;
            if (this.maxAngle >= 1.0)
                this.maxAngle = 180.0;
            else if (flag1)
                this.maxAngle = num2 * Math.Acos(Math.Sqrt(this.maxAngle));
            else
                this.maxAngle = 180.0 - num2 * Math.Acos(Math.Sqrt(this.maxAngle));
        }
    }
}