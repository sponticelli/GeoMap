using UnityEngine;

namespace GeoMap
{
    /// <summary>
    /// Manages user input for interacting with the GeoMap.
    /// </summary>
    [System.Serializable]
    public class InputManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private CountrySelectionManager selectionManager;

        [Header("Settings")]
        [SerializeField] private LayerMask countryLayerMask = -1; // Default to everything
        [SerializeField] private float maxRaycastDistance = 1000f;

        /// <summary>
        /// Initializes the component.
        /// </summary>
        private void Start()
        {
            // If no camera is assigned, try to find the main camera
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            // If no selection manager is assigned, try to find one on this GameObject
            if (selectionManager == null)
            {
                selectionManager = GetComponent<CountrySelectionManager>();
            }
        }

        /// <summary>
        /// Processes input each frame.
        /// </summary>
        private void Update()
        {
            // Check for mouse click
            if (Input.GetMouseButtonDown(0))
            {
                HandleMouseClick();
            }
        }

        /// <summary>
        /// Handles mouse click by raycasting and detecting country objects.
        /// </summary>
        private void HandleMouseClick()
        {
            if (mainCamera == null || selectionManager == null)
                return;

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Perform raycast
            if (Physics.Raycast(ray, out hit, maxRaycastDistance, countryLayerMask))
            {
                // Find the country parent of the hit object
                CountryInfo countryInfo = FindCountryParent(hit.transform);
                
                if (countryInfo != null)
                {
                    // Notify the selection manager about the country click
                    selectionManager.SelectCountry(countryInfo);
                }
            }
        }

        /// <summary>
        /// Finds the CountryInfo component in the parent hierarchy of the hit transform.
        /// </summary>
        /// <param name="hitTransform">The transform that was hit by the raycast.</param>
        /// <returns>The CountryInfo component, or null if not found.</returns>
        private CountryInfo FindCountryParent(Transform hitTransform)
        {
            // First check if the hit object itself has a CountryInfo component
            CountryInfo countryInfo = hitTransform.GetComponent<CountryInfo>();
            
            // If not, traverse up the hierarchy to find a parent with CountryInfo
            Transform current = hitTransform;
            while (countryInfo == null && current != null)
            {
                countryInfo = current.GetComponent<CountryInfo>();
                if (countryInfo != null)
                    break;
                
                current = current.parent;
            }
            
            return countryInfo;
        }
    }
}