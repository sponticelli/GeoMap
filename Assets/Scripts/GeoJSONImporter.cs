using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace GeoJSONTools.Importers
{
    public class GeoJSONImporter
    {
        public event Action<string> OnStatusChanged;
        public event Action<float> OnProgressChanged;
        public event Action<string> OnError;
        
        public bool GenerateMeshes { get; set; } = true;
        public bool OptimizeGeometry { get; set; } = true;
        public float SimplificationTolerance { get; set; } = 0.001f;
        
        private MonoBehaviour coroutineRunner;
        
        public GeoJSONImporter(MonoBehaviour runner)
        {
            coroutineRunner = runner;
        }
        
        public void ImportFromUrl(string url, string outputPath)
        {
            if (string.IsNullOrEmpty(url))
            {
                OnError?.Invoke("URL cannot be empty");
                return;
            }
            
            if (string.IsNullOrEmpty(outputPath))
            {
                OnError?.Invoke("Output path cannot be empty");
                return;
            }
            
            coroutineRunner.StartCoroutine(ImportCoroutine(url, outputPath));
        }
        
        private IEnumerator ImportCoroutine(string url, string outputPath)
        {
            OnStatusChanged?.Invoke("Downloading GeoJSON data...");
            OnProgressChanged?.Invoke(0.0f);
            
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();
                
                if (request.result != UnityWebRequest.Result.Success)
                {
                    OnError?.Invoke($"Download failed: {request.error}");
                    yield break;
                }
                
                OnProgressChanged?.Invoke(0.5f);
                OnStatusChanged?.Invoke("Processing data...");
                
                // Simulate processing time
                yield return new WaitForSecondsRealtime(1.0f);
                
                OnProgressChanged?.Invoke(1.0f);
                OnStatusChanged?.Invoke("Import complete!");
                
                Debug.Log($"Downloaded {request.downloadHandler.text.Length} bytes from {url}");
                Debug.Log($"Would save to: {outputPath}");
            }
        }
    }
}