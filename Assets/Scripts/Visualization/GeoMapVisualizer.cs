using GeoData;
using UnityEngine;

namespace Visualization
{
    /// <summary>
    /// Main component for visualizing GeoJSON data in Unity.
    /// Coordinates the rendering, interaction, and styling of the map.
    /// </summary>
    public class GeoMapVisualizer : MonoBehaviour
    {
        [Header("Data Source")]

        /// <summary>
        /// The GeoJSONCollection asset to visualize.
        /// </summary>
        [SerializeField]
        public GeoJSONCollection geoJsonCollection;

        [Header("Visualization Settings")]

        /// <summary>
        /// The projection type to use for the map.
        /// </summary>
        [SerializeField]
        public GeoCoordinateConverter.ProjectionType projectionType = GeoCoordinateConverter.ProjectionType.Equirectangular;

        /// <summary>
        /// Scale factor for the map.
        /// </summary>
        [SerializeField]
        public float mapScale = 10f;

        /// <summary>
        /// Height of the extruded country meshes.
        /// </summary>
        [SerializeField]
        public float extrusionHeight = 0.1f;

        /// <summary>
        /// Whether to generate separate meshes for each polygon in a country.
        /// </summary>
        [SerializeField]
        public bool separatePolygonMeshes = true;

        [Header("Materials")]

        /// <summary>
        /// Default material for country meshes.
        /// </summary>
        [SerializeField]
        public Material defaultMaterial;

        /// <summary>
        /// Material for highlighted countries.
        /// </summary>
        [SerializeField]
        public Material highlightMaterial;

        [Header("Camera Controls")]

        /// <summary>
        /// The camera used to view the map.
        /// </summary>
        [SerializeField]
        public Camera mapCamera;

        /// <summary>
        /// Whether to enable camera controls.
        /// </summary>
        [SerializeField]
        public bool enableCameraControls = true;

        // Private components
        private GeoCoordinateConverter coordinateConverter;
        private GeoMapRenderer mapRenderer;
        private GeoMapController mapController;

        /// <summary>
        /// Called when the component is initialized.
        /// </summary>
        private void Start()
        {
            InitializeComponents();

            if (geoJsonCollection != null)
            {
                RenderMap();
            }
            else
            {
                Debug.LogWarning("No GeoJSONCollection assigned. Please assign a collection in the inspector.");
            }
        }

        /// <summary>
        /// Initializes the required components.
        /// </summary>
        private void InitializeComponents()
        {
            // Initialize the coordinate converter
            coordinateConverter = new GeoCoordinateConverter
            {
                projectionType = projectionType,
                mapScale = mapScale,
                mapCenter = transform.position
            };

            // Initialize the map renderer
            mapRenderer = new GeoMapRenderer(coordinateConverter, transform, defaultMaterial)
            {
                highlightMaterial = highlightMaterial
            };

            // Initialize the map controller
            if (enableCameraControls)
            {
                // Use the main camera if none is specified
                if (mapCamera == null)
                {
                    mapCamera = Camera.main;
                }

                // Add a controller component if it doesn't exist
                mapController = GetComponent<GeoMapController>();
                if (mapController == null)
                {
                    mapController = gameObject.AddComponent<GeoMapController>();
                }

                // Configure the controller
                mapController.mapCamera = mapCamera;
                mapController.target = transform;
            }
        }

