using System;
using System.Collections.Generic;

namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Provides functionality for iterating through connected regions of triangles in a mesh.
    /// </summary>
    /// <remarks>
    /// The RegionIterator class implements a flood-fill algorithm to traverse and process
    /// connected triangles in a mesh. It is primarily used for region identification and
    /// for applying operations to all triangles within a connected region.
    ///
    /// The class uses the triangle's "infected" flag to mark visited triangles during traversal,
    /// ensuring that each triangle is processed exactly once.
    /// </remarks>
    [System.Serializable]
    public class RegionIterator
    {
        /// <summary>
        /// Reference to the mesh being processed.
        /// </summary>
        private TriangularMesh _triangularMesh;

        /// <summary>
        /// List of triangles in the current region being processed.
        /// </summary>
        private List<Triangle> viri;

        /// <summary>
        /// Initializes a new instance of the <see cref="RegionIterator"/> class.
        /// </summary>
        /// <param name="triangularMesh">The mesh to iterate through.</param>
        /// <remarks>
        /// The constructor initializes the region iterator with a reference to the mesh
        /// and creates an empty list to store triangles during region traversal.
        /// </remarks>
        public RegionIterator(TriangularMesh triangularMesh)
        {
            this._triangularMesh = triangularMesh;
            this.viri = new List<Triangle>();
        }

        /// <summary>
        /// Processes all triangles in the current region by applying the specified function to each triangle.
        /// </summary>
        /// <param name="func">The function to apply to each triangle in the region.</param>
        /// <remarks>
        /// This method implements a breadth-first traversal of the connected triangles in the region.
        /// It starts with the triangles in the viri list and expands outward, visiting adjacent triangles
        /// that are not separated by subsegments and have not been visited yet.
        ///
        /// The specified function is applied to each triangle as it is visited. After all triangles
        /// in the region have been processed, their "infected" flags are cleared.
        /// </remarks>
        private void ProcessRegion(Action<Triangle> func)
        {
            Otri otri = new Otri();
            Otri o2 = new Otri();
            Osub os = new Osub();
            TriangulationSettings behavior = this._triangularMesh.behavior;
            for (int index = 0; index < this.viri.Count; ++index)
            {
                otri.triangle = this.viri[index];
                otri.Uninfect();
                func(otri.triangle);
                for (otri.orient = 0; otri.orient < 3; ++otri.orient)
                {
                    otri.Sym(ref o2);
                    otri.SegPivot(ref os);
                    if (o2.triangle != TriangularMesh.dummytri && !o2.IsInfected() && os.seg == TriangularMesh.dummysub)
                    {
                        o2.Infect();
                        this.viri.Add(o2.triangle);
                    }
                }
                otri.Infect();
            }
            foreach (Triangle triangle in this.viri)
                triangle.infected = false;
            this.viri.Clear();
        }

        /// <summary>
        /// Processes all triangles in the region containing the specified triangle,
        /// assigning them the same region identifier.
        /// </summary>
        /// <param name="triangle">The starting triangle for region processing.</param>
        /// <remarks>
        /// This method is a convenience wrapper that processes a region and assigns
        /// the region identifier of the starting triangle to all triangles in the region.
        /// </remarks>
        public void Process(Triangle triangle)
        {
            this.Process(triangle, (Action<Triangle>) (tri => tri.region = triangle.region));
        }

        /// <summary>
        /// Processes all triangles in the region containing the specified triangle,
        /// applying the specified function to each triangle.
        /// </summary>
        /// <param name="triangle">The starting triangle for region processing.</param>
        /// <param name="func">The function to apply to each triangle in the region.</param>
        /// <remarks>
        /// This method initiates region processing from the specified triangle if it is valid
        /// (not a dummy triangle and not dead). It marks the starting triangle as infected,
        /// adds it to the processing list, and then calls ProcessRegion to handle the traversal.
        ///
        /// After processing is complete, the viri list is cleared to prepare for future operations.
        /// </remarks>
        public void Process(Triangle triangle, Action<Triangle> func)
        {
            if (triangle != TriangularMesh.dummytri && !Otri.IsDead(triangle))
            {
                triangle.infected = true;
                this.viri.Add(triangle);
                this.ProcessRegion(func);
            }
            this.viri.Clear();
        }
    }
}