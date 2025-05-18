using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace GeoJSON
{
    /// <summary>
    /// Handles loading and parsing GeoJSON data.
    /// </summary>
    public class GeoJSONLoader
    {
        public delegate void OnGeoJSONLoadedCallback(GeoJSONFeatureCollection featureCollection);
        
        /// <summary>
        /// Loads GeoJSON data from a URL asynchronously.
        /// </summary>
        /// <param name="url">The URL of the GeoJSON file.</param>
        /// <param name="callback">Callback to invoke when loading is complete.</param>
        public void LoadFromURL(string url, OnGeoJSONLoadedCallback callback)
        {
            UnityWebRequest www = UnityWebRequest.Get(url);
            var operation = www.SendWebRequest();
            
            // Register a callback for when the request completes
            operation.completed += (asyncOperation) =>
            {
                if (www.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        string jsonText = www.downloadHandler.text;
                        GeoJSONFeatureCollection featureCollection = Parse(jsonText);
                        callback?.Invoke(featureCollection);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to parse GeoJSON: {e.Message}");
                        callback?.Invoke(null);
                    }
                }
                else
                {
                    Debug.LogError($"Failed to load GeoJSON: {www.error}");
                    callback?.Invoke(null);
                }
                
                www.Dispose();
            };
        }

        /// <summary>
        /// Loads GeoJSON data from a local file asynchronously.
        /// </summary>
        /// <param name="filePath">The path to the GeoJSON file, relative to the StreamingAssets folder.</param>
        /// <param name="callback">Callback to invoke when loading is complete.</param>
        public void LoadFromFile(string filePath, OnGeoJSONLoadedCallback callback)
        {
            string fullPath = System.IO.Path.Combine(Application.streamingAssetsPath, filePath);
            string url = "file://" + fullPath;
            LoadFromURL(url, callback);
        }

        /// <summary>
        /// Parses a GeoJSON string into a GeoJSONFeatureCollection object.
        /// </summary>
        /// <param name="jsonText">The GeoJSON string to parse.</param>
        /// <returns>A GeoJSONFeatureCollection object containing the parsed data.</returns>
        private GeoJSONFeatureCollection Parse(string jsonText)
        {
            return JsonUtility.FromJson<GeoJSONFeatureCollection>(jsonText);
        }
        
        /// <summary>
        /// Creates a coroutine for loading GeoJSON data. This is useful for MonoBehaviour classes.
        /// </summary>
        /// <param name="url">The URL of the GeoJSON file.</param>
        /// <param name="callback">Callback to invoke when loading is complete.</param>
        public IEnumerator LoadFromURLCoroutine(string url, OnGeoJSONLoadedCallback callback)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                yield return www.SendWebRequest();
                
                if (www.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        string jsonText = www.downloadHandler.text;
                        GeoJSONFeatureCollection featureCollection = Parse(jsonText);
                        callback?.Invoke(featureCollection);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to parse GeoJSON: {e.Message}");
                        callback?.Invoke(null);
                    }
                }
                else
                {
                    Debug.LogError($"Failed to load GeoJSON: {www.error}");
                    callback?.Invoke(null);
                }
            }
        }
        
        /// <summary>
        /// Asynchronously loads GeoJSON data using Task-based async pattern.
        /// </summary>
        /// <param name="url">The URL of the GeoJSON file.</param>
        /// <returns>A Task that resolves to a GeoJSONFeatureCollection object.</returns>
        public async Task<GeoJSONFeatureCollection> LoadFromURLAsync(string url)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                var operation = www.SendWebRequest();
                
                while (!operation.isDone)
                {
                    await Task.Delay(10);
                }
                
                if (www.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        string jsonText = www.downloadHandler.text;
                        return Parse(jsonText);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to parse GeoJSON: {e.Message}");
                        return null;
                    }
                }
                else
                {
                    Debug.LogError($"Failed to load GeoJSON: {www.error}");
                    return null;
                }
            }
        }
    }
}
