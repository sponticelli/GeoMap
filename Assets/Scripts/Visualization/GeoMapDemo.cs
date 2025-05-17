using GeoData;
using UnityEngine;

namespace Visualization
{
    /// <summary>
    /// Demo component for showcasing the GeoMap visualization.
    /// Provides a simple UI for controlling the map and displaying country information.
    /// </summary>
    public class GeoMapDemo : MonoBehaviour
    {
        /// <summary>
        /// Reference to the GeoMapVisualizer component.
        /// </summary>
        [SerializeField]
        public GeoMapVisualizer mapVisualizer;

        /// <summary>
        /// The GeoJSONCollection asset to visualize.
        /// </summary>
        [SerializeField]
        public GeoJSONCollection geoJsonCollection;

        // Custom GUI styles
        private GUIStyle titleStyle;

        /// <summary>
        /// Called when the component is initialized.
        /// </summary>
        private void Start()
        {
            if (mapVisualizer == null)
            {
                mapVisualizer = GetComponent<GeoMapVisualizer>();

                if (mapVisualizer == null)
                {
                    Debug.LogError("GeoMapDemo requires a GeoMapVisualizer component");
                    return;
                }
            }

            // Set the GeoJSONCollection if one is assigned
            if (geoJsonCollection != null && mapVisualizer.geoJsonCollection == null)
            {
                mapVisualizer.SetGeoJSONCollection(geoJsonCollection);
            }

            // Initialize GUI styles
            InitializeGUIStyles();
        }

        /// <summary>
        /// Initializes custom GUI styles for the demo UI.
        /// </summary>
        private void InitializeGUIStyles()
        {
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };
        }

        /// <summary>
        /// Called when GUI should be rendered.
        /// </summary>
        private void OnGUI()
        {
            // Make sure styles are initialized
            if (titleStyle == null)
            {
                InitializeGUIStyles();
            }

            GUILayout.BeginArea(new Rect(10, 10, 200, 300));
            GUILayout.BeginVertical("box");

            GUILayout.Label("GeoMap Demo", titleStyle);

            if (mapVisualizer != null && mapVisualizer.geoJsonCollection != null)
            {
                GUILayout.Label($"Countries: {mapVisualizer.geoJsonCollection.CountryCount}");
                GUILayout.Label($"Polygons: {mapVisualizer.geoJsonCollection.totalPolygons}");
                GUILayout.Label($"Points: {mapVisualizer.geoJsonCollection.totalPoints}");

                GUILayout.Space(10);

                // Projection selector
                GUILayout.Label("Projection:");
                if (GUILayout.Button("Equirectangular"))
                {
                    mapVisualizer.SetProjection(GeoCoordinateConverter.ProjectionType.Equirectangular);
                }
                if (GUILayout.Button("Mercator"))
                {
                    mapVisualizer.SetProjection(GeoCoordinateConverter.ProjectionType.Mercator);
                }

                GUILayout.Space(10);

                // Scale slider
                GUILayout.Label($"Scale: {mapVisualizer.mapScale:F1}");
                float newScale = GUILayout.HorizontalSlider(mapVisualizer.mapScale, 1f, 50f);
                if (Mathf.Abs(newScale - mapVisualizer.mapScale) > 0.1f)
                {
                    mapVisualizer.SetMapScale(newScale);
                }

                GUILayout.Space(10);

                // Camera controls
                if (GUILayout.Button("Reset Camera"))
                {
                    mapVisualizer.ResetCamera();
                }
            }
            else
            {
                GUILayout.Label("No GeoJSONCollection assigned");
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
