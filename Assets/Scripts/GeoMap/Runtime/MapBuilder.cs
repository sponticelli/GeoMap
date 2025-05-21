using GeoMap.Utils;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GeoMap
{
    public class MapBuilder : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private TextAsset geoGeometryJson;
        [SerializeField] private bool createSurface = true;

        [Header("References")]
        [SerializeField] private CountryMeshBuilder countryMeshBuilder;
        [SerializeField] private Transform countriesParent;
        [SerializeField] private Transform countryOutlineParent;
        [SerializeField] private Transform countrySurfaceParent;

        [Header("Events")]
        [SerializeField] private UnityEvent onMapBuilt;

        // Dictionary to store country visuals components by country name
        private Dictionary<string, CountryVisuals> countryVisualsMap = new Dictionary<string, CountryVisuals>();

        /// <summary>
        /// Event triggered when the map is built.
        /// </summary>
        public UnityEvent OnMapBuilt => onMapBuilt;

        /// <summary>
        /// Gets the CountryVisuals component for a specific country by name.
        /// </summary>
        /// <param name="countryName">The name of the country.</param>
        /// <returns>The CountryVisuals component, or null if not found.</returns>
        public CountryVisuals GetCountryVisuals(string countryName)
        {
            if (countryVisualsMap.TryGetValue(countryName, out CountryVisuals visuals))
            {
                return visuals;
            }
            return null;
        }

        /// <summary>
        /// Highlights a country by name.
        /// </summary>
        /// <param name="countryName">The name of the country to highlight.</param>
        /// <returns>True if the country was found and highlighted, false otherwise.</returns>
        public bool HighlightCountry(string countryName)
        {
            var visuals = GetCountryVisuals(countryName);
            if (visuals != null)
            {
                visuals.Highlight();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Normalizes a country's appearance by name.
        /// </summary>
        /// <param name="countryName">The name of the country to normalize.</param>
        /// <returns>True if the country was found and normalized, false otherwise.</returns>
        public bool NormalizeCountry(string countryName)
        {
            var visuals = GetCountryVisuals(countryName);
            if (visuals != null)
            {
                visuals.Normalize();
                return true;
            }
            return false;
        }

        private void Start()
        {
            BuildMap();
        }

        private void BuildMap()
        {
            JsonNode rootNode = new JsonNode(geoGeometryJson.text);

            JsonNode featuresNode = rootNode["features"];
            if (featuresNode == null)
            {
                Debug.LogError("Failed to find features in GeoJSON");
                return;
            }

            // Create parent objects if they don't exist
            if (countriesParent == null)
            {
                GameObject countriesObj = new GameObject("Countries");
                countriesParent = countriesObj.transform;
                countriesParent.SetParent(transform, false);
            }

            if (countryOutlineParent == null)
            {
                GameObject outlinesObj = new GameObject("Outlines");
                countryOutlineParent = outlinesObj.transform;
                countryOutlineParent.SetParent(countriesParent, false);
            }

            if (countrySurfaceParent == null && createSurface)
            {
                GameObject surfacesObj = new GameObject("Surfaces");
                countrySurfaceParent = surfacesObj.transform;
                countrySurfaceParent.SetParent(countriesParent, false);
            }

            // Clear any existing country visuals
            countryVisualsMap.Clear();

            int featureCount = featuresNode.Count;
            foreach (JsonNode featureNode in featuresNode.list)
            {
                // Create the country and get its visuals component
                CountryVisuals countryVisuals = countryMeshBuilder.Create(
                    featureNode,
                    transform.position,
                    countryOutlineParent,
                    countrySurfaceParent,
                    createSurface
                );

                // Store the visuals component if created successfully
                if (countryVisuals != null)
                {
                    string countryName = GetCountryName(featureNode);
                    countryVisualsMap[countryName] = countryVisuals;
                }
            }

            Debug.Log($"Built map with {countryVisualsMap.Count} countries");

            // Trigger the map built event
            onMapBuilt?.Invoke();
        }

        /// <summary>
        /// Builds the map and returns when complete.
        /// </summary>
        /// <returns>True if the map was built successfully.</returns>
        public bool BuildMapAndWait()
        {
            BuildMap();
            return countryVisualsMap.Count > 0;
        }

        private string GetCountryName(JsonNode country)
        {
            if (country["properties"]["id"] != null)
                return country["properties"]["id"].ToString();
            if (country["properties"]["name"] != null)
                return country["properties"]["name"].ToString();
            return "Unknown";
        }
    }
}