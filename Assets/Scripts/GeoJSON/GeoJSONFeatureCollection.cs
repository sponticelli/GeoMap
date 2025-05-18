using System.Collections.Generic;
using UnityEngine;

namespace GeoJSON
{
    /// <summary>
    /// Represents a GeoJSON FeatureCollection, which is a collection of Features.
    /// </summary>
    [System.Serializable]
    public class GeoJSONFeatureCollection
    {
        public string type = "FeatureCollection";
        public List<GeoJSONFeature> features = new List<GeoJSONFeature>();
    }
}
