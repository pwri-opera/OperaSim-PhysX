using UnityEditor;
using UnityEngine;
using laszip.net;

public class HeightmapFromLas : EditorWindow
{
    [MenuItem("OPERA/Import/Heightmap from LAS")]
    static void OpenFilePanel()
    {
        var path = EditorUtility.OpenFilePanel("Select Asset",  Application.dataPath, "las,laz");
        if (string.IsNullOrEmpty(path))
            return;
        
        var lazReader = new laszip_dll();
        var compressed = true;
        lazReader.laszip_open_reader(path, ref compressed);
        var numberOfPoints = lazReader.header.number_of_point_records;

    	Undo.RegisterCompleteObjectUndo(Terrain.activeTerrain.terrainData, "Heightmap from LAS");

    	TerrainData terrain = Terrain.activeTerrain.terrainData;

    	int w = 2048; // terrain.heightmapResolution;
        terrain.heightmapResolution = w;
        terrain.alphamapResolution = w;

        int sizeX = ((int)(lazReader.header.max_x - lazReader.header.min_x)) / 16 * 16;
        int sizeY = ((int)(lazReader.header.max_y - lazReader.header.min_y)) / 16 * 16;
        float scaleX = (float)(lazReader.header.max_x - lazReader.header.min_x) / w;
        float scaleY = (float)(lazReader.header.max_y - lazReader.header.min_y) / w;
        float scaleZ = 100.0f;

    	float[,] heightmapData = new float[w, w];
    	Color[] colorData = new Color[w * w];

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
            heightmapData[x, y] = (float)point_z / scaleZ;

            float r = lazReader.point.rgb[0] / 65535.0f;
            float g = lazReader.point.rgb[1] / 65535.0f;
            float b = lazReader.point.rgb[2] / 65535.0f;
            colorData[y + x * w] = new Color(r, g, b);
        }
        terrain.size = new Vector3(sizeX, 100.0f, sizeY);
        terrain.SetHeights(0, 0, heightmapData);

        var layer = new TerrainLayer();
        layer.tileSize = new Vector2(sizeX, sizeY);
        layer.diffuseTexture = new Texture2D(w, w);
        layer.diffuseTexture.SetPixels(colorData);
        layer.diffuseTexture.Apply();
        terrain.terrainLayers = new TerrainLayer[] { layer };

        float[,,] splatmapData = new float[w, w, 1];
        for (int sx = 0; sx < w; sx++) {
            for (int sy = 0; sy < w; sy++) {
                splatmapData[sy, sx, 0] = 1.0f;
            }
        }
        terrain.SetAlphamaps(0, 0, splatmapData);

        lazReader.laszip_close_reader();

        EditorUtility.ClearProgressBar();
    }
}
