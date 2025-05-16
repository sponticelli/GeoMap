using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GeoData
{
    /// <summary>
    /// ScriptableObject container for storing and managing a collection of country features.
    /// Provides efficient access to normalized GeoJSON data with Unity serialization support.
    /// </summary>
    [CreateAssetMenu(fileName = "GeoJSONCollection", menuName = "GeoData/Country Collection")]
    [Serializable]
    public class GeoJSONCollection : ScriptableObject
    {
        [Header("Collection Metadata")]
        
        /// <summary>
        /// Human-readable description of this collection.
        /// </summary>
        [SerializeField]
        [TextArea(2, 4)]
        public string description = "Collection of country features with normalized geometric data";
        
        /// <summary>
        /// Version identifier for tracking data updates.
        /// </summary>
        [SerializeField]
        public string version = "1.0.0";
        
        /// <summary>
        /// Date when this collection was last updated.
        /// </summary>
        [SerializeField]
        public string lastUpdated;
        
        [Header("Country Data")]
        
        /// <summary>
        /// Collection of all country features with their geometric data.
        /// </summary>
        [SerializeField]
        public List<CountryFeature> countries;
        
        [Header("Statistics")]
        
        /// <summary>
        /// Cached total number of polygons across all countries.
        /// Updated during data processing.
        /// </summary>
        [SerializeField]
        public int totalPolygons;
        
        /// <summary>
        /// Cached total number of points across all countries.
        /// Updated during data processing.
        /// </summary>
        [SerializeField]
        public int totalPoints;
        
        /// <summary>
        /// Cached total number of rings across all countries.
        /// Updated during data processing.
        /// </summary>
        [SerializeField]
        public int totalRings;
        
        /// <summary>
        /// Initializes a new GeoJSONCollection.
        /// </summary>
        public GeoJSONCollection()
        {
            countries = new List<CountryFeature>();
            UpdateLastModified();
        }
        
        /// <summary>
        /// Adds a country feature to the collection.
        /// </summary>
        /// <param name="country">The country feature to add.</param>
        /// <returns>True if added successfully, false if invalid or already exists.</returns>
        public bool AddCountry(CountryFeature country)
        {
            if (country == null || !country.IsValid())
                return false;
                
            // Check for duplicates by ISO code
            if (HasCountry(country.isoCode))
                return false;
                
            countries.Add(country);
            UpdateStatistics();
            UpdateLastModified();
            return true;
        }
        
        /// <summary>
        /// Checks if a country with the specified ISO code exists in the collection.
        /// </summary>
        /// <param name="isoCode">The ISO code to check.</param>
        /// <returns>True if the country exists, false otherwise.</returns>
        public bool HasCountry(string isoCode)
        {
            return GetCountryByIsoCode(isoCode) != null;
        }
        
        /// <summary>
        /// Retrieves a country feature by its ISO code.
        /// </summary>
        /// <param name="isoCode">The ISO 3166-1 alpha-3 code.</param>
        /// <returns>The matching country feature, or null if not found.</returns>
        public CountryFeature GetCountryByIsoCode(string isoCode)
        {
            if (string.IsNullOrEmpty(isoCode))
                return null;
                
            return countries?.FirstOrDefault(c => 
                string.Equals(c.isoCode, isoCode, StringComparison.OrdinalIgnoreCase));
        }
        
        /// <summary>
        /// Updates the cached statistics for the collection.
        /// </summary>
        public void UpdateStatistics()
        {
            if (countries == null)
            {
                totalPolygons = totalPoints = totalRings = 0;
                return;
            }
            
            totalPolygons = countries.Sum(c => c.GetPolygonCount());
            totalPoints = countries.Sum(c => c.GetTotalPointCount());
            totalRings = countries.Sum(c => c.GetTotalRingCount());
        }
        
        /// <summary>
        /// Updates the last modified timestamp.
        /// </summary>
        private void UpdateLastModified()
        {
            lastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
        
        /// <summary>
        /// Gets the total number of countries in the collection.
        /// </summary>
        public int CountryCount => countries?.Count ?? 0;
    }
}