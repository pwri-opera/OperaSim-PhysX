using System.IO;
using UnityEditor;
using UnityEngine;

public class LandXML : EditorWindow
{
    public string landXMLFilePath;

    [MenuItem("OPERA/Import/LandXML")]
    static void OpenFilePanel()
    {
        var path = EditorUtility.OpenFilePanel("Select Asset",  Application.dataPath, "xml");
        if (string.IsNullOrEmpty(path))
            return;

        LandXML window = (LandXML)EditorWindow.GetWindow(typeof(LandXML));
        window.landXMLFilePath = path;
        window.Show();
    }

    LandXML() {
    }

    void OnGUI()
    {
        GUILayout.Label("LandXML Path: " + landXMLFilePath);
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        GUILayout.Label("Import Setting", EditorStyles.boldLabel);
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        // create button to process the data
        if (GUILayout.Button("Import")) {
            var landXMLData = LandXMLSurfaceParser.ParseSurfaces(landXMLFilePath);
            foreach (var surface in landXMLData)
            {
                Debug.Log("Surface Name: " + surface.Name);
                Debug.Log("Surface Description: " + surface.Description);
                Debug.Log("Surface Area2D: " + surface.Area2D);
                Debug.Log("Surface Area3D: " + surface.Area3D);
                Debug.Log("Surface MaxElevation: " + surface.MaxElevation);
                Debug.Log("Surface MinElevation: " + surface.MinElevation);
                Debug.Log("Surface Points Count: " + surface.Points.Count);
                Debug.Log("Surface Faces Count: " + surface.Faces.Count);
            }
        }
        if (GUILayout.Button("Close")) {
            Close();
        }
    }
}
