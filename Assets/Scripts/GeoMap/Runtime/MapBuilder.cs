using GeoMap.Utils;
using UnityEngine;

namespace GeoMap
{
    public class MapBuilder : MonoBehaviour
    {
        [SerializeField] private TextAsset geoGeometryJson;


        private void Start()
        {
            BuildMap();
        }

        private void BuildMap()
        {
            JsonNode rootNode = new JsonNode(geoGeometryJson.text);

            JsonNode featuresNode = rootNode["features"];
            if (featuresNode == null)
            {
                Debug.LogError("Failed to find features in GeoJSON");
                return;
            }

            int featureCount = featuresNode.Count;
            foreach (JsonNode featureNode in featuresNode.list)
            {
                Debug.Log(featureNode["properties"]["name"]);

            }
        }
    }
}