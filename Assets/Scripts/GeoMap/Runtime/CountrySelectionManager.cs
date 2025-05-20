using UnityEngine;
using UnityEngine.Events;

namespace GeoMap
{
    /// <summary>
    /// Manages the selection of countries on the GeoMap.
    /// </summary>
    [System.Serializable]
    public class CountrySelectionManager : MonoBehaviour
    {
        [Header("Events")]
        [SerializeField] private UnityEvent<CountryInfo> onCountrySelected;
        [SerializeField] private UnityEvent<CountryInfo> onCountryDeselected;

        /// <summary>
        /// The currently selected country, if any.
        /// </summary>
        private CountryInfo selectedCountry;

        /// <summary>
        /// Gets the currently selected country.
        /// </summary>
        public CountryInfo SelectedCountry => selectedCountry;

        /// <summary>
        /// Event triggered when a country is selected.
        /// </summary>
        public UnityEvent<CountryInfo> OnCountrySelected => onCountrySelected;

        /// <summary>
        /// Event triggered when a country is deselected.
        /// </summary>
        public UnityEvent<CountryInfo> OnCountryDeselected => onCountryDeselected;

        /// <summary>
        /// Selects a country, deselecting any previously selected country.
        /// </summary>
        /// <param name="country">The country to select.</param>
        public void SelectCountry(CountryInfo country)
        {
            // If the same country is already selected, do nothing
            if (selectedCountry == country)
                return;

            // Deselect the previously selected country
            if (selectedCountry != null)
            {
                CountryInfo previousCountry = selectedCountry;
                selectedCountry = null;
                
                // Unhighlight the previous country
                if (previousCountry.CountryVisuals != null)
                {
                    previousCountry.CountryVisuals.Normalize();
                }
                
                // Trigger the deselection event
                onCountryDeselected?.Invoke(previousCountry);
            }

            // Select the new country
            if (country != null)
            {
                selectedCountry = country;
                
                // Highlight the new country
                if (selectedCountry.CountryVisuals != null)
                {
                    selectedCountry.CountryVisuals.Highlight();
                }
                
                // Trigger the selection event
                onCountrySelected?.Invoke(selectedCountry);
            }
        }

        /// <summary>
        /// Deselects the currently selected country, if any.
        /// </summary>
        public void ClearSelection()
        {
            SelectCountry(null);
        }
    }
}