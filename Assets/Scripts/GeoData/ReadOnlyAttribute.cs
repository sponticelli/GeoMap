using UnityEngine;

namespace GeoData
{
    /// <summary>
    /// Custom property attribute to mark fields as read-only in the inspector.
    /// Used for displaying calculated statistics that should not be manually edited.
    /// </summary>
    public class ReadOnlyAttribute : PropertyAttribute
    {
        /// <summary>
        /// Initializes a new ReadOnlyAttribute.
        /// </summary>
        public ReadOnlyAttribute() { }
    }
}