using UnityEngine;

namespace GeoJSON
{
    /// <summary>
    /// Utility class for GeoJSON coordinate transformations.
    /// </summary>
    public static class GeoJSONCoordinateUtils
    {
        /// <summary>
        /// Converts GeoJSON longitude and latitude to Unity world coordinates.
        /// </summary>
        /// <param name="longitude">The longitude coordinate.</param>
        /// <param name="latitude">The latitude coordinate.</param>
        /// <param name="scale">The scale factor to apply to the result.</param>
        /// <param name="heightValue">Optional height/altitude value.</param>
        /// <returns>A Vector3 representing the position in Unity world space.</returns>
        public static Vector3 GeoToUnityPosition(float longitude, float latitude, float scale = 1.0f, float heightValue = 0.0f)
        {
            // Simple equirectangular projection
            float x = longitude * scale;
            float z = latitude * scale;
            float y = heightValue * scale;
            
            return new Vector3(x, y, z);
        }
        
        /// <summary>
        /// Converts a GeoJSON coordinate array [longitude, latitude] to Unity world coordinates.
        /// </summary>
        /// <param name="geoCoordinate">The GeoJSON coordinate pair [longitude, latitude].</param>
        /// <param name="scale">The scale factor to apply to the result.</param>
        /// <param name="heightValue">Optional height/altitude value.</param>
        /// <returns>A Vector3 representing the position in Unity world space.</returns>
        public static Vector3 GeoToUnityPosition(float[] geoCoordinate, float scale = 1.0f, float heightValue = 0.0f)
        {
            if (geoCoordinate.Length < 2)
            {
                Debug.LogError("Invalid GeoJSON coordinate: expected at least [longitude, latitude]");
                return Vector3.zero;
            }
            
            float longitude = geoCoordinate[0];
            float latitude = geoCoordinate[1];
            
            // If altitude is provided in the coordinate, use it
            if (geoCoordinate.Length > 2)
            {
                heightValue = geoCoordinate[2];
            }
            
            return GeoToUnityPosition(longitude, latitude, scale, heightValue);
        }
        
        /// <summary>
        /// Normalizes the given coordinates to fit within a specified range.
        /// Useful for scaling world coordinates to a reasonable display size.
        /// </summary>
        /// <param name="coordinates">The array of coordinates to normalize.</param>
        /// <param name="minLongitude">The minimum longitude value in the dataset.</param>
        /// <param name="maxLongitude">The maximum longitude value in the dataset.</param>
        /// <param name="minLatitude">The minimum latitude value in the dataset.</param>
        /// <param name="maxLatitude">The maximum latitude value in the dataset.</param>
        /// <param name="targetWidth">The desired width after normalization.</param>
        /// <param name="targetHeight">The desired height after normalization.</param>
        /// <returns>A new array of normalized coordinates.</returns>
        public static Vector3[] NormalizeCoordinates(Vector3[] coordinates, 
                                                    float minLongitude, float maxLongitude,
                                                    float minLatitude, float maxLatitude,
                                                    float targetWidth = 10.0f, float targetHeight = 10.0f)
        {
            float longitudeRange = maxLongitude - minLongitude;
            float latitudeRange = maxLatitude - minLatitude;
            
            if (longitudeRange == 0f || latitudeRange == 0f)
            {
                Debug.LogError("Cannot normalize: longitude or latitude range is zero");
                return coordinates;
            }
            
            Vector3[] normalizedCoords = new Vector3[coordinates.Length];
            
            for (int i = 0; i < coordinates.Length; i++)
            {
                // Normalize to [0,1] range
                float normalizedX = (coordinates[i].x - minLongitude) / longitudeRange;
                float normalizedZ = (coordinates[i].z - minLatitude) / latitudeRange;
                
                // Scale to target size and center
                normalizedCoords[i] = new Vector3(
                    normalizedX * targetWidth - (targetWidth / 2),
                    coordinates[i].y,
                    normalizedZ * targetHeight - (targetHeight / 2)
                );
            }
            
            return normalizedCoords;
        }
        
        /// <summary>
        /// Finds the bounding box (min/max coordinates) for an array of GeoJSON coordinates.
        /// </summary>
        /// <param name="coordinates">The coordinates to find bounds for.</param>
        /// <param name="minLong">Output minimum longitude.</param>
        /// <param name="maxLong">Output maximum longitude.</param>
        /// <param name="minLat">Output minimum latitude.</param>
        /// <param name="maxLat">Output maximum latitude.</param>
        public static void FindBounds(float[][] coordinates, 
                                     out float minLong, out float maxLong, 
                                     out float minLat, out float maxLat)
        {
            minLong = float.MaxValue;
            maxLong = float.MinValue;
            minLat = float.MaxValue;
            maxLat = float.MinValue;
            
            foreach (var coord in coordinates)
            {
                if (coord.Length < 2)
                    continue;
                    
                float longitude = coord[0];
                float latitude = coord[1];
                
                minLong = Mathf.Min(minLong, longitude);
                maxLong = Mathf.Max(maxLong, longitude);
                minLat = Mathf.Min(minLat, latitude);
                maxLat = Mathf.Max(maxLat, latitude);
            }
        }
    }
}