        /// <summary>
        /// Renders the map using the assigned GeoJSONCollection.
        /// </summary>
        public void RenderMap()
        {
            if (geoJsonCollection == null)
            {
                Debug.LogError("Cannot render map: No GeoJSONCollection assigned");
                return;
            }

            try
            {
                // Make sure components are initialized
                if (coordinateConverter == null || mapRenderer == null)
                {
                    InitializeComponents();
                }

                // Update coordinate converter settings
                if (coordinateConverter != null)
                {
                    coordinateConverter.projectionType = projectionType;
                    coordinateConverter.mapScale = mapScale;
                    coordinateConverter.mapCenter = transform.position;
                }

                // Update the mesh generator settings and render
                if (mapRenderer != null)
                {
                    // Clear existing map first to free memory
                    mapRenderer.ClearMap();

                    // Force garbage collection to free memory
                    System.GC.Collect();

                    // Update the mesh generator in the renderer
                    mapRenderer.UpdateMeshGenerator(extrusionHeight, separatePolygonMeshes);

                    // Render the collection
                    mapRenderer.RenderCollection(geoJsonCollection);

                    Debug.Log($"Rendered map with {geoJsonCollection.CountryCount} countries");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error rendering map: {e.Message}");

                // Try to clean up any partial rendering
                try
                {
                    if (mapRenderer != null)
                    {
                        mapRenderer.ClearMap();
                    }
                }
                catch (System.Exception cleanupEx)
                {
                    Debug.LogError($"Error cleaning up after failed render: {cleanupEx.Message}");
                }

                // Force garbage collection
                System.GC.Collect();
            }
        }

        /// <summary>
        /// Called when a country is selected.
        /// </summary>
        /// <param name="country">The selected country identifier.</param>
        public void OnCountrySelected(CountryIdentifier country)
        {
            if (country == null || mapRenderer == null)
                return;

            Debug.Log($"Country selected: {country.countryName} ({country.isoCode})");

            // Highlight the selected country
            if (country.isSelected)
            {
                mapRenderer.HighlightCountry(country.isoCode);
            }
            else
            {
                mapRenderer.UnhighlightCountry(country.isoCode);
            }
        }

        /// <summary>
        /// Clears the map by removing all country GameObjects.
        /// </summary>
        public void ClearMap()
        {
            if (mapRenderer != null)
            {
                mapRenderer.ClearMap();
            }
        }

        /// <summary>
        /// Sets the GeoJSONCollection to visualize and renders the map.
        /// </summary>
        /// <param name="collection">The GeoJSONCollection to visualize.</param>
        public void SetGeoJSONCollection(GeoJSONCollection collection)
        {
            geoJsonCollection = collection;
            RenderMap();
        }

        /// <summary>
        /// Resets the camera to its default position.
        /// </summary>
        public void ResetCamera()
        {
            if (mapController != null)
            {
                mapController.ResetCamera();
            }
        }

        /// <summary>
        /// Sets the map projection type and re-renders the map.
        /// </summary>
        /// <param name="projection">The projection type to use.</param>
        public void SetProjection(GeoCoordinateConverter.ProjectionType projection)
        {
            // Store the old projection type in case we need to revert
            GeoCoordinateConverter.ProjectionType oldProjection = projectionType;

            try
            {
                // Update the projection type
                projectionType = projection;

                if (coordinateConverter != null)
                {
                    // Clear the map first to free memory
                    if (mapRenderer != null)
                    {
                        mapRenderer.ClearMap();
                    }

                    // Force garbage collection to free memory
                    System.GC.Collect();

                    // Update the projection type
                    coordinateConverter.projectionType = projection;

                    // Render with the new projection
                    RenderMap();

                    Debug.Log($"Changed projection to {projection}");
                }
            }
            catch (System.Exception e)
            {
                // If something goes wrong, revert to the old projection
                Debug.LogError($"Error changing projection: {e.Message}. Reverting to previous projection.");
                projectionType = oldProjection;

                if (coordinateConverter != null)
                {
                    coordinateConverter.projectionType = oldProjection;
                }

                // Try to render with the old projection
                try
                {
                    RenderMap();
                }
                catch (System.Exception renderEx)
                {
                    Debug.LogError($"Failed to revert projection: {renderEx.Message}");
                }
            }
        }

        /// <summary>
        /// Sets the map scale and re-renders the map.
        /// </summary>
        /// <param name="scale">The scale factor to use.</param>
        public void SetMapScale(float scale)
        {
            // Store the old scale in case we need to revert
            float oldScale = mapScale;

            try
            {
                // Clamp scale to reasonable values to prevent extreme sizes
                scale = Mathf.Clamp(scale, 0.1f, 100f);

                mapScale = scale;

                if (coordinateConverter != null)
                {
                    // Clear the map first to free memory
                    if (mapRenderer != null)
                    {
                        mapRenderer.ClearMap();
                    }

                    // Force garbage collection to free memory
                    System.GC.Collect();

                    // Update the scale
                    coordinateConverter.mapScale = scale;

                    // Render with the new scale
                    RenderMap();

                    Debug.Log($"Changed map scale to {scale}");
                }
            }
            catch (System.Exception e)
            {
                // If something goes wrong, revert to the old scale
                Debug.LogError($"Error changing map scale: {e.Message}. Reverting to previous scale.");
                mapScale = oldScale;

                if (coordinateConverter != null)
                {
                    coordinateConverter.mapScale = oldScale;
                }

                // Try to render with the old scale
                try
                {
                    RenderMap();
                }
                catch (System.Exception renderEx)
                {
                    Debug.LogError($"Failed to revert map scale: {renderEx.Message}");
                }
            }
        }
    }
}
