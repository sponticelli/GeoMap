using System;

namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Implements a simple Laplacian smoothing algorithm for improving mesh quality.
    /// </summary>
    /// <remarks>
    /// The SimpleMeshSmoother class implements a basic Laplacian smoothing technique that
    /// repositions each vertex to the centroid of its Voronoi cell. This tends to improve
    /// triangle quality by making triangles more equilateral.
    ///
    /// The algorithm works by:
    /// 1. Computing the Voronoi diagram of the mesh
    /// 2. For each vertex, calculating the centroid of its Voronoi cell
    /// 3. Moving the vertex to this centroid position
    /// 4. Retriangulating the mesh with the new vertex positions
    ///
    /// This process is repeated for a fixed number of iterations to progressively improve
    /// the mesh quality. The smoothing preserves the boundary of the mesh and any internal
    /// constraints (segments).
    /// </remarks>
    [Serializable]
    public class SimpleMeshSmoother : IMeshSmoother
    {
        /// <summary>
        /// Reference to the mesh being smoothed.
        /// </summary>
        private TriangularMesh _triangularMesh;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleMeshSmoother"/> class.
        /// </summary>
        /// <param name="triangularMesh">The mesh to smooth.</param>
        /// <remarks>
        /// The constructor initializes the smoother with a reference to the mesh that will be smoothed.
        /// </remarks>
        public SimpleMeshSmoother(TriangularMesh triangularMesh) => _triangularMesh = triangularMesh;

        /// <summary>
        /// Smooths the mesh by adjusting vertex positions to improve triangle quality.
        /// </summary>
        /// <remarks>
        /// This method implements the IMeshSmoother.Smooth interface method. It performs 5 iterations
        /// of Laplacian smoothing, where each iteration:
        /// 1. Moves vertices to the centroid of their Voronoi cells (Step method)
        /// 2. Rebuilds the input geometry with the new vertex positions (Rebuild method)
        /// 3. Retriangulates the mesh with the updated geometry
        ///
        /// The method temporarily disables quality constraints during smoothing to allow
        /// for more flexibility in vertex placement.
        /// </remarks>
        public void Smooth()
        {
            _triangularMesh.behavior.Quality = false;
            for (int index = 0; index < 5; ++index)
            {
                Step();
                _triangularMesh.Triangulate(Rebuild());
            }
        }

        /// <summary>
        /// Performs one step of Laplacian smoothing by moving vertices to the centroid of their Voronoi cells.
        /// </summary>
        /// <remarks>
        /// This method:
        /// 1. Computes the bounded Voronoi diagram of the current mesh
        /// 2. For each Voronoi region (corresponding to a mesh vertex):
        ///    a. Calculates the centroid of the region's boundary vertices
        ///    b. Moves the generator vertex (mesh vertex) to this centroid
        ///
        /// This repositioning tends to improve triangle quality by making triangles more equilateral.
        /// The boundary vertices remain fixed because the DelaunayDualVoronoiDiagram constructor is called with
        /// includeBoundary=false, which excludes boundary vertices from the smoothing process.
        /// </remarks>
        private void Step()
        {
            foreach (VoronoiRegion region in new DelaunayDualVoronoiDiagram(_triangularMesh, false).Regions)
            {
                int num1 = 0;
                double num2;
                double num3 = num2 = 0.0;
                foreach (Point vertex in region.Vertices)
                {
                    ++num1;
                    num3 += vertex.x;
                    num2 += vertex.y;
                }
                region.Generator.x = num3 / num1;
                region.Generator.y = num2 / num1;
            }
        }

        /// <summary>
        /// Rebuilds the input geometry from the current mesh state.
        /// </summary>
        /// <returns>A new MeshInputData object containing the current mesh vertices, segments, holes, and regions.</returns>
        /// <remarks>
        /// This method creates a new MeshInputData object that represents the current state of the mesh,
        /// including:
        /// - All vertices with their updated positions
        /// - All segments (boundary and constraint edges)
        /// - All holes
        /// - All region pointers
        ///
        /// The resulting MeshInputData is used to retriangulate the mesh while preserving
        /// its topological structure and constraints.
        /// </remarks>
        private MeshInputData Rebuild()
        {
            MeshInputData meshInputData = new MeshInputData(_triangularMesh.vertices.Count);
            foreach (Vertex vertex in _triangularMesh.vertices.Values)
                meshInputData.AddPoint(vertex.x, vertex.y, vertex.mark);
            foreach (Segment segment in _triangularMesh.subsegs.Values)
                meshInputData.AddSegment(segment.P0, segment.P1, segment.Boundary);
            foreach (Point hole in _triangularMesh.holes)
                meshInputData.AddHole(hole.x, hole.y);
            foreach (RegionPointer region in _triangularMesh.regions)
                meshInputData.AddRegion(region.point.x, region.point.y, region.id);
            return meshInputData;
        }
    }
}