using UnityEngine;
using UnityEditor;

public class SoilParticleSettings : MonoBehaviour
{
    public static SoilParticleSettings instance = null;

    public bool enable = true;
    public float particleVisualRadius = 0.2f;
    public double partileStickDistance = 0.25;
    public float stickForce = 30.0f;

    [SerializeField]
    public Vector2 m_AngleOfRepose = new Vector2(32.0f, 36.0f);
    [SerializeField]
    public int m_ReposeJitter = 0;
    [SerializeField]
    public float m_dt = 0.0025f;

    public float syncPeriod = 1.0f;

    private float timeElapsed = 0.0f;
    
    ComputeShader cs = null;
    int thermalKernelIdx = -1;
    int xRes = -1;
    int yRes = -1;
    RenderTexture heightmapRT0 = null;
    RenderTexture heightmapRT1 = null;
    RenderTexture sedimentRT = null;
    RenderTexture hardnessRT = null;
    RenderTexture reposeAngleRT = null;
    RenderTexture collisionRT = null;

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
            throw new MissingReferenceException("Could not find compute shader");
        }
        thermalKernelIdx = cs.FindKernel("ThermalErosion");
    }

    private void Start()
    {
        var terrainData = gameObject.GetComponent<Terrain>().terrainData;
        var terrainTexture = terrainData.heightmapTexture;
        var terrainScale = terrainData.heightmapScale;
        var texelSize = new Vector2(32, 32);

        xRes = terrainTexture.width;
        yRes = terrainTexture.height;

        heightmapRT0 = new RenderTexture(xRes, yRes, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
        heightmapRT0.enableRandomWrite = true;
        heightmapRT1 = new RenderTexture(xRes, yRes, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
        heightmapRT1.enableRandomWrite = true;
        sedimentRT = new RenderTexture(xRes, yRes, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
        sedimentRT.enableRandomWrite = true;
        hardnessRT = new RenderTexture(xRes, yRes, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
        hardnessRT.enableRandomWrite = true;
        reposeAngleRT = new RenderTexture(xRes, yRes, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
        reposeAngleRT.enableRandomWrite = true;
        collisionRT = new RenderTexture(xRes, yRes, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
        collisionRT.enableRandomWrite = true;

        Graphics.Blit(terrainTexture, heightmapRT0);
        Graphics.Blit(terrainTexture, heightmapRT1);
        Graphics.Blit(Texture2D.blackTexture, sedimentRT);
        Graphics.Blit(Texture2D.blackTexture, hardnessRT);
        Graphics.Blit(Texture2D.blackTexture, reposeAngleRT);
        Graphics.Blit(Texture2D.blackTexture, collisionRT);

        float dx = (float)texelSize.x;
        float dy = (float)texelSize.y;
        float dxdy = Mathf.Sqrt(dx * dx + dy * dy);

        cs.SetFloat("dt", m_dt);
        cs.SetFloat("InvDiagMag", 1.0f / dxdy);
        cs.SetVector("dxdy", new Vector4(dx, dy, 1.0f / dx, 1.0f / dy));
        cs.SetVector("terrainDim", new Vector4(terrainScale.x, terrainScale.y, terrainScale.z));
        cs.SetVector("texDim", new Vector4((float)xRes, (float)yRes, 0.0f, 0.0f));
        cs.SetTexture(thermalKernelIdx, "Sediment", sedimentRT);
        cs.SetTexture(thermalKernelIdx, "ReposeMask", reposeAngleRT);
        cs.SetTexture(thermalKernelIdx, "Collision", collisionRT);
        cs.SetTexture(thermalKernelIdx, "Hardness", hardnessRT);

        timeElapsed = 0.0f;
    }

    void OnApplicationQuit()
    {
        if (heightmapRT0 != null) heightmapRT0.Release();
        if (heightmapRT1 != null) heightmapRT1.Release();
        if (sedimentRT != null) sedimentRT.Release();
        if (hardnessRT != null) hardnessRT.Release();
        if (reposeAngleRT != null) reposeAngleRT.Release();
        if (collisionRT != null) collisionRT.Release();
    }

    public void ModifyTerrain(Vector3 point, float diff)
    {
        var terrainData = gameObject.GetComponent<Terrain>().terrainData;
        Vector3 relpos = (point - gameObject.transform.position);
        Vector3 pos;
        pos.x = relpos.x / terrainData.size.x;
        pos.y = relpos.y / terrainData.size.y;
        pos.z = relpos.z / terrainData.size.z;
        var posXInTerrain = (int)(pos.x * terrainData.heightmapResolution);
        var posYInTerrain = (int)(pos.z * terrainData.heightmapResolution);
        int size = 6;
        int offset = size / 2;
        float[,] heights = terrainData.GetHeights(posXInTerrain - offset, posYInTerrain - offset, size, size);
        for (int i = 0; i < size; i++)
            for (int j = 0; j < size; j++)
                heights[i, j] += diff;
        terrainData.SetHeights(posXInTerrain - offset, posYInTerrain - offset, heights);
        terrainData.SyncHeightmap();
    }

    // Part of this code is from:
    //  com.unity.terrain-tools/Editor/TerrainTools/Erosion/ThermalEroder.cs
    void FixedUpdate()
    {
        int[] numWorkGroups = { 1, 1, 1 };

        var terrainData = gameObject.GetComponent<Terrain>().terrainData;

        //Graphics.Blit(terrainData.heightmapTexture, heightmapRT0);

        RenderTexture prevRT = RenderTexture.active;

        cs.SetTexture(thermalKernelIdx, "TerrainHeightPrev", heightmapRT0);
        cs.SetTexture(thermalKernelIdx, "TerrainHeight", heightmapRT1);

        Vector2 jitteredTau = m_AngleOfRepose + new Vector2(0.9f * (float)m_ReposeJitter * (UnityEngine.Random.value - 0.5f), 0.9f * (float)m_ReposeJitter * (UnityEngine.Random.value - 0.5f));
        jitteredTau.x = Mathf.Clamp(jitteredTau.x, 0.0f, 89.9f);
        jitteredTau.y = Mathf.Clamp(jitteredTau.y, 0.0f, 89.9f);
        Vector2 m = new Vector2(Mathf.Tan(jitteredTau.x * Mathf.Deg2Rad), Mathf.Tan(jitteredTau.y * Mathf.Deg2Rad));
        cs.SetVector("angleOfRepose", new Vector4(m.x, m.y, 0.0f, 0.0f));

        cs.Dispatch(thermalKernelIdx, xRes / numWorkGroups[0], yRes / numWorkGroups[1], numWorkGroups[2]);

        timeElapsed += Time.deltaTime;

        if (timeElapsed >= syncPeriod)
        {
            Debug.Log("SoilParticle sync.");
            RenderTexture.active = heightmapRT1;
            RectInt rect = new RectInt(0, 0, xRes, yRes);
            terrainData.CopyActiveRenderTextureToHeightmap(rect, rect.min, TerrainHeightmapSyncControl.HeightAndLod);
            //terrainData.SyncHeightmap();
            timeElapsed = 0.0f;
        }

        // swap
        var temp = heightmapRT0;
        heightmapRT0 = heightmapRT1;
        heightmapRT1 = temp;

        RenderTexture.active = prevRT;
    }
}