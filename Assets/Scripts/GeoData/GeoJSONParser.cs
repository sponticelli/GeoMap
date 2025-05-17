using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace GeoData
{
    /// <summary>
    /// Utility class for parsing GeoJSON data into Unity-compatible objects.
    /// Handles conversion from raw GeoJSON to CountryFeature objects.
    /// </summary>
    public static class GeoJSONParser
    {
        /// <summary>
        /// Parses a GeoJSON string into a GeoJSONCollection.
        /// </summary>
        /// <param name="jsonData">The raw GeoJSON string.</param>
        /// <param name="optimizeGeometry">Whether to simplify geometry to reduce point count.</param>
        /// <param name="simplificationTolerance">Tolerance for geometry simplification.</param>
        /// <returns>A populated GeoJSONCollection object.</returns>
        public static GeoJSONCollection ParseGeoJSON(string jsonData, bool optimizeGeometry = true, float simplificationTolerance = 0.001f)
        {
            if (string.IsNullOrEmpty(jsonData))
                throw new ArgumentException("GeoJSON data cannot be empty");

            Debug.Log($"Parsing GeoJSON data: {jsonData.Substring(0, 1000)}...");

            try
            {
                // Parse the JSON data
                var rootObject = JsonUtility.FromJson<GeoJSONRoot>(FixJsonForUnity(jsonData));
                if (rootObject == null || rootObject.features == null)
                    throw new FormatException("Invalid GeoJSON format");

                // Create a new collection
                var collection = ScriptableObject.CreateInstance<GeoJSONCollection>();
                collection.description = "Collection of country features imported from GeoJSON";
                collection.version = "1.0.0";
                collection.countries = new List<CountryFeature>();

                Debug.Log($"Found {rootObject.features.Length} features in GeoJSON");

                // Process each feature
                foreach (var feature in rootObject.features)
                {
                    if (feature == null || feature.properties == null || feature.geometry == null)
                        continue;

                    // Extract country name and ISO code using the new property names
                    string countryName = feature.properties.name;
                    string isoCode = feature.properties.ISO3166_1_Alpha_3;

                    Debug.Log($"Processing feature: {feature}");

                    // If the new property names are empty, try the old ones for backward compatibility
                    if (string.IsNullOrEmpty(countryName))
                        countryName = feature.properties.ADMIN;

                    if (string.IsNullOrEmpty(isoCode))
                        isoCode = feature.properties.ISO_A3;

                    if (string.IsNullOrEmpty(countryName) || string.IsNullOrEmpty(isoCode))
                    {
                        Debug.LogWarning($"Skipping feature with missing name or ISO code: {feature}");
                        continue;
                    }

                    GeometryType geometryType = feature.geometry.type.Equals("Polygon", StringComparison.OrdinalIgnoreCase)
                        ? GeometryType.Polygon
                        : GeometryType.MultiPolygon;

                    var countryFeature = new CountryFeature(countryName, isoCode, geometryType);

                    // Process geometry based on type
                    if (geometryType == GeometryType.Polygon)
                    {
                        // Extract coordinates - use a greedy match to get the entire coordinates array
                        var coordinatesMatch = System.Text.RegularExpressions.Regex.Match(feature.geometry.coordinates,
                            @"(\[.*\])",
                            System.Text.RegularExpressions.RegexOptions.Singleline);

                        if (coordinatesMatch.Success)
                        {
                            string coordinatesJson = coordinatesMatch.Groups[1].Value;
                            var polygon = ProcessPolygonDirectly(coordinatesJson, optimizeGeometry, simplificationTolerance);
                            if (polygon != null)
                            {
                                countryFeature.polygons = new CountryPolygon[] { polygon };
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"Could not extract coordinates for {countryName}");
                        }
                    }
                    else // MultiPolygon
                    {
                        // Extract coordinates - use a greedy match to get the entire coordinates array
                        var coordinatesMatch = System.Text.RegularExpressions.Regex.Match(feature.geometry.coordinates,
                            @"(\[.*\])",
                            System.Text.RegularExpressions.RegexOptions.Singleline);

                        if (coordinatesMatch.Success)
                        {
                            string coordinatesJson = coordinatesMatch.Groups[1].Value;
                            var polygons = ProcessMultiPolygonDirectly(coordinatesJson, optimizeGeometry, simplificationTolerance);
                            if (polygons != null && polygons.Length > 0)
                            {
                                countryFeature.polygons = polygons;
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"Could not extract coordinates for {countryName}");
                        }
                    }

                    if (countryFeature.IsValid())
                    {
                        Debug.Log($"Adding country: {countryName} with {countryFeature.GetPolygonCount()} polygons and {countryFeature.GetTotalPointCount()} points");
                        collection.AddCountry(countryFeature);
                    }
                    else
                    {
                        Debug.LogWarning($"Country {countryName} is not valid, skipping. Polygons: {countryFeature.GetPolygonCount()}, Points: {countryFeature.GetTotalPointCount()}");
                    }
                }

                collection.UpdateStatistics();
                return collection;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error parsing GeoJSON: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Processes a polygon from GeoJSON coordinates.
        /// </summary>
        private static CountryPolygon ProcessPolygon(string coordinatesJson, bool optimize, float tolerance)
        {
            try
            {
                // Parse the coordinates JSON
                var coordinates = JsonUtility.FromJson<CoordinateArray>(FixJsonForUnity(coordinatesJson));
                if (coordinates == null || coordinates.values == null || coordinates.values.Length == 0)
                    return null;

                var rings = new List<PolygonRing>();

                // First array is the exterior ring
                if (coordinates.values.Length > 0 && coordinates.values[0] != null)
                {
                    var exteriorPoints = ConvertCoordinatesToPoints(coordinates.values[0]);
                    if (optimize)
                    {
                        exteriorPoints = GeoDataUtilities.SimplifyPolygon(exteriorPoints, tolerance);
                    }

                    if (exteriorPoints.Count >= 3)
                    {
                        rings.Add(new PolygonRing(exteriorPoints.ToArray(), true));
                    }
                }

                // Subsequent arrays are holes
                for (int i = 1; i < coordinates.values.Length; i++)
                {
                    if (coordinates.values[i] != null)
                    {
                        var holePoints = ConvertCoordinatesToPoints(coordinates.values[i]);
                        if (optimize)
                        {
                            holePoints = GeoDataUtilities.SimplifyPolygon(holePoints, tolerance);
                        }

                        if (holePoints.Count >= 3)
                        {
                            rings.Add(new PolygonRing(holePoints.ToArray(), false));
                        }
                    }
                }

                if (rings.Count > 0)
                {
                    return new CountryPolygon(rings.ToArray());
                }

                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing polygon: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Processes a multi-polygon from GeoJSON coordinates.
        /// </summary>
        private static CountryPolygon[] ProcessMultiPolygon(string coordinatesJson, bool optimize, float tolerance)
        {
            try
            {
                // Parse the coordinates JSON
                var coordinates = JsonUtility.FromJson<CoordinateArray>(FixJsonForUnity(coordinatesJson));
                if (coordinates == null || coordinates.values == null || coordinates.values.Length == 0)
                    return null;

                var polygons = new List<CountryPolygon>();

                foreach (var polygonCoords in coordinates.values)
                {
                    if (polygonCoords != null)
                    {
                        var polygon = ProcessPolygon(polygonCoords, optimize, tolerance);
                        if (polygon != null)
                        {
                            polygons.Add(polygon);
                        }
                    }
                }

                return polygons.ToArray();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing multi-polygon: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Converts GeoJSON coordinate arrays to Vector2 points.
        /// </summary>
        private static List<Vector2> ConvertCoordinatesToPoints(string coordinatesJson)
        {
            var points = new List<Vector2>();

            try
            {
                var coordinates = JsonUtility.FromJson<CoordinateArray>(FixJsonForUnity(coordinatesJson));
                if (coordinates == null || coordinates.values == null)
                    return points;

                foreach (var coordPair in coordinates.values)
                {
                    if (coordPair != null && coordPair.Length >= 2)
                    {
                        double longitude = coordPair[0];
                        double latitude = coordPair[1];

                        if (GeoDataUtilities.IsValidCoordinate(longitude, latitude))
                        {
                            points.Add(GeoDataUtilities.LonLatToVector2(longitude, latitude));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error converting coordinates to points: {ex.Message}");
            }

            return points;
        }

        /// <summary>
        /// Fixes GeoJSON format to be compatible with Unity's JsonUtility.
        /// Unity's JsonUtility cannot handle nested arrays directly, so we need to convert them to a format it can handle.
        /// </summary>
        private static string FixJsonForUnity(string json)
        {
            // For debugging
            Debug.Log("Parsing GeoJSON data: " + json.Substring(0, Math.Min(100, json.Length)) + "...");

            try
            {
                // For GeoJSON, we need to handle:
                // 1. Nested arrays in the coordinates
                // 2. Property names with hyphens (Unity's JsonUtility can't handle these)

                // First, replace property names with hyphens with underscore versions
                var modifiedJson = json;

                // Replace ISO3166-1-Alpha-3 with ISO3166_1_Alpha_3
                modifiedJson = System.Text.RegularExpressions.Regex.Replace(
                    modifiedJson,
                    @"""ISO3166-1-Alpha-3""",
                    "\"ISO3166_1_Alpha_3\"");

                // Replace ISO3166-1-Alpha-2 with ISO3166_1_Alpha_2
                modifiedJson = System.Text.RegularExpressions.Regex.Replace(
                    modifiedJson,
                    @"""ISO3166-1-Alpha-2""",
                    "\"ISO3166_1_Alpha_2\"");

                // Now handle the coordinate arrays
                // Find all coordinate arrays and replace them with string representations
                var coordArrayPattern = @"""coordinates""\s*:\s*(\[(?:[^\[\]]|\[(?:[^\[\]]|\[(?:[^\[\]]|\[[^\[\]]*\])*\])*\])*\])";
                var coordArrayMatches = System.Text.RegularExpressions.Regex.Matches(modifiedJson, coordArrayPattern, System.Text.RegularExpressions.RegexOptions.Singleline);

                Debug.Log($"Found {coordArrayMatches.Count} coordinate arrays in GeoJSON");

                foreach (System.Text.RegularExpressions.Match match in coordArrayMatches)
                {
                    if (match.Groups.Count >= 2)
                    {
                        var coordArray = match.Groups[1].Value;
                        modifiedJson = modifiedJson.Replace(match.Value, $"\"coordinates\": \"{coordArray.Replace("\"", "\\\"").Replace("\n", "\\n")}\"");
                    }
                }

                // Log a sample of the modified JSON to verify our changes
                Debug.Log($"Modified JSON sample: {modifiedJson.Substring(0, Math.Min(500, modifiedJson.Length))}...");

                return modifiedJson;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error fixing JSON for Unity: {ex.Message}");
                return json;
            }
        }

        // Helper classes for JSON deserialization
        [Serializable]
        private class GeoJSONRoot
        {
            public string type;
            public GeoJSONFeature[] features;
        }

        [Serializable]
        private class GeoJSONFeature
        {
            public string type;
            public GeoJSONProperties properties;
            public GeoJSONGeometry geometry;

            public override string ToString()
            {
                string countryName = properties?.name ?? properties?.ADMIN ?? "Unknown";
                string isoCode = properties?.ISO3166_1_Alpha_3 ?? properties?.ISO_A3 ?? "Unknown";
                return $"Feature: {countryName}, {isoCode} Type: {type} Geometry: {geometry?.type} Coordinates: {geometry?.coordinates?.Substring(0, Math.Min(20, (geometry?.coordinates?.Length ?? 0)))}...";
            }
        }

        [Serializable]
        private class GeoJSONProperties
        {
            // Updated property names to match the actual GeoJSON structure
            public string name;           // Country name (was ADMIN)
            public string ISO3166_1_Alpha_3;  // ISO3166-1-Alpha-3 code (was ISO_A3)
            public string ISO3166_1_Alpha_2;  // ISO3166-1-Alpha-2 code (new field)

            // For backward compatibility, provide getters for the old property names
            public string ADMIN => name;
            public string ISO_A3 => ISO3166_1_Alpha_3;
        }

        [Serializable]
        private class GeoJSONGeometry
        {
            public string type;
            public string coordinates;
        }

        [Serializable]
        private class CoordinateArray
        {
            public string[] values;
        }

        /// <summary>
        /// Directly processes a polygon from GeoJSON coordinates string.
        /// This method parses the coordinates manually without relying on JsonUtility.
        /// </summary>
        private static CountryPolygon ProcessPolygonDirectly(string coordinatesJson, bool optimize, float tolerance)
        {
            try
            {
                Debug.Log($"Processing polygon coordinates: {coordinatesJson.Substring(0, Math.Min(100, coordinatesJson.Length))}...");

                // Extract rings using the balanced bracket approach
                var ringJsons = ExtractRingsFromPolygon(coordinatesJson);

                Debug.Log($"Found {ringJsons.Count} coordinate rings in polygon");

                if (ringJsons.Count == 0)
                {
                    Debug.LogWarning("No coordinate rings found in polygon");
                    return null;
                }

                var rings = new List<PolygonRing>();

                // Process each ring
                for (int ringIndex = 0; ringIndex < ringJsons.Count; ringIndex++)
                {
                    string ringJson = ringJsons[ringIndex];

                    // Extract coordinate pairs - simpler pattern to match longitude,latitude pairs
                    var coordPairMatches = System.Text.RegularExpressions.Regex.Matches(ringJson,
                        @"\[\s*(-?\d+\.?\d*)\s*,\s*(-?\d+\.?\d*)\s*\]");

                    if (coordPairMatches.Count < 3)
                    {
                        Debug.LogWarning($"Ring has too few points: {coordPairMatches.Count}");
                        continue;
                    }

                    var points = new List<Vector2>();

                    foreach (System.Text.RegularExpressions.Match coordPairMatch in coordPairMatches)
                    {
                        if (coordPairMatch.Groups.Count >= 3)
                        {
                            double longitude = double.Parse(coordPairMatch.Groups[1].Value);
                            double latitude = double.Parse(coordPairMatch.Groups[2].Value);

                            if (GeoDataUtilities.IsValidCoordinate(longitude, latitude))
                            {
                                points.Add(GeoDataUtilities.LonLatToVector2(longitude, latitude));
                            }
                        }
                    }

                    if (optimize)
                    {
                        points = GeoDataUtilities.SimplifyPolygon(points, tolerance);
                    }

                    if (points.Count >= 3)
                    {
                        // First ring is exterior, others are holes
                        rings.Add(new PolygonRing(points.ToArray(), ringIndex == 0));
                    }
                }

                if (rings.Count > 0)
                {
                    return new CountryPolygon(rings.ToArray());
                }

                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing polygon directly: {ex.Message}\nStack trace: {ex.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// Extracts individual ring structures from a Polygon coordinates array.
        /// Uses a balanced bracket approach to correctly extract complete ring data.
        /// </summary>
        /// <param name="polygonJson">The Polygon coordinates JSON string.</param>
        /// <returns>A list of ring JSON strings.</returns>
        private static List<string> ExtractRingsFromPolygon(string polygonJson)
        {
            var rings = new List<string>();

            try
            {
                // Remove outer brackets if present
                string content = polygonJson.Trim();
                if (content.StartsWith("[") && content.EndsWith("]"))
                {
                    content = content.Substring(1, content.Length - 2).Trim();
                }

                int startIndex = 0;
                int bracketCount = 0;
                bool inRing = false;

                for (int i = 0; i < content.Length; i++)
                {
                    char c = content[i];

                    if (c == '[')
                    {
                        bracketCount++;
                        if (bracketCount == 1 && !inRing)
                        {
                            startIndex = i;
                            inRing = true;
                        }
                    }
                    else if (c == ']')
                    {
                        bracketCount--;
                        if (bracketCount == 0 && inRing)
                        {
                            // Extract the complete ring
                            string ring = content.Substring(startIndex, i - startIndex + 1);
                            rings.Add(ring);
                            inRing = false;
                        }
                    }
                }

                Debug.Log($"Extracted {rings.Count} rings from Polygon using balanced bracket approach");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error extracting rings from Polygon: {ex.Message}");
            }

            return rings;
        }

        /// <summary>
        /// Directly processes a multi-polygon from GeoJSON coordinates string.
        /// This method parses the coordinates manually without relying on JsonUtility.
        /// </summary>
        private static CountryPolygon[] ProcessMultiPolygonDirectly(string coordinatesJson, bool optimize, float tolerance)
        {
            try
            {
                Debug.Log($"Processing multi-polygon coordinates: {coordinatesJson.Substring(0, Math.Min(100, coordinatesJson.Length))}...");

                // Extract complete polygon arrays using an improved regex pattern
                // This pattern matches complete polygon structures with balanced brackets
                var polygonMatches = ExtractPolygonsFromMultiPolygon(coordinatesJson);

                Debug.Log($"Found {polygonMatches.Count} polygons in multi-polygon");

                if (polygonMatches.Count == 0)
                {
                    Debug.LogWarning("No polygons found in multi-polygon");
                    return null;
                }

                var polygons = new List<CountryPolygon>();

                // Process each polygon
                foreach (string polygonJson in polygonMatches)
                {
                    Debug.Log($"Processing polygon from multi-polygon: {polygonJson.Substring(0, Math.Min(50, polygonJson.Length))}...");
                    var polygon = ProcessPolygonDirectly(polygonJson, optimize, tolerance);

                    if (polygon != null)
                    {
                        polygons.Add(polygon);
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to process polygon: {polygonJson.Substring(0, Math.Min(100, polygonJson.Length))}...");
                    }
                }

                return polygons.ToArray();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing multi-polygon directly: {ex.Message}\nStack trace: {ex.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// Extracts individual polygon structures from a MultiPolygon coordinates array.
        /// Uses a balanced bracket approach to correctly extract complete polygon data.
        /// </summary>
        /// <param name="multiPolygonJson">The MultiPolygon coordinates JSON string.</param>
        /// <returns>A list of polygon JSON strings.</returns>
        private static List<string> ExtractPolygonsFromMultiPolygon(string multiPolygonJson)
        {
            var polygons = new List<string>();

            try
            {
                // Remove outer brackets if present
                string content = multiPolygonJson.Trim();
                if (content.StartsWith("[") && content.EndsWith("]"))
                {
                    content = content.Substring(1, content.Length - 2).Trim();
                }

                int startIndex = 0;
                int bracketCount = 0;
                bool inPolygon = false;

                for (int i = 0; i < content.Length; i++)
                {
                    char c = content[i];

                    if (c == '[')
                    {
                        bracketCount++;
                        if (bracketCount == 1 && !inPolygon)
                        {
                            startIndex = i;
                            inPolygon = true;
                        }
                    }
                    else if (c == ']')
                    {
                        bracketCount--;
                        if (bracketCount == 0 && inPolygon)
                        {
                            // Extract the complete polygon
                            string polygon = content.Substring(startIndex, i - startIndex + 1);
                            polygons.Add(polygon);
                            inPolygon = false;
                        }
                    }
                }

                Debug.Log($"Extracted {polygons.Count} polygons from MultiPolygon using balanced bracket approach");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error extracting polygons from MultiPolygon: {ex.Message}");
            }

            return polygons;
        }
    }
}