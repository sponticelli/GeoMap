using System.Collections.Generic;
using GeoData;
using UnityEngine;

namespace Visualization
{
    /// <summary>
    /// Generates Unity meshes from country polygon data.
    /// Handles triangulation of country boundaries including holes.
    /// </summary>
    [System.Serializable]
    public class CountryMeshGenerator
    {
        /// <summary>
        /// Height of the extruded country meshes.
        /// </summary>
        [SerializeField]
        public float extrusionHeight = 0.1f;

        /// <summary>
        /// Whether to generate separate meshes for each polygon in a country.
        /// </summary>
        [SerializeField]
        public bool separatePolygonMeshes = true;

        /// <summary>
        /// The coordinate converter used to transform geographic coordinates to world positions.
        /// </summary>
        private GeoCoordinateConverter coordinateConverter;

        /// <summary>
        /// Initializes a new instance of the CountryMeshGenerator.
        /// </summary>
        /// <param name="converter">The coordinate converter to use.</param>
        public CountryMeshGenerator(GeoCoordinateConverter converter)
        {
            coordinateConverter = converter;
        }

        /// <summary>
        /// Generates a mesh for a country feature.
        /// </summary>
        /// <param name="country">The country feature to generate a mesh for.</param>
        /// <returns>A list of generated meshes (one per polygon if separatePolygonMeshes is true, otherwise a single mesh).</returns>
        public List<Mesh> GenerateCountryMeshes(CountryFeature country)
        {
            if (country == null || country.polygons == null || country.polygons.Length == 0)
            {
                Debug.LogWarning($"Cannot generate mesh for invalid country: {country?.countryName ?? "null"}");
                return new List<Mesh>();
            }

            List<Mesh> meshes = new List<Mesh>();

            try
            {
                if (separatePolygonMeshes)
                {
                    // Generate a separate mesh for each polygon
                    foreach (var polygon in country.polygons)
                    {
                        if (polygon != null && polygon.IsValid())
                        {
                            try
                            {
                                Mesh mesh = GeneratePolygonMesh(polygon);
                                if (mesh != null)
                                {
                                    // Check if mesh is valid
                                    if (mesh.vertexCount > 0 && mesh.triangles.Length > 0)
                                    {
                                        meshes.Add(mesh);
                                    }
                                    else
                                    {
                                        // Clean up invalid mesh
                                        Object.Destroy(mesh);
                                    }
                                }
                            }
                            catch (System.Exception e)
                            {
                                Debug.LogError($"Error generating polygon mesh: {e.Message}");
                            }
                        }
                    }
                }
                else
                {
                    // Generate a single combined mesh for all polygons
                    Mesh combinedMesh = new Mesh();
                    combinedMesh.name = $"{country.countryName}_Mesh";

                    List<Vector3> allVertices = new List<Vector3>();
                    List<int> allTriangles = new List<int>();

                    // Limit the number of polygons to prevent memory issues
                    int maxPolygons = Mathf.Min(country.polygons.Length, 100);

                    for (int p = 0; p < maxPolygons; p++)
                    {
                        var polygon = country.polygons[p];
                        if (polygon != null && polygon.IsValid())
                        {
                            try
                            {
                                List<Vector3> vertices = new List<Vector3>();
                                List<int> triangles = new List<int>();

                                GeneratePolygonMeshData(polygon, vertices, triangles);

                                // Only add if we got valid data
                                if (vertices.Count > 0 && triangles.Count > 0)
                                {
                                    // Offset triangle indices to account for existing vertices
                                    int vertexOffset = allVertices.Count;
                                    for (int i = 0; i < triangles.Count; i++)
                                    {
                                        triangles[i] += vertexOffset;
                                    }

                                    allVertices.AddRange(vertices);
                                    allTriangles.AddRange(triangles);
                                }
                            }
                            catch (System.Exception e)
                            {
                                Debug.LogError($"Error generating polygon mesh data: {e.Message}");
                            }
                        }
                    }

                    // Check if we have enough data to create a mesh
                    if (allVertices.Count > 0 && allTriangles.Count > 0)
                    {
                        try
                        {
                            // Limit the number of vertices to prevent memory issues
                            int maxVertices = Mathf.Min(allVertices.Count, 65000); // Unity mesh vertex limit is 65535
                            if (allVertices.Count > maxVertices)
                            {
                                allVertices = allVertices.GetRange(0, maxVertices);

                                // Adjust triangles to not reference vertices beyond our limit
                                for (int i = allTriangles.Count - 1; i >= 0; i--)
                                {
                                    if (allTriangles[i] >= maxVertices)
                                    {
                                        // Remove this triangle (3 indices)
                                        int triangleIndex = i / 3 * 3;
                                        allTriangles.RemoveRange(triangleIndex, 3);
                                        i = triangleIndex;
                                    }
                                }
                            }

                            combinedMesh.SetVertices(allVertices);
                            combinedMesh.SetTriangles(allTriangles, 0);
                            combinedMesh.RecalculateNormals();
                            combinedMesh.RecalculateBounds();

                            meshes.Add(combinedMesh);
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"Error creating combined mesh: {e.Message}");
                            Object.Destroy(combinedMesh);
                        }
                    }
                    else
                    {
                        Object.Destroy(combinedMesh);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in GenerateCountryMeshes for {country?.countryName}: {e.Message}");
            }

            return meshes;
        }

        /// <summary>
        /// Generates a mesh for a single polygon.
        /// </summary>
        /// <param name="polygon">The polygon to generate a mesh for.</param>
        /// <returns>A generated mesh.</returns>
        private Mesh GeneratePolygonMesh(CountryPolygon polygon)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            GeneratePolygonMeshData(polygon, vertices, triangles);

            if (vertices.Count > 0 && triangles.Count > 0)
            {
                Mesh mesh = new Mesh();
                mesh.SetVertices(vertices);
                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                return mesh;
            }

            return null;
        }

