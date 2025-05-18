using System;
using UnityEngine;

namespace GeoJSON
{
    /// <summary>
    /// Factory for creating appropriate geometry objects based on GeoJSON type.
    /// </summary>
    public static class GeoJSONGeometryFactory
    {
        /// <summary>
        /// Creates a geometry object based on the specified GeoJSON geometry type.
        /// </summary>
        /// <param name="geometryType">The type of geometry from GeoJSON.</param>
        /// <returns>The appropriate geometry object, or null if type is unsupported.</returns>
        public static GeoJSONGeometry CreateGeometry(string geometryType)
        {
            switch (geometryType)
            {
                case "Polygon":
                    return new GeoJSONPolygon();
                case "MultiPolygon":
                    return new GeoJSONMultiPolygon();
                default:
                    Debug.LogWarning($"Unsupported GeoJSON geometry type: {geometryType}");
                    return null;
            }
        }
    }
}
