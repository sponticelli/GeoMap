using UnityEngine;

namespace GeoMap
{
    /// <summary>
    /// Main controller for the GeoMap, coordinating map building and country selection.
    /// </summary>
    [System.Serializable]
    public class GeoMapController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MapBuilder mapBuilder;
        [SerializeField] private CountrySelectionManager selectionManager;
        [SerializeField] private InputManager inputManager;
        [SerializeField] private CameraController cameraController;
        [SerializeField] private Camera mainCamera;

        [Header("Settings")]
        [SerializeField] private bool autoInitialize = true;
        [SerializeField] private LayerMask countryLayerMask = -1; // Default to everything

        /// <summary>
        /// Initializes the component.
        /// </summary>
        private void Start()
        {
            if (autoInitialize)
            {
                Initialize();
            }
        }

        /// <summary>
        /// Initializes the GeoMap system.
        /// </summary>
        public void Initialize()
        {
            // Find or create required components
            if (mapBuilder == null)
            {
                mapBuilder = GetComponent<MapBuilder>();
                if (mapBuilder == null)
                {
                    mapBuilder = gameObject.AddComponent<MapBuilder>();
                }
            }

            if (selectionManager == null)
            {
                selectionManager = GetComponent<CountrySelectionManager>();
                if (selectionManager == null)
                {
                    selectionManager = gameObject.AddComponent<CountrySelectionManager>();
                }
            }

            if (inputManager == null)
            {
                inputManager = GetComponent<InputManager>();
                if (inputManager == null)
                {
                    inputManager = gameObject.AddComponent<InputManager>();
                }
            }

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            // Initialize camera controller
            if (cameraController == null)
            {
                cameraController = GetComponent<CameraController>();
                if (cameraController == null)
                {
                    cameraController = gameObject.AddComponent<CameraController>();
                }
            }

            // Configure the input manager
            if (inputManager != null)
            {
                // Use reflection to set private fields
                var cameraField = inputManager.GetType().GetField("mainCamera", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var selectionManagerField = inputManager.GetType().GetField("selectionManager", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var layerMaskField = inputManager.GetType().GetField("countryLayerMask", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                if (cameraField != null)
                    cameraField.SetValue(inputManager, mainCamera);

                if (selectionManagerField != null)
                    selectionManagerField.SetValue(inputManager, selectionManager);

                if (layerMaskField != null)
                    layerMaskField.SetValue(inputManager, countryLayerMask);
            }

            // Configure the camera controller
            if (cameraController != null)
            {
                // Set references via reflection to avoid making fields public
                var cameraField = cameraController.GetType().GetField("mainCamera", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var mapBuilderField = cameraController.GetType().GetField("mapBuilder", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var countriesParentField = cameraController.GetType().GetField("countriesParent", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                if (cameraField != null)
                    cameraField.SetValue(cameraController, mainCamera);

                if (mapBuilderField != null)
                    mapBuilderField.SetValue(cameraController, mapBuilder);

                if (countriesParentField != null && mapBuilder != null)
                {
                    // Try to find the countries parent transform
                    Transform countriesParent = mapBuilder.transform.Find("Countries");
                    if (countriesParent != null)
                        countriesParentField.SetValue(cameraController, countriesParent);
                }

                // Initialize the camera controller
                cameraController.Initialize();
            }
        }

        /// <summary>
        /// Selects a country by name.
        /// </summary>
        /// <param name="countryName">The name of the country to select.</param>
        /// <returns>True if the country was found and selected, false otherwise.</returns>
        public bool SelectCountry(string countryName)
        {
            if (mapBuilder == null || selectionManager == null)
                return false;

            // Get the country visuals from the map builder
            CountryVisuals countryVisuals = mapBuilder.GetCountryVisuals(countryName);
            if (countryVisuals == null)
                return false;

            // Find the CountryInfo component in the parent hierarchy
            Transform current = countryVisuals.transform;
            CountryInfo countryInfo = null;

            while (current != null && countryInfo == null)
            {
                countryInfo = current.GetComponent<CountryInfo>();
                if (countryInfo != null)
                    break;

                current = current.parent;
            }

            if (countryInfo == null)
                return false;

            // Select the country
            selectionManager.SelectCountry(countryInfo);
            return true;
        }

        /// <summary>
        /// Clears the current country selection.
        /// </summary>
        public void ClearSelection()
        {
            if (selectionManager != null)
            {
                selectionManager.ClearSelection();
            }
        }

        /// <summary>
        /// Gets the currently selected country, if any.
        /// </summary>
        /// <returns>The selected country's info, or null if no country is selected.</returns>
        public CountryInfo GetSelectedCountry()
        {
            return selectionManager != null ? selectionManager.SelectedCountry : null;
        }

        /// <summary>
        /// Focuses the camera on a specific country.
        /// </summary>
        /// <param name="countryName">The name of the country to focus on.</param>
        /// <returns>True if the country was found and focused, false otherwise.</returns>
        public bool FocusOnCountry(string countryName)
        {
            if (mapBuilder == null || cameraController == null)
                return false;

            // Get the country visuals from the map builder
            CountryVisuals countryVisuals = mapBuilder.GetCountryVisuals(countryName);
            if (countryVisuals == null)
                return false;

            // Focus the camera on the country's position
            cameraController.FocusOnPosition(countryVisuals.transform.position);

            // Set a closer zoom level for better viewing
            cameraController.SetZoom(30f);

            return true;
        }

        /// <summary>
        /// Resets the camera view to show the entire map.
        /// </summary>
        public void ResetCameraView()
        {
            if (cameraController != null)
            {
                cameraController.ResetView();
            }
        }
    }
}