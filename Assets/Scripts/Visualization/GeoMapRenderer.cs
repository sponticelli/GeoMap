using System.Collections.Generic;
using GeoData;
using UnityEngine;

namespace Visualization
{
    /// <summary>
    /// Handles the rendering of GeoJSON data as Unity GameObjects with meshes.
    /// Manages the creation, updating, and styling of country visualizations.
    /// </summary>
    [System.Serializable]
    public class GeoMapRenderer
    {
        /// <summary>
        /// Default material to use for country meshes.
        /// </summary>
        [SerializeField]
        public Material defaultMaterial;

        /// <summary>
        /// Material to use for highlighted countries.
        /// </summary>
        [SerializeField]
        public Material highlightMaterial;

        /// <summary>
        /// Parent transform for all country GameObjects.
        /// </summary>
        [SerializeField]
        public Transform mapParent;

        /// <summary>
        /// The coordinate converter used to transform geographic coordinates to world positions.
        /// </summary>
        private GeoCoordinateConverter coordinateConverter;

        /// <summary>
        /// The mesh generator used to create country meshes.
        /// </summary>
        private CountryMeshGenerator meshGenerator;

        /// <summary>
        /// Dictionary mapping country ISO codes to their GameObjects.
        /// </summary>
        private Dictionary<string, GameObject> countryObjects = new Dictionary<string, GameObject>();

        /// <summary>
        /// Initializes a new instance of the GeoMapRenderer.
        /// </summary>
        /// <param name="converter">The coordinate converter to use.</param>
        /// <param name="parent">The parent transform for country GameObjects.</param>
        /// <param name="defaultMat">The default material for country meshes.</param>
        public GeoMapRenderer(GeoCoordinateConverter converter, Transform parent, Material defaultMat)
        {
            coordinateConverter = converter;
            mapParent = parent;
            defaultMaterial = defaultMat;

            meshGenerator = new CountryMeshGenerator(coordinateConverter);
        }

        /// <summary>
        /// Renders a GeoJSONCollection as Unity GameObjects with meshes.
        /// </summary>
        /// <param name="collection">The GeoJSONCollection to render.</param>
        public void RenderCollection(GeoJSONCollection collection)
        {
            if (collection == null || collection.countries == null)
            {
                Debug.LogError("Cannot render null or empty GeoJSONCollection");
                return;
            }

            // Clear any existing country objects
            ClearMap();

            // Create a GameObject for each country
            foreach (var country in collection.countries)
            {
                if (country != null && country.IsValid())
                {
                    CreateCountryObject(country);
                }
            }

            Debug.Log($"Rendered {countryObjects.Count} countries from collection");
        }

        /// <summary>
        /// Creates a GameObject with meshes for a country feature.
        /// </summary>
        /// <param name="country">The country feature to create a GameObject for.</param>
        /// <returns>The created GameObject.</returns>
        private GameObject CreateCountryObject(CountryFeature country)
        {
            if (country == null || string.IsNullOrEmpty(country.isoCode))
                return null;

            // Create a parent GameObject for the country
            GameObject countryObject = new GameObject(country.countryName);
            countryObject.transform.SetParent(mapParent);

            // Generate meshes for the country
            List<Mesh> countryMeshes = meshGenerator.GenerateCountryMeshes(country);

            // Create a child GameObject with a MeshFilter and MeshRenderer for each mesh
            for (int i = 0; i < countryMeshes.Count; i++)
            {
                GameObject meshObject = new GameObject($"Polygon_{i}");
                meshObject.transform.SetParent(countryObject.transform);

                // Add mesh components
                MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = countryMeshes[i];

                MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterial = defaultMaterial;

                // Add a MeshCollider for interaction
                MeshCollider meshCollider = meshObject.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = countryMeshes[i];
            }

            // Add a CountryIdentifier component to store country data
            CountryIdentifier identifier = countryObject.AddComponent<CountryIdentifier>();
            identifier.countryName = country.countryName;
            identifier.isoCode = country.isoCode;

            // Store the country object in the dictionary
            countryObjects[country.isoCode] = countryObject;

            return countryObject;
        }

        /// <summary>
        /// Highlights a country by changing its material.
        /// </summary>
        /// <param name="isoCode">The ISO code of the country to highlight.</param>
        public void HighlightCountry(string isoCode)
        {
            if (string.IsNullOrEmpty(isoCode) || !countryObjects.ContainsKey(isoCode) || highlightMaterial == null)
                return;

            GameObject countryObject = countryObjects[isoCode];

            // Change the material of all child mesh renderers
            MeshRenderer[] renderers = countryObject.GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in renderers)
            {
                renderer.sharedMaterial = highlightMaterial;
            }
        }

        /// <summary>
        /// Removes highlighting from a country by resetting its material.
        /// </summary>
        /// <param name="isoCode">The ISO code of the country to unhighlight.</param>
        public void UnhighlightCountry(string isoCode)
        {
            if (string.IsNullOrEmpty(isoCode) || !countryObjects.ContainsKey(isoCode) || defaultMaterial == null)
                return;

            GameObject countryObject = countryObjects[isoCode];

            // Reset the material of all child mesh renderers
            MeshRenderer[] renderers = countryObject.GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in renderers)
            {
                renderer.sharedMaterial = defaultMaterial;
            }
        }

        /// <summary>
        /// Clears all country GameObjects from the map.
        /// </summary>
        public void ClearMap()
        {
            foreach (var countryObject in countryObjects.Values)
            {
                if (countryObject != null)
                {
                    Object.Destroy(countryObject);
                }
            }

            countryObjects.Clear();
        }

        /// <summary>
        /// Gets a country GameObject by its ISO code.
        /// </summary>
        /// <param name="isoCode">The ISO code of the country.</param>
        /// <returns>The country GameObject, or null if not found.</returns>
        public GameObject GetCountryObject(string isoCode)
        {
            if (string.IsNullOrEmpty(isoCode) || !countryObjects.ContainsKey(isoCode))
                return null;

            return countryObjects[isoCode];
        }

        /// <summary>
        /// Updates the mesh generator settings.
        /// </summary>
        /// <param name="extrusionHeight">The height of the extruded country meshes.</param>
        /// <param name="separatePolygonMeshes">Whether to generate separate meshes for each polygon.</param>
        public void UpdateMeshGenerator(float extrusionHeight, bool separatePolygonMeshes)
        {
            if (meshGenerator != null)
            {
                meshGenerator.extrusionHeight = extrusionHeight;
                meshGenerator.separatePolygonMeshes = separatePolygonMeshes;
            }
            else
            {
                // Create a new mesh generator if it doesn't exist
                meshGenerator = new CountryMeshGenerator(coordinateConverter)
                {
                    extrusionHeight = extrusionHeight,
                    separatePolygonMeshes = separatePolygonMeshes
                };
            }
        }
    }
}
