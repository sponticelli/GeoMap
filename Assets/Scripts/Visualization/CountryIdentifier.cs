using UnityEngine;

namespace Visualization
{
    /// <summary>
    /// Component that identifies a country GameObject.
    /// Stores country metadata for use in interaction and selection.
    /// </summary>
    public class CountryIdentifier : MonoBehaviour
    {
        /// <summary>
        /// The name of the country.
        /// </summary>
        [SerializeField]
        public string countryName;
        
        /// <summary>
        /// The ISO 3166-1 alpha-3 code of the country.
        /// </summary>
        [SerializeField]
        public string isoCode;
        
        /// <summary>
        /// Whether this country is currently selected.
        /// </summary>
        [SerializeField]
        public bool isSelected;
        
        /// <summary>
        /// Called when the country is clicked.
        /// </summary>
        public void OnCountryClicked()
        {
            Debug.Log($"Country clicked: {countryName} ({isoCode})");
            
            // Toggle selection state
            isSelected = !isSelected;
            
            // Notify any listeners (e.g., GeoMapVisualizer)
            SendMessageUpwards("OnCountrySelected", this, SendMessageOptions.DontRequireReceiver);
        }
    }
}
