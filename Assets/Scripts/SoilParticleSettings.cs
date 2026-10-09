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
    [Tooltip("Terrainとして扱うGameObject名")]
    public string terrainObjectName = "Terrain";

    public double timecreated { get; private set; } = 0.0;
    public Vector3 pos_last_terrain_collision { get; private set; } = Vector3.zero;  // 最後にTerrainと衝突した位置
    public double last_terrain_collision_time { get; private set; } = double.NegativeInfinity;  // 最後にTerrainと衝突した時刻


    private void Start()
    {
        timecreated = Time.timeAsDouble;

        pos_last_terrain_collision = transform.position;
        last_terrain_collision_time = timecreated;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name == terrainObjectName)
        {
            pos_last_terrain_collision = transform.position;
            last_terrain_collision_time = Time.timeAsDouble;
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.name == terrainObjectName)
        {
            pos_last_terrain_collision = transform.position;
            last_terrain_collision_time = Time.timeAsDouble;
        }
    }

    private void FixedUpdate()
    {
        var now = Time.timeAsDouble;
        if (now - timecreated > 1.5)
        {
            var rigidbody = GetComponent<Rigidbody>();

            var velocity = rigidbody.velocity.sqrMagnitude;
            if (velocity < 0.2 && Vector3.Distance(transform.position, pos_last_terrain_collision) < 0.1)
            {
                manager.OnRockTerrainCollision(this.gameObject);
            }
        }
        if (this.transform.position.y - pos_last_terrain_collision.y < -1.0)
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

    [Tooltip("Terrainとして扱うGameObject名")]
    public string terrainObjectName = "Terrain";

    [Tooltip("バケットとして扱うGameObject名")]
    public string bucketObjectName = "Bucket";

    [Tooltip("掘削時の地形変形を有効にする")]
    public bool enable = true;

    [Tooltip("粒子の見た目の大きさ(m)")]
    public float particleVisualRadius = 0.2f;

    [Tooltip("粒子間力を有効化する粒子間距離(m)")]
    public double partileStickDistance = 0.25;

    [Tooltip("粒子反発力を有効化する粒子間距離(m)")]
    public double partileRepulseDistance = 0.05;

    [Tooltip("粒子間力の強さ")]
    public float stickForce = 30.0f;

    [Tooltip("粒子反発力の強さ")]
    public float repulseForce = 0.0f;

    [Tooltip("バケット内側に粒子を出現させる際の接触点からのオフセット(m)")]
    public float bucketSpawnInset = 0.2f;

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
    private List<GameObject> buckets;
    private float particle_volume;
    private List<Mesh> mesh_patterns;
    private double last_created_time = 0.0;

    private ParticleForceJob job;
    private JobHandle job_handle;
    private bool job_started = false;
    private NativeArray<float3> job_positions;
    private NativeArray<float3> job_stickForces;
    private NativeArray<float3> job_repulseForces;

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
            throw new MissingReferenceException("Could not find compute shader for digging");
        }
        digKernelIdx = cs2.FindKernel("Dig");

        // pregenerate particle meshes
        calc = new ConvexHullCalculator();
        rocks = new List<GameObject>();
        buckets = new List<GameObject>();
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

    public void RegisterBucket(GameObject bucket)
    {
        buckets.Add(bucket);
    }

    public void UnregisterBucket(GameObject bucket)
    {
        buckets.Remove(bucket);
    }

    private void CreateRock(Vector3 point)
    {
        var rock = Instantiate(rockPrefab);

        rock.transform.parent = null;
        rock.transform.position = point;
        rock.AddComponent<RockObjectDetector>();
        var detector = rock.GetComponent<RockObjectDetector>();
        detector.manager = this;
        detector.terrainObjectName = terrainObjectName;
        rock.GetComponent<MeshFilter>().sharedMesh = mesh_patterns[UnityEngine.Random.Range(0, mesh_patterns.Count)];

        rocks.Add(rock);
    }

    public void OnBucketCollision(Collision other, Vector3 bucketPosition)
    {
        if (!enable)
            return;

        if (other.gameObject.name != terrainObjectName)
            return;

        var now = Time.timeAsDouble;
        if (now - last_created_time > 0.01)
        {
            if (other.contactCount == 0)
                return;
            var contact = other.GetContact(0);
            var terrainPoint = contact.point;
            var rockSpawnPoint = GetBucketInteriorSpawnPoint(terrainPoint, bucketPosition, contact.normal);
            ModifyTerrain(terrainPoint, -particle_volume);
            CreateRock(rockSpawnPoint);
            last_created_time = now;
        }
    }

    private Vector3 GetBucketInteriorSpawnPoint(Vector3 contactPoint, Vector3 bucketPosition, Vector3 contactNormal)
    {
        var direction = bucketPosition - contactPoint;
        if (direction.sqrMagnitude < 1e-5f)
        {
            direction = -contactNormal;
        }
        direction.Normalize();
        var inset = Mathf.Max(bucketSpawnInset, 0.0f);
        return contactPoint + direction * inset;
    }

    public void OnBucketTrigger(Collider other, Vector3 bucketPosition)
    {
        if (!enable)
            return;

        if (other.gameObject.name != terrainObjectName)
            return;

        var now = Time.timeAsDouble;
        if (now - last_created_time > 0.01)
        {
            var point = other.ClosestPointOnBounds(bucketPosition);
            ModifyTerrain(point, -particle_volume);
            CreateRock(point);
            last_created_time = now;
        }
    }

    public void OnRockTerrainCollision(GameObject rock)
    {
        // bucketとの距離が2m以上離れている場合のみ地形に粒子を回収する
        foreach(var bucket in buckets)
        {
            var dist = Vector3.Distance(bucket.transform.position, rock.transform.position);
            if (dist < 2.0) {
                return;
            }
        }
        ModifyTerrain(rock.transform.position, particle_volume);
        Destroy(rock);
        rocks.Remove(rock);
    }

    [BurstCompile]
    private struct ParticleForceJob : IJobParallelFor
    {
        [ReadOnly]
        public float particleStickDistance;
        [ReadOnly]
        public float particleRepulseDistance;
        [ReadOnly]
        public NativeArray<float3> positions;
        [WriteOnly]
        public NativeArray<float3> stickForces;
        [WriteOnly]
        public NativeArray<float3> repulseForces;

        public void Execute(int index)
        {
            var rock1 = positions[index];
            var stickvector = new float3(0, 0, 0);
            var repulvector = new float3(0, 0, 0);
            for (var j = 0; j < positions.Length; j++)
            {
                var rock2 = positions[j];
                float dist = math.distance(rock1, rock2);
                if (dist < particleRepulseDistance)
                {
                    repulvector += rock1 - rock2;
                }
                else if (dist < particleStickDistance)
                {
                    stickvector += rock1 - rock2;
                }
            }
            stickForces[index] = math.normalize(stickvector);
            repulseForces[index] = math.normalize(repulvector);
        }
    }

    private void Destroy()
    {
        if (job_started)
        {
            job_handle.Complete();
            job_positions.Dispose();
            job_stickForces.Dispose();
            job_repulseForces.Dispose();
        }
    }

    // Update is called once per frame
    void UpdateStickForce()
    {
        if (job_started)
        {
            job_handle.Complete();
            if (rocks.Count == job.stickForces.Length && rocks.Count == job.repulseForces.Length)
            {
                for (var i = 0; i < rocks.Count; i++)
                {
                    var rock1 = rocks[i];
                    var stick = job.stickForces[i];
                    var repulse = job.repulseForces[i];
                    if (!float.IsNaN(repulse.x))
                    {
                        rock1.GetComponent<Rigidbody>().AddForce(repulse * SoilParticleSettings.instance.repulseForce);
                    }
                    else if (!float.IsNaN(stick.x))
                    {
                        rock1.GetComponent<Rigidbody>().AddForce(-stick * SoilParticleSettings.instance.stickForce);
                    }
                }
            }
            job_positions.Dispose();
            job_stickForces.Dispose();
            job_repulseForces.Dispose();
        }

        job_positions = new NativeArray<float3>(rocks.Count, Allocator.Persistent);
        job_stickForces = new NativeArray<float3>(rocks.Count, Allocator.Persistent);
        job_repulseForces = new NativeArray<float3>(rocks.Count, Allocator.Persistent);

        for (var i = 0; i < rocks.Count; i++)
        {
            var rock1 = rocks[i];
            job_positions[i] = rock1.transform.position;
        }

        job = new ParticleForceJob();
        job.particleStickDistance = (float)SoilParticleSettings.instance.partileStickDistance;
        job.particleRepulseDistance = (float)SoilParticleSettings.instance.partileRepulseDistance;
        job.positions = job_positions;
        job.stickForces = job_stickForces;
        job.repulseForces = job_repulseForces;

        job_handle = job.Schedule(job_positions.Length, 1);
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
        var posXInTerrain = Mathf.Clamp(Mathf.RoundToInt(relpos.x / terrainData.size.x * (xRes - 1)), 0, xRes - 1);
        var posYInTerrain = Mathf.Clamp(Mathf.RoundToInt(relpos.z / terrainData.size.z * (yRes - 1)), 0, yRes - 1);

        cs2.SetTexture(digKernelIdx, "heightmap", heightmapRT0);
        cs2.SetFloat("diff", diff);
        cs2.SetVector("pos", new Vector2(posXInTerrain, posYInTerrain));

        cs2.Dispatch(digKernelIdx, 1, 1, 1);

        RenderTexture.active = heightmapRT0;

        RectInt rect = new RectInt(posXInTerrain - 10, posYInTerrain - 10, 20, 20);
        rect = ClampRectToBounds(rect, xRes, yRes);
        if (rect.width <= 0 || rect.height <= 0)
        {
            RenderTexture.active = prevRT;
            return;
        }
        if (!tiler) {
            terrainData.CopyActiveRenderTextureToHeightmap(rect, rect.min, TerrainHeightmapSyncControl.None);
        } else {
            CopyActiveRenderTextureToHeightmapTiled(rect, point);
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

    void CopyActiveRenderTextureToHeightmapTiled(RectInt rect, Vector3 point) {
        var terrainData = gameObject.GetComponent<Terrain>().terrainData;
        foreach (var t in tiler.terrains) {
            var terrainData2 = t.GetComponent<Terrain>().terrainData;
            Vector3 tileRel = t.transform.position - transform.position;
            var tileStartX = Mathf.RoundToInt(tileRel.x / terrainData.size.x * (xRes - 1));
            var tileStartY = Mathf.RoundToInt(tileRel.z / terrainData.size.z * (yRes - 1));
            RectInt rect2 = new RectInt(rect.x - tileStartX, rect.y - tileStartY, rect.width, rect.height);
            //DrawDebugRect(rect2, t.transform, terrainData.size, UnityEngine.Color.green, syncPeriod);
            try {
                var tileBounds = new RectInt(0, 0, terrainData2.heightmapResolution, terrainData2.heightmapResolution);
                var rect2Clamped = IntersectRect(rect2, tileBounds);
                if (rect2Clamped.width <= 0 || rect2Clamped.height <= 0)
                    continue;
                int dx = rect2Clamped.x - rect2.x;
                int dy = rect2Clamped.y - rect2.y;
                var rect1 = new RectInt(rect.x + dx, rect.y + dy, rect2Clamped.width, rect2Clamped.height);
                terrainData2.CopyActiveRenderTextureToHeightmap(rect1, rect2Clamped.min, TerrainHeightmapSyncControl.None);
                //var rect3 = new RectInt();
                //rect3.x = rect2.x;
                //rect3.y = rect2.y;
                //rect3.width = rect1.width;
                //rect3.height = rect1.height;
                //DrawDebugRect(rect3, t.transform, terrainData.size, UnityEngine.Color.red, syncPeriod);
            } catch (Exception e) {
                //Debug.LogException(e);
                //Debug.Log(rect2 + " " + rect);
            }
        }
    }

    private static RectInt ClampRectToBounds(RectInt rect, int width, int height)
    {
        int xMin = Mathf.Max(rect.x, 0);
        int yMin = Mathf.Max(rect.y, 0);
        int xMax = Mathf.Min(rect.x + rect.width, width);
        int yMax = Mathf.Min(rect.y + rect.height, height);
        int w = xMax - xMin;
        int h = yMax - yMin;
        if (w <= 0 || h <= 0)
            return new RectInt(0, 0, 0, 0);
        return new RectInt(xMin, yMin, w, h);
    }

    private static RectInt IntersectRect(RectInt a, RectInt b)
    {
        int xMin = Mathf.Max(a.x, b.x);
        int yMin = Mathf.Max(a.y, b.y);
        int xMax = Mathf.Min(a.x + a.width, b.x + b.width);
        int yMax = Mathf.Min(a.y + a.height, b.y + b.height);
        int w = xMax - xMin;
        int h = yMax - yMin;
        if (w <= 0 || h <= 0)
            return new RectInt(0, 0, 0, 0);
        return new RectInt(xMin, yMin, w, h);
    }

    // Part of this code is from:
    //  com.unity.terrain-tools/Editor/TerrainTools/Erosion/ThermalEroder.cs
    void FixedUpdate()
    {
        timeElapsed += Time.fixedDeltaTime;

        if (!enable)
            return;

        UpdateStickForce();

        var terrainData = gameObject.GetComponent<Terrain>().terrainData;

        RenderTexture prevRT = RenderTexture.active;

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
                        CopyActiveRenderTextureToHeightmapTiled(rect, base_link.transform.position);
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
