using System.Collections.Generic;

namespace Ludo.ComputationalGeometry
{
    /// <summary>
    /// Defines the interface for a Voronoi diagram generator.
    /// A Voronoi diagram partitions a plane into regions based on distance to a set of points.
    /// </summary>
    public interface IVoronoiDiagram
    {
        /// <summary>
        /// Gets the array of points in the Voronoi diagram.
        /// These typically represent the circumcenters of triangles in the dual Delaunay triangulation.
        /// </summary>
        Point[] Points { get; }

        /// <summary>
        /// Gets the list of Voronoi regions in the diagram.
        /// Each region corresponds to a generator point and contains the vertices that form the region's boundary.
        /// </summary>
        List<VoronoiRegion> Regions { get; }
    }
}