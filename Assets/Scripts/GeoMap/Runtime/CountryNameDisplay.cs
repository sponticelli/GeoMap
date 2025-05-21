using UnityEngine;
using TMPro;

namespace GeoMap
{
    /// <summary>
    /// Displays the name of the selected country at the top of the screen.
    /// </summary>
    [System.Serializable]
    public class CountryNameDisplay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CountrySelectionManager selectionManager;
        [SerializeField] private TextMeshProUGUI countryNameText;
        
        [Header("Display Settings")]
        [SerializeField] private string defaultText = "No Country Selected";
        [SerializeField] private string displayFormat = "{0}";
        [SerializeField] private float displayDuration = 0f; // 0 means display until another country is selected
        
        private float displayTimer = 0f;
        private bool isDisplayingName = false;

        /// <summary>
        /// Initializes the component.
        /// </summary>
        private void Start()
        {
            // Find required components if not assigned
            if (selectionManager == null)
            {
                selectionManager = FindObjectOfType<CountrySelectionManager>();
            }
            
            // Subscribe to selection events
            if (selectionManager != null)
            {
                selectionManager.OnCountrySelected.AddListener(OnCountrySelected);
                selectionManager.OnCountryDeselected.AddListener(OnCountryDeselected);
            }
            
            // Initialize text
            UpdateDisplayText(null);
        }

        /// <summary>
        /// Updates the display when a country is selected.
        /// </summary>
        /// <param name="countryInfo">The selected country's info.</param>
        public void OnCountrySelected(CountryInfo countryInfo)
        {
            UpdateDisplayText(countryInfo);
            
            // Start timer if duration is set
            if (displayDuration > 0f)
            {
                displayTimer = displayDuration;
                isDisplayingName = true;
            }
        }

        /// <summary>
        /// Updates the display when a country is deselected.
        /// </summary>
        /// <param name="countryInfo">The deselected country's info.</param>
        public void OnCountryDeselected(CountryInfo countryInfo)
        {
            // Only clear if we're not using a timer, or if the timer has expired
            if (displayDuration <= 0f || !isDisplayingName)
            {
                UpdateDisplayText(null);
            }
        }

        /// <summary>
        /// Updates the display text based on the country info.
        /// </summary>
        /// <param name="countryInfo">The country info, or null if no country is selected.</param>
        private void UpdateDisplayText(CountryInfo countryInfo)
        {
            if (countryNameText != null)
            {
                if (countryInfo != null)
                {
                    countryNameText.text = string.Format(displayFormat, countryInfo.CountryName);
                }
                else
                {
                    countryNameText.text = defaultText;
                }
            }
        }

        /// <summary>
        /// Updates the timer for temporary display.
        /// </summary>
        private void Update()
        {
            // Handle timer for temporary display
            if (isDisplayingName && displayDuration > 0f)
            {
                displayTimer -= Time.deltaTime;
                
                if (displayTimer <= 0f)
                {
                    isDisplayingName = false;
                    UpdateDisplayText(null);
                }
            }
        }

        /// <summary>
        /// Clean up event subscriptions when the component is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            if (selectionManager != null)
            {
                selectionManager.OnCountrySelected.RemoveListener(OnCountrySelected);
                selectionManager.OnCountryDeselected.RemoveListener(OnCountryDeselected);
            }
        }
    }
}