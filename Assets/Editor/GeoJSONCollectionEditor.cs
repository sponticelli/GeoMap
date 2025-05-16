using UnityEngine;
using UnityEditor;
using GeoData;

namespace GeoJSONTools.Editor
{
    [CustomEditor(typeof(GeoJSONCollection))]
    public class GeoJSONCollectionEditor : UnityEditor.Editor
    {
        private Vector2 scrollPosition;
        private bool showStatistics = true;

        public override void OnInspectorGUI()
        {
            var collection = target as GeoJSONCollection;
            if (collection == null) return;

            // Header
            EditorGUILayout.BeginVertical("box");
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("GeoJSON Country Collection", titleStyle);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // Statistics Section
            EditorGUILayout.BeginVertical("box");
            showStatistics = EditorGUILayout.Foldout(showStatistics, "Collection Statistics", true);

            if (showStatistics)
            {
                EditorGUI.indentLevel++;
                DrawStatistics(collection);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // Draw default inspector
            DrawDefaultInspector();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawStatistics(GeoJSONCollection collection)
        {
            collection.UpdateStatistics();

            EditorGUILayout.LabelField($"Total Countries: {collection.CountryCount}");
            EditorGUILayout.LabelField($"Total Polygons: {collection.totalPolygons}");
            EditorGUILayout.LabelField($"Total Points: {collection.totalPoints}");
            EditorGUILayout.LabelField($"Total Rings: {collection.totalRings}");
        }
    }
}