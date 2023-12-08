using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.TerrainTools;
using System;

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
    [SerializeField]
    public int m_ThermalIterations = 10;

    ComputeShader cs = null;

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
    }

    // The following code is from:
    //  com.unity.terrain-tools/Editor/TerrainTools/Erosion/ThermalEroder.cs
    void FixedUpdate()
    {
        var terrainData = gameObject.GetComponent<Terrain>().terrainData;
        var terrainTexture = terrainData.heightmapTexture;
        var terrainScale = terrainData.heightmapScale;
        var texelSize = new Vector2(32, 32);

        RenderTexture prevRT = RenderTexture.active;

        int[] numWorkGroups = { 1, 1, 1 };

        int xRes = terrainTexture.width;
        int yRes = terrainTexture.height;

        var heightmapRT0 = RTUtils.GetTempHandle(RTUtils.GetDescriptorRW(xRes, yRes, 0, RenderTextureFormat.RFloat));
        var heightmapRT1 = RTUtils.GetTempHandle(RTUtils.GetDescriptorRW(xRes, yRes, 0, RenderTextureFormat.RFloat));

        var sedimentRT = RTUtils.GetTempHandle(RTUtils.GetDescriptorRW(xRes, yRes, 0, RenderTextureFormat.RFloat));
        var hardnessRT = RTUtils.GetTempHandle(RTUtils.GetDescriptorRW(xRes, yRes, 0, RenderTextureFormat.RFloat));
        var reposeAngleRT = RTUtils.GetTempHandle(RTUtils.GetDescriptorRW(xRes, yRes, 0, RenderTextureFormat.RFloat));
        var collisionRT = RTUtils.GetTempHandle(RTUtils.GetDescriptorRW(xRes, yRes, 0, RenderTextureFormat.RFloat));

        Graphics.Blit(terrainTexture, heightmapRT0);
        Graphics.Blit(terrainTexture, heightmapRT1);
        Graphics.Blit(Texture2D.blackTexture, sedimentRT);
        Graphics.Blit(Texture2D.blackTexture, hardnessRT);
        Graphics.Blit(Texture2D.blackTexture, reposeAngleRT);
        Graphics.Blit(Texture2D.blackTexture, collisionRT);

        int thermalKernelIdx = cs.FindKernel("ThermalErosion");

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

        for (int i = 0; i < m_ThermalIterations; i++)
        {
            cs.SetTexture(thermalKernelIdx, "TerrainHeightPrev", heightmapRT0);
            cs.SetTexture(thermalKernelIdx, "TerrainHeight", heightmapRT1);

            Vector2 jitteredTau = m_AngleOfRepose + new Vector2(0.9f * (float)m_ReposeJitter * (UnityEngine.Random.value - 0.5f), 0.9f * (float)m_ReposeJitter * (UnityEngine.Random.value - 0.5f));
            jitteredTau.x = Mathf.Clamp(jitteredTau.x, 0.0f, 89.9f);
            jitteredTau.y = Mathf.Clamp(jitteredTau.y, 0.0f, 89.9f);
            Vector2 m = new Vector2(Mathf.Tan(jitteredTau.x * Mathf.Deg2Rad), Mathf.Tan(jitteredTau.y * Mathf.Deg2Rad));
            cs.SetVector("angleOfRepose", new Vector4(m.x, m.y, 0.0f, 0.0f));

            cs.Dispatch(thermalKernelIdx, xRes / numWorkGroups[0], yRes / numWorkGroups[1], numWorkGroups[2]);

            // swap
            var temp = heightmapRT0;
            heightmapRT0 = heightmapRT1;
            heightmapRT1 = temp;
        }

        Graphics.Blit((m_ThermalIterations - 1) % 2 == 0 ? heightmapRT1 : heightmapRT0, RenderTexture.active);

        RectInt rect = new RectInt(0, 0, xRes, yRes);
        terrainData.CopyActiveRenderTextureToHeightmap(rect, rect.min, TerrainHeightmapSyncControl.None);
        terrainData.SyncHeightmap();

        RenderTexture.active = prevRT;

        RTUtils.Release(heightmapRT0);
        RTUtils.Release(heightmapRT1);
        RTUtils.Release(sedimentRT);
        RTUtils.Release(hardnessRT);
        RTUtils.Release(reposeAngleRT);
        RTUtils.Release(collisionRT);
    }
}