using System;
using UnityEngine;

namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Controls the behavior of the triangulation algorithm, including quality constraints and algorithm options.
    /// </summary>
    [System.Serializable]
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
            this.Update();
        }

        /// <summary>
        /// Updates the internal state based on the current quality constraints.
        /// </summary>
        private void Update()
        {
            this.quality = true;
            if (this.minAngle < 0.0 || this.minAngle > 60.0)
            {
                this.minAngle = 0.0;
                this.quality = false;
                Debug.LogWarning("Invalid quality option (minimum angle).");
            }
            if (this.maxAngle != 0.0 && this.maxAngle < 90.0 || this.maxAngle > 180.0)
            {
                this.maxAngle = 0.0;
                this.quality = false;
                Debug.LogWarning("Invalid quality option (maximum angle).");
            }
            this.useSegments = this.Poly || this.Quality || this.Convex;
            this.goodAngle = Math.Cos(this.MinAngle * Math.PI / 180.0);
            this.maxGoodAngle = Math.Cos(this.MaxAngle * Math.PI / 180.0);
            this.offconstant = this.goodAngle != 1.0 ? 0.475 * Math.Sqrt((1.0 + this.goodAngle) / (1.0 - this.goodAngle)) : 0.0;
            this.goodAngle *= this.goodAngle;
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
            get => this.quality;
            set
            {
                this.quality = value;
                if (!this.quality)
                    return;
                this.Update();
            }
        }

        /// <summary>
        /// Gets or sets the minimum angle constraint for quality mesh generation (in degrees).
        /// </summary>
        public double MinAngle
        {
            get => this.minAngle;
            set
            {
                this.minAngle = value;
                this.Update();
            }
        }

        /// <summary>
        /// Gets or sets the maximum angle constraint for quality mesh generation (in degrees).
        /// </summary>
        public double MaxAngle
        {
            get => this.maxAngle;
            set
            {
                this.maxAngle = value;
                this.Update();
            }
        }

        /// <summary>
        /// Gets or sets the maximum area constraint for triangles in the mesh.
        /// </summary>
        public double MaxArea
        {
            get => this.maxArea;
            set
            {
                this.maxArea = value;
                this.fixedArea = value > 0.0;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use variable area constraints.
        /// </summary>
        public bool VarArea
        {
            get => this.varArea;
            set => this.varArea = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use polygon mode for triangulation.
        /// </summary>
        public bool Poly
        {
            get => this.poly;
            set => this.poly = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use user-defined tests.
        /// </summary>
        public bool Usertest
        {
            get => this.usertest;
            set => this.usertest = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to create a convex mesh.
        /// </summary>
        public bool Convex
        {
            get => this.convex;
            set => this.convex = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to create a conforming Delaunay triangulation.
        /// </summary>
        public bool ConformingDelaunay
        {
            get => this.conformDel;
            set => this.conformDel = value;
        }

        /// <summary>
        /// Gets or sets the triangulation algorithm to use.
        /// </summary>
        public TriangulationAlgorithm Algorithm
        {
            get => this.algorithm;
            set => this.algorithm = value;
        }

        /// <summary>
        /// Gets or sets the no-bisect option for segment splitting.
        /// </summary>
        public int NoBisect
        {
            get => this.noBisect;
            set
            {
                this.noBisect = value;
                if (this.noBisect >= 0 && this.noBisect <= 2)
                    return;
                this.noBisect = 0;
            }
        }

        /// <summary>
        /// Gets or sets the maximum number of Steiner points to add.
        /// </summary>
        public int SteinerPoints
        {
            get => this.steiner;
            set => this.steiner = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use boundary markers.
        /// </summary>
        public bool UseBoundaryMarkers
        {
            get => this.boundaryMarkers;
            set => this.boundaryMarkers = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to ignore holes in the input.
        /// </summary>
        public bool NoHoles
        {
            get => this.noHoles;
            set => this.noHoles = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to jettison unused vertices.
        /// </summary>
        public bool Jettison
        {
            get => this.jettison;
            set => this.jettison = value;
        }
    }
}