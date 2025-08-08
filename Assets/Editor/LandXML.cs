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
            var landXMLSurfaces = LandXMLSurfaceParser.ParseSurfaces(path, (progress) => {
                EditorUtility.DisplayProgressBar("Parsing LandXML", "Parsing...", progress);
            });
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
                surfaceColors[landXMLSurfaces[i].Name] = new Color(0, 0, 1, 0.5f);  // default color is transparent blue
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
            Debug.Log($"Min: ({minX}, {minY}, {minZ})");
            Debug.Log($"Max: ({maxX}, {maxY}, {maxZ})");

            double center_x = (minX + maxX) / 2;
            double center_y = (minY + maxY) / 2;
            double center_z = (minZ + maxZ) / 2;
            Debug.Log($"Center: ({center_x}, {center_y}, {center_z})");
            
            // Create a parent object to hold all surfaces
            GameObject parentObject = new GameObject("LandXML_Surfaces");
            
            foreach (var surface in landXMLData)
            {
                // Create mesh for each surface
                GameObject surfaceObject = LandXMLMeshConverter.CreateMeshFromSurface(surface, new Vector3((float)center_x, (float)center_y, (float)center_z));
                
                var renderer = surfaceObject.GetComponent<MeshRenderer>();
                var color = surfaceColors[surface.Name];

                // make transparent
                renderer.sharedMaterial.SetFloat("_Mode", 2);
                if (color.a < 1)
                {
                    renderer.sharedMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    renderer.sharedMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                }
                renderer.sharedMaterial.SetInt("_ZWrite", 1);
                renderer.sharedMaterial.DisableKeyword("_ALPHATEST_ON");
                renderer.sharedMaterial.EnableKeyword("_ALPHABLEND_ON");
                renderer.sharedMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");

                // set color
                renderer.sharedMaterial.SetColor("_Color", color);

                renderer.sharedMaterial.renderQueue = -1;

                surfaceObject.transform.SetParent(parentObject.transform, false);

                Debug.Log($"Created mesh for surface: {surface.Name}");
                Debug.Log($"Points: {surface.Points.Count}, Faces: {surface.Faces.Count}");
            }

            // Select the parent object in the hierarchy
            Selection.activeGameObject = parentObject;

            Close();
        }
    }
}
