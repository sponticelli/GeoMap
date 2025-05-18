using GeoMap.Utils;
using UnityEngine;

namespace GeoMap
{
    public class MapBuilder : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private TextAsset geoGeometryJson;
        [SerializeField] private bool createSurface = true;

        [Header("References")]
        [SerializeField] private CountryMeshBuilder countryMeshBuilder;
        [SerializeField] private Transform countryOutlineParent;
        [SerializeField] private Transform countrySurfacdParent;
        

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
                countryMeshBuilder.Create(featureNode, transform.position, countryOutlineParent, countrySurfacdParent,createSurface);
            }
        }
    }
}