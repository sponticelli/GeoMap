using System.Collections.Generic;
using UnityEngine;

namespace GeoMap
{
    /// <summary>
    /// Manages the visual appearance of a country, providing methods to highlight and normalize its appearance.
    /// </summary>
    [System.Serializable]
    public class CountryVisuals : MonoBehaviour
    {
        [Header("Highlight Materials")]
        [SerializeField] private Material highlightBorderMaterial;
        [SerializeField] private Material highlightSurfaceMaterial;

        private List<MeshRenderer> outlineRenderers = new List<MeshRenderer>();
        private List<MeshRenderer> surfaceRenderers = new List<MeshRenderer>();
        
        private Dictionary<MeshRenderer, Material> originalMaterials = new Dictionary<MeshRenderer, Material>();
        private bool isHighlighted = false;

        /// <summary>
        /// Initializes the component by finding all mesh renderers in the Outlines and Surfaces children.
        /// </summary>
        public void Initialize()
        {
            // Find all outline renderers
            Transform outlinesTransform = transform.Find("Outlines");
            if (outlinesTransform != null)
            {
                CollectRenderers(outlinesTransform, outlineRenderers);
            }

            // Find all surface renderers
            Transform surfacesTransform = transform.Find("Surfaces");
            if (surfacesTransform != null)
            {
                CollectRenderers(surfacesTransform, surfaceRenderers);
            }

            // Store original materials
            StoreOriginalMaterials();
        }

        /// <summary>
        /// Recursively collects all mesh renderers from a transform and its children.
        /// </summary>
        /// <param name="parent">The parent transform to search.</param>
        /// <param name="renderers">The list to add found renderers to.</param>
        private void CollectRenderers(Transform parent, List<MeshRenderer> renderers)
        {
            // Get renderers directly on this level
            MeshRenderer[] directRenderers = parent.GetComponentsInChildren<MeshRenderer>(false);
            renderers.AddRange(directRenderers);
        }

        /// <summary>
        /// Stores the original materials for all renderers.
        /// </summary>
        private void StoreOriginalMaterials()
        {
            originalMaterials.Clear();
            
            // Store outline renderer materials
            foreach (var renderer in outlineRenderers)
            {
                if (renderer != null)
                {
                    originalMaterials[renderer] = renderer.material;
                }
            }
            
            // Store surface renderer materials
            foreach (var renderer in surfaceRenderers)
            {
                if (renderer != null)
                {
                    originalMaterials[renderer] = renderer.material;
                }
            }
        }

        /// <summary>
        /// Highlights the country by changing all mesh materials to highlight materials.
        /// </summary>
        public void Highlight()
        {
            if (isHighlighted) return;
            
            // Change outline materials
            foreach (var renderer in outlineRenderers)
            {
                if (renderer != null && highlightBorderMaterial != null)
                {
                    renderer.material = highlightBorderMaterial;
                }
            }
            
            // Change surface materials
            foreach (var renderer in surfaceRenderers)
            {
                if (renderer != null && highlightSurfaceMaterial != null)
                {
                    renderer.material = highlightSurfaceMaterial;
                }
            }
            
            isHighlighted = true;
        }

        /// <summary>
        /// Returns the country to its normal appearance by restoring original materials.
        /// </summary>
        public void Normalize()
        {
            if (!isHighlighted) return;
            
            // Restore all original materials
            foreach (var kvp in originalMaterials)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.material = kvp.Value;
                }
            }
            
            isHighlighted = false;
        }

        /// <summary>
        /// Toggles between highlighted and normal appearance.
        /// </summary>
        public void ToggleHighlight()
        {
            if (isHighlighted)
                Normalize();
            else
                Highlight();
        }

        /// <summary>
        /// Gets whether the country is currently highlighted.
        /// </summary>
        public bool IsHighlighted => isHighlighted;
    }
}