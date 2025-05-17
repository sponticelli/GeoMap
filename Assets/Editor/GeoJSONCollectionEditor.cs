using UnityEngine;
using UnityEditor;
using GeoData;
using System.Linq;

namespace GeoJSONTools.Editor
{
    [CustomEditor(typeof(GeoJSONCollection))]
    public class GeoJSONCollectionEditor : UnityEditor.Editor
    {
        // Foldout states
        private bool showMetadata = true;
        private bool showStatistics = true;
        private bool showCountries = false;

        // Scrolling positions
        private Vector2 scrollPosition;
        private Vector2 countriesScrollPosition;

        // Search filter
        private string countrySearchFilter = "";

        // Selected country index
        private int selectedCountryIndex = -1;

        public override void OnInspectorGUI()
        {
            var collection = target as GeoJSONCollection;
            if (collection == null) return;

            // Header
            EditorGUILayout.BeginVertical("box");
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("GeoJSON Country Collection", titleStyle);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // Metadata section
            EditorGUILayout.BeginVertical("box");
            showMetadata = EditorGUILayout.Foldout(showMetadata, "Collection Metadata", true);
            if (showMetadata)
            {
                EditorGUI.indentLevel++;

                // Description
                string newDescription = EditorGUILayout.TextArea(collection.description, GUILayout.Height(60));
                if (newDescription != collection.description)
                {
                    Undo.RecordObject(collection, "Change Description");
                    collection.description = newDescription;
                }

                // Version
                string newVersion = EditorGUILayout.TextField("Version", collection.version);
                if (newVersion != collection.version)
                {
                    Undo.RecordObject(collection, "Change Version");
                    collection.version = newVersion;
                }

                // Last updated (read-only)
                EditorGUILayout.LabelField("Last Updated", collection.lastUpdated);

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // Statistics Section
            EditorGUILayout.BeginVertical("box");
            showStatistics = EditorGUILayout.Foldout(showStatistics, "Collection Statistics", true);

            if (showStatistics)
            {
                EditorGUI.indentLevel++;
                DrawStatistics(collection);

                // Draw a bar chart of countries by polygon count
                if (collection.countries != null && collection.countries.Count > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Top 10 Countries by Polygon Count");

                    var topCountries = collection.countries
                        .OrderByDescending(c => c.GetPolygonCount())
                        .Take(10)
                        .ToList();

                    float maxPolygons = topCountries.First().GetPolygonCount();

                    foreach (var country in topCountries)
                    {
                        int polygons = country.GetPolygonCount();
                        float barWidth = (float)polygons / maxPolygons;

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(country.countryName, GUILayout.Width(100));
                        EditorGUILayout.LabelField(polygons.ToString(), GUILayout.Width(50));

                        Rect rect = EditorGUILayout.GetControlRect(false, 10);
                        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width * barWidth, rect.height), new Color(0.3f, 0.6f, 0.9f));

                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Top 10 Countries by Point Count");

                    var topPointCountries = collection.countries
                        .OrderByDescending(c => c.GetTotalPointCount())
                        .Take(10)
                        .ToList();

                    float maxPoints = topPointCountries.First().GetTotalPointCount();

                    foreach (var country in topPointCountries)
                    {
                        int points = country.GetTotalPointCount();
                        float barWidth = (float)points / maxPoints;

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(country.countryName, GUILayout.Width(100));
                        EditorGUILayout.LabelField(points.ToString(), GUILayout.Width(50));

                        Rect rect = EditorGUILayout.GetControlRect(false, 10);
                        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width * barWidth, rect.height), new Color(0.3f, 0.9f, 0.6f));

                        EditorGUILayout.EndHorizontal();
                    }
                }

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // Countries section
            EditorGUILayout.BeginVertical("box");
            showCountries = EditorGUILayout.Foldout(showCountries, "Countries", true);
            if (showCountries && collection.countries != null)
            {
                EditorGUI.indentLevel++;

                // Search filter
                countrySearchFilter = EditorGUILayout.TextField("Search", countrySearchFilter);

                // Country list
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Country List");

                countriesScrollPosition = EditorGUILayout.BeginScrollView(countriesScrollPosition, GUILayout.Height(200));

                var filteredCountries = collection.countries
                    .Where(c => string.IsNullOrEmpty(countrySearchFilter) ||
                                c.countryName.ToLower().Contains(countrySearchFilter.ToLower()) ||
                                c.isoCode.ToLower().Contains(countrySearchFilter.ToLower()))
                    .ToList();

                for (int i = 0; i < filteredCountries.Count; i++)
                {
                    var country = filteredCountries[i];

                    EditorGUILayout.BeginHorizontal();

                    bool isSelected = i == selectedCountryIndex;
                    bool newIsSelected = EditorGUILayout.ToggleLeft(
                        $"{country.countryName} ({country.isoCode})",
                        isSelected,
                        EditorStyles.boldLabel
                    );

                    if (newIsSelected && !isSelected)
                    {
                        selectedCountryIndex = i;
                    }
                    else if (!newIsSelected && isSelected)
                    {
                        selectedCountryIndex = -1;
                    }

                    EditorGUILayout.LabelField($"Polygons: {country.GetPolygonCount()}", GUILayout.Width(100));
                    EditorGUILayout.LabelField($"Points: {country.GetTotalPointCount()}", GUILayout.Width(100));

                    EditorGUILayout.EndHorizontal();

                    // Show details if selected
                    if (isSelected)
                    {
                        EditorGUI.indentLevel++;

                        EditorGUILayout.LabelField("Country Name", country.countryName);
                        EditorGUILayout.LabelField("ISO Code", country.isoCode);
                        EditorGUILayout.LabelField("Geometry Type", country.originalGeometryType.ToString());
                        EditorGUILayout.LabelField("Polygon Count", country.GetPolygonCount().ToString());
                        EditorGUILayout.LabelField("Total Points", country.GetTotalPointCount().ToString());
                        EditorGUILayout.LabelField("Total Rings", country.GetTotalRingCount().ToString());

                        // Show polygon details
                        if (country.polygons != null)
                        {
                            EditorGUILayout.Space();
                            EditorGUILayout.LabelField("Polygons");

                            for (int p = 0; p < country.polygons.Length; p++)
                            {
                                var polygon = country.polygons[p];
                                if (polygon != null)
                                {
                                    EditorGUILayout.LabelField($"Polygon {p + 1}", $"Rings: {polygon.RingCount}, Points: {polygon.TotalPointCount}");
                                }
                            }
                        }

                        EditorGUI.indentLevel--;
                    }
                }

                EditorGUILayout.EndScrollView();

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // Draw default inspector for remaining properties
            DrawPropertiesExcluding(serializedObject, new string[] { "description", "version", "lastUpdated" });

            serializedObject.ApplyModifiedProperties();

            // Action buttons
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Update Statistics"))
            {
                Undo.RecordObject(collection, "Update Statistics");
                collection.UpdateStatistics();
                EditorUtility.SetDirty(collection);
            }

            if (GUILayout.Button("Create Visualizer"))
            {
                CreateVisualizer(collection);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawStatistics(GeoJSONCollection collection)
        {
            collection.UpdateStatistics();

            EditorGUILayout.LabelField($"Total Countries: {collection.CountryCount}");
            EditorGUILayout.LabelField($"Total Polygons: {collection.totalPolygons}");
            EditorGUILayout.LabelField($"Total Points: {collection.totalPoints}");
            EditorGUILayout.LabelField($"Total Rings: {collection.totalRings}");
        }

        /// <summary>
        /// Creates a GeoMapVisualizer GameObject for the collection.
        /// </summary>
        /// <param name="collection">The GeoJSONCollection to visualize.</param>
        private void CreateVisualizer(GeoJSONCollection collection)
        {
            // Create a new GameObject
            GameObject visualizerObject = new GameObject($"GeoMap_{collection.name}");

            // Add the GeoMapVisualizer component
            Visualization.GeoMapVisualizer visualizer = visualizerObject.AddComponent<Visualization.GeoMapVisualizer>();

            // Assign the collection
            visualizer.geoJsonCollection = collection;

            // Create default materials if needed
            if (visualizer.defaultMaterial == null)
            {
                visualizer.defaultMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                visualizer.defaultMaterial.color = new Color(0.3f, 0.6f, 0.9f);
            }

            if (visualizer.highlightMaterial == null)
            {
                visualizer.highlightMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                visualizer.highlightMaterial.color = new Color(0.9f, 0.3f, 0.3f);
            }

            // Select the new GameObject
            Selection.activeGameObject = visualizerObject;

            Debug.Log($"Created GeoMapVisualizer for {collection.name}");
        }
    }
}