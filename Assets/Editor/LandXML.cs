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

            // calculate max and min of each xyz axes
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double minZ = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;
            double maxZ = double.MinValue;     
            foreach (var surface in landXMLData)
            {
                foreach (var point in surface.Points)
                {
                    if (point.X < minX) minX = point.X;
                    if (point.Y < minY) minY = point.Y;
                    if (point.Z < minZ) minZ = point.Z;
                    if (point.X > maxX) maxX = point.X;
                    if (point.Y > maxY) maxY = point.Y;
                    if (point.Z > maxZ) maxZ = point.Z;
                }
            }
            double center_x = (minX + maxX) / 2;
            double center_y = (minY + maxY) / 2;
            double center_z = (minZ + maxZ) / 2;
            
            // Create a parent object to hold all surfaces
            GameObject parentObject = new GameObject("LandXML_Surfaces");
            
            foreach (var surface in landXMLData)
            {
                // Create mesh for each surface
                GameObject surfaceObject = LandXMLMeshConverter.CreateMeshFromSurface(surface, new Vector3((float)center_x, (float)center_y, (float)center_z));
                surfaceObject.transform.SetParent(parentObject.transform, false);
                
                Debug.Log($"Created mesh for surface: {surface.Name}");
                Debug.Log($"Points: {surface.Points.Count}, Faces: {surface.Faces.Count}");
            }

            // Select the parent object in the hierarchy
            Selection.activeGameObject = parentObject;
        }

        if (GUILayout.Button("Close")) {
            Close();
        }
    }
}
