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
    [Serializable]
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
        private double _minEdge;

        /// <summary>
        /// The length of the longest edge in the mesh.
        /// </summary>
        private double _maxEdge;

        /// <summary>
        /// The shortest altitude (height) of any triangle in the mesh.
        /// </summary>
        private double _minAspect;

        /// <summary>
        /// The largest aspect ratio of any triangle in the mesh.
        /// </summary>
        private double _maxAspect;

        /// <summary>
        /// The area of the smallest triangle in the mesh.
        /// </summary>
        private double _minArea;

        /// <summary>
        /// The area of the largest triangle in the mesh.
        /// </summary>
        private double _maxArea;

        /// <summary>
        /// The smallest angle (in degrees) of any triangle in the mesh.
        /// </summary>
        private double _minAngle;

        /// <summary>
        /// The largest angle (in degrees) of any triangle in the mesh.
        /// </summary>
        private double _maxAngle;

        /// <summary>
        /// The number of vertices in the input geometry.
        /// </summary>
        private int _inVetrices;

        /// <summary>
        /// The number of triangles in the input geometry.
        /// </summary>
        private int _inTriangles;

        /// <summary>
        /// The number of segments in the input geometry.
        /// </summary>
        private int _inSegments;

        /// <summary>
        /// The number of holes in the input geometry.
        /// </summary>
        private int _inHoles;

        /// <summary>
        /// The number of vertices in the output mesh.
        /// </summary>
        private int _outVertices;

        /// <summary>
        /// The number of triangles in the output mesh.
        /// </summary>
        private int _outTriangles;

        /// <summary>
        /// The number of edges in the output mesh.
        /// </summary>
        private int _outEdges;

        /// <summary>
        /// The number of boundary edges in the output mesh.
        /// </summary>
        private int _boundaryEdges;

        /// <summary>
        /// The number of interior boundary edges in the output mesh.
        /// </summary>
        private int _intBoundaryEdges;

        /// <summary>
        /// The number of constrained edges in the output mesh.
        /// </summary>
        private int _constrainedEdges;

        /// <summary>
        /// Histogram of angles in the mesh.
        /// </summary>
        private int[] _angleTable;

        /// <summary>
        /// Histogram of minimum angles per triangle.
        /// </summary>
        private int[] _minAngles;

        /// <summary>
        /// Histogram of maximum angles per triangle.
        /// </summary>
        private int[] _maxAngles;

        /// <summary>
        /// Lookup table for adding 1 modulo 3 to an index.
        /// </summary>
        /// <remarks>
        /// This is used to efficiently cycle through the three vertices of a triangle
        /// in a clockwise direction.
        /// </remarks>
        private static readonly int[] Plus1Mod3 = new int[3]
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
        private static readonly int[] Minus1Mod3 = new int[3]
        {
            2,
            0,
            1
        };

        /// <summary>
        /// Gets the length of the shortest edge in the mesh.
        /// </summary>
        public double ShortestEdge => _minEdge;

        /// <summary>
        /// Gets the length of the longest edge in the mesh.
        /// </summary>
        public double LongestEdge => _maxEdge;

        /// <summary>
        /// Gets the shortest altitude (height) of any triangle in the mesh.
        /// </summary>
        public double ShortestAltitude => _minAspect;

        /// <summary>
        /// Gets the largest aspect ratio of any triangle in the mesh.
        /// </summary>
        public double LargestAspectRatio => _maxAspect;

        /// <summary>
        /// Gets the area of the smallest triangle in the mesh.
        /// </summary>
        public double SmallestArea => _minArea;

        /// <summary>
        /// Gets the area of the largest triangle in the mesh.
        /// </summary>
        public double LargestArea => _maxArea;

        /// <summary>
        /// Gets the smallest angle (in degrees) of any triangle in the mesh.
        /// </summary>
        public double SmallestAngle => _minAngle;

        /// <summary>
        /// Gets the largest angle (in degrees) of any triangle in the mesh.
        /// </summary>
        public double LargestAngle => _maxAngle;

        /// <summary>
        /// Gets the number of vertices in the input geometry.
        /// </summary>
        public int InputVertices => _inVetrices;

        /// <summary>
        /// Gets the number of triangles in the input geometry.
        /// </summary>
        public int InputTriangles => _inTriangles;

        /// <summary>
        /// Gets the number of segments in the input geometry.
        /// </summary>
        public int InputSegments => _inSegments;

        /// <summary>
        /// Gets the number of holes in the input geometry.
        /// </summary>
        public int InputHoles => _inHoles;

        /// <summary>
        /// Gets the number of vertices in the output mesh.
        /// </summary>
        public int Vertices => _outVertices;

        /// <summary>
        /// Gets the number of triangles in the output mesh.
        /// </summary>
        public int Triangles => _outTriangles;

        /// <summary>
        /// Gets the number of edges in the output mesh.
        /// </summary>
        public int Edges => _outEdges;

        /// <summary>
        /// Gets the number of boundary edges in the output mesh.
        /// </summary>
        public int BoundaryEdges => _boundaryEdges;

        /// <summary>
        /// Gets the number of interior boundary edges in the output mesh.
        /// </summary>
        public int InteriorBoundaryEdges => _intBoundaryEdges;

        /// <summary>
        /// Gets the number of constrained edges in the output mesh.
        /// </summary>
        public int ConstrainedEdges => _constrainedEdges;

        /// <summary>
        /// Gets the histogram of angles in the mesh.
        /// </summary>
        public int[] AngleHistogram => _angleTable;

        /// <summary>
        /// Gets the histogram of minimum angles per triangle.
        /// </summary>
        public int[] MinAngleHistogram => _minAngles;

        /// <summary>
        /// Gets the histogram of maximum angles per triangle.
        /// </summary>
        public int[] MaxAngleHistogram => _maxAngles;

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
            OrientedTriangle orientedTriangle = new OrientedTriangle();
            Vertex[] vertexArray = new Vertex[3];
            double[] numArray3 = new double[3];
            double[] numArray4 = new double[3];
            double[] numArray5 = new double[3];
            orientedTriangle.orient = 0;
            foreach (Triangle triangle in triangularMesh.TriangleDictionary.Values)
            {
                orientedTriangle.triangle = triangle;
                vertexArray[0] = orientedTriangle.Origin();
                vertexArray[1] = orientedTriangle.Destination();
                vertexArray[2] = orientedTriangle.Apex();
                double num1 = 0.0;
                for (int index1 = 0; index1 < 3; ++index1)
                {
                    int index2 = Plus1Mod3[index1];
                    int index3 = Minus1Mod3[index1];
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
        /// </remarks>
        public void Update(TriangularMesh triangularMesh, int sampleDegrees = 60)
        {
            _inVetrices = triangularMesh.invertices;
            _inTriangles = triangularMesh.inelements;
            _inSegments = triangularMesh.insegments;
            _inHoles = triangularMesh.holes.Count;
            _outVertices = triangularMesh.VertexDictionary.Count - triangularMesh.undeads;
            _outTriangles = triangularMesh.TriangleDictionary.Count;
            _outEdges = triangularMesh.edges;
            _boundaryEdges = triangularMesh.hullsize;
            _intBoundaryEdges = triangularMesh.SubSegmentDictionary.Count - triangularMesh.hullsize;
            _constrainedEdges = triangularMesh.SubSegmentDictionary.Count;
            Point[] pointArray = new Point[3];
            double[] numArray1 = new double[sampleDegrees / 2 - 1];
            double[] numArray2 = new double[3];
            double[] numArray3 = new double[3];
            double[] numArray4 = new double[3];
            double num1 = Math.PI / sampleDegrees;
            double num2 = 180.0 / Math.PI;
            _angleTable = new int[sampleDegrees];
            _minAngles = new int[sampleDegrees];
            _maxAngles = new int[sampleDegrees];
            for (int index = 0; index < sampleDegrees / 2 - 1; ++index)
            {
                numArray1[index] = Math.Cos(num1 * (index + 1));
                numArray1[index] = numArray1[index] * numArray1[index];
            }

            for (int index = 0; index < sampleDegrees; ++index)
                _angleTable[index] = 0;
            _minAspect = triangularMesh.bounds.Width + triangularMesh.bounds.Height;
            _minAspect *= _minAspect;
            _maxAspect = 0.0;
            _minEdge = _minAspect;
            _maxEdge = 0.0;
            _minArea = _minAspect;
            _maxArea = 0.0;
            _minAngle = 0.0;
            _maxAngle = 2.0;
            bool flag1 = true;
            bool flag2 = true;
            foreach (Triangle triangle in triangularMesh.TriangleDictionary.Values)
            {
                double num3 = 0.0;
                double num4 = 1.0;
                pointArray[0] = triangle.vertices[0];
                pointArray[1] = triangle.vertices[1];
                pointArray[2] = triangle.vertices[2];
                double num5 = 0.0;
                for (int index1 = 0; index1 < 3; ++index1)
                {
                    int index2 = Plus1Mod3[index1];
                    int index3 = Minus1Mod3[index1];
                    numArray2[index1] = pointArray[index2].X - pointArray[index3].X;
                    numArray3[index1] = pointArray[index2].Y - pointArray[index3].Y;
                    numArray4[index1] = numArray2[index1] * numArray2[index1] + numArray3[index1] * numArray3[index1];
                    if (numArray4[index1] > num5)
                        num5 = numArray4[index1];
                    if (numArray4[index1] > _maxEdge)
                        _maxEdge = numArray4[index1];
                    if (numArray4[index1] < _minEdge)
                        _minEdge = numArray4[index1];
                }

                double num6 = Math.Abs((pointArray[2].X - pointArray[0].X) * (pointArray[1].Y - pointArray[0].Y) -
                                       (pointArray[1].X - pointArray[0].X) * (pointArray[2].Y - pointArray[0].Y));
                if (num6 < _minArea)
                    _minArea = num6;
                if (num6 > _maxArea)
                    _maxArea = num6;
                double num7 = num6 * num6 / num5;
                if (num7 < _minAspect)
                    _minAspect = num7;
                double num8 = num5 / num7;
                if (num8 > _maxAspect)
                    _maxAspect = num8;
                for (int index4 = 0; index4 < 3; ++index4)
                {
                    int index5 = Plus1Mod3[index4];
                    int index6 = Minus1Mod3[index4];
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
                        ++_angleTable[index7];
                        if (num10 > _minAngle)
                            _minAngle = num10;
                        if (flag1 && num10 < _maxAngle)
                            _maxAngle = num10;
                        if (num10 > num3)
                            num3 = num10;
                        if (flag2 && num10 < num4)
                            num4 = num10;
                    }
                    else
                    {
                        ++_angleTable[sampleDegrees - index7 - 1];
                        if (flag1 || num10 > _maxAngle)
                        {
                            _maxAngle = num10;
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

                ++_minAngles[index9];
                int index11 = sampleDegrees / 2 - 1;
                for (int index12 = index11 - 1; index12 >= 0; --index12)
                {
                    if (num4 > numArray1[index12])
                        index11 = index12;
                }

                if (flag2)
                    ++_maxAngles[index11];
                else
                    ++_maxAngles[sampleDegrees - index11 - 1];
                flag2 = true;
            }

            _minEdge = Math.Sqrt(_minEdge);
            _maxEdge = Math.Sqrt(_maxEdge);
            _minAspect = Math.Sqrt(_minAspect);
            _maxAspect = Math.Sqrt(_maxAspect);
            _minArea *= 0.5;
            _maxArea *= 0.5;
            _minAngle = _minAngle < 1.0 ? num2 * Math.Acos(Math.Sqrt(_minAngle)) : 0.0;
            if (_maxAngle >= 1.0)
                _maxAngle = 180.0;
            else if (flag1)
                _maxAngle = num2 * Math.Acos(Math.Sqrt(_maxAngle));
            else
                _maxAngle = 180.0 - num2 * Math.Acos(Math.Sqrt(_maxAngle));
        }
    }
}