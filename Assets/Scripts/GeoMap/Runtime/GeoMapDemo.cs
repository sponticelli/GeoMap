using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GeoMap
{
    /// <summary>
    /// Demo script to showcase the GeoMap country selection system.
    /// </summary>
    [System.Serializable]
    public class GeoMapDemo : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GeoMapController mapController;
        [SerializeField] private TextMeshProUGUI selectedCountryText;
        [SerializeField] private Button clearSelectionButton;

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

            // Set up the clear selection button
            if (clearSelectionButton != null)
            {
                clearSelectionButton.onClick.AddListener(ClearSelection);
            }

            // Initialize the selected country text
            UpdateSelectedCountryText(null);
        }

        /// <summary>
        /// Updates the UI when a country is selected.
        /// </summary>
        /// <param name="countryInfo">The selected country's info.</param>
        public void OnCountrySelected(CountryInfo countryInfo)
        {
            UpdateSelectedCountryText(countryInfo);
        }

        /// <summary>
        /// Updates the UI when a country is deselected.
        /// </summary>
        /// <param name="countryInfo">The deselected country's info.</param>
        public void OnCountryDeselected(CountryInfo countryInfo)
        {
            UpdateSelectedCountryText(null);
        }

        /// <summary>
        /// Clears the current country selection.
        /// </summary>
        public void ClearSelection()
        {
            if (mapController != null)
            {
                mapController.ClearSelection();
            }
        }

        /// <summary>
        /// Updates the selected country text UI element.
        /// </summary>
        /// <param name="countryInfo">The selected country's info, or null if no country is selected.</param>
        private void UpdateSelectedCountryText(CountryInfo countryInfo)
        {
            if (selectedCountryText != null)
            {
                if (countryInfo != null)
                {
                    selectedCountryText.text = $"Selected: {countryInfo.CountryName}";
                }
                else
                {
                    selectedCountryText.text = "No country selected";
                }
            }
        }
    }
}