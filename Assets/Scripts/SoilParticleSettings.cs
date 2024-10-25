using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using GK;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;

/// <summary>
/// 各粒子にアタッチしてTerrainとの衝突イベントを通知するユーティリティスクリプト
/// </summary>
public class RockObjectDetector : MonoBehaviour
{

    [Tooltip("衝突イベントを通知する先のマネージャクラス")]
    public SoilParticleSettings manager;
    private double timecreated = 0.0;
    private Vector3 pos_last_collision = Vector3.zero;

    private void Start()
    {
        timecreated = Time.timeAsDouble;
        pos_last_collision = transform.position;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name == "Terrain")
        {
            pos_last_collision = transform.position;
        }
    }
    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.name == "Terrain")
        {
            pos_last_collision = transform.position;
        }
    }

    private void FixedUpdate()
    {
        if (Time.timeAsDouble - timecreated > 1.5)
        {
            var rigidbody = GetComponent<Rigidbody>();
            var velocity = rigidbody.velocity.sqrMagnitude;
            if (velocity < 0.2 && Vector3.Distance(transform.position, pos_last_collision) < 0.1)
            {
                manager.OnRockTerrainCollision(this.gameObject);
            }
        }
        if (this.transform.position.y - pos_last_collision.y < -1.0)
        {
            manager.OnRockTerrainCollision(this.gameObject);
        }
    }
}

/// <summary>
/// 掘削時の地形変形パラメータの設定
/// </summary>
public class SoilParticleSettings : MonoBehaviour
{
    public static SoilParticleSettings instance = null;

    [Tooltip("掘削時の地形変形を有効にする")]
    public bool enable = true;

    [Tooltip("粒子の見た目の大きさ(m)")]
    public float particleVisualRadius = 0.2f;

    [Tooltip("粒子間力を有効化する粒子間距離(m)")]
    public double partileStickDistance = 0.25;

    [Tooltip("粒子間力の強さ")]
    public float stickForce = 30.0f;

    [SerializeField]
    [Tooltip("地面の自然崩壊をシミュレートする際に用いる安息角の大きさ(x,y比)")]
    public Vector2 m_AngleOfRepose = new Vector2(30.0f, 45.0f);

    [SerializeField]
    [Tooltip("地面の自然崩壊をシミュレートする際に用いるノイズの大きさ")]
    public int m_ReposeJitter = 0;

    [Tooltip("heightmapを同期する間隔(秒)")]
    public float syncPeriod = 1.0f;

    [Tooltip("生成する粒子のPefabを設定してください")]
    public GameObject rockPrefab;

    private List<GameObject> rocks;
    private ConvexHullCalculator calc;
    private float particle_volume;
    private List<Mesh> mesh_patterns;
    private double last_created_time = 0.0;

    private ParticleForceJob job;
    private JobHandle job_handle;
    private bool job_started = false;
    private NativeArray<float3> job_positions;
    private NativeArray<float3> job_forces;

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

    private TerrainTiler tiler = null;

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

        tiler = GetComponent<TerrainTiler>();

        cs = Instantiate(Resources.Load<ComputeShader>("Thermal"));
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

        // pregenerate particle meshes
        calc = new ConvexHullCalculator();
        rocks = new List<GameObject>();
        particle_volume = (float)(4.0 / 3.0 * Math.PI * Math.Pow(particleVisualRadius, 3));
        mesh_patterns = new List<Mesh>();
        for (int i = 0; i < 10; i++)
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            var normals = new List<Vector3>();
            var points = new List<Vector3>();

            points.Clear();

            for (int j = 0; j < 100; j++)
            {
                points.Add(UnityEngine.Random.insideUnitSphere * particleVisualRadius);
            }

            calc.GenerateHull(points, true, ref verts, ref tris, ref normals);

            var mesh = new Mesh();
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.SetNormals(normals);

