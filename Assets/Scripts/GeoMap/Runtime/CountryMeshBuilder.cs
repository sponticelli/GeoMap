using System;
using System.Collections;
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

        [Header("Highlight Materials")]
        [SerializeField] private Material highlightBorderMaterial;
        [SerializeField] private Material highlightSurfaceMaterial;

        [Header("Triangulation")]
        [SerializeField] private TriangulationAlgorithm triangulationAlgorithm = TriangulationAlgorithm.Dwyer;
        [SerializeField] private bool useQualityMeshGenerator = true;

        /// <summary>
        /// Creates a country GameObject with the specified geometry asynchronously.
        /// </summary>
        /// <param name="country">The GeoJSON node containing the country data.</param>
        /// <param name="center">The center position for the map.</param>
        /// <param name="parent">The parent transform for the country.</param>
        /// <param name="createSurface">Whether to create surface meshes.</param>
        /// <param name="onComplete">Optional callback when country creation is complete.</param>
        /// <param name="verticesPerFrame">Number of vertices to process per frame.</param>
        /// <returns>An IEnumerator that yields the CountryVisuals component when complete.</returns>
        public IEnumerator CreateAsync(JsonNode country, Vector3 center, Transform parent,
            bool createSurface, Action<CountryVisuals> onComplete = null, int verticesPerFrame = 10000)
        {
            // Check if the geometry type is supported
            string geometryType = country["geometry"]["type"].str;
            if (geometryType != "Polygon" && geometryType != "MultiPolygon")
            {
                onComplete?.Invoke(null);
                yield break;
            }

            // Get the country name
            string countryName = GetCountryName(country);

            // Create main GameObject for the country
            GameObject countryMainObject = new GameObject(countryName);
            countryMainObject.transform.SetParent(parent, false);

            // Add CountryInfo component to store country data
            CountryInfo countryInfo = countryMainObject.AddComponent<CountryInfo>();

            // Add CountryHighlighter component for selection highlighting
            CountryHighlighter countryHighlighter = countryMainObject.AddComponent<CountryHighlighter>();

            // Create Visuals GameObject as a container for visual elements
            GameObject countryVisualsObject = new GameObject("Visuals");
            countryVisualsObject.transform.SetParent(countryMainObject.transform, false);

            // Add CountryVisuals component to manage visual state
            CountryVisuals countryVisuals = countryVisualsObject.AddComponent<CountryVisuals>();

            // Create child GameObjects for outlines and surfaces under the Visuals GameObject
            GameObject countryOutlinesObject = new GameObject("Outlines");
            countryOutlinesObject.transform.SetParent(countryVisualsObject.transform, false);

            GameObject countrySurfacesObject = null;
            if (createSurface)
            {
                countrySurfacesObject = new GameObject("Surfaces");
                countrySurfacesObject.transform.SetParent(countryVisualsObject.transform, false);
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


            // First pass: collect all vertices to calculate the center
            List<Vector3> allVertices = new List<Vector3>();

            // Process each polygon in the coordinates to collect all vertices
            int coordinateSetCount = 0;
            foreach (var coordinateSet in country["geometry"]["coordinates"].list)
            {
                var processedCoordinates = ExtractCoordinates(coordinateSet);
                if (processedCoordinates.list == null) continue;

                var vertexCount = processedCoordinates.list.Count;
                if (vertexCount < 3) continue;

                for (int i = 0; i < vertexCount; i++)
                {
                    var coordinate = processedCoordinates.list[i];
                    var worldPosition = GeoConvert.LatLonToMetersForEarth(coordinate[1].f, coordinate[0].f);
                    worldPosition += new DoubleVector3(center.x, center.y, center.z);
                    allVertices.Add((Vector3)worldPosition);
                }

                coordinateSetCount++;
            }

            // Calculate the center of all vertices
            Vector3 countryCenter = Vector3.zero;
            if (allVertices.Count > 0)
            {
                foreach (var vertex in allVertices)
                {
                    countryCenter += vertex;
                }
                countryCenter /= allVertices.Count;
            }

            // Set the country GameObject's position to the calculated center
            countryMainObject.transform.position = countryCenter;

            // Second pass: process each polygon with vertices relative to the center
            int polygonIndex = 0;
            coordinateSetCount = 0;
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
                    worldPosition += new DoubleVector3(center.x, center.y, center.z);
                    // Make the vertex position relative to the country center
                    Vector3 relativePosition = (Vector3)worldPosition - countryCenter;
                    boundaryVertices.Add(relativePosition);
                }

                // Build border mesh with appropriate parent
                Transform borderParentTransform = (geometryType == "MultiPolygon") ?
                    borderParent.transform :
                    countryOutlinesObject.transform;

                string borderName = (geometryType == "MultiPolygon") ? $"Part_{polygonIndex}" : countryName;

                // Build border mesh asynchronously
                yield return StartCoroutine(BuildBorderMeshAsync(borderName, borderParentTransform, boundaryVertices));

                // Build surface mesh with appropriate parent if needed
                if (createSurface)
                {
                    Transform surfaceParentTransform = (geometryType == "MultiPolygon") ?
                        surfaceLayerParent.transform :
                        countrySurfacesObject.transform;

                    string surfaceName = (geometryType == "MultiPolygon") ? $"Part_{polygonIndex}" : countryName;

                    // Build surface mesh asynchronously
                    yield return StartCoroutine(BuildSurfaceMeshAsync(surfaceName, surfaceParentTransform, boundaryVertices, geometryType == "Polygon"));
                }

                polygonIndex++;
                coordinateSetCount++;
            }
            

            // Configure the CountryVisuals component with highlight materials
            if (countryVisuals != null)
            {
                // Set the highlight materials via reflection to avoid serialization issues
                var highlightBorderField = countryVisuals.GetType().GetField("highlightBorderMaterial", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var highlightSurfaceField = countryVisuals.GetType().GetField("highlightSurfaceMaterial", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                if (highlightBorderField != null)
                    highlightBorderField.SetValue(countryVisuals, highlightBorderMaterial);

                if (highlightSurfaceField != null)
                    highlightSurfaceField.SetValue(countryVisuals, highlightSurfaceMaterial);

                // Initialize the component to find and store all renderers
                countryVisuals.Initialize();
            }

            // Initialize the CountryInfo component
            if (countryInfo != null)
            {
                countryInfo.Initialize(countryName);
            }

            // Initialize the CountryHighlighter component
            if (countryHighlighter != null)
            {
                // The CountryHighlighter will find its references in Start()
            }

            // Invoke completion callback if provided
            onComplete?.Invoke(countryVisuals);

            // Return the CountryVisuals component for external access
            yield return countryVisuals;
        }

        /// <summary>
        /// Creates a country GameObject with the specified geometry (synchronous version).
        /// </summary>
        /// <param name="country">The GeoJSON node containing the country data.</param>
        /// <param name="center">The center position for the map.</param>
        /// <param name="parent">The parent transform for the country.</param>
        /// <param name="createSurface">Whether to create surface meshes.</param>
        /// <returns>The CountryVisuals component attached to the created country.</returns>
        public CountryVisuals Create(JsonNode country, Vector3 center, Transform parent,
            bool createSurface)
        {
            // Check if the geometry type is supported
            string geometryType = country["geometry"]["type"].str;
            if (geometryType != "Polygon" && geometryType != "MultiPolygon") return null;

            // Get the country name
            string countryName = GetCountryName(country);

            // Create main GameObject for the country
            GameObject countryMainObject = new GameObject(countryName);
            countryMainObject.transform.SetParent(parent, false);

            // Add CountryInfo component to store country data
            CountryInfo countryInfo = countryMainObject.AddComponent<CountryInfo>();

            // Add CountryHighlighter component for selection highlighting
            CountryHighlighter countryHighlighter = countryMainObject.AddComponent<CountryHighlighter>();

            // Create Visuals GameObject as a container for visual elements
            GameObject countryVisualsObject = new GameObject("Visuals");
            countryVisualsObject.transform.SetParent(countryMainObject.transform, false);

            // Add CountryVisuals component to manage visual state
            CountryVisuals countryVisuals = countryVisualsObject.AddComponent<CountryVisuals>();

            // Create child GameObjects for outlines and surfaces under the Visuals GameObject
            GameObject countryOutlinesObject = new GameObject("Outlines");
            countryOutlinesObject.transform.SetParent(countryVisualsObject.transform, false);

            GameObject countrySurfacesObject = null;
            if (createSurface)
            {
                countrySurfacesObject = new GameObject("Surfaces");
                countrySurfacesObject.transform.SetParent(countryVisualsObject.transform, false);
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

            // First pass: collect all vertices to calculate the center
            List<Vector3> allVertices = new List<Vector3>();

            // Process each polygon in the coordinates to collect all vertices
            foreach (var coordinateSet in country["geometry"]["coordinates"].list)
            {
                var processedCoordinates = ExtractCoordinates(coordinateSet);
                if (processedCoordinates.list == null) continue;

                var vertexCount = processedCoordinates.list.Count;
                if (vertexCount < 3) continue;

                for (int i = 0; i < vertexCount; i++)
                {
                    var coordinate = processedCoordinates.list[i];
                    var worldPosition = GeoConvert.LatLonToMetersForEarth(coordinate[1].f, coordinate[0].f);
                    worldPosition += new DoubleVector3(center.x, center.y, center.z);
                    allVertices.Add((Vector3)worldPosition);
                }
            }

            // Calculate the center of all vertices
            Vector3 countryCenter = Vector3.zero;
            if (allVertices.Count > 0)
            {
                foreach (var vertex in allVertices)
                {
                    countryCenter += vertex;
                }
                countryCenter /= allVertices.Count;
            }

            // Set the country GameObject's position to the calculated center
            countryMainObject.transform.position = countryCenter;

            // Second pass: process each polygon with vertices relative to the center
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
                    worldPosition += new DoubleVector3(center.x, center.y, center.z);
                    // Make the vertex position relative to the country center
                    Vector3 relativePosition = (Vector3)worldPosition - countryCenter;
                    boundaryVertices.Add(relativePosition);
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

            // Configure the CountryVisuals component with highlight materials
            if (countryVisuals != null)
            {
                // Set the highlight materials via reflection to avoid serialization issues
                var highlightBorderField = countryVisuals.GetType().GetField("highlightBorderMaterial", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var highlightSurfaceField = countryVisuals.GetType().GetField("highlightSurfaceMaterial", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                if (highlightBorderField != null)
                    highlightBorderField.SetValue(countryVisuals, highlightBorderMaterial);

                if (highlightSurfaceField != null)
                    highlightSurfaceField.SetValue(countryVisuals, highlightSurfaceMaterial);

                // Initialize the component to find and store all renderers
                countryVisuals.Initialize();
            }

            // Initialize the CountryInfo component
            if (countryInfo != null)
            {
                countryInfo.Initialize(countryName);
            }

            // Initialize the CountryHighlighter component
            if (countryHighlighter != null)
            {
                // The CountryHighlighter will find its references in Start()
            }

            // Return the CountryVisuals component for external access
            return countryVisuals;
        }

        /// <summary>
        /// Builds a surface mesh for a country asynchronously.
        /// </summary>
        /// <param name="name">The name of the mesh.</param>
        /// <param name="parentTransform">The parent transform for the mesh.</param>
        /// <param name="boundaryVertices">The boundary vertices of the country.</param>
        /// <param name="createLayer">Whether to create a layer for the mesh.</param>
        /// <param name="onComplete">Optional callback when mesh building is complete.</param>
        /// <param name="verticesPerFrame">Number of vertices to process per frame.</param>
        /// <returns>An IEnumerator for the coroutine.</returns>
        private IEnumerator BuildSurfaceMeshAsync(string name, Transform parentTransform, List<Vector3> boundaryVertices,
            bool createLayer = true, Action<MeshFilter> onComplete = null)
        {
            var surfaceMeshObject = new GameObject("mesh").AddComponent<MeshFilter>();
            surfaceMeshObject.gameObject.AddComponent<MeshRenderer>();
            var surfaceMesh = surfaceMeshObject.mesh;
            var points = boundaryVertices;
            var meshData = new MeshData();
            var meshInput = new MeshInputData(points.Count);

            // Process vertices in chunks
            for (int vertexIndex = 0; vertexIndex < points.Count; vertexIndex++)
            {
                meshInput.AddPoint(points[vertexIndex].x, points[vertexIndex].y);
                meshInput.AddSegment(vertexIndex, (vertexIndex + 1) % points.Count);
            }

            yield return StartCoroutine(CreateMeshForPolygonAsync(meshInput, meshData, points, false));

            // Apply mesh data
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

            // Invoke completion callback if provided
            onComplete?.Invoke(surfaceMeshObject);
        }

        /// <summary>
        /// Builds a surface mesh for a country (synchronous version).
        /// </summary>
        /// <param name="name">The name of the mesh.</param>
        /// <param name="parentTransform">The parent transform for the mesh.</param>
        /// <param name="boundaryVertices">The boundary vertices of the country.</param>
        /// <param name="createLayer">Whether to create a layer for the mesh.</param>
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

        /// <summary>
        /// Builds a border mesh for a country asynchronously.
        /// </summary>
        /// <param name="name">The name of the mesh.</param>
        /// <param name="parentTransform">The parent transform for the mesh.</param>
        /// <param name="boundaryVertices">The boundary vertices of the country.</param>
        /// <param name="onComplete">Optional callback when mesh building is complete.</param>
        /// <param name="verticesPerFrame">Number of vertices to process per frame.</param>
        /// <returns>An IEnumerator for the coroutine.</returns>
        private IEnumerator BuildBorderMeshAsync(string name, Transform parentTransform, List<Vector3> boundaryVertices,
            Action<MeshFilter> onComplete = null)
        {
            var borderMeshObject = new GameObject("mesh").AddComponent<MeshFilter>();
            borderMeshObject.gameObject.AddComponent<MeshRenderer>();
            var borderMesh = borderMeshObject.mesh;

            var meshData = new MeshData();

            yield return StartCoroutine(CreateMeshForLinestringAsync(boundaryVertices, meshData, borderWidth));

            // Apply mesh data
            borderMesh.vertices = meshData.Vertices.ToArray();
            borderMesh.triangles = meshData.Indices.ToArray();
            borderMesh.SetUVs(0, meshData.UV);

            borderMesh.RecalculateNormals();

            borderMeshObject.name = name;

            borderMeshObject.GetComponent<MeshRenderer>().material = borderMaterial;
            borderMeshObject.transform.SetParent(parentTransform, false);

            // Invoke completion callback if provided
            onComplete?.Invoke(borderMeshObject);
        }

        /// <summary>
        /// Builds a border mesh for a country (synchronous version).
        /// </summary>
        /// <param name="name">The name of the mesh.</param>
        /// <param name="parentTransform">The parent transform for the mesh.</param>
        /// <param name="boundaryVertices">The boundary vertices of the country.</param>
        /// <returns>The MeshFilter component of the created mesh.</returns>
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
            string result = "Unknown";
            if (country["properties"]["id"] != null)
                result = country["properties"]["id"].ToString();
            if (country["properties"]["name"] != null)
                result = country["properties"]["name"].ToString();
            // trim whitespace
            result = result.Trim();
            // remove double quotes
            result = result.Replace("\"", "");

            return result;
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

        /// <summary>
        /// Creates a mesh for a polygon asynchronously.
        /// </summary>
        /// <param name="corners">The input mesh data containing points and segments.</param>
        /// <param name="meshData">The mesh data to populate.</param>
        /// <param name="vertices">The vertices of the polygon.</param>
        /// <param name="reverseWinding">Whether to reverse the winding order of triangles.</param>
        /// <param name="trianglesPerFrame">Number of triangles to process per frame.</param>
        /// <returns>An IEnumerator for the coroutine.</returns>
        private IEnumerator CreateMeshForPolygonAsync(MeshInputData corners, MeshData meshData, List<Vector3> vertices,
            bool reverseWinding, int trianglesPerFrame = 100)
        {
            var triangularMesh = new TriangularMesh();
            triangularMesh.Behavior.Algorithm = triangulationAlgorithm;
            triangularMesh.Behavior.Quality = useQualityMeshGenerator;

            // Triangulation can be expensive, so yield after this operation
            triangularMesh.Triangulate(corners);
            yield return null;

            var verticesStartIndex = meshData.Vertices.Count;
            meshData.Vertices = vertices;

            // Process triangles in chunks
            int triangleCount = 0;
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
                
                triangleCount++;
            }
        }

        /// <summary>
        /// Creates a mesh for a polygon (synchronous version).
        /// </summary>
        /// <param name="corners">The input mesh data containing points and segments.</param>
        /// <param name="meshData">The mesh data to populate.</param>
        /// <param name="vertices">The vertices of the polygon.</param>
        /// <param name="reverseWinding">Whether to reverse the winding order of triangles.</param>
        private void CreateMeshForPolygon(MeshInputData corners, MeshData meshData, List<Vector3> vertices, bool reverseWinding)
        {
            var triangularMesh = new TriangularMesh();
            triangularMesh.Behavior.Algorithm = triangulationAlgorithm;
            triangularMesh.Behavior.Quality = useQualityMeshGenerator;
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


        /// <summary>
        /// Creates a mesh for a linestring asynchronously.
        /// </summary>
        /// <param name="vertices">The vertices of the linestring.</param>
        /// <param name="meshData">The mesh data to populate.</param>
        /// <param name="width">The width of the linestring.</param>
        /// <param name="verticesPerFrame">Number of vertices to process per frame.</param>
        /// <returns>An IEnumerator for the coroutine.</returns>
        private IEnumerator CreateMeshForLinestringAsync(List<Vector3> vertices, MeshData meshData, float width)
        {
            var verticesStartIndex = meshData.Vertices.Count;
            Vector3 lastPosition = Vector3.zero;
            var normal = Vector3.zero;

            // Process vertices in chunks
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

            

            // Process indices in chunks
            int indicesProcessed = 0;
            for (int vertexIndex = verticesStartIndex; vertexIndex <= meshData.Vertices.Count - 3; vertexIndex += 2)
            {
                meshData.Indices.Add(vertexIndex);
                meshData.Indices.Add(vertexIndex + 2);
                meshData.Indices.Add(vertexIndex + 1);
                meshData.Indices.Add(vertexIndex + 1);
                meshData.Indices.Add(vertexIndex + 2);
                meshData.Indices.Add(vertexIndex + 3);

                indicesProcessed++;
            }
            yield return null;
        }

        /// <summary>
        /// Creates a mesh for a linestring (synchronous version).
        /// </summary>
        /// <param name="vertices">The vertices of the linestring.</param>
        /// <param name="meshData">The mesh data to populate.</param>
        /// <param name="width">The width of the linestring.</param>
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
            Vector3 direction;
            if (currentPoint == previousPoint || currentPoint == nextPoint)
            {
                direction = (nextPoint - previousPoint).normalized;
                return new Vector3(-direction.y, direction.x, 0);
            }

            var vectorToNext = (nextPoint - currentPoint).normalized + currentPoint;
            var vectorToPrevious = (previousPoint - currentPoint).normalized + currentPoint;
            var tangent = (vectorToNext - vectorToPrevious).normalized;
            if (tangent != Vector3.zero) return new Vector3(-tangent.y, tangent.x, 0);

            direction = (nextPoint - previousPoint).normalized;
            return new Vector3(-direction.y, direction.x, 0);
        }
    }
}