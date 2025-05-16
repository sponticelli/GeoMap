using UnityEditor;
using GeoJSONTools.Editor;

public static class OpenGeoJSONImporter
{
    [MenuItem("Window/Open GeoJSON Importer")]
    public static void OpenWindow()
    {
        GeoJSONImporterWindow.ShowWindow();
    }
}