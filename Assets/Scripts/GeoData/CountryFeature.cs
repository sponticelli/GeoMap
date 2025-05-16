using System;
using System.Collections.Generic;
using UnityEngine;

namespace GeoData
{
    /// <summary>
    /// Represents a single country feature with Unity-serializable geometric data.
    /// Contains essential country information and polygon data optimized for Unity's serialization system.
    /// Uses arrays instead of nested Lists to avoid Unity serialization issues.
    /// </summary>
    [Serializable]
    public class CountryFeature
    {
        [Header("Country Information")]
        
        /// <summary>
        /// The official country name (from GeoJSON properties.ADMIN).
        /// </summary>
        [SerializeField]
        public string countryName;
        
        /// <summary>
        /// The ISO 3166-1 alpha-3 country code (from GeoJSON properties.ISO_A3).
        /// </summary>
        [SerializeField]
        public string isoCode;
        
        [Header("Geometry Data")]
        
        /// <summary>
        /// The original geometry type from the GeoJSON data.
        /// </summary>
        [SerializeField]
        public GeometryType originalGeometryType;
        
        /// <summary>
        /// Array of polygons that make up this country.
        /// Single-polygon countries will have one element, multi-polygon countries will have multiple.
        /// </summary>
        [SerializeField]
        public CountryPolygon[] polygons;
        
        /// <summary>
        /// Initializes a new CountryFeature with the specified properties.
        /// </summary>
        /// <param name="countryName">The official country name.</param>
        /// <param name="isoCode">The ISO 3166-1 alpha-3 country code.</param>
        /// <param name="geometryType">The original geometry type.</param>
        public CountryFeature(string countryName, string isoCode, GeometryType geometryType)
        {
            this.countryName = countryName;
            this.isoCode = isoCode;
            this.originalGeometryType = geometryType;
            this.polygons = new CountryPolygon[0];
        }
        
        /// <summary>
        /// Default constructor for Unity serialization.
        /// </summary>
        public CountryFeature()
        {
            polygons = new CountryPolygon[0];
        }
        
        /// <summary>
        /// Gets the total number of polygons in this country feature.
        /// </summary>
        /// <returns>The number of polygons.</returns>
        public int GetPolygonCount()
        {
            return polygons?.Length ?? 0;
        }
        
        /// <summary>
        /// Gets the total number of points across all polygons and rings.
        /// </summary>
        /// <returns>The total point count.</returns>
        public int GetTotalPointCount()
        {
            if (polygons == null) return 0;
            
            int totalPoints = 0;
            foreach (var polygon in polygons)
            {
                totalPoints += polygon?.TotalPointCount ?? 0;
            }
            return totalPoints;
        }
        
        /// <summary>
        /// Gets the total number of rings (exterior boundaries + holes) across all polygons.
        /// </summary>
        /// <returns>The total ring count.</returns>
        public int GetTotalRingCount()
        {
            if (polygons == null) return 0;
            
            int totalRings = 0;
            foreach (var polygon in polygons)
            {
                totalRings += polygon?.RingCount ?? 0;
            }
            return totalRings;
        }
        
        /// <summary>
        /// Validates the integrity of the polygon data structure.
        /// </summary>
        /// <returns>True if the data structure is valid, false otherwise.</returns>
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(countryName) || string.IsNullOrEmpty(isoCode))
                return false;
                
            if (polygons == null || polygons.Length == 0)
                return false;
                
            // Validate each polygon
            foreach (var polygon in polygons)
            {
                if (polygon == null || !polygon.IsValid())
                    return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Gets all exterior boundary rings across all polygons.
        /// Useful for rendering or calculating overall bounds.
        /// </summary>
        /// <returns>Array of exterior rings.</returns>
        public PolygonRing[] GetExteriorRings()
        {
            if (polygons == null) return new PolygonRing[0];
            
            var exteriorRings = new List<PolygonRing>();
            foreach (var polygon in polygons)
            {
                if (polygon?.ExteriorRing != null)
                    exteriorRings.Add(polygon.ExteriorRing);
            }
            return exteriorRings.ToArray();
        }
        
        /// <summary>
        /// Gets all hole rings across all polygons.
        /// </summary>
        /// <returns>Array of hole rings.</returns>
        public PolygonRing[] GetHoleRings()
        {
            if (polygons == null) return new PolygonRing[0];
            
            var holeRings = new List<PolygonRing>();
            foreach (var polygon in polygons)
            {
                if (polygon?.HoleRings != null)
                    holeRings.AddRange(polygon.HoleRings);
            }
            return holeRings.ToArray();
        }
    }
}