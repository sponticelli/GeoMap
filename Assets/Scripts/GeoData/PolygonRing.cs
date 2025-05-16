using System;
using UnityEngine;

namespace GeoData
{
    /// <summary>
    /// Represents a single ring of a polygon (exterior boundary or hole).
    /// Unity-serializable wrapper for a collection of Vector2 points.
    /// </summary>
    [Serializable]
    public class PolygonRing
    {
        /// <summary>
        /// Array of points forming this ring.
        /// First point should match the last point to close the ring.
        /// </summary>
        [SerializeField]
        public Vector2[] points;
        
        /// <summary>
        /// Indicates if this is the exterior boundary (true) or a hole (false).
        /// </summary>
        [SerializeField]
        public bool isExterior;
        
        /// <summary>
        /// Initializes a new polygon ring.
        /// </summary>
        /// <param name="points">Points forming the ring.</param>
        /// <param name="isExterior">True for exterior boundary, false for holes.</param>
        public PolygonRing(Vector2[] points, bool isExterior = true)
        {
            this.points = points ?? new Vector2[0];
            this.isExterior = isExterior;
        }
        
        /// <summary>
        /// Default constructor for Unity serialization.
        /// </summary>
        public PolygonRing()
        {
            points = new Vector2[0];
            isExterior = true;
        }
        
        /// <summary>
        /// Gets the number of points in this ring.
        /// </summary>
        public int PointCount => points?.Length ?? 0;
        
        /// <summary>
        /// Validates that the ring has valid data.
        /// </summary>
        /// <returns>True if the ring is valid, false otherwise.</returns>
        public bool IsValid()
        {
            return points != null && points.Length >= 3;
        }
    }
}