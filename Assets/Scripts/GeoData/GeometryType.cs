using System;

namespace GeoData
{
    /// <summary>
    /// Defines the type of geometry for a country feature.
    /// </summary>
    [Serializable]
    public enum GeometryType
    {
        /// <summary>
        /// Single contiguous landmass with potential holes.
        /// </summary>
        Polygon,
        
        /// <summary>
        /// Multiple disjoint landmasses (islands, territories, etc.).
        /// </summary>
        MultiPolygon
    }
}