using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainTiler : MonoBehaviour
{
    int divides = 4;

    private Terrain masterTerrain;

    private float[,] originalHeights;
    private float[,,] originalSplat;

    private List<Terrain> terrains;

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

        terrains = new List<Terrain>();

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

                // divide splatmap into tiles
                int splatmapWidth = terrainData.alphamapWidth / divides;
                int splatmapHeight = terrainData.alphamapHeight / divides;
                float[,,] splatmapData = new float[splatmapHeight, splatmapWidth, terrainData.alphamapLayers];
                for (int sx = 0; sx < splatmapWidth; sx++) {
                    for (int sy = 0; sy < splatmapHeight; sy++) {
                        for (int splat = 0; splat < terrainData.alphamapLayers; splat++) {
                            splatmapData[sy, sx, splat] = originalSplat[y * splatmapHeight + sy, x * splatmapWidth + sx, splat];
                        }
                    }
                }
                terrain.terrainData.SetAlphamaps(0, 0, splatmapData);

                // divide heightmap into tiles
                int heightmapWidth = terrainData.heightmapResolution / divides;
                int heightmapHeight = terrainData.heightmapResolution / divides;
                float[,] heightData = new float[heightmapHeight, heightmapWidth];
                for (int hx = 0; hx < heightmapWidth; hx++) {
                    for (int hy = 0; hy < heightmapHeight; hy++) {
                        heightData[hy, hx] = originalHeights[y * heightmapHeight + hy, x * heightmapWidth + hx];
                    }
                }
                terrain.terrainData.SetHeights(0, 0, heightData);

                terrains.Add(terrain);
            }
        }

        // set neighbor terrains
        for (int y = 0; y < divides; y++) {
            for (int x = 0; x < divides; x++) {
                int index = (y * divides) + x;
                Terrain terrain = terrains[index];
                Terrain topTerrain = (x > 0) ? terrains[index - 1] : null;
                Terrain bottomTerrain = (x < divides - 1) ? terrains[index + 1] : null;
                Terrain leftTerrain = (y > 0) ? terrains[index - divides] : null;
                Terrain rightTerrain = (y < divides - 1) ? terrains[index + divides] : null;
                terrain.SetNeighbors(leftTerrain, bottomTerrain, rightTerrain, topTerrain);
            }
        }

        // hide the original terrain
        gameObject.SetActive(false);
    }

    private GameObject CreateTile()
    {
        var t = new TerrainData();
        t.heightmapResolution = masterTerrain.terrainData.heightmapResolution / divides;
        t.alphamapResolution = masterTerrain.terrainData.alphamapResolution / divides;
        t.size = new Vector3(masterTerrain.terrainData.size.x / divides, masterTerrain.terrainData.size.y, masterTerrain.terrainData.size.z / divides);
        var tile = Terrain.CreateTerrainGameObject(t);
        tile.tag = "terrain";
        return tile;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
