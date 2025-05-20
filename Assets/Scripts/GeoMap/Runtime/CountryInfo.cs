using UnityEngine;

namespace GeoMap
{
    /// <summary>
    /// Stores information about a country in the GeoMap.
    /// </summary>
    [System.Serializable]
    public class CountryInfo : MonoBehaviour
    {
        /// <summary>
        /// The name of the country.
        /// </summary>
        [SerializeField] private string countryName;

        /// <summary>
        /// Reference to the country's visual component.
        /// </summary>
        [SerializeField] private CountryVisuals countryVisuals;

        /// <summary>
        /// Gets the name of the country.
        /// </summary>
        public string CountryName => countryName;

        /// <summary>
        /// Gets the country's visual component.
        /// </summary>
        public CountryVisuals CountryVisuals => countryVisuals;

        /// <summary>
        /// Initializes the CountryInfo component with the specified name.
        /// </summary>
        /// <param name="name">The name of the country.</param>
        public void Initialize(string name)
        {
            countryName = name;
            
            // Try to find the CountryVisuals component in children
            if (countryVisuals == null)
            {
                countryVisuals = GetComponentInChildren<CountryVisuals>();
            }
        }
    }
}