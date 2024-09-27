using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainTiler : MonoBehaviour
{
    public int divides = 4;

    private Terrain masterTerrain;

    private float[,] originalHeights;
    private float[,,] originalSplat;

    [HideInInspector]
    public List<GameObject> terrains;

    private static TerrainTiler instance = null;
    public void Awake()
    {
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        masterTerrain = gameObject.GetComponent<Terrain>();
        var terrainData = masterTerrain.terrainData;
        var terrainScale = terrainData.size;

        originalHeights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
        originalSplat = terrainData.GetAlphamaps(0, 0, terrainData.alphamapResolution, terrainData.alphamapResolution);

        // hide the original terrain
        gameObject.GetComponent<Terrain>().enabled = false;
        gameObject.GetComponent<TerrainCollider>().enabled = false;

        terrains = new List<GameObject>();

        for (int y = 0; y < divides; y++) {
            for (int x = 0; x < divides; x++) {
                var tile = CreateTile();
                var terrain = tile.GetComponent<Terrain>();
                tile.transform.position = new Vector3(gameObject.transform.position.x + terrainScale.x / divides * x, gameObject.transform.position.y, gameObject.transform.position.z + terrainScale.z / divides * y);

                // copy terrain layers to tile
                var layers = new List<TerrainLayer>();
                foreach (var layer in terrainData.terrainLayers) {
                    var tileLayer = new TerrainLayer();
                    tileLayer.diffuseTexture = layer.diffuseTexture;
                    tileLayer.normalMapTexture = layer.normalMapTexture;
                    tileLayer.tileSize = layer.tileSize;
                    tileLayer.tileOffset = new Vector2(terrainScale.x / divides * x, terrainScale.z / divides * y);
                    layers.Add(tileLayer);
                }
                terrain.terrainData.terrainLayers = layers.ToArray();

                terrains.Add(tile);
            }
        }

        // set neighbor terrains
        for (int y = 0; y < divides; y++) {
            for (int x = 0; x < divides; x++) {
                int index = (y * divides) + x;
                var terrain = terrains[index].GetComponent<Terrain>();
                Terrain topTerrain = (x > 0) ? terrains[index - 1].GetComponent<Terrain>() : null;
                Terrain bottomTerrain = (x < divides - 1) ? terrains[index + 1].GetComponent<Terrain>() : null;
                Terrain leftTerrain = (y > 0) ? terrains[index - divides].GetComponent<Terrain>() : null;
                Terrain rightTerrain = (y < divides - 1) ? terrains[index + divides].GetComponent<Terrain>() : null;
                terrain.SetNeighbors(leftTerrain, bottomTerrain, rightTerrain, topTerrain);
            }
        }

        for (int y = 0; y < divides; y++) {
            for (int x = 0; x < divides; x++) {
                int index = (y * divides) + x;
                var tile = terrains[index];
                var terrain = tile.GetComponent<Terrain>();

                // divide splatmap into tiles
                int splatmapWidth = terrainData.alphamapWidth / divides;
                int splatmapHeight = terrainData.alphamapHeight / divides;
                float[,,] splatmapData = new float[splatmapHeight + 1, splatmapWidth + 1, terrainData.alphamapLayers];
                for (int sx = 0; sx < splatmapWidth + 1; sx++) {
                    for (int sy = 0; sy < splatmapHeight + 1; sy++) {
                        int isx = sx > splatmapWidth - 1 ? splatmapWidth - 1 : sx;
                        int isy = sy > splatmapHeight - 1 ? splatmapHeight - 1 : sy;
                        for (int splat = 0; splat < terrainData.alphamapLayers; splat++) {
                            splatmapData[sy, sx, splat] = originalSplat[y * splatmapHeight + isy, x * splatmapWidth + isx, splat];
                        }
                    }
                }
                terrain.terrainData.SetAlphamaps(0, 0, splatmapData);

                // divide heightmap into tiles
                int heightmapWidth = terrainData.heightmapResolution / divides;
                int heightmapHeight = terrainData.heightmapResolution / divides;
                float[,] heightData = new float[heightmapHeight + 1, heightmapWidth + 1];
                for (int hx = 0; hx < heightmapWidth + 1; hx++) {
                    for (int hy = 0; hy < heightmapHeight + 1; hy++) {
                        int ihx = hx > heightmapWidth - 1 ? heightmapWidth - 1 : hx;
                        int ihy = hy > heightmapHeight - 1 ? heightmapHeight - 1 : hy;
                        heightData[hy, hx] = originalHeights[y * heightmapHeight + ihy, x * heightmapWidth + ihx];
                    }
                }
                terrain.terrainData.SetHeights(0, 0, heightData);
            }
        }
    }

    private GameObject CreateTile()
    {
        var t = new TerrainData();
        t.heightmapResolution = masterTerrain.terrainData.heightmapResolution / divides + 1;
        t.alphamapResolution = masterTerrain.terrainData.alphamapResolution / divides + 1;
        t.size = new Vector3(masterTerrain.terrainData.size.x / divides, masterTerrain.terrainData.size.y, masterTerrain.terrainData.size.z / divides);
        var tile = Terrain.CreateTerrainGameObject(t);
        return tile;
    }
}
