using System.Collections.Generic;
using Ludo.ComputationalGeometry;
using GeoMap.MathUtils;
using GeoMap.Utils;
using UnityEngine;

namespace GeoMap
{
    public class CountryMeshBuilder : MonoBehaviour
    {
        [Header("Border Settings")]
        [SerializeField] private float borderWidth = 0.4f;
        [SerializeField] private Material borderMaterial;

        [Header("Surface Settings")]
        [SerializeField] private Material surfaceMaterial;

        public void Create(JsonNode country, Vector3 center, Transform outlineParent, Transform surfaceParent,
            bool createSurface)
        {
            // Check if the geometry type is supported
            string geometryType = country["geometry"]["type"].str;
            if (geometryType != "Polygon" && geometryType != "MultiPolygon") return;

            // Get the country name
            string countryName = GetCountryName(country);

            // Create main GameObject for the country
            GameObject countryMainObject = new GameObject(countryName);
            countryMainObject.transform.SetParent(outlineParent.parent, false);

            // Create child GameObjects for outlines and surfaces
            GameObject countryOutlinesObject = new GameObject("Outlines");
            countryOutlinesObject.transform.SetParent(countryMainObject.transform, false);

            GameObject countrySurfacesObject = null;
            if (createSurface)
            {
                countrySurfacesObject = new GameObject("Surfaces");
                countrySurfacesObject.transform.SetParent(countryMainObject.transform, false);
            }

            // Create parent GameObjects for MultiPolygon types
            GameObject borderParent = null;
            GameObject surfaceLayerParent = null;

            if (geometryType == "MultiPolygon")
            {
                // Create parent for border meshes
                borderParent = new GameObject(countryName);
                borderParent.transform.SetParent(countryOutlinesObject.transform, false);

                // Create parent for surface meshes if needed
                if (createSurface)
                {
                    surfaceLayerParent = new GameObject(countryName + " Surface");
                    surfaceLayerParent.transform.SetParent(countrySurfacesObject.transform, false);
                }
            }

            // Process each polygon in the coordinates
            int polygonIndex = 0;
            foreach (var coordinateSet in country["geometry"]["coordinates"].list)
            {
                var processedCoordinates = ExtractCoordinates(coordinateSet);
                if (processedCoordinates.list == null) continue;

                var vertexCount = processedCoordinates.list.Count;
                if (vertexCount < 3) continue;

                List<Vector3> boundaryVertices = new List<Vector3>();
                boundaryVertices.Clear();

                for (int i = 0; i < vertexCount; i++)
                {
                    var coordinate = processedCoordinates.list[i];
                    var worldPosition = GeoConvert.LatLonToMetersForEarth(coordinate[1].f, coordinate[0].f);
                    worldPosition += new Vector3d(center.x, center.y, center.z);
                    boundaryVertices.Add((Vector3)worldPosition);
                }

                // Build border mesh with appropriate parent
                Transform borderParentTransform = (geometryType == "MultiPolygon") ?
                    borderParent.transform :
                    countryOutlinesObject.transform;

                string borderName = (geometryType == "MultiPolygon") ? $"Part_{polygonIndex}" : countryName;
                var borderMeshContainer = BuildBorderMesh(borderName, borderParentTransform, boundaryVertices);

                // Build surface mesh with appropriate parent if needed
                if (createSurface)
                {
                    Transform surfaceParentTransform = (geometryType == "MultiPolygon") ?
                        surfaceLayerParent.transform :
                        countrySurfacesObject.transform;

                    string surfaceName = (geometryType == "MultiPolygon") ? $"Part_{polygonIndex}" : countryName;
                    BuildSurfaceMesh(surfaceName, surfaceParentTransform, boundaryVertices, geometryType == "Polygon");
                }

                polygonIndex++;
            }
        }

        private void BuildSurfaceMesh(string name, Transform parentTransform, List<Vector3> boundaryVertices, bool createLayer = true)
        {
            var surfaceMeshObject = new GameObject("mesh").AddComponent<MeshFilter>();
            surfaceMeshObject.gameObject.AddComponent<MeshRenderer>();
            var surfaceMesh = surfaceMeshObject.mesh;
            var points = boundaryVertices;
            var meshData = new MeshData();
            var meshInput = new MeshInputData(points.Count);
            for (int vertexIndex = 0; vertexIndex < points.Count; vertexIndex++)
            {
                meshInput.AddPoint(points[vertexIndex].x, points[vertexIndex].y);
                meshInput.AddSegment(vertexIndex, (vertexIndex + 1) % points.Count);
            }

            CreateMeshForPolygon(meshInput, meshData, points, false);

            surfaceMesh.vertices = meshData.Vertices.ToArray();
            surfaceMesh.triangles = meshData.Indices.ToArray();
            surfaceMesh.SetUVs(0, meshData.UV);
            surfaceMesh.RecalculateNormals();

            surfaceMeshObject.GetComponent<MeshRenderer>().material = surfaceMaterial;
            surfaceMeshObject.transform.name = name;

            if (name.Contains("Lesotho"))
                surfaceMeshObject.transform.localPosition -= new Vector3(0, 0, 1);

            // Set parent directly - our hierarchy is already properly structured
            surfaceMeshObject.transform.SetParent(parentTransform, false);

            surfaceMeshObject.gameObject.AddComponent<MeshCollider>();
        }

