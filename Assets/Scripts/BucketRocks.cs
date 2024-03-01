using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GK;
using System;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using System.Runtime.InteropServices.WindowsRuntime;

public class RockObjectDetector : MonoBehaviour
{
    public BucketRocks manager;
    public GameObject terrain;
    private double timecreated = 0.0;
    private Vector3 pos_last_collision = Vector3.zero;

    private void Start()
    {
        timecreated = Time.timeAsDouble;
        pos_last_collision = transform.position;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject == terrain)
        {
            pos_last_collision = transform.position;
        }
    }
    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject == terrain)
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

public class BucketRocks : MonoBehaviour
{
    public GameObject rockPrefab;
    public GameObject terrain;

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

    private void Awake()
    {
        calc = new ConvexHullCalculator();
        rocks = new List<GameObject>();
        particle_volume = (float)(4.0 / 3.0 * Math.PI * Math.Pow(SoilParticleSettings.instance.particleVisualRadius, 3));

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
                points.Add(UnityEngine.Random.insideUnitSphere * SoilParticleSettings.instance.particleVisualRadius);
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

        rock.transform.SetParent(transform.root, false);
        rock.transform.position = point;
        rock.AddComponent<RockObjectDetector>();
        rock.GetComponent<RockObjectDetector>().manager = this;
        rock.GetComponent<RockObjectDetector>().terrain = terrain;
        rock.GetComponent<MeshFilter>().sharedMesh = mesh_patterns[UnityEngine.Random.Range(0, mesh_patterns.Count)];

        rocks.Add(rock);
    }

    private void OnCollisionStay(Collision other)
    {
        if (SoilParticleSettings.instance.enable == false) return;

        if (other.gameObject == terrain && Time.timeAsDouble - last_created_time > 0.01)
        {
            var point = other.GetContact(0).point;
            SoilParticleSettings.ModifyTerrain(point, -particle_volume);
            CreateRock(point);
            last_created_time = Time.timeAsDouble;
        }
    }

    public void OnRockTerrainCollision(GameObject rock)
    {
        if (Vector3.Distance(transform.position, rock.transform.position) > 2.0)
        {
            SoilParticleSettings.ModifyTerrain(rock.transform.position, particle_volume);
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
    void FixedUpdate()
    {
        if (SoilParticleSettings.instance.enable == false) return;

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
}
