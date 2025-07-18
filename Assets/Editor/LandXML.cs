using System.IO;
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;

public class LandXML : EditorWindow
{
    public string landXMLFilePath;
    public List<Surface> landXMLSurfaces;
    private Dictionary<string, Color> surfaceColors;

    [MenuItem("OPERA/Import/LandXML")]
    static void OpenFilePanel()
    {
        var path = EditorUtility.OpenFilePanel("Select Asset",  Application.dataPath, "xml");
        if (string.IsNullOrEmpty(path))
            return;

        try {
            var landXMLSurfaces = LandXMLSurfaceParser.ParseSurfaces(path);
            LandXML window = (LandXML)EditorWindow.GetWindow(typeof(LandXML));
            window.landXMLFilePath = path;
            window.landXMLSurfaces = landXMLSurfaces;
            window.Show();
        } catch (Exception e) {
            Debug.LogError(e.Message);
            EditorUtility.DisplayDialog("Error", "Failed to parse LandXML file:\n" + e.Message, "OK");
            return;
        }
    }

    LandXML() {
        surfaceColors = new Dictionary<string, Color>();
    }

    void OnGUI()
    {
        GUILayout.Label("LandXML Path: " + landXMLFilePath);
        GUILayout.Label("Number of Surfaces: " + landXMLSurfaces.Count);
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));

        for (int i = 0; i < landXMLSurfaces.Count; i++) {
            GUILayout.Label("Surface " + i + ": " + landXMLSurfaces[i].Name);
            // color picker for each surface
            if (!surfaceColors.ContainsKey(landXMLSurfaces[i].Name)) {
                surfaceColors[landXMLSurfaces[i].Name] = Color.blue;
            }
            surfaceColors[landXMLSurfaces[i].Name] = EditorGUILayout.ColorField("Color", surfaceColors[landXMLSurfaces[i].Name]);
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        }
        // create button to process the data
        if (GUILayout.Button("Import")) {
            var landXMLData = landXMLSurfaces;

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
                
                // set color
                surfaceObject.GetComponent<MeshRenderer>().material.color = surfaceColors[surface.Name];
                
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
