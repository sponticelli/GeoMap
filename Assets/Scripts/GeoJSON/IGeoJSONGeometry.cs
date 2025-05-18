using System.Collections.Generic;

namespace GeoJSON
{
    /// <summary>
    /// Interface for all geometry types in GeoJSON.
    /// </summary>
    public interface IGeoJSONGeometry
    {
        string GetGeometryType();
        List<List<List<float[]>>> GetCoordinates();
    }
}
