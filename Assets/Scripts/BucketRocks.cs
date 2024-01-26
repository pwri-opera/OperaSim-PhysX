using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GK;
using System;

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
    private double last_created_time = 0.0;

    private void Start()
    {
		calc = new ConvexHullCalculator();
		rocks = new List<GameObject>();
        particle_volume = (float)(4.0 / 3.0 * Math.PI * Math.Pow(SoilParticleSettings.instance.particleVisualRadius, 3));
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

        var verts = new List<Vector3>();
        var tris = new List<int>();
        var normals = new List<Vector3>();
        var points = new List<Vector3>();

        points.Clear();

        for (int i = 0; i < 100; i++)
        {
            points.Add(UnityEngine.Random.insideUnitSphere * SoilParticleSettings.instance.particleVisualRadius);
        }

        calc.GenerateHull(points, true, ref verts, ref tris, ref normals);

		var mesh = new Mesh();
		mesh.SetVertices(verts);
		mesh.SetTriangles(tris, 0);
		mesh.SetNormals(normals);
		rock.GetComponent<MeshFilter>().sharedMesh = mesh;

		rocks.Add(rock);
	}

	private void OnCollisionStay(Collision other)
    {
        if (SoilParticleSettings.instance.enable == false) return;

		if (other.gameObject == terrain && Time.timeAsDouble - last_created_time > 0.05)
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

    // Update is called once per frame
    void FixedUpdate()
    {
		if (SoilParticleSettings.instance.enable == false) return;

		for (var i = 0; i < rocks.Count; i++)
        {
			var rock1 = rocks[i];
			var repulvector = new Vector3();
			for (var j = 0; j < rocks.Count; j++)
			{
				var rock2 = rocks[j];
				float dist = Vector3.Distance(rock1.transform.position, rock2.transform.position);
				if (dist < SoilParticleSettings.instance.partileStickDistance)
                {
					repulvector += rock1.transform.position - rock2.transform.position;
                }
			}
			repulvector.Normalize();
			rock1.GetComponent<Rigidbody>().AddForce(-repulvector * SoilParticleSettings.instance.stickForce);
        }
    }
}
