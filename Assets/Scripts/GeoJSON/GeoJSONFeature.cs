using System;

namespace GeoJSON
{
    /// <summary>
    /// Represents a GeoJSON Feature object containing properties and geometry.
    /// </summary>
    [Serializable]
    public class GeoJSONFeature
    {
        public string type = "Feature";
        public GeoJSONProperties properties;
        public GeoJSONGeometry geometry;
    }
}
