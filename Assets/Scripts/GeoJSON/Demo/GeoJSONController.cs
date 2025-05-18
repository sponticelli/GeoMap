using System.Collections;
using UnityEngine;

namespace GeoJSON.Demo
{
    /// <summary>
    /// Demo controller for GeoJSON loading and processing.
    /// </summary>
    public class GeoJSONController : MonoBehaviour
    {
        // URL to the countries GeoJSON file
        [SerializeField] private string geoJsonUrl = "https://raw.githubusercontent.com/datasets/geo-countries/master/data/countries.geojson";
        
        // Flag to indicate whether to load from URL or local file
        [SerializeField] private bool loadFromUrl = true;
        
        // Local file path (relative to StreamingAssets folder)
        [SerializeField] private string localFilePath = "countries.geojson";
        
        // Reference to the GeoJSONLoader
        private GeoJSONLoader loader;
        
        void Start()
        {
            loader = new GeoJSONLoader();
            
            if (loadFromUrl)
            {
                // Load from URL
                StartCoroutine(LoadGeoJSON());
            }
            else
            {
                // Load from local file
                loader.LoadFromFile(localFilePath, OnGeoJSONLoaded);
            }
        }
        
        /// <summary>
        /// Coroutine to load GeoJSON data from URL.
        /// </summary>
        private IEnumerator LoadGeoJSON()
        {
            yield return StartCoroutine(loader.LoadFromURLCoroutine(geoJsonUrl, OnGeoJSONLoaded));
        }
        
        /// <summary>
        /// Callback method invoked when GeoJSON data is loaded.
        /// </summary>
        /// <param name="featureCollection">The loaded GeoJSON feature collection.</param>
        private void OnGeoJSONLoaded(GeoJSONFeatureCollection featureCollection)
        {
            if (featureCollection == null)
            {
                Debug.LogError("Failed to load GeoJSON data.");
                return;
            }
            
            Debug.Log($"Loaded {featureCollection.features.Count} country features.");
            
            // Example of processing the loaded data
            foreach (var feature in featureCollection.features)
            {
                if (feature.properties != null)
                {
                    Debug.Log($"Country: {feature.properties.ADMIN}, ISO: {feature.properties.ISO_A3}");
                }
                
                if (feature.geometry != null)
                {
                    Debug.Log($"Geometry type: {feature.geometry.GetGeometryType()}");
                    
                    // Process coordinates based on geometry type
                    var coordinates = feature.geometry.GetCoordinates();
                    if (coordinates != null)
                    {
                        Debug.Log($"Number of polygon groups: {coordinates.Count}");
                    }
                }
            }
            
            // This is where you would typically process the GeoJSON data
            // For example, creating game objects for each country
        }
    }
}
