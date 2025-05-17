using System;
using System.IO;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace GeoMap.Editor
{
    /// <summary>
    /// Editor window that allows downloading GeoJSON data from a URL and saving it to the project.
    /// </summary>
    public class GeoJSONDownloader : EditorWindow
    {
        private const string GeoJSONUrl = "https://raw.githubusercontent.com/datasets/geo-countries/master/data/countries.geojson";
        private const string SavePath = "Assets/Data/countries.json";
        
        private bool _isDownloading;
        private float _downloadProgress;
        private string _statusMessage = "";
        private EditorCoroutine _downloadCoroutine;

        [MenuItem("GeoMap/Download GeoJSON")]
        public static void ShowWindow()
        {
            GeoJSONDownloader window = GetWindow<GeoJSONDownloader>("GeoJSON Downloader");
            window.minSize = new Vector2(400, 200);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("GeoJSON Downloader", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("This tool downloads country GeoJSON data from:");
            EditorGUILayout.SelectableLabel(GeoJSONUrl, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("The file will be saved to:");
            EditorGUILayout.SelectableLabel(SavePath, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.Space(10);

            GUI.enabled = !_isDownloading;
            if (GUILayout.Button("Download GeoJSON", GUILayout.Height(30)))
            {
                StartDownload();
            }
            GUI.enabled = true;
            
            EditorGUILayout.Space(10);
            
            if (_isDownloading)
            {
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(GUILayout.Height(20)), _downloadProgress, "Downloading...");
            }
            
            if (!string.IsNullOrEmpty(_statusMessage))
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox(_statusMessage, _statusMessage.Contains("Error") ? MessageType.Error : MessageType.Info);
            }
        }

        private void StartDownload()
        {
            if (_isDownloading)
                return;
            
            _isDownloading = true;
            _downloadProgress = 0f;
            _statusMessage = "Starting download...";
            
            _downloadCoroutine = EditorCoroutineUtility.StartCoroutine(DownloadGeoJSON(), this);
        }

        private System.Collections.IEnumerator DownloadGeoJSON()
        {
            UnityWebRequest www = UnityWebRequest.Get(GeoJSONUrl);
            
            // Send the request and wait for it to complete
            var operation = www.SendWebRequest();
            
            while (!operation.isDone)
            {
                _downloadProgress = www.downloadProgress;
                _statusMessage = $"Downloading... {(_downloadProgress * 100):F0}%";
                yield return null;
            }
            
            if (www.result != UnityWebRequest.Result.Success)
            {
                _statusMessage = $"Error: {www.error}";
                Debug.LogError($"Failed to download GeoJSON: {www.error}");
            }
            else
            {
                try
                {
                    // Ensure the directory exists
                    string directory = Path.GetDirectoryName(SavePath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    
                    // Write the downloaded data to file
                    File.WriteAllText(SavePath, www.downloadHandler.text);
                    
                    _statusMessage = "Download completed successfully!";
                    Debug.Log($"GeoJSON downloaded and saved to {SavePath}");
                    
                    // Refresh the AssetDatabase to show the new file
                    AssetDatabase.Refresh();
                }
                catch (Exception e)
                {
                    _statusMessage = $"Error saving file: {e.Message}";
                    Debug.LogError($"Failed to save GeoJSON: {e.Message}");
                }
            }
            
            _isDownloading = false;
            www.Dispose();
        }

        private void OnDestroy()
        {
            // Clean up the coroutine if the window is closed during download
            if (_isDownloading && _downloadCoroutine != null)
            {
                EditorCoroutineUtility.StopCoroutine(_downloadCoroutine);
                _isDownloading = false;
            }
        }
    }
}