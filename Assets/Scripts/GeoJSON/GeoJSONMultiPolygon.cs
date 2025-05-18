using System;
using System.Collections.Generic;

namespace GeoJSON
{
    /// <summary>
    /// Represents a MultiPolygon geometry in GeoJSON.
    /// </summary>
    [Serializable]
    public class GeoJSONMultiPolygon : GeoJSONGeometry
    {
        // In GeoJSON, MultiPolygon is an array of Polygon coordinate arrays
        public List<List<List<float[]>>> coordinates;

        public GeoJSONMultiPolygon()
        {
            type = "MultiPolygon";
        }

        public override string GetGeometryType()
        {
            return type;
        }

        public override List<List<List<float[]>>> GetCoordinates()
        {
            return coordinates;
        }
    }
}
