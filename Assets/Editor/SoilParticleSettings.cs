using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

public class SoilParticleSettings : MonoBehaviour
{
    public static SoilParticleSettings instance = null;

    public bool enable = true;
    public float particleVisualRadius = 0.2f;
    public double partileStickDistance = 0.25;
    public float stickForce = 30.0f;

    [SerializeField]
    public Vector2 m_AngleOfRepose = new Vector2(30.0f, 45.0f);
    [SerializeField]
    public int m_ReposeJitter = 0;

    public float syncPeriod = 1.0f;

    private float timeElapsed = 0.0f;
    
    ComputeShader cs = null;
    int thermalKernelIdx = -1;
    int xRes = -1;
    int yRes = -1;
    RenderTexture heightmapRT0 = null;
    RenderTexture heightmapRT1 = null;
    RenderTexture sedimentRT = null;

    ComputeShader cs2 = null;
    int digKernelIdx = -1;

    private float[,] originalHeights;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }


        cs = AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/com.unity.terrain-tools/Editor/TerrainTools/Compute/Thermal.compute");
        if (cs == null)
        {
            throw new MissingReferenceException("Could not find compute shader for thermal erosion");
        }
        thermalKernelIdx = cs.FindKernel("ThermalErosion");

        cs2 = Instantiate(Resources.Load<ComputeShader>("Dig"));
        if (cs2 == null)
        {
            throw new MissingReferenceException("Could not find compute shader fo digging");
        }
        digKernelIdx = cs2.FindKernel("Dig");
    }

    private void Start()
    {
        var terrainData = gameObject.GetComponent<Terrain>().terrainData;

        originalHeights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);

        var terrainTexture = terrainData.heightmapTexture;
        var terrainScale = terrainData.size;

        xRes = terrainTexture.width;
        yRes = terrainTexture.height;

        var texelSize = new Vector2(terrainScale.x / xRes,
                                    terrainScale.z / yRes);

        heightmapRT0 = new RenderTexture(xRes, yRes, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
        heightmapRT0.enableRandomWrite = true;
        heightmapRT0.filterMode = FilterMode.Point;
        heightmapRT1 = new RenderTexture(xRes, yRes, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
        heightmapRT1.enableRandomWrite = true;
        heightmapRT1.filterMode = FilterMode.Point;
        sedimentRT = new RenderTexture(xRes, yRes, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
        sedimentRT.enableRandomWrite = true;
        sedimentRT.filterMode = FilterMode.Point;

        Graphics.Blit(terrainTexture, heightmapRT0);
        Graphics.Blit(terrainTexture, heightmapRT1);
        Graphics.Blit(Texture2D.blackTexture, sedimentRT);

        float dx = (float)texelSize.x;
        float dy = (float)texelSize.y;
        float dxdy = Mathf.Sqrt(dx * dx + dy * dy);

        cs.SetFloat("dt", Time.fixedDeltaTime);
        cs.SetFloat("InvDiagMag", 1.0f / dxdy);
        cs.SetVector("dxdy", new Vector4(dx, dy, 1.0f / dx, 1.0f / dy));
        cs.SetVector("terrainDim", new Vector4(terrainScale.x, terrainScale.y, terrainScale.z));
        cs.SetVector("texDim", new Vector4((float)xRes, (float)yRes, 0.0f, 0.0f));
        cs.SetTexture(thermalKernelIdx, "Sediment", sedimentRT);

        timeElapsed = 0.0f;
    }

    void OnApplicationQuit()
    {
        if (heightmapRT0 != null) heightmapRT0.Release();
        if (heightmapRT1 != null) heightmapRT1.Release();
        if (sedimentRT != null) sedimentRT.Release();
        gameObject.GetComponent<Terrain>().terrainData.SetHeights(0, 0, originalHeights);
    }

    public static void ModifyTerrain(Vector3 point, float diff)
    {
        if (instance == null)
            return;

        if (!instance.enable)
            return;

        RenderTexture prevRT = RenderTexture.active;

        var terrainData = instance.GetComponent<Terrain>().terrainData;
        Vector3 relpos = (point - instance.transform.position);
        var posXInTerrain = (int)(relpos.x / terrainData.size.x * instance.xRes);
        var posYInTerrain = (int)(relpos.z / terrainData.size.z * instance.yRes);

        instance.cs2.SetTexture(instance.digKernelIdx, "heightmap", instance.heightmapRT0);
        instance.cs2.SetFloat("diff", diff);
        instance.cs2.SetVector("pos", new Vector2(posXInTerrain, posYInTerrain));

        instance.cs2.Dispatch(instance.digKernelIdx, 1, 1, 1);

        RenderTexture.active = instance.heightmapRT0;
        RectInt rect = new RectInt(posXInTerrain - 10, posYInTerrain - 10, 20, 20);
        terrainData.CopyActiveRenderTextureToHeightmap(rect, rect.min, TerrainHeightmapSyncControl.None);

        RenderTexture.active = prevRT;
    }

    // Part of this code is from:
    //  com.unity.terrain-tools/Editor/TerrainTools/Erosion/ThermalEroder.cs
    void FixedUpdate()
    {
        timeElapsed += Time.fixedDeltaTime;

        if (!enable)
            return;

        RenderTexture prevRT = RenderTexture.active;

        var terrainData = gameObject.GetComponent<Terrain>().terrainData;

        cs.SetTexture(thermalKernelIdx, "TerrainHeightPrev", heightmapRT0);
        cs.SetTexture(thermalKernelIdx, "TerrainHeight", heightmapRT1);

        Vector2 jitteredTau = m_AngleOfRepose + new Vector2(0.9f * (float)m_ReposeJitter * (UnityEngine.Random.value - 0.5f), 0.9f * (float)m_ReposeJitter * (UnityEngine.Random.value - 0.5f));
        jitteredTau.x = Mathf.Clamp(jitteredTau.x, 0.0f, 89.9f);
        jitteredTau.y = Mathf.Clamp(jitteredTau.y, 0.0f, 89.9f);
        Vector2 m = new Vector2(Mathf.Tan(jitteredTau.x * Mathf.Deg2Rad), Mathf.Tan(jitteredTau.y * Mathf.Deg2Rad));
        cs.SetVector("angleOfRepose", new Vector4(m.x, m.y, 0.0f, 0.0f));

        cs.Dispatch(thermalKernelIdx, xRes, yRes, 1);

        if (timeElapsed >= syncPeriod)
        {
            //Debug.Log("SoilParticle sync.");

            RenderTexture.active = heightmapRT1;

            foreach (var robot in GameObject.FindGameObjectsWithTag("robot"))
            {
                if (robot.activeInHierarchy)
                {
                    Component base_link = null;
                    try
                    {
                        var childs = new List<Component>(robot.GetComponentsInChildren(typeof(Component)));
                        base_link = childs.Find(c => c.name == "base_link");
                    }
                    catch (ArgumentNullException e)
                    {
                        base_link = robot.GetComponent(typeof(Component));
                    }
                    Vector3 relpos = (base_link.transform.position - gameObject.transform.position);
                    var posXInTerrain = (int)(relpos.x / terrainData.size.x * xRes);
                    var posYInTerrain = (int)(relpos.z / terrainData.size.z * yRes);
                    RectInt rect = new RectInt(posXInTerrain - 30, posYInTerrain - 30, 60, 60);
                    terrainData.CopyActiveRenderTextureToHeightmap(rect, rect.min, TerrainHeightmapSyncControl.None);
                }
            }

            timeElapsed = 0.0f;
        }
        terrainData.SyncHeightmap();

        // swap
        var temp = heightmapRT0;
        heightmapRT0 = heightmapRT1;
        heightmapRT1 = temp;

        RenderTexture.active = prevRT;
    }
}