        private MeshFilter BuildBorderMesh(string name, Transform parentTransform, List<Vector3> boundaryVertices)
        {
            var borderMeshObject = new GameObject("mesh").AddComponent<MeshFilter>();
            borderMeshObject.gameObject.AddComponent<MeshRenderer>();
            var borderMesh = borderMeshObject.mesh;

            var meshData = new MeshData();

            CreateMeshForLinestring(boundaryVertices, meshData, borderWidth);
            borderMesh.vertices = meshData.Vertices.ToArray();
            borderMesh.triangles = meshData.Indices.ToArray();
            borderMesh.SetUVs(0, meshData.UV);
            borderMesh.RecalculateNormals();

            borderMeshObject.name = name;

            borderMeshObject.GetComponent<MeshRenderer>().material = borderMaterial;
            borderMeshObject.transform.SetParent(parentTransform, false);

            return borderMeshObject;
        }

        private string GetCountryName(JsonNode country)
        {
            if (country["properties"]["id"] != null)
                return country["properties"]["id"].ToString();
            if (country["properties"]["name"] != null)
                return country["properties"]["name"].ToString();
            return "Unknown";
        }

        private static JsonNode ExtractCoordinates(JsonNode coordinateNode)
        {
            JsonNode processedNode = null;
            if (coordinateNode.list == null)
            {
                processedNode = coordinateNode;
            }
            else if (coordinateNode.list.Count == 0)
            {
                processedNode = coordinateNode;
            }
            else if (coordinateNode.list[0].list == null)
            {
                processedNode = coordinateNode;
            }
            else if (coordinateNode.list[0].list.Count == 0)
            {
                processedNode = coordinateNode;
            }
            else processedNode = (coordinateNode.list[0].list[0].IsArray) ? coordinateNode.list[0] : coordinateNode;

            return processedNode;
        }

        private void CreateMeshForPolygon(MeshInputData corners, MeshData meshData, List<Vector3> vertices, bool reverseWinding)
        {
            var triangularMesh = new TriangularMesh();
            triangularMesh.Behavior.Algorithm = TriangulationAlgorithm.SweepLine;
            triangularMesh.Behavior.Quality = true;
            triangularMesh.Triangulate(corners);
            var verticesStartIndex = meshData.Vertices.Count;
            meshData.Vertices = vertices;
            foreach (var triangle in triangularMesh.Triangles)
            {
                if ((triangle.P0 >= corners.Count) || (triangle.P1 >= corners.Count) || (triangle.P2 >= corners.Count)) continue;

                if (!reverseWinding)
                {
                    meshData.Indices.Add(verticesStartIndex + triangle.P1);
                    meshData.Indices.Add(verticesStartIndex + triangle.P0);
                    meshData.Indices.Add(verticesStartIndex + triangle.P2);
                }
                else
                {
                    meshData.Indices.Add(verticesStartIndex + triangle.P0);
                    meshData.Indices.Add(verticesStartIndex + triangle.P1);
                    meshData.Indices.Add(verticesStartIndex + triangle.P2);
                }
            }
        }


        private void CreateMeshForLinestring(List<Vector3> vertices, MeshData meshData, float width)
        {
            var verticesStartIndex = meshData.Vertices.Count;
            Vector3 lastPosition = Vector3.zero;
            var normal = Vector3.zero;
            for (int i = 1; i < vertices.Count; i++)
            {
                var previousPoint = vertices[i - 1];
                var currentPoint = vertices[i];
                var nextPoint = currentPoint;
                if (i + 1 < vertices.Count)
                    nextPoint = vertices[i + 1];
                if (lastPosition == Vector3.zero)
                {
                    lastPosition = Vector3.Lerp(previousPoint, currentPoint, 0f);
                    normal = GetNormal(previousPoint, lastPosition, currentPoint) * width;
                    meshData.Vertices.Add(lastPosition + normal);
                    meshData.Vertices.Add(lastPosition - normal);
                }

                lastPosition = Vector3.Lerp(previousPoint, currentPoint, 1f); // currentPoint
                normal = GetNormal(previousPoint, lastPosition, nextPoint) * width;
                meshData.Vertices.Add(lastPosition + normal);
                meshData.Vertices.Add(lastPosition - normal);
            }

            for (int vertexIndex = verticesStartIndex; vertexIndex <= meshData.Vertices.Count - 3; vertexIndex += 2)
            {
                meshData.Indices.Add(vertexIndex);
                meshData.Indices.Add(vertexIndex + 2);
                meshData.Indices.Add(vertexIndex + 1);
                meshData.Indices.Add(vertexIndex + 1);
                meshData.Indices.Add(vertexIndex + 2);
                meshData.Indices.Add(vertexIndex + 3);
            }
        }

        private Vector3 GetNormal(Vector3 previousPoint, Vector3 currentPoint, Vector3 nextPoint)
        {
            if (currentPoint == previousPoint || currentPoint == nextPoint)
            {
                var direction = (nextPoint - previousPoint).normalized;
                return new Vector3(-direction.y, direction.x, 0);
            }

            var vectorToNext = (nextPoint - currentPoint).normalized + currentPoint;
            var vectorToPrevious = (previousPoint - currentPoint).normalized + currentPoint;
            var tangent = (vectorToNext - vectorToPrevious).normalized;
            if (tangent == Vector3.zero)
            {
                var direction = (nextPoint - previousPoint).normalized;
                return new Vector3(-direction.y, direction.x, 0);
            }

            return new Vector3(-tangent.y, tangent.x, 0);
        }
    }
}