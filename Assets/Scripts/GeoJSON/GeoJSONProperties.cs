using System;

namespace GeoJSON
{
    /// <summary>
    /// Represents the properties of a GeoJSON Feature, specifically for country data.
    /// </summary>
    [Serializable]
    public class GeoJSONProperties
    {
        public string ADMIN;     // Country name
        public string ISO_A3;    // 3-letter ISO code
        public string ISO_A2;    // 2-letter ISO code
        
        // Additional properties might be present in the GeoJSON but we're focusing on the essentials
    }
}
