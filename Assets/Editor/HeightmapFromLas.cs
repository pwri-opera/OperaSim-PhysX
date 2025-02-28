using System.IO;
using UnityEditor;
using UnityEngine;
using laszip.net;

public class HeightmapFromLas : EditorWindow
{
    public string lasFilePath;
    private laszip_dll lazReader;

    private string[] resolutionOptions;
    private int selectedResolution;

    private string[] divisionOptions;
    private int selectedDivision;

    [MenuItem("OPERA/Import/Heightmap from LAS")]
    static void OpenFilePanel()
    {
        var path = EditorUtility.OpenFilePanel("Select Asset",  Application.dataPath, "las,laz");
        if (string.IsNullOrEmpty(path))
            return;

        HeightmapFromLas window = (HeightmapFromLas)EditorWindow.GetWindow(typeof(HeightmapFromLas));
        window.lasFilePath = path;
        window.Show();
    }

    HeightmapFromLas() {
        resolutionOptions = new[]
        {
            "256",
            "512",
            "1024",
            "2048",
            "4096",
        };
        selectedResolution = 3;

        divisionOptions = new[]
        {
            "1",
            "2",
            "4",
            "8",
        };
        selectedDivision = 2;
    }

    public void Process(int resolution, int divide)
    {
    	Undo.RegisterCompleteObjectUndo(Terrain.activeTerrain.terrainData, "Heightmap from LAS");

        var numberOfPoints = lazReader.header.number_of_point_records;

    	TerrainData terrain = Terrain.activeTerrain.terrainData;

    	int w = terrain.heightmapResolution = resolution;
        int alphaw = terrain.alphamapResolution = resolution;

        float sizeX = (float)(lazReader.header.max_x - lazReader.header.min_x);
        float sizeY = (float)(lazReader.header.max_y - lazReader.header.min_y);
        float scaleX = (float)(lazReader.header.max_x - lazReader.header.min_x) / w;
        float scaleY = (float)(lazReader.header.max_y - lazReader.header.min_y) / w;
        float scaleXAlpha = (float)(lazReader.header.max_x - lazReader.header.min_x) / alphaw;
        float scaleYAlpha = (float)(lazReader.header.max_y - lazReader.header.min_y) / alphaw;
        float scaleZ = 100.0f;

        Terrain.activeTerrain.gameObject.transform.position = new Vector3(-sizeX / 2.0f, -(float)lazReader.header.z_offset, -sizeY / 2.0f);

    	float[,] heightmapData = new float[w, w];
    	Color[] colorData = new Color[alphaw * alphaw];

        for (int pointIndex = 0; pointIndex < numberOfPoints; pointIndex++)
        {
            if (pointIndex % 20 == 0) {
                EditorUtility.DisplayProgressBar("Heightmap from LAS", "Converting LAS data", Mathf.InverseLerp(0.0f, numberOfPoints, pointIndex));
            }

            lazReader.laszip_read_point();
            double point_x = lazReader.header.x_scale_factor * lazReader.point.X + lazReader.header.x_offset;
            double point_y = lazReader.header.y_scale_factor * lazReader.point.Y + lazReader.header.y_offset;
            double point_z = lazReader.header.z_scale_factor * lazReader.point.Z + lazReader.header.z_offset;

            int x = (int)((point_x - lazReader.header.min_x) / scaleX);
            if (x < 0 || x >= w) continue;
            int y = (int)((point_y - lazReader.header.min_y) / scaleY);
            if (y < 0 || y >= w) continue;
            heightmapData[y, x] = (float)point_z / scaleZ;

            int ax = (int)((point_x - lazReader.header.min_x) / scaleXAlpha);
            if (ax < 0 || ax >= alphaw) continue;
            int ay = (int)((point_y - lazReader.header.min_y) / scaleYAlpha);
            if (ay < 0 || ay >= alphaw) continue;
            float r = lazReader.point.rgb[0] / 65535.0f;
            float g = lazReader.point.rgb[1] / 65535.0f;
            float b = lazReader.point.rgb[2] / 65535.0f;
            colorData[ax + ay * alphaw] = new Color(r, g, b);
        }
        terrain.size = new Vector3(sizeX, scaleZ, sizeY);
        terrain.SetHeights(0, 0, heightmapData);

        var originalFname = Path.GetFileNameWithoutExtension(lasFilePath);

        var textureName = AssetDatabase.GenerateUniqueAssetPath(
                        Path.Combine("Assets", "Terrains", originalFname + ".png"));
        var diffuseTexture = new Texture2D(alphaw, alphaw, TextureFormat.ARGB32, false, false);
        diffuseTexture.SetPixels(colorData);
        diffuseTexture.Apply(true, false);
        File.WriteAllBytes(textureName, diffuseTexture.EncodeToPNG());
        AssetDatabase.ImportAsset(textureName);

        var layerName = AssetDatabase.GenerateUniqueAssetPath(
                        Path.Combine("Assets", "Terrains", originalFname + ".terrainlayer"));
        var layer = new TerrainLayer();
        layer.tileSize = new Vector2(sizeX, sizeY);
        layer.diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(textureName);
        AssetDatabase.CreateAsset(layer, layerName);
        terrain.terrainLayers = new TerrainLayer[] { layer };

        float[,,] splatmapData = new float[alphaw, alphaw, 1];
        for (int sx = 0; sx < alphaw; sx++) {
            for (int sy = 0; sy < alphaw; sy++) {
                splatmapData[sy, sx, 0] = 1.0f;
            }
        }
        terrain.SetAlphamaps(0, 0, splatmapData);

        lazReader.laszip_close_reader();

        if (divide > 1) {
            var terrainTiler = Terrain.activeTerrain.gameObject.GetComponent<TerrainTiler>();
            if (terrainTiler == null) {
                terrainTiler = Terrain.activeTerrain.gameObject.AddComponent<TerrainTiler>();
            }
            terrainTiler.divides = divide;
        }

        EditorUtility.ClearProgressBar();
    }

    void OnGUI()
    {
        lazReader = new laszip_dll();
        var compressed = true;
        lazReader.laszip_open_reader(lasFilePath, ref compressed);
        // create area to display the las header info
        GUILayout.Label("LAS Header Info", EditorStyles.boldLabel);
        GUILayout.Label("Number of point records: " + lazReader.header.number_of_point_records);
        GUILayout.Label("X size: " + (lazReader.header.max_x - lazReader.header.min_x) + " [m]");
        GUILayout.Label("Y size: " + (lazReader.header.max_y - lazReader.header.min_y) + " [m]");
        GUILayout.Label("Z size: " + (lazReader.header.max_z - lazReader.header.min_z) + " [m]");
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        // create dropdown box to select the resolution
        GUILayout.Label("Import Setting", EditorStyles.boldLabel);
        selectedResolution = EditorGUILayout.Popup(
            label: new GUIContent("Resolution"),
            selectedIndex: selectedResolution,
            displayedOptions: resolutionOptions
        );
        selectedDivision = EditorGUILayout.Popup(
            label: new GUIContent("Divide"),
            selectedIndex: selectedDivision,
            displayedOptions: divisionOptions
        );
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        // create button to process the data
        if (GUILayout.Button("Import")) {
            Process(int.Parse(resolutionOptions[selectedResolution]), int.Parse(divisionOptions[selectedDivision]));
        }
        if (GUILayout.Button("Close")) {
            Close();
        }
    }
}
