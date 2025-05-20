using UnityEngine;
using UnityEngine.Events;

namespace GeoMap
{
    /// <summary>
    /// Manages the highlighting of a country in the GeoMap.
    /// </summary>
    [System.Serializable]
    public class CountryHighlighter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CountryInfo countryInfo;
        [SerializeField] private CountryVisuals countryVisuals;

        [Header("Events")]
        [SerializeField] private UnityEvent onHighlighted;
        [SerializeField] private UnityEvent onNormalized;

        /// <summary>
        /// Gets whether the country is currently highlighted.
        /// </summary>
        public bool IsHighlighted => countryVisuals != null && countryVisuals.IsHighlighted;

        /// <summary>
        /// Event triggered when the country is highlighted.
        /// </summary>
        public UnityEvent OnHighlighted => onHighlighted;

        /// <summary>
        /// Event triggered when the country is normalized (unhighlighted).
        /// </summary>
        public UnityEvent OnNormalized => onNormalized;

        /// <summary>
        /// Initializes the component.
        /// </summary>
        private void Start()
        {
            // If no country info is assigned, try to find one on this GameObject
            if (countryInfo == null)
            {
                countryInfo = GetComponent<CountryInfo>();
            }

            // If no country visuals is assigned, try to find one in children
            if (countryVisuals == null && countryInfo != null)
            {
                countryVisuals = countryInfo.CountryVisuals;
            }
        }

        /// <summary>
        /// Highlights the country.
        /// </summary>
        public void Highlight()
        {
            if (countryVisuals != null && !countryVisuals.IsHighlighted)
            {
                countryVisuals.Highlight();
                onHighlighted?.Invoke();
            }
        }

        /// <summary>
        /// Normalizes (unhighlights) the country.
        /// </summary>
        public void Normalize()
        {
            if (countryVisuals != null && countryVisuals.IsHighlighted)
            {
                countryVisuals.Normalize();
                onNormalized?.Invoke();
            }
        }

        /// <summary>
        /// Toggles the highlight state of the country.
        /// </summary>
        public void ToggleHighlight()
        {
            if (countryVisuals != null)
            {
                if (countryVisuals.IsHighlighted)
                {
                    Normalize();
                }
                else
                {
                    Highlight();
                }
            }
        }
    }
}