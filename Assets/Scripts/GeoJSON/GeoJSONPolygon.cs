using System;
using System.Collections.Generic;

namespace GeoJSON
{
    /// <summary>
    /// Represents a Polygon geometry in GeoJSON.
    /// </summary>
    [Serializable]
    public class GeoJSONPolygon : GeoJSONGeometry
    {
        // In GeoJSON, coordinates for polygons are an array of linear rings
        // First ring is exterior, others are holes
        public List<List<float[]>> coordinates;

        public GeoJSONPolygon()
        {
            type = "Polygon";
        }

        public override string GetGeometryType()
        {
            return type;
        }

        public override List<List<List<float[]>>> GetCoordinates()
        {
            // Wrap the coordinates in another list to normalize the return type
            // across different geometry types
            return new List<List<List<float[]>>> { coordinates };
        }
    }
}
