using UnityEngine;
using System.Collections;

namespace GeoMap
{
    /// <summary>
    /// Controls camera movement and zoom for the GeoMap, allowing users to navigate the map with mouse input.
    /// </summary>
    [System.Serializable]
    public class CameraController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private MapBuilder mapBuilder;
        [SerializeField] private Transform countriesParent;
        [SerializeField] private GameObject mapBack;

        [Header("Boundary Settings")]
        [Tooltip("Automatically calculate map boundaries from country meshes")]
        [SerializeField] private bool autoCalculateBoundaries = true;
        [Tooltip("Margin to add around the calculated map boundaries (percentage of map size)")]
        [SerializeField] private float boundaryMargin = 0.1f;
        [SerializeField] private float minX = -100f;
        [SerializeField] private float maxX = 100f;
        [SerializeField] private float minY = -50f;
        [SerializeField] private float maxY = 50f;

        [Header("Zoom Settings")]
        [SerializeField] private float minZoom = 10f;
        [SerializeField] private float maxZoom = 65f;
        [SerializeField] private float startingZoom = 44f;

        [Header("Pan Settings")]
        [Tooltip("How fast the camera pans with mouse drag")]
        [SerializeField] private float panSpeed = 0.5f;

        // Private variables for camera control
        private Vector3 startPointForMove = Vector3.zero;
        private Vector3 mousePosForMove = Vector3.zero;
        private Vector3 moveVector = Vector3.zero;
        private bool initialized = false;
        

        /// <summary>
        /// Initializes the camera controller with references and settings.
        /// </summary>
        public void Initialize()
        {
            if (initialized)
                return;

            // If no camera is assigned, use the component's camera
            if (mainCamera == null)
            {
                mainCamera = GetComponent<Camera>();
                if (mainCamera == null)
                {
                    mainCamera = Camera.main;
                }
            }

            // Find map builder if not assigned
            if (mapBuilder == null)
            {
                mapBuilder = FindFirstObjectByType<MapBuilder>();
            }

            // Find countries parent
            if (countriesParent == null && mapBuilder != null)
            {
                countriesParent = mapBuilder.transform.Find("Countries");
                if (countriesParent == null)
                {
                    GameObject countriesObj = GameObject.Find("Countries");
                    if (countriesObj != null)
                        countriesParent = countriesObj.transform;
                }
            }

            // Create or find map back object with collider
            if (mapBack == null)
            {
                mapBack = GameObject.Find("MapBack");
                if (mapBack == null)
                {
                    mapBack = new GameObject("MapBack");
                    mapBack.transform.SetParent(transform.parent);
                    mapBack.AddComponent<BoxCollider>();

                    // Position it behind the map
                    mapBack.transform.position = new Vector3(0, 0, 1);
                }
            }

            // Calculate map boundaries
            if (autoCalculateBoundaries)
            {
                StartCoroutine(WaitForMapAndCalculateBoundaries());
            }

            initialized = true;
        }

        /// <summary>
        /// Waits for the map to be built before calculating boundaries.
        /// </summary>
        private IEnumerator WaitForMapAndCalculateBoundaries()
        {
            // Wait a short time to ensure map is built
            yield return new WaitForSeconds(0.5f);

            // Calculate boundaries
            CalculateMapBoundaries();

            // Reset view to show the entire map
            ResetView();
            
            SetZoom(startingZoom);
        }

        /// <summary>
        /// Updates the camera position and zoom based on user input.
        /// </summary>
        private void Update()
        {
            if (!initialized || mainCamera == null)
                return;

            // Handle zoom input
            HandleZoomInput();

            // Handle pan input
            HandlePanInput();
        }

        /// <summary>
        /// Handles mouse wheel input for zooming.
        /// </summary>
        private void HandleZoomInput()
        {
            float wheelDelta = Input.GetAxis("Mouse ScrollWheel");

            if (wheelDelta > 0)
            {
                // Zoom in
                mainCamera.fieldOfView -= 2f;
                if (mainCamera.fieldOfView < minZoom)
                    mainCamera.fieldOfView = minZoom;
            }
            else if (wheelDelta < 0)
            {
                // Zoom out
                mainCamera.fieldOfView += 2f;
                if (mainCamera.fieldOfView > maxZoom)
                    mainCamera.fieldOfView = maxZoom;
            }

            // Always adjust position to keep map in view after zooming
            if (wheelDelta != 0)
            {
                AdjustCameraPositionToKeepMapInView();
            }
        }

