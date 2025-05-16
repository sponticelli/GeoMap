using UnityEngine;
using UnityEditor;
using Unity.EditorCoroutines.Editor;
using GeoJSONTools.Importers;
using System.Collections;
using UnityEngine.Networking;
using System.IO;
using GeoData;
using System;
using System.Collections.Generic;

namespace GeoJSONTools.Editor
{
    /// <summary>
    /// Editor window for downloading, converting, and creating GeoJSON assets.
    /// Provides a user-friendly interface for importing country data from GeoJSON sources.
    /// </summary>
    public class GeoJSONImporterWindow : EditorWindow, IHasCustomMenu
    {
        private string sourceURL = "https://raw.githubusercontent.com/datasets/geo-countries/master/data/countries.geojson";
        private string outputFolder = "Assets/GeoData";
        private string assetName = "WorldCountries";

        // Import options
        private bool optimizeGeometry = true;
        private float simplificationTolerance = 0.001f;
        private bool generateMeshes = true;
        private bool showAdvancedOptions = false;

        // State tracking
        private bool isImporting = false;
        private float importProgress = 0.0f;
        private string currentStatus = "Ready to import";
        private string errorMessage = "";

        // Coroutine reference
        private Unity.EditorCoroutines.Editor.EditorCoroutine importCoroutine;

        [MenuItem("Tools/GeoJSON/Country Importer")]
        public static void ShowWindow()
        {
            var window = GetWindow<GeoJSONImporterWindow>("GeoJSON Country Importer");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnGUI()
        {
            // Apply custom styling
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };

            // Header
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("GeoJSON Country Importer", headerStyle);
            EditorGUILayout.Space();

            // Configuration Section
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);

            EditorGUILayout.LabelField("Source URL:");
            sourceURL = EditorGUILayout.TextField(sourceURL);

