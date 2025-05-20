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
        [SerializeField] private Transform countriesParent;
        [SerializeField] private Transform countryOutlineParent;
        [SerializeField] private Transform countrySurfaceParent;


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

            // Create parent objects if they don't exist
            if (countriesParent == null)
            {
                GameObject countriesObj = new GameObject("Countries");
                countriesParent = countriesObj.transform;
                countriesParent.SetParent(transform, false);
            }

            if (countryOutlineParent == null)
            {
                GameObject outlinesObj = new GameObject("Outlines");
                countryOutlineParent = outlinesObj.transform;
                countryOutlineParent.SetParent(countriesParent, false);
            }

            if (countrySurfaceParent == null && createSurface)
            {
                GameObject surfacesObj = new GameObject("Surfaces");
                countrySurfaceParent = surfacesObj.transform;
                countrySurfaceParent.SetParent(countriesParent, false);
            }

            int featureCount = featuresNode.Count;
            foreach (JsonNode featureNode in featuresNode.list)
            {
                countryMeshBuilder.Create(featureNode, transform.position, countryOutlineParent, countrySurfaceParent, createSurface);
            }
        }
    }
}