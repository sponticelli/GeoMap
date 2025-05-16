using System;
using System.Collections.Generic;

namespace GeoJSON
{
    /// <summary>
    /// Base abstract class for GeoJSON geometry objects.
    /// </summary>
    [Serializable]
    public abstract class GeoJSONGeometry : IGeoJSONGeometry
    {
        public string type;

        public abstract string GetGeometryType();
        public abstract List<List<List<float[]>>> GetCoordinates();
    }
}
