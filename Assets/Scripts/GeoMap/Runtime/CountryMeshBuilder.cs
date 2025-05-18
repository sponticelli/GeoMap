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

            // Create parent GameObjects for MultiPolygon types
            GameObject borderParent = null;
            GameObject surfaceParent2 = null;

            if (geometryType == "MultiPolygon")
            {
                // Create parent for border meshes
                borderParent = new GameObject(countryName);
                borderParent.transform.SetParent(outlineParent, false);

                // Create parent for surface meshes if needed
                if (createSurface)
                {
                    surfaceParent2 = new GameObject(countryName + " layer");
                    surfaceParent2.transform.SetParent(surfaceParent, false);
                }
            }

            // Process each polygon in the coordinates
            int polygonIndex = 0;
            foreach (var bb in country["geometry"]["coordinates"].list)
            {
                var jo = ExtractCoordinates(bb);
                if (jo.list == null) continue;

                var count = jo.list.Count;
                if (count < 3) continue;

                List<Vector3> earthBoundarEnds = new List<Vector3>();
                earthBoundarEnds.Clear();

                bool lonSign = false;
                int plus = 0, minus = 0;

                for (int i = 0; i < count; i++)
                {
                    var c = jo.list[i];
                    if (c[0].f > 0)
                    {
                        plus++;
                    }
                    else
                        minus++;

                    var dotMerc = GeoConvert.LatLonToMetersForEarth(c[1].f, c[0].f);
                    dotMerc += new Vector3d(center.x, center.y, center.z);
                    earthBoundarEnds.Add((Vector3)dotMerc);
                }

                // Build border mesh with appropriate parent
                Transform borderParentTransform = (geometryType == "MultiPolygon") ? borderParent.transform : outlineParent;
                string borderName = (geometryType == "MultiPolygon") ? $"Part_{polygonIndex}" : countryName;
                var borderMeshContainer = BuildBorderMesh(borderName, borderParentTransform, earthBoundarEnds);

                // Build surface mesh with appropriate parent if needed
                if (createSurface)
                {
                    Transform surfaceParentTransform = (geometryType == "MultiPolygon") ? surfaceParent2.transform : surfaceParent;
                    string surfaceName = (geometryType == "MultiPolygon") ? $"Part_{polygonIndex}" : countryName;
                    BuildSurfaceMesh(surfaceName, surfaceParentTransform, earthBoundarEnds, geometryType == "Polygon");
                }

                polygonIndex++;
            }
        }

        private void BuildSurfaceMesh(string name, Transform parentTransform, List<Vector3> earthBoundarEnds, bool createLayer = true)
        {
            var earthMeshBoundary2 = new GameObject("mesh").AddComponent<MeshFilter>();
            earthMeshBoundary2.gameObject.AddComponent<MeshRenderer>();
            var earthMesh2 = earthMeshBoundary2.mesh;
            var points = earthBoundarEnds;
            var md2 = new MeshData();
            var inp = new MeshInputData(points.Count);
            for (int d = 0; d < points.Count; d++)
            {
                inp.AddPoint(points[d].x, points[d].y);
                inp.AddSegment(d, (d + 1) % points.Count);
            }

            CreateMeshForPolygon(inp, md2, points, false);

            earthMesh2.vertices = md2.Vertices.ToArray();
            earthMesh2.triangles = md2.Indices.ToArray();
            earthMesh2.SetUVs(0, md2.UV);
            earthMesh2.RecalculateNormals();

            earthMeshBoundary2.GetComponent<MeshRenderer>().material = surfaceMaterial;
            earthMeshBoundary2.transform.name = name;

            if (name.Contains("Lesotho"))
                earthMeshBoundary2.transform.localPosition -= new Vector3(0, 0, 1);

            // Set parent directly if we're not creating a layer (for MultiPolygon parts)
            if (!createLayer)
            {
                earthMeshBoundary2.transform.SetParent(parentTransform, false);
            }
            else
            {
                // For single polygons, maintain the original layer structure
                bool containsLayer = false;
                GameObject parentLayer = null;
                for (int child = 0; child < parentTransform.childCount; child++)
                {
                    if (parentTransform.GetChild(child).name.Contains(name + " layer"))
                    {
                        containsLayer = true;
                        parentLayer = parentTransform.GetChild(child).gameObject;
                    }
                }

                if (containsLayer)
                {
                    earthMeshBoundary2.transform.parent = parentLayer.transform;
                }
                else
                {
                    parentLayer = new GameObject(name + " layer");
                    parentLayer.transform.parent = parentTransform;
                    earthMeshBoundary2.transform.parent = parentLayer.transform;
                }
            }

            earthMeshBoundary2.gameObject.AddComponent<MeshCollider>();
        }

        private MeshFilter BuildBorderMesh(string name, Transform parentTransform, List<Vector3> earthBoundarEnds)
        {
            var meshContainer = new GameObject("mesh").AddComponent<MeshFilter>();
            meshContainer.gameObject.AddComponent<MeshRenderer>();
            var countryMesh = meshContainer.mesh;

            var md = new MeshData();

            CreateMeshForLinestring(earthBoundarEnds, md, borderWidth);
            countryMesh.vertices = md.Vertices.ToArray();
            countryMesh.triangles = md.Indices.ToArray();
            countryMesh.SetUVs(0, md.UV);
            countryMesh.RecalculateNormals();

            meshContainer.name = name;

            meshContainer.GetComponent<MeshRenderer>().material = borderMaterial;
            meshContainer.transform.SetParent(parentTransform, false);

            return meshContainer;
        }

        private string GetCountryName(JsonNode country)
        {
            if (country["properties"]["id"] != null)
                return country["properties"]["id"].ToString();
            if (country["properties"]["name"] != null)
                return country["properties"]["name"].ToString();
            return "Unknown";
        }

        private static JsonNode ExtractCoordinates(JsonNode bb)
        {
            JsonNode jo = null;
            if (bb.list == null)
            {
                //print("-1");
                jo = bb;
            }
            else if (bb.list.Count == 0)
            {
                //print("-1.0");
                jo = bb;
            }
            else if (bb.list[0].list == null)
            {
                //print("-2");
                jo = bb;
            }
            else if (bb.list[0].list.Count == 0)
            {
                //print("-2.0");
                jo = bb;
            }
            else jo = (bb.list[0].list[0].IsArray) ? bb.list[0] : bb;

            return jo;
        }

        private void CreateMeshForPolygon(MeshInputData corners, MeshData meshdata, List<Vector3> ends, bool temp)
        {
            var mesh = new TriangularMesh();
            mesh.Behavior.Algorithm = TriangulationAlgorithm.SweepLine;
            mesh.Behavior.Quality = true;
            mesh.Triangulate(corners);
            var vertsStartCount = meshdata.Vertices.Count;
            meshdata.Vertices = ends;/*AddRange(corners.Points.Select(x => new Vector3((float)x.X, (float)x.Y, 0)).ToList());*/
            foreach (var tri in mesh.Triangles)
            {
                if ((tri.P0 >= corners.Count) || (tri.P1 >= corners.Count) || (tri.P2 >= corners.Count)) continue;

                if (!temp)
                {
                    meshdata.Indices.Add(vertsStartCount + tri.P1);//1
                    meshdata.Indices.Add(vertsStartCount + tri.P0);//0
                    meshdata.Indices.Add(vertsStartCount + tri.P2);//2
                }
                else
                {
                    meshdata.Indices.Add(vertsStartCount + tri.P0);//1
                    meshdata.Indices.Add(vertsStartCount + tri.P1);//0
                    meshdata.Indices.Add(vertsStartCount + tri.P2);//2
                }
            }
        }


        private void CreateMeshForLinestring(List<Vector3> list, MeshData md, float width)
        {
            var vertsStartCount = md.Vertices.Count;
            Vector3 lastPos = Vector3.zero;
            var norm = Vector3.zero;
            for (int i = 1; i < list.Count; i++)
            {
                var p1 = list[i - 1];
                var p2 = list[i];
                var p3 = p2;
                if (i + 1 < list.Count)
                    p3 = list[i + 1];
                if (lastPos == Vector3.zero)
                {
                    lastPos = Vector3.Lerp(p1, p2, 0f);
                    norm = GetNormal(p1, lastPos, p2) * width; // 0.0025f;
                    md.Vertices.Add(lastPos + norm);
                    md.Vertices.Add(lastPos - norm);
                }

                lastPos = Vector3.Lerp(p1, p2, 1f); //p2
                norm = GetNormal(p1, lastPos, p3) * width; // 0.0025f;
                md.Vertices.Add(lastPos + norm);
                md.Vertices.Add(lastPos - norm);
            }

            for (int j = vertsStartCount; j <= md.Vertices.Count - 3; j += 2)
            {
                {
                    md.Indices.Add(j);
                    md.Indices.Add(j + 2);
                    md.Indices.Add(j + 1);
                    md.Indices.Add(j + 1);
                    md.Indices.Add(j + 2);
                    md.Indices.Add(j + 3);
                }
            }
        }

        private Vector3 GetNormal(Vector3 p1, Vector3 newPos, Vector3 p2)
        {
            if (newPos == p1 || newPos == p2)
            {
                var n = (p2 - p1).normalized;
                //return new Vector3(-n.z, 0, n.x);
                return new Vector3(-n.y, n.x, 0);
            }

            var b = (p2 - newPos).normalized + newPos;
            var a = (p1 - newPos).normalized + newPos;
            var t = (b - a).normalized;
            if (t == Vector3.zero)
            {
                var n = (p2 - p1).normalized;
                //return new Vector3(-n.z, 0, n.x);
                return new Vector3(-n.y, n.x, 0);
            }

            //return new Vector3(-t.z, 0, t.x);
            return new Vector3(-t.y, t.x, 0);
        }
    }
}