            mesh_patterns.Add(mesh);
        }

        last_created_time = Time.timeAsDouble;
    }

    private void CreateRock(Vector3 point)
    {
        var rock = Instantiate(rockPrefab);

        rock.transform.parent = null;
        rock.transform.position = point;
        rock.AddComponent<RockObjectDetector>();
        rock.GetComponent<RockObjectDetector>().manager = this;
        rock.GetComponent<MeshFilter>().sharedMesh = mesh_patterns[UnityEngine.Random.Range(0, mesh_patterns.Count)];

        rocks.Add(rock);
    }

    public void OnBucketCollision(Collision other)
    {
        if (!enable)
            return;

        if (other.gameObject.name != "Terrain")
            return;

        var now = Time.timeAsDouble;
        if (now - last_created_time > 0.01)
        {
            var point = other.GetContact(0).point;
            ModifyTerrain(point, -particle_volume);
            CreateRock(point);
            last_created_time = now;
        }
    }

    public void OnRockTerrainCollision(GameObject rock)
    {
        if (Vector3.Distance(transform.position, rock.transform.position) > 2.0)
        {
            ModifyTerrain(rock.transform.position, particle_volume);
            Destroy(rock);
            rocks.Remove(rock);
        }
    }

    [BurstCompile]
    private struct ParticleForceJob : IJobParallelFor
    {
        [ReadOnly]
        public float particleStickDistance;
        [ReadOnly]
        public NativeArray<float3> positions;
        [WriteOnly]
        public NativeArray<float3> forces;

        public void Execute(int index)
        {
            var rock1 = positions[index];
            var repulvector = new float3(0, 0, 0);
            for (var j = 0; j < positions.Length; j++)
            {
                var rock2 = positions[j];
                float dist = math.distance(rock1, rock2);
                if (dist < particleStickDistance)
                {
                    repulvector += rock1 - rock2;
                }
            }
            forces[index] = math.normalize(repulvector);
        }
    }

    private void Destroy()
    {
        if (job_started)
        {
            job_handle.Complete();
            job_positions.Dispose();
            job_forces.Dispose();
        }
    }

    // Update is called once per frame
    void UpdateStickForce()
    {
        if (job_started)
        {
            job_handle.Complete();
            if (rocks.Count == job.forces.Length)
            {
                for (var i = 0; i < rocks.Count; i++)
                {
                    var rock1 = rocks[i];
                    var f = job.forces[i];
                    if (!float.IsNaN(f.x))
                    {
                        rock1.GetComponent<Rigidbody>().AddForce(-f * SoilParticleSettings.instance.stickForce);
                    }
                }
            }
            job_positions.Dispose();
            job_forces.Dispose();
        }

        job_positions = new NativeArray<float3>(rocks.Count, Allocator.Persistent);
        job_forces = new NativeArray<float3>(rocks.Count, Allocator.Persistent);

        for (var i = 0; i < rocks.Count; i++)
        {
            var rock1 = rocks[i];
            job_positions[i] = rock1.transform.position;
        }

        job = new ParticleForceJob();
        job.particleStickDistance = (float)SoilParticleSettings.instance.partileStickDistance;
        job.positions = job_positions;
        job.forces = job_forces;

        job_handle = job.Schedule(job_forces.Length, 1);
        job_started = true;
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

        UnityEngine.Graphics.Blit(terrainTexture, heightmapRT0);
        UnityEngine.Graphics.Blit(terrainTexture, heightmapRT1);
        UnityEngine.Graphics.Blit(Texture2D.blackTexture, sedimentRT);

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

    public void ModifyTerrain(Vector3 point, float diff)
    {
        if (!enable)
            return;

        RenderTexture prevRT = RenderTexture.active;

        var terrainData = GetComponent<Terrain>().terrainData;
        Vector3 relpos = (point - transform.position);
        var posXInTerrain = (int)(relpos.x / terrainData.size.x * xRes);
        var posYInTerrain = (int)(relpos.z / terrainData.size.z * yRes);

        cs2.SetTexture(digKernelIdx, "heightmap", heightmapRT0);
        cs2.SetFloat("diff", diff);
        cs2.SetVector("pos", new Vector2(posXInTerrain, posYInTerrain));

        cs2.Dispatch(digKernelIdx, 1, 1, 1);

        RenderTexture.active = heightmapRT0;

        // TODO: need to modifiy for tiled heightmap
        RectInt rect = new RectInt(posXInTerrain - 10, posYInTerrain - 10, 20, 20);
        if (!tiler) {
            terrainData.CopyActiveRenderTextureToHeightmap(rect, rect.min, TerrainHeightmapSyncControl.None);
        } else {
            RectInt tilesize = new RectInt(0, 0, instance.xRes / tiler.divides, instance.yRes / tiler.divides);
            foreach (var t in tiler.terrains) {
                var terrainData2 = t.GetComponent<Terrain>().terrainData;
                Vector3 relpos2 = (point - t.transform.position);
                var posXInTerrain2 = (int)(relpos2.x / terrainData.size.x * xRes);
                var posYInTerrain2 = (int)(relpos2.z / terrainData.size.z * yRes);
                RectInt rect2 = new RectInt(posXInTerrain2 - 10, posYInTerrain2 - 10, 20, 20);
                if (rect2.Overlaps(tilesize)) {
                    try {
                        if (rect2.x < 0) rect2.x = 0;
                        if (rect2.y < 0) rect2.y = 0;
                        if (rect2.x + rect2.width > terrainData2.heightmapResolution) {
                            rect2.width = terrainData2.heightmapResolution - rect2.x;
                        }
                        if (rect2.y + rect2.height > terrainData2.heightmapResolution) {
                            rect2.height = terrainData2.heightmapResolution - rect2.y;
                        }
                        terrainData2.CopyActiveRenderTextureToHeightmap(rect, rect2.min, TerrainHeightmapSyncControl.None);
                    } catch (Exception e) {
                        //Debug.LogException(e);
                        //Debug.Log(rect2 + " " + rect + " " + tilesize);
                    }
                }
            }
        }

        RenderTexture.active = prevRT;
    }

    void DrawDebugRect(RectInt rect, Transform t, Vector3 terrainDatasize, Color color, float duration) {
        var x = rect.x * terrainDatasize.x / xRes + t.position.x;
        var y = rect.y * terrainDatasize.z / yRes + t.position.z;
        var width = rect.width * terrainDatasize.x / xRes;
        var height = rect.height * terrainDatasize.z / yRes;
        var pt1 = new Vector3(x, 0, y);
        var pt2 = new Vector3(x + width, 0, y);
        var pt3 = new Vector3(x + width, 0, y + height);
        var pt4 = new Vector3(x, 0, y + height);
        Debug.DrawLine(pt1, pt2, color, duration, false);
        Debug.DrawLine(pt2, pt3, color, duration, false);
        Debug.DrawLine(pt3, pt4, color, duration, false);
        Debug.DrawLine(pt4, pt1, color, duration, false);
    }

    // Part of this code is from:
    //  com.unity.terrain-tools/Editor/TerrainTools/Erosion/ThermalEroder.cs
    void FixedUpdate()
    {
        timeElapsed += Time.fixedDeltaTime;

        if (!enable)
            return;

        UpdateStickForce();

        RenderTexture prevRT = RenderTexture.active;

        var terrainData = gameObject.GetComponent<Terrain>().terrainData;

        cs.SetTexture(thermalKernelIdx, "TerrainHeightPrev", heightmapRT0);
        cs.SetTexture(thermalKernelIdx, "TerrainHeight", heightmapRT1);

        Vector2 jitteredTau = m_AngleOfRepose + new Vector2(0.9f * (float)m_ReposeJitter * (UnityEngine.Random.value - 0.5f), 0.9f * (float)m_ReposeJitter * (UnityEngine.Random.value - 0.5f));
        jitteredTau.x = Mathf.Clamp(jitteredTau.x, 0.0f, 89.9f);
        jitteredTau.y = Mathf.Clamp(jitteredTau.y, 0.0f, 89.9f);
        Vector2 m = new Vector2(Mathf.Tan(jitteredTau.x * Mathf.Deg2Rad), Mathf.Tan(jitteredTau.y * Mathf.Deg2Rad));
        cs.SetVector("angleOfRepose", new Vector4(m.x, m.y, 0.0f, 0.0f));

        cs.Dispatch(thermalKernelIdx, xRes / 32, yRes / 32, 1);

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

                    Vector3 relpos = base_link.transform.position - gameObject.transform.position;
                    var posXInTerrain = (int)(relpos.x / terrainData.size.x * xRes);
                    var posYInTerrain = (int)(relpos.z / terrainData.size.z * yRes);
                    RectInt rect = new RectInt(posXInTerrain - 30, posYInTerrain - 30, 60, 60);
                    if (!tiler) {
                        terrainData.CopyActiveRenderTextureToHeightmap(rect, rect.min, TerrainHeightmapSyncControl.None);
                    } else {
                        // in case if the terrain is tiled
                        RectInt tilesize = new RectInt(0, 0, xRes / tiler.divides, yRes / tiler.divides);
                        foreach (var t in tiler.terrains) {
                            var terrainData2 = t.GetComponent<Terrain>().terrainData;
                            Vector3 relpos2 = base_link.transform.position - t.transform.position;
                            var posXInTerrain2 = (int)(relpos2.x / terrainData.size.x * xRes);
                            var posYInTerrain2 = (int)(relpos2.z / terrainData.size.z * yRes);
                            RectInt rect2 = new RectInt(posXInTerrain2 - 30, posYInTerrain2 - 30, 60, 60);
                            if (rect2.Overlaps(tilesize)) {
                                //DrawDebugRect(rect2, t.transform, terrainData.size, UnityEngine.Color.green, syncPeriod);
                                try {
                                    var rect1 = new RectInt();
                                    rect1.x = rect.x;
                                    rect1.y = rect.y;
                                    rect1.width = rect.width;
                                    rect1.height = rect.height;
                                    if (rect2.x + rect2.width > terrainData2.heightmapResolution) {
                                        rect1.width = terrainData2.heightmapResolution - rect2.x;
                                    }
                                    if (rect2.y + rect2.height > terrainData2.heightmapResolution) {
                                        rect1.height = terrainData2.heightmapResolution - rect2.y;
                                    }
                                    if (rect2.x < 0) {
                                        rect1.x -= rect2.x;
                                        rect2.x = 0;
                                    }
                                    if (rect2.y < 0) {
                                        rect1.y -= rect2.y;
                                        rect2.y = 0;
                                    }
                                    terrainData2.CopyActiveRenderTextureToHeightmap(rect1, rect2.min, TerrainHeightmapSyncControl.None);
                                    //var rect3 = new RectInt();
                                    //rect3.x = rect2.x;
                                    //rect3.y = rect2.y;
                                    //rect3.width = rect1.width;
                                    //rect3.height = rect1.height;
                                    //DrawDebugRect(rect3, t.transform, terrainData.size, UnityEngine.Color.red, syncPeriod);
                                } catch (Exception e) {
                                    //Debug.LogException(e);
                                    //Debug.Log(rect2 + " " + rect + " " + tilesize);
                                }
                            }
                        }
                    }
                }
            }

            timeElapsed = 0.0f;
        }

        if (!tiler) {
            terrainData.SyncHeightmap();
        } else {
            foreach (var t in tiler.terrains) {
                t.GetComponent<Terrain>().terrainData.SyncHeightmap();
            }
        }

        // swap
        var temp = heightmapRT0;
        heightmapRT0 = heightmapRT1;
        heightmapRT1 = temp;

        RenderTexture.active = prevRT;
    }
}