        /// <summary>
        /// Adjusts the camera position to keep the map in view.
        /// </summary>
        private void AdjustCameraPositionToKeepMapInView()
        {
            if (mapBack == null)
                return;

            BoxCollider mapBoundsCollider = mapBack.GetComponent<BoxCollider>();
            if (mapBoundsCollider == null)
                return;

            // Store the bounds min and max in screen space
            Vector3 minScreenPoint = mainCamera.WorldToScreenPoint(mapBoundsCollider.bounds.min);
            Vector3 maxScreenPoint = mainCamera.WorldToScreenPoint(mapBoundsCollider.bounds.max);

            // Calculate adjustment vector
            Vector3 adjustment = Vector3.zero;

            // Check left edge
            if (minScreenPoint.x > 0)
                adjustment.x += minScreenPoint.x;

            // Check right edge
            if (maxScreenPoint.x < Screen.width)
                adjustment.x -= (Screen.width - maxScreenPoint.x);

            // Check bottom edge
            if (minScreenPoint.y > 0)
                adjustment.y += minScreenPoint.y;

            // Check top edge
            if (maxScreenPoint.y < Screen.height)
                adjustment.y -= (Screen.height - maxScreenPoint.y);

            // Apply adjustment if needed
            if (adjustment != Vector3.zero)
            {
                // Convert screen space adjustment to world space
                float zoomFactor = mainCamera.fieldOfView / maxZoom;
                adjustment *= zoomFactor * 0.01f;
                transform.position += new Vector3(adjustment.x, adjustment.y, 0);
            }
        }

        /// <summary>
        /// Handles mouse drag input for panning.
        /// </summary>
        private void HandlePanInput()
        {
            // Start dragging when right mouse button is pressed
            if (Input.GetMouseButtonDown(0))
            {
                startPointForMove = Input.mousePosition;
            }

            // Handle dragging while right mouse button is held
            if (Input.GetMouseButton(0))
            {
                mousePosForMove = Input.mousePosition;
                moveVector = mousePosForMove - startPointForMove;

                // Calculate zoom factor for consistent movement at different zoom levels
                float zoomFactor = mainCamera.fieldOfView / maxZoom;

                // Apply movement directly with the configured pan speed
                transform.position -= new Vector3(moveVector.x * zoomFactor * panSpeed, moveVector.y * zoomFactor * panSpeed, 0);

                // If we have map boundaries, check and adjust position
                if (mapBack != null)
                {
                    BoxCollider mapBoundsCollider = mapBack.GetComponent<BoxCollider>();
                    if (mapBoundsCollider != null)
                    {
                        // Check if map edges are visible and adjust camera position
                        AdjustCameraPositionToKeepMapInView();
                    }
                }

                // Update start position for next frame
                startPointForMove = Input.mousePosition;
            }
        }

        /// <summary>
        /// Calculates map boundaries based on the extents of all country meshes.
        /// </summary>
        public void CalculateMapBoundaries()
        {
            if (countriesParent == null)
            {
                Debug.LogWarning("CameraController: Cannot calculate map boundaries - no countries parent found.");
                return;
            }

            Debug.Log("CameraController: Calculating map boundaries...");

            // Find all mesh renderers in the countries parent
            MeshRenderer[] meshRenderers = countriesParent.GetComponentsInChildren<MeshRenderer>(true);
            if (meshRenderers.Length == 0)
            {
                Debug.LogWarning("CameraController: No mesh renderers found in countries parent.");
                return;
            }

            Debug.Log($"CameraController: Found {meshRenderers.Length} mesh renderers.");

            // Initialize bounds with the first mesh renderer
            Bounds mapBounds = meshRenderers[0].bounds;

            // Expand bounds to include all mesh renderers
            foreach (MeshRenderer renderer in meshRenderers)
            {
                mapBounds.Encapsulate(renderer.bounds);
            }

            // Calculate boundary values with margin
            float width = mapBounds.size.x;
            float height = mapBounds.size.y;
            float margin = Mathf.Max(width, height) * boundaryMargin;

            minX = mapBounds.min.x - margin;
            maxX = mapBounds.max.x + margin;
            minY = mapBounds.min.y - margin;
            maxY = mapBounds.max.y + margin;

            // Update the map bounds collider
            if (mapBack != null)
            {
                BoxCollider mapBoundsCollider = mapBack.GetComponent<BoxCollider>();
                if (mapBoundsCollider != null)
                {
                    mapBoundsCollider.center = mapBounds.center;
                    mapBoundsCollider.size = new Vector3(width + margin * 2, height + margin * 2, 0.1f);
                }
            }

            Debug.Log($"CameraController: Calculated map boundaries - X: {minX} to {maxX}, Y: {minY} to {maxY}");
        }

        /// <summary>
        /// Sets the camera position to focus on a specific point.
        /// </summary>
        /// <param name="position">The world position to focus on.</param>
        public void FocusOnPosition(Vector3 position)
        {
            Vector3 targetPosition = new Vector3(position.x, position.y, transform.position.z);
            transform.position = targetPosition;
        }

        /// <summary>
        /// Sets the camera zoom to a specific value.
        /// </summary>
        /// <param name="zoom">The field of view value to set.</param>
        public void SetZoom(float zoom)
        {
            mainCamera.fieldOfView = Mathf.Clamp(zoom, minZoom, maxZoom);
        }

        /// <summary>
        /// Resets the camera to show the entire map.
        /// </summary>
        public void ResetView()
        {
            // Center the camera on the map
            if (mapBack != null)
            {
                BoxCollider mapBoundsCollider = mapBack.GetComponent<BoxCollider>();
                if (mapBoundsCollider != null)
                {
                    Vector3 center = mapBoundsCollider.bounds.center;
                    transform.position = new Vector3(center.x, center.y, transform.position.z);
                }
            }
            else
            {
                transform.position = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, transform.position.z);
            }

            // Set zoom to show the entire map
            SetZoom(maxZoom);
        }
    }
}