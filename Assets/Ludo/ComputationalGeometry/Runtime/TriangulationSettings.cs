using System;
using UnityEngine;

namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Controls the behavior of the triangulation algorithm, including quality constraints and algorithm options.
    /// </summary>
    [Serializable]
    public class TriangulationSettings
    {
        private bool poly;
        private bool quality;
        private bool varArea;
        private bool usertest;
        private bool convex;
        private bool jettison;
        private bool boundaryMarkers = true;
        private bool noHoles;
        private bool conformDel;
        private TriangulationAlgorithm algorithm;
        private int noBisect;
        private int steiner = -1;
        private double minAngle;
        private double maxAngle;
        private double maxArea = -1.0;
        internal bool fixedArea;
        internal bool useSegments = true;
        internal bool useRegions;
        internal double goodAngle;
        internal double maxGoodAngle;
        internal double offconstant;

        /// <summary>
        /// Initializes a new instance of the <see cref="TriangulationSettings"/> class with optional quality constraints.
        /// </summary>
        /// <param name="quality">If true, enables quality mesh generation with the specified minimum angle.</param>
        /// <param name="minAngle">The minimum angle constraint for quality mesh generation (in degrees).</param>
        public TriangulationSettings(bool quality = false, double minAngle = 20.0)
        {
            if (!quality)
                return;
            this.quality = true;
            this.minAngle = minAngle;
            Update();
        }

        /// <summary>
        /// Updates the internal state based on the current quality constraints.
        /// </summary>
        private void Update()
        {
            quality = true;
            if (minAngle < 0.0 || minAngle > 60.0)
            {
                minAngle = 0.0;
                quality = false;
                Debug.LogWarning("Invalid quality option (minimum angle).");
            }
            if (maxAngle != 0.0 && maxAngle < 90.0 || maxAngle > 180.0)
            {
                maxAngle = 0.0;
                quality = false;
                Debug.LogWarning("Invalid quality option (maximum angle).");
            }
            useSegments = Poly || Quality || Convex;
            goodAngle = Math.Cos(MinAngle * Math.PI / 180.0);
            maxGoodAngle = Math.Cos(MaxAngle * Math.PI / 180.0);
            offconstant = goodAngle != 1.0 ? 0.475 * Math.Sqrt((1.0 + goodAngle) / (1.0 - goodAngle)) : 0.0;
            goodAngle *= goodAngle;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use exact arithmetic.
        /// </summary>
        public static bool NoExact { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use quality mesh generation.
        /// </summary>
        public bool Quality
        {
            get => quality;
            set
            {
                quality = value;
                if (!quality)
                    return;
                Update();
            }
        }

        /// <summary>
        /// Gets or sets the minimum angle constraint for quality mesh generation (in degrees).
        /// </summary>
        public double MinAngle
        {
            get => minAngle;
            set
            {
                minAngle = value;
                Update();
            }
        }

        /// <summary>
        /// Gets or sets the maximum angle constraint for quality mesh generation (in degrees).
        /// </summary>
        public double MaxAngle
        {
            get => maxAngle;
            set
            {
                maxAngle = value;
                Update();
            }
        }

        /// <summary>
        /// Gets or sets the maximum area constraint for triangles in the mesh.
        /// </summary>
        public double MaxArea
        {
            get => maxArea;
            set
            {
                maxArea = value;
                fixedArea = value > 0.0;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use variable area constraints.
        /// </summary>
        public bool VarArea
        {
            get => varArea;
            set => varArea = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use polygon mode for triangulation.
        /// </summary>
        public bool Poly
        {
            get => poly;
            set => poly = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use user-defined tests.
        /// </summary>
        public bool Usertest
        {
            get => usertest;
            set => usertest = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to create a convex mesh.
        /// </summary>
        public bool Convex
        {
            get => convex;
            set => convex = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to create a conforming Delaunay triangulation.
        /// </summary>
        public bool ConformingDelaunay
        {
            get => conformDel;
            set => conformDel = value;
        }

        /// <summary>
        /// Gets or sets the triangulation algorithm to use.
        /// </summary>
        public TriangulationAlgorithm Algorithm
        {
            get => algorithm;
            set => algorithm = value;
        }

        /// <summary>
        /// Gets or sets the no-bisect option for segment splitting.
        /// </summary>
        public int NoBisect
        {
            get => noBisect;
            set
            {
                noBisect = value;
                if (noBisect >= 0 && noBisect <= 2)
                    return;
                noBisect = 0;
            }
        }

        /// <summary>
        /// Gets or sets the maximum number of Steiner points to add.
        /// </summary>
        public int SteinerPoints
        {
            get => steiner;
            set => steiner = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use boundary markers.
        /// </summary>
        public bool UseBoundaryMarkers
        {
            get => boundaryMarkers;
            set => boundaryMarkers = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to ignore holes in the input.
        /// </summary>
        public bool NoHoles
        {
            get => noHoles;
            set => noHoles = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to jettison unused vertices.
        /// </summary>
        public bool Jettison
        {
            get => jettison;
            set => jettison = value;
        }
    }
}