            EditorGUILayout.LabelField("Output Folder:");
            EditorGUILayout.BeginHorizontal();
            outputFolder = EditorGUILayout.TextField(outputFolder);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("Select Output Folder", "Assets", "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    // Convert to relative path
                    string assetsPath = Application.dataPath;
                    if (selectedPath.StartsWith(assetsPath))
                    {
                        outputFolder = "Assets" + selectedPath.Substring(assetsPath.Length);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Asset Name:");
            assetName = EditorGUILayout.TextField(assetName);

            // Advanced Options Foldout
            showAdvancedOptions = EditorGUILayout.Foldout(showAdvancedOptions, "Advanced Options", true);
            if (showAdvancedOptions)
            {
                EditorGUI.indentLevel++;

                optimizeGeometry = EditorGUILayout.Toggle(new GUIContent("Optimize Geometry",
                    "Simplify polygons to reduce point count while maintaining shape"), optimizeGeometry);

                EditorGUI.BeginDisabledGroup(!optimizeGeometry);
                simplificationTolerance = EditorGUILayout.Slider(new GUIContent("Simplification Tolerance",
                    "Higher values result in fewer points but less accuracy (0.0001-0.01)"),
                    simplificationTolerance, 0.0001f, 0.01f);
                EditorGUI.EndDisabledGroup();

                generateMeshes = EditorGUILayout.Toggle(new GUIContent("Generate Meshes",
                    "Create mesh assets for each country"), generateMeshes);

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // Import Section
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Import", EditorStyles.boldLabel);

            bool isValid = !string.IsNullOrEmpty(sourceURL) && !string.IsNullOrEmpty(outputFolder) && !string.IsNullOrEmpty(assetName);

            EditorGUI.BeginDisabledGroup(isImporting || !isValid);
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold
            };

            if (GUILayout.Button(isImporting ? "Importing..." : "Download & Convert GeoJSON", buttonStyle, GUILayout.Height(30)))
            {
                StartImport();
            }
            EditorGUI.EndDisabledGroup();

            if (isImporting)
            {
                if (GUILayout.Button("Cancel Import"))
                {
                    CancelImport();
                }
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // Status Section
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(currentStatus);

            if (isImporting)
            {
                Rect progressRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(20));
                EditorGUI.ProgressBar(progressRect, importProgress, $"{(importProgress * 100):F1}%");
            }

            if (!string.IsNullOrEmpty(errorMessage))
            {
                EditorGUILayout.HelpBox(errorMessage, MessageType.Error);
                if (GUILayout.Button("Clear Error"))
                {
                    errorMessage = "";
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void StartImport()
        {
            Debug.Log("GeoJSON Import Started");
            isImporting = true;
            currentStatus = "Starting import...";
            importProgress = 0.0f;
            errorMessage = "";

            // Create output directory if it doesn't exist
            if (!AssetDatabase.IsValidFolder(outputFolder))
            {
                string[] folders = outputFolder.Split('/');
                string currentPath = folders[0];

                for (int i = 1; i < folders.Length; i++)
                {
                    string newPath = currentPath + "/" + folders[i];
                    if (!AssetDatabase.IsValidFolder(newPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, folders[i]);
                    }
                    currentPath = newPath;
                }
            }

            // Start the import coroutine using the extension method for EditorWindow
            importCoroutine = this.StartCoroutine(ImportCoroutine());
        }

        private void CancelImport()
        {
            if (importCoroutine != null)
            {
                EditorCoroutineUtility.StopCoroutine(importCoroutine);
                importCoroutine = null;
            }

            isImporting = false;
            currentStatus = "Import cancelled";
            Repaint();
        }

        private IEnumerator ImportCoroutine()
        {
            // Download the GeoJSON data
            currentStatus = "Downloading GeoJSON data...";
            importProgress = 0.0f;
            Repaint();

            UnityWebRequest request = UnityWebRequest.Get(sourceURL);
            yield return request.SendWebRequest();

            // Check for download errors
            if (request.result != UnityWebRequest.Result.Success)
            {
                HandleError($"Download failed: {request.error}");
                yield break;
            }

            string jsonData = request.downloadHandler.text;
            if (string.IsNullOrEmpty(jsonData))
            {
                HandleError("Downloaded data is empty");
                yield break;
            }

            // Update progress
            importProgress = 0.3f;
            currentStatus = "Parsing GeoJSON data...";
            Repaint();

            // Give the UI a chance to update
            yield return null;

            // Parse the GeoJSON data
            GeoJSONCollection collection = null;
            try
            {
                collection = ParseGeoJSON(jsonData);

                if (collection == null)
                {
                    HandleError("Failed to parse GeoJSON data");
                    yield break;
                }
            }
            catch (Exception ex)
            {
                HandleError($"Error parsing GeoJSON: {ex.Message}");
                yield break;
            }

            // Update progress
            importProgress = 0.7f;
            currentStatus = "Creating asset...";
            Repaint();

            // Give the UI a chance to update
            yield return null;

            // Save the collection as an asset
            string assetPath = $"{outputFolder}/{assetName}.asset";
            try
            {
                // Create the asset
                AssetDatabase.CreateAsset(collection, assetPath);

                // Save the changes
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // Select the new asset
                Selection.activeObject = collection;

                // Complete the import
                CompleteImport(assetPath);
            }
            catch (Exception ex)
            {
                HandleError($"Error creating asset: {ex.Message}");
            }
        }

        /// <summary>
        /// Parses GeoJSON data into a GeoJSONCollection using the GeoJSONParser utility.
        /// </summary>
        /// <param name="jsonData">The raw GeoJSON string data to parse.</param>
        /// <returns>A populated GeoJSONCollection object.</returns>
        private GeoJSONCollection ParseGeoJSON(string jsonData)
        {
            if (string.IsNullOrEmpty(jsonData))
            {
                throw new ArgumentException("GeoJSON data cannot be empty");
            }

            try
            {
                // Use the GeoJSONParser utility to parse the data
                // Pass the optimization settings from the editor window
                return GeoData.GeoJSONParser.ParseGeoJSON(
                    jsonData,
                    optimizeGeometry,
                    simplificationTolerance
                );
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error parsing GeoJSON: {ex.Message}");
                throw;
            }
        }

        private void CompleteImport(string assetPath)
        {
            importProgress = 1.0f;
            currentStatus = "Import completed successfully!";
            isImporting = false;

            Debug.Log($"GeoJSON import completed: {assetPath}");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GeoJSONCollection>(assetPath));
            Repaint();
        }

        private void HandleError(string message)
        {
            Debug.LogError($"GeoJSON Import Error: {message}");
            errorMessage = message;
            isImporting = false;
            importCoroutine = null;
            Repaint();
        }

        /// <summary>
        /// Adds custom menu items to the window's gear menu.
        /// </summary>
        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Reset to Defaults"), false, ResetToDefaults);
            menu.AddItem(new GUIContent("Open Documentation"), false, OpenDocumentation);
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("About"), false, ShowAboutInfo);
        }

        /// <summary>
        /// Resets all settings to their default values.
        /// </summary>
        private void ResetToDefaults()
        {
            sourceURL = "https://raw.githubusercontent.com/datasets/geo-countries/master/data/countries.geojson";
            outputFolder = "Assets/GeoData";
            assetName = "WorldCountries";
            optimizeGeometry = true;
            simplificationTolerance = 0.001f;
            generateMeshes = true;
            Repaint();
        }

        /// <summary>
        /// Opens the documentation for the GeoJSON importer.
        /// </summary>
        private void OpenDocumentation()
        {
            Application.OpenURL("https://github.com/datasets/geo-countries");
        }

        /// <summary>
        /// Shows information about the GeoJSON importer.
        /// </summary>
        private void ShowAboutInfo()
        {
            EditorUtility.DisplayDialog("About GeoJSON Importer",
                "GeoJSON Country Importer\n\n" +
                "A tool for downloading, converting, and creating GeoJSON assets in Unity.\n\n" +
                "Version: 1.0.0",
                "OK");
        }
    }
}