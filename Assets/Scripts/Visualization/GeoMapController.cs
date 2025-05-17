using UnityEngine;

namespace Visualization
{
    /// <summary>
    /// Handles user interaction with the map, including camera controls and country selection.
    /// </summary>
    public class GeoMapController : MonoBehaviour
    {
        /// <summary>
        /// The camera used to view the map.
        /// </summary>
        [SerializeField]
        public Camera mapCamera;
        
        /// <summary>
        /// The target transform to orbit around.
        /// </summary>
        [SerializeField]
        public Transform target;
        
        /// <summary>
        /// Minimum zoom distance.
        /// </summary>
        [SerializeField]
        public float minZoomDistance = 2f;
        
        /// <summary>
        /// Maximum zoom distance.
        /// </summary>
        [SerializeField]
        public float maxZoomDistance = 20f;
        
        /// <summary>
        /// Zoom speed multiplier.
        /// </summary>
        [SerializeField]
        public float zoomSpeed = 1f;
        
        /// <summary>
        /// Pan speed multiplier.
        /// </summary>
        [SerializeField]
        public float panSpeed = 10f;
        
        /// <summary>
        /// Rotation speed multiplier.
        /// </summary>
        [SerializeField]
        public float rotationSpeed = 100f;
        
        /// <summary>
        /// Whether to enable rotation controls.
        /// </summary>
        [SerializeField]
        public bool enableRotation = true;
        
        /// <summary>
        /// Whether to enable panning controls.
        /// </summary>
        [SerializeField]
        public bool enablePanning = true;
        
        /// <summary>
        /// Whether to enable zooming controls.
        /// </summary>
        [SerializeField]
        public bool enableZooming = true;
        
        /// <summary>
        /// Whether to enable country selection.
        /// </summary>
        [SerializeField]
        public bool enableSelection = true;
        
        // Private variables for tracking camera state
        private float currentZoomDistance;
        private Vector3 lastMousePosition;
        private bool isDragging = false;
        
        /// <summary>
        /// Called when the component is initialized.
        /// </summary>
        private void Start()
        {
            // Use the main camera if none is specified
            if (mapCamera == null)
            {
                mapCamera = Camera.main;
            }
            
            // Use this transform as the target if none is specified
            if (target == null)
            {
                target = transform;
            }
            
            // Initialize zoom distance
            if (mapCamera != null)
            {
                currentZoomDistance = Vector3.Distance(mapCamera.transform.position, target.position);
                currentZoomDistance = Mathf.Clamp(currentZoomDistance, minZoomDistance, maxZoomDistance);
            }
        }
        
        /// <summary>
        /// Called every frame.
        /// </summary>
        private void Update()
        {
            if (mapCamera == null || target == null)
                return;
                
            HandleMouseInput();
            UpdateCameraPosition();
        }
        
        /// <summary>
        /// Handles mouse input for camera controls.
        /// </summary>
        private void HandleMouseInput()
        {
            // Handle zooming with mouse wheel
            if (enableZooming)
            {
                float scrollDelta = Input.mouseScrollDelta.y;
                if (scrollDelta != 0)
                {
                    currentZoomDistance -= scrollDelta * zoomSpeed;
                    currentZoomDistance = Mathf.Clamp(currentZoomDistance, minZoomDistance, maxZoomDistance);
                }
            }
            
            // Handle panning and rotation with mouse drag
            if (Input.GetMouseButtonDown(0))
            {
                lastMousePosition = Input.mousePosition;
                isDragging = true;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                isDragging = false;
                
                // Handle country selection on mouse up (if not dragging significantly)
                if (enableSelection && Vector3.Distance(lastMousePosition, Input.mousePosition) < 5f)
                {
                    HandleCountrySelection();
                }
            }
            
            if (isDragging && Input.GetMouseButton(0))
            {
                Vector3 delta = Input.mousePosition - lastMousePosition;
                
                // Right mouse button or Shift+Left for rotation
                if (enableRotation && (Input.GetMouseButton(1) || Input.GetKey(KeyCode.LeftShift)))
                {
                    target.Rotate(Vector3.up, -delta.x * Time.deltaTime * rotationSpeed, Space.World);
                    target.Rotate(Vector3.right, delta.y * Time.deltaTime * rotationSpeed, Space.World);
                }
                // Left mouse button for panning
                else if (enablePanning)
                {
                    Vector3 right = mapCamera.transform.right;
                    Vector3 up = mapCamera.transform.up;
                    
                    // Adjust pan speed based on zoom level
                    float adjustedPanSpeed = panSpeed * (currentZoomDistance / maxZoomDistance);
                    
                    target.position -= right * delta.x * Time.deltaTime * adjustedPanSpeed;
                    target.position -= up * delta.y * Time.deltaTime * adjustedPanSpeed;
                }
                
                lastMousePosition = Input.mousePosition;
            }
        }
        
        /// <summary>
        /// Updates the camera position based on the current zoom distance.
        /// </summary>
        private void UpdateCameraPosition()
        {
            // Calculate the desired camera position
            Vector3 direction = (mapCamera.transform.position - target.position).normalized;
            Vector3 desiredPosition = target.position + direction * currentZoomDistance;
            
            // Smoothly move the camera to the desired position
            mapCamera.transform.position = Vector3.Lerp(mapCamera.transform.position, desiredPosition, Time.deltaTime * 10f);
            
            // Make the camera look at the target
            mapCamera.transform.LookAt(target);
        }
        
        /// <summary>
        /// Handles country selection with raycasting.
        /// </summary>
        private void HandleCountrySelection()
        {
            Ray ray = mapCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                // Check if we hit a country
                CountryIdentifier countryIdentifier = hit.collider.GetComponentInParent<CountryIdentifier>();
                if (countryIdentifier != null)
                {
                    countryIdentifier.OnCountryClicked();
                }
            }
        }
        
        /// <summary>
        /// Resets the camera to its default position.
        /// </summary>
        public void ResetCamera()
        {
            if (mapCamera != null && target != null)
            {
                // Reset zoom
                currentZoomDistance = (minZoomDistance + maxZoomDistance) / 2f;
                
                // Reset position
                Vector3 direction = new Vector3(0, 0, -1);
                mapCamera.transform.position = target.position + direction * currentZoomDistance;
                
                // Reset rotation
                mapCamera.transform.LookAt(target);
            }
        }
    }
}
