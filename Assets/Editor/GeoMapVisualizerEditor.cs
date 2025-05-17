using UnityEditor;
using UnityEngine;
using Visualization;

namespace GeoData.Editor
{
    /// <summary>
    /// Custom editor for the GeoMapVisualizer component.
    /// Provides a user-friendly interface for configuring and controlling the map visualization.
    /// </summary>
    [CustomEditor(typeof(GeoMapVisualizer))]
    public class GeoMapVisualizerEditor : UnityEditor.Editor
    {
        // Foldout states
        private bool showDataSettings = true;
        private bool showVisualizationSettings = true;
        private bool showMaterialSettings = true;
        private bool showCameraSettings = true;
        private bool showDebugInfo = false;
        
        /// <summary>
        /// Called when the inspector GUI is drawn.
        /// </summary>
        public override void OnInspectorGUI()
        {
            GeoMapVisualizer visualizer = (GeoMapVisualizer)target;
            
            EditorGUI.BeginChangeCheck();
            
            // Data Settings
            showDataSettings = EditorGUILayout.Foldout(showDataSettings, "Data Settings", true);
            if (showDataSettings)
            {
                EditorGUI.indentLevel++;
                
                // GeoJSONCollection field
                GeoJSONCollection newCollection = (GeoJSONCollection)EditorGUILayout.ObjectField(
                    new GUIContent("GeoJSON Collection", "The GeoJSONCollection asset to visualize"),
                    visualizer.geoJsonCollection,
                    typeof(GeoJSONCollection),
                    false
                );
                
                // If the collection changed, update it
                if (newCollection != visualizer.geoJsonCollection)
                {
                    Undo.RecordObject(visualizer, "Change GeoJSON Collection");
                    visualizer.geoJsonCollection = newCollection;
                    
                    // Auto-render if in play mode
                    if (Application.isPlaying && newCollection != null)
                    {
                        visualizer.RenderMap();
                    }
                }
                
                // Display collection info if available
                if (visualizer.geoJsonCollection != null)
                {
                    EditorGUILayout.LabelField("Countries", visualizer.geoJsonCollection.CountryCount.ToString());
                    EditorGUILayout.LabelField("Total Polygons", visualizer.geoJsonCollection.totalPolygons.ToString());
                    EditorGUILayout.LabelField("Total Points", visualizer.geoJsonCollection.totalPoints.ToString());
                }
                
                EditorGUI.indentLevel--;
            }
            
            // Visualization Settings
            showVisualizationSettings = EditorGUILayout.Foldout(showVisualizationSettings, "Visualization Settings", true);
            if (showVisualizationSettings)
            {
                EditorGUI.indentLevel++;
                
                // Projection type
                GeoCoordinateConverter.ProjectionType newProjection = (GeoCoordinateConverter.ProjectionType)EditorGUILayout.EnumPopup(
                    new GUIContent("Projection Type", "The map projection to use"),
                    visualizer.projectionType
                );
                
                if (newProjection != visualizer.projectionType)
                {
                    Undo.RecordObject(visualizer, "Change Projection Type");
                    visualizer.projectionType = newProjection;
                    
                    // Auto-update if in play mode
                    if (Application.isPlaying)
                    {
                        visualizer.SetProjection(newProjection);
                    }
                }
                
                // Map scale
                float newScale = EditorGUILayout.Slider(
                    new GUIContent("Map Scale", "Scale factor for the map"),
                    visualizer.mapScale,
                    1f,
                    50f
                );
                
                if (newScale != visualizer.mapScale)
                {
                    Undo.RecordObject(visualizer, "Change Map Scale");
                    visualizer.mapScale = newScale;
                    
                    // Auto-update if in play mode
                    if (Application.isPlaying)
                    {
                        visualizer.SetMapScale(newScale);
                    }
                }
                
                // Extrusion height
                float newHeight = EditorGUILayout.Slider(
                    new GUIContent("Extrusion Height", "Height of the extruded country meshes"),
                    visualizer.extrusionHeight,
                    0.01f,
                    1f
                );
                
                if (newHeight != visualizer.extrusionHeight)
                {
                    Undo.RecordObject(visualizer, "Change Extrusion Height");
                    visualizer.extrusionHeight = newHeight;
                }
                
                // Separate polygon meshes
                bool newSeparatePolygons = EditorGUILayout.Toggle(
                    new GUIContent("Separate Polygon Meshes", "Generate separate meshes for each polygon in a country"),
                    visualizer.separatePolygonMeshes
                );
                
                if (newSeparatePolygons != visualizer.separatePolygonMeshes)
                {
                    Undo.RecordObject(visualizer, "Change Separate Polygon Meshes");
                    visualizer.separatePolygonMeshes = newSeparatePolygons;
                }
                
                EditorGUI.indentLevel--;
            }
            
            // Material Settings
            showMaterialSettings = EditorGUILayout.Foldout(showMaterialSettings, "Material Settings", true);
            if (showMaterialSettings)
            {
                EditorGUI.indentLevel++;
                
                // Default material
                Material newDefaultMaterial = (Material)EditorGUILayout.ObjectField(
                    new GUIContent("Default Material", "Material for country meshes"),
                    visualizer.defaultMaterial,
                    typeof(Material),
                    false
                );
                
                if (newDefaultMaterial != visualizer.defaultMaterial)
                {
                    Undo.RecordObject(visualizer, "Change Default Material");
                    visualizer.defaultMaterial = newDefaultMaterial;
                }
                
                // Highlight material
                Material newHighlightMaterial = (Material)EditorGUILayout.ObjectField(
                    new GUIContent("Highlight Material", "Material for highlighted countries"),
                    visualizer.highlightMaterial,
                    typeof(Material),
                    false
                );
                
                if (newHighlightMaterial != visualizer.highlightMaterial)
                {
                    Undo.RecordObject(visualizer, "Change Highlight Material");
                    visualizer.highlightMaterial = newHighlightMaterial;
                }
                
                EditorGUI.indentLevel--;
            }
            
            // Camera Settings
            showCameraSettings = EditorGUILayout.Foldout(showCameraSettings, "Camera Settings", true);
            if (showCameraSettings)
            {
                EditorGUI.indentLevel++;
                
                // Map camera
                Camera newCamera = (Camera)EditorGUILayout.ObjectField(
                    new GUIContent("Map Camera", "Camera used to view the map"),
                    visualizer.mapCamera,
                    typeof(Camera),
                    true
                );
                
                if (newCamera != visualizer.mapCamera)
                {
                    Undo.RecordObject(visualizer, "Change Map Camera");
                    visualizer.mapCamera = newCamera;
                }
                
                // Enable camera controls
                bool newEnableCameraControls = EditorGUILayout.Toggle(
                    new GUIContent("Enable Camera Controls", "Enable camera panning, zooming, and rotation"),
                    visualizer.enableCameraControls
                );
                
                if (newEnableCameraControls != visualizer.enableCameraControls)
                {
                    Undo.RecordObject(visualizer, "Change Enable Camera Controls");
                    visualizer.enableCameraControls = newEnableCameraControls;
                }
                
                EditorGUI.indentLevel--;
            }
            
            // Debug Info
            showDebugInfo = EditorGUILayout.Foldout(showDebugInfo, "Debug Info", true);
            if (showDebugInfo && visualizer.geoJsonCollection != null)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.LabelField("Collection Name", visualizer.geoJsonCollection.name);
                EditorGUILayout.LabelField("Version", visualizer.geoJsonCollection.version);
                EditorGUILayout.LabelField("Last Updated", visualizer.geoJsonCollection.lastUpdated);
                EditorGUILayout.LabelField("Total Countries", visualizer.geoJsonCollection.CountryCount.ToString());
                EditorGUILayout.LabelField("Total Polygons", visualizer.geoJsonCollection.totalPolygons.ToString());
                EditorGUILayout.LabelField("Total Points", visualizer.geoJsonCollection.totalPoints.ToString());
                EditorGUILayout.LabelField("Total Rings", visualizer.geoJsonCollection.totalRings.ToString());
                
                EditorGUI.indentLevel--;
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
            
            // Action buttons
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Render Map"))
            {
                if (Application.isPlaying)
                {
                    visualizer.RenderMap();
                }
                else
                {
                    EditorUtility.DisplayDialog("Cannot Render in Edit Mode", 
                        "Map rendering is only available in Play Mode. Please enter Play Mode to render the map.", 
                        "OK");
                }
            }
            
            if (GUILayout.Button("Clear Map"))
            {
                if (Application.isPlaying)
                {
                    visualizer.ClearMap();
                }
                else
                {
                    EditorUtility.DisplayDialog("Cannot Clear in Edit Mode", 
                        "Map clearing is only available in Play Mode. Please enter Play Mode to clear the map.", 
                        "OK");
                }
            }
            
            if (GUILayout.Button("Reset Camera"))
            {
                if (Application.isPlaying)
                {
                    visualizer.ResetCamera();
                }
                else
                {
                    EditorUtility.DisplayDialog("Cannot Reset Camera in Edit Mode", 
                        "Camera reset is only available in Play Mode. Please enter Play Mode to reset the camera.", 
                        "OK");
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }
    }
}
