using UnityEngine;

namespace Visualization
{
    /// <summary>
    /// Handles conversion between geographic coordinates (latitude/longitude) and Unity world coordinates.
    /// Supports different map projections and provides scaling and centering functionality.
    /// </summary>
    [System.Serializable]
    public class GeoCoordinateConverter
    {
        /// <summary>
        /// Available map projection types.
        /// </summary>
        public enum ProjectionType
        {
            /// <summary>
            /// Simple equirectangular projection (plate carrée).
            /// Longitude and latitude are directly mapped to X and Y.
            /// </summary>
            Equirectangular,

            /// <summary>
            /// Mercator projection. Preserves angles but distorts sizes near poles.
            /// </summary>
            Mercator
        }

        /// <summary>
        /// The projection type to use for coordinate conversion.
        /// </summary>
        [SerializeField]
        public ProjectionType projectionType = ProjectionType.Equirectangular;

        /// <summary>
        /// Scale factor for the map. Higher values create a larger map.
        /// </summary>
        [SerializeField]
        public float mapScale = 10f;

        /// <summary>
        /// Center of the map in Unity world coordinates.
        /// </summary>
        [SerializeField]
        public Vector3 mapCenter = Vector3.zero;

        /// <summary>
        /// Converts a geographic coordinate (longitude, latitude) to a Unity world position.
        /// </summary>
        /// <param name="longitude">Longitude in degrees (-180 to 180)</param>
        /// <param name="latitude">Latitude in degrees (-90 to 90)</param>
        /// <returns>Position in Unity world space</returns>
        public Vector3 GeoToWorldPosition(float longitude, float latitude)
        {
            Vector2 projectedCoordinate = ProjectCoordinate(longitude, latitude);

            // Convert to 3D position (using Y as up-axis)
            return new Vector3(
                projectedCoordinate.x * mapScale + mapCenter.x,
                mapCenter.y,
                projectedCoordinate.y * mapScale + mapCenter.z
            );
        }

        /// <summary>
        /// Converts a Vector2 geographic coordinate to a Unity world position.
        /// </summary>
        /// <param name="geoCoordinate">Vector2 with x=longitude, y=latitude</param>
        /// <returns>Position in Unity world space</returns>
        public Vector3 GeoToWorldPosition(Vector2 geoCoordinate)
        {
            return GeoToWorldPosition(geoCoordinate.x, geoCoordinate.y);
        }

        /// <summary>
        /// Projects a geographic coordinate according to the selected projection type.
        /// </summary>
        /// <param name="longitude">Longitude in degrees (-180 to 180)</param>
        /// <param name="latitude">Latitude in degrees (-90 to 90)</param>
        /// <returns>Projected coordinate in normalized space</returns>
        private Vector2 ProjectCoordinate(float longitude, float latitude)
        {
            switch (projectionType)
            {
                case ProjectionType.Mercator:
                    return MercatorProjection(longitude, latitude);

                case ProjectionType.Equirectangular:
                default:
                    return EquirectangularProjection(longitude, latitude);
            }
        }

        /// <summary>
        /// Applies an equirectangular projection to a geographic coordinate.
        /// </summary>
        /// <param name="longitude">Longitude in degrees (-180 to 180)</param>
        /// <param name="latitude">Latitude in degrees (-90 to 90)</param>
        /// <returns>Projected coordinate in normalized space</returns>
        private Vector2 EquirectangularProjection(float longitude, float latitude)
        {
            // Normalize longitude from -180..180 to -1..1
            float x = longitude / 180f;

            // Normalize latitude from -90..90 to -1..1
            float z = latitude / 90f;

            return new Vector2(x, z);
        }

        /// <summary>
        /// Applies a Mercator projection to a geographic coordinate.
        /// </summary>
        /// <param name="longitude">Longitude in degrees (-180 to 180)</param>
        /// <param name="latitude">Latitude in degrees (-90 to 90)</param>
        /// <returns>Projected coordinate in normalized space</returns>
        private Vector2 MercatorProjection(float longitude, float latitude)
        {
            // Normalize longitude from -180..180 to -1..1
            float x = longitude / 180f;

            // Apply Mercator formula for latitude
            // Clamp latitude to avoid infinity at poles (more conservative than before)
            float clampedLatitude = Mathf.Clamp(latitude, -80f, 80f);
            float latRad = clampedLatitude * Mathf.Deg2Rad;

            // Mercator formula: y = ln(tan(π/4 + φ/2))
            float z = Mathf.Log(Mathf.Tan(Mathf.PI / 4f + latRad / 2f));

            // Normalize the result to a reasonable range (-1 to 1)
            // The maximum value of z at latitude 80° is approximately 2.3
            z = z / 2.5f;

            return new Vector2(x, z);
        }

        /// <summary>
        /// Calculates the appropriate map scale based on the geographic bounds.
        /// </summary>
        /// <param name="bounds">Geographic bounds (min/max longitude and latitude)</param>
        /// <param name="targetWidth">Target width in Unity units</param>
        /// <returns>Appropriate scale factor</returns>
        public float CalculateScaleFromBounds(Rect bounds, float targetWidth)
        {
            // Project the bounds corners
            Vector2 projectedMin = ProjectCoordinate(bounds.xMin, bounds.yMin);
            Vector2 projectedMax = ProjectCoordinate(bounds.xMax, bounds.yMax);

            // Calculate the projected width
            float projectedWidth = Mathf.Abs(projectedMax.x - projectedMin.x);

            // Calculate scale to achieve target width
            return projectedWidth > 0 ? targetWidth / projectedWidth : 1f;
        }
    }
}