        /// <summary>
        /// Generates mesh data (vertices and triangles) for a polygon.
        /// </summary>
        /// <param name="polygon">The polygon to generate mesh data for.</param>
        /// <param name="vertices">List to store the generated vertices.</param>
        /// <param name="triangles">List to store the generated triangle indices.</param>
        private void GeneratePolygonMeshData(CountryPolygon polygon, List<Vector3> vertices, List<int> triangles)
        {
            if (polygon == null || polygon.rings == null || polygon.rings.Length == 0)
                return;

            try
            {
                // Get the exterior ring
                PolygonRing exteriorRing = polygon.ExteriorRing;
                if (exteriorRing == null || exteriorRing.points == null || exteriorRing.points.Length < 3)
                    return;

                // Convert geographic coordinates to world positions
                List<Vector3> topVertices = new List<Vector3>();

                // Limit the number of points to prevent memory issues
                int maxPoints = Mathf.Min(exteriorRing.points.Length, 1000);

                // Sample points evenly if we have too many
                if (exteriorRing.points.Length > maxPoints)
                {
                    float step = (float)exteriorRing.points.Length / maxPoints;
                    for (int i = 0; i < maxPoints; i++)
                    {
                        int index = Mathf.Min(Mathf.FloorToInt(i * step), exteriorRing.points.Length - 1);
                        Vector3 worldPos = coordinateConverter.GeoToWorldPosition(exteriorRing.points[index]);

                        // Check for NaN or Infinity
                        if (float.IsNaN(worldPos.x) || float.IsNaN(worldPos.y) || float.IsNaN(worldPos.z) ||
                            float.IsInfinity(worldPos.x) || float.IsInfinity(worldPos.y) || float.IsInfinity(worldPos.z))
                        {
                            continue;
                        }

                        topVertices.Add(worldPos);
                    }
                }
                else
                {
                    foreach (var point in exteriorRing.points)
                    {
                        Vector3 worldPos = coordinateConverter.GeoToWorldPosition(point);

                        // Check for NaN or Infinity
                        if (float.IsNaN(worldPos.x) || float.IsNaN(worldPos.y) || float.IsNaN(worldPos.z) ||
                            float.IsInfinity(worldPos.x) || float.IsInfinity(worldPos.y) || float.IsInfinity(worldPos.z))
                        {
                            continue;
                        }

                        topVertices.Add(worldPos);
                    }
                }

                // Make sure we have enough vertices for triangulation
                if (topVertices.Count < 3)
                    return;

                // Simple triangulation for now (assuming convex polygons)
                // For complex polygons with holes, a more sophisticated triangulation algorithm would be needed
                int maxTriangles = Mathf.Min(topVertices.Count - 2, 500); // Limit triangles to prevent memory issues

                for (int i = 1; i <= maxTriangles; i++)
                {
                    try
                    {
                        int baseIndex = vertices.Count;

                        // Add top face vertices
                        vertices.Add(topVertices[0]);
                        vertices.Add(topVertices[i]);
                        vertices.Add(topVertices[i + 1]);

                        // Add top face triangle
                        triangles.Add(baseIndex);
                        triangles.Add(baseIndex + 1);
                        triangles.Add(baseIndex + 2);

                        // Add bottom face vertices
                        vertices.Add(new Vector3(topVertices[0].x, topVertices[0].y - extrusionHeight, topVertices[0].z));
                        vertices.Add(new Vector3(topVertices[i].x, topVertices[i].y - extrusionHeight, topVertices[i].z));
                        vertices.Add(new Vector3(topVertices[i + 1].x, topVertices[i + 1].y - extrusionHeight, topVertices[i + 1].z));

                        // Add bottom face triangle (reverse winding)
                        triangles.Add(baseIndex + 3);
                        triangles.Add(baseIndex + 5);
                        triangles.Add(baseIndex + 4);

                        // Add side faces (only if we haven't reached vertex limit)
                        if (vertices.Count < 65000 - 12) // Leave room for 12 more vertices
                        {
                            // Side 1
                            vertices.Add(topVertices[0]);
                            vertices.Add(topVertices[i]);
                            vertices.Add(new Vector3(topVertices[0].x, topVertices[0].y - extrusionHeight, topVertices[0].z));
                            vertices.Add(new Vector3(topVertices[i].x, topVertices[i].y - extrusionHeight, topVertices[i].z));

                            triangles.Add(baseIndex + 6);
                            triangles.Add(baseIndex + 8);
                            triangles.Add(baseIndex + 7);

                            triangles.Add(baseIndex + 7);
                            triangles.Add(baseIndex + 8);
                            triangles.Add(baseIndex + 9);

                            // Side 2
                            vertices.Add(topVertices[i]);
                            vertices.Add(topVertices[i + 1]);
                            vertices.Add(new Vector3(topVertices[i].x, topVertices[i].y - extrusionHeight, topVertices[i].z));
                            vertices.Add(new Vector3(topVertices[i + 1].x, topVertices[i + 1].y - extrusionHeight, topVertices[i + 1].z));

                            triangles.Add(baseIndex + 10);
                            triangles.Add(baseIndex + 12);
                            triangles.Add(baseIndex + 11);

                            triangles.Add(baseIndex + 11);
                            triangles.Add(baseIndex + 12);
                            triangles.Add(baseIndex + 13);

                            // Side 3
                            vertices.Add(topVertices[i + 1]);
                            vertices.Add(topVertices[0]);
                            vertices.Add(new Vector3(topVertices[i + 1].x, topVertices[i + 1].y - extrusionHeight, topVertices[i + 1].z));
                            vertices.Add(new Vector3(topVertices[0].x, topVertices[0].y - extrusionHeight, topVertices[0].z));

                            triangles.Add(baseIndex + 14);
                            triangles.Add(baseIndex + 16);
                            triangles.Add(baseIndex + 15);

                            triangles.Add(baseIndex + 15);
                            triangles.Add(baseIndex + 16);
                            triangles.Add(baseIndex + 17);
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Error generating triangle {i}: {e.Message}");
                        // Continue with the next triangle
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in GeneratePolygonMeshData: {e.Message}");
            }
        }
    }
}
