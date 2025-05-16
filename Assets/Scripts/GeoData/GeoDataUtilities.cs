using UnityEngine;
using System.Collections.Generic;

namespace GeoData
{
    /// <summary>
    /// Utility functions for working with geographic data in Unity.
    /// Provides coordinate conversion and validation helpers.
    /// </summary>
    public static class GeoDataUtilities
    {
        /// <summary>
        /// Converts longitude/latitude coordinates to Unity Vector2.
        /// </summary>
        /// <param name="longitude">Longitude value (-180 to 180).</param>
        /// <param name="latitude">Latitude value (-90 to 90).</param>
        /// <returns>Vector2 with x=longitude, y=latitude.</returns>
        public static Vector2 LonLatToVector2(double longitude, double latitude)
        {
            return new Vector2((float)longitude, (float)latitude);
        }
        
        /// <summary>
        /// Validates that coordinates are within valid geographic ranges.
        /// </summary>
        /// <param name="longitude">Longitude to validate.</param>
        /// <param name="latitude">Latitude to validate.</param>
        /// <returns>True if coordinates are valid, false otherwise.</returns>
        public static bool IsValidCoordinate(double longitude, double latitude)
        {
            return longitude >= -180.0 && longitude <= 180.0 &&
                   latitude >= -90.0 && latitude <= 90.0;
        }
        
        /// <summary>
        /// Calculates the geographic bounds (min/max lat/lon) for a collection of points.
        /// </summary>
        /// <param name="points">Collection of geographic points.</param>
        /// <returns>A Rect representing the bounding box (x=minLon, y=minLat, width=lonRange, height=latRange).</returns>
        public static Rect CalculateBounds(List<Vector2> points)
        {
            if (points == null || points.Count == 0)
                return new Rect(0, 0, 0, 0);
                
            float minLon = points[0].x, maxLon = points[0].x;
            float minLat = points[0].y, maxLat = points[0].y;
            
            foreach (var point in points)
            {
                if (point.x < minLon) minLon = point.x;
                if (point.x > maxLon) maxLon = point.x;
                if (point.y < minLat) minLat = point.y;
                if (point.y > maxLat) maxLat = point.y;
            }
            
            return new Rect(minLon, minLat, maxLon - minLon, maxLat - minLat);
        }
        
        /// <summary>
        /// Calculates the center point of a collection of geographic coordinates.
        /// </summary>
        /// <param name="points">Collection of points to find center of.</param>
        /// <returns>The center point as Vector2.</returns>
        public static Vector2 CalculateCenter(List<Vector2> points)
        {
            if (points == null || points.Count == 0)
                return Vector2.zero;
                
            float totalLon = 0f, totalLat = 0f;
            foreach (var point in points)
            {
                totalLon += point.x;
                totalLat += point.y;
            }
            
            return new Vector2(totalLon / points.Count, totalLat / points.Count);
        }
        
        /// <summary>
        /// Simplifies a polygon by removing points that are too close together.
        /// Useful for reducing data size while maintaining shape accuracy.
        /// </summary>
        /// <param name="points">Original polygon points.</param>
        /// <param name="tolerance">Minimum distance between points to keep.</param>
        /// <returns>Simplified list of points.</returns>
        public static List<Vector2> SimplifyPolygon(List<Vector2> points, float tolerance = 0.001f)
        {
            if (points == null || points.Count <= 3)
                return points;
                
            var simplified = new List<Vector2> { points[0] };
            
            for (int i = 1; i < points.Count; i++)
            {
                var lastPoint = simplified[simplified.Count - 1];
                if (Vector2.Distance(lastPoint, points[i]) > tolerance)
                {
                    simplified.Add(points[i]);
                }
            }
            
            // Ensure the polygon is closed
            if (simplified.Count > 2 && Vector2.Distance(simplified[0], simplified[simplified.Count - 1]) > tolerance)
            {
                simplified.Add(simplified[0]);
            }
            
            return simplified;
        }
    }
}