using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GeoMap
{
    /// <summary>
    /// Demo script to showcase the GeoMap camera control system.
    /// </summary>
    [System.Serializable]
    public class CameraControlDemo : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GeoMapController mapController;
        [SerializeField] private TMP_InputField countryNameInput;
        [SerializeField] private Button focusButton;
        [SerializeField] private Button resetViewButton;
        [SerializeField] private TextMeshProUGUI statusText;

        /// <summary>
        /// Initializes the component.
        /// </summary>
        private void Start()
        {
            // Find or create required components
            if (mapController == null)
            {
                mapController = FindObjectOfType<GeoMapController>();
            }

            // Set up the focus button
            if (focusButton != null)
            {
                focusButton.onClick.AddListener(FocusOnCountry);
            }

            // Set up the reset view button
            if (resetViewButton != null)
            {
                resetViewButton.onClick.AddListener(ResetView);
            }

            // Initialize status text
            if (statusText != null)
            {
                statusText.text = "Use mouse wheel to zoom, right/middle mouse button to pan";
            }
        }

        /// <summary>
        /// Focuses the camera on the country specified in the input field.
        /// </summary>
        public void FocusOnCountry()
        {
            if (mapController == null || countryNameInput == null || string.IsNullOrEmpty(countryNameInput.text))
                return;

            string countryName = countryNameInput.text.Trim();
            bool success = mapController.FocusOnCountry(countryName);

            if (statusText != null)
            {
                statusText.text = success 
                    ? $"Focused on {countryName}" 
                    : $"Country '{countryName}' not found";
            }
        }

        /// <summary>
        /// Resets the camera view to show the entire map.
        /// </summary>
        public void ResetView()
        {
            if (mapController == null)
                return;

            mapController.ResetCameraView();

            if (statusText != null)
            {
                statusText.text = "View reset to show entire map";
            }
        }
    }
}
