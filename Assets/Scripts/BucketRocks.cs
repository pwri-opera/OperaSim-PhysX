using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GK;

public class RockObjectDetector : MonoBehaviour
{
	public GameObject manager;
	public GameObject terrain;

    private void OnCollisionEnter(Collision collision)
    {
		if (collision.gameObject == terrain)
        {
			manager.SendMessage("OnRockTerrainCollision", this.gameObject);
		}
	}
}

public class BucketRocks : MonoBehaviour
{
    public GameObject rockPrefab;
	public GameObject terrain;

	private List<GameObject> rocks;
	private ConvexHullCalculator calc;
	private Bounds bounds;
	private float[,] originalHeights;

	private void Start()
    {
		calc = new ConvexHullCalculator();
		rocks = new List<GameObject>();
		bounds = gameObject.GetComponent<MeshFilter>().mesh.bounds;
		var terrainData = terrain.GetComponent<Terrain>().terrainData;
		originalHeights = terrainData.GetHeights(0, 0,terrainData.heightmapResolution, terrainData.heightmapResolution);
	}

	private void OnApplicationQuit()
	{
		this.terrain.GetComponent<Terrain>().terrainData.SetHeights(0, 0, originalHeights);
	}

	private void CreateRock(Vector3 point)
    {
		var verts = new List<Vector3>();
		var tris = new List<int>();
		var normals = new List<Vector3>();
		var points = new List<Vector3>();

		points.Clear();

		for (int i = 0; i < 100; i++)
		{
			points.Add(Random.insideUnitSphere * 0.2f);
		}

		calc.GenerateHull(points, true, ref verts, ref tris, ref normals);

		var rock = Instantiate(rockPrefab);

		rock.AddComponent<RockObjectDetector>();
		rock.GetComponent<RockObjectDetector>().manager = gameObject;
		rock.GetComponent<RockObjectDetector>().terrain = terrain;

		rock.transform.SetParent(transform, false);
		rock.transform.localPosition = Vector3.zero;
		rock.transform.localRotation = Quaternion.identity;
		rock.transform.localScale = Vector3.one;

		var mesh = new Mesh();
		mesh.SetVertices(verts);
		mesh.SetTriangles(tris, 0);
		mesh.SetNormals(normals);

		rock.GetComponent<MeshFilter>().sharedMesh = mesh;

		rock.transform.position = point;

		rocks.Add(rock);
	}

	private void ModifyTerrain(Vector3 point, float diff)
    {
		var terrainData = terrain.GetComponent<Terrain>().terrainData;
		Vector3 relpos = (point - terrain.gameObject.transform.position);
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

	private void OnTriggerStay(Collider other)
    {
        if (other.gameObject == terrain)
        {
			var position = transform.TransformPoint(bounds.center);
			var point = other.gameObject.GetComponent<Collider>().ClosestPointOnBounds(position);
			ModifyTerrain(point, -0.0001f);
			CreateRock(point - (position - point) * 0.1f + Random.insideUnitSphere * 0.2f);
        }
    }

    public void OnRockTerrainCollision(GameObject rock)
    {
		ModifyTerrain(rock.transform.position, 0.0001f);
		Destroy(rock);
		rocks.Remove(rock);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        for (var i = 0; i < rocks.Count; i++)
        {
			var rock1 = rocks[i];
			var repulvector = new Vector3();
			for (var j = 0; j < rocks.Count; j++)
			{
				var rock2 = rocks[j];
				float dist = Vector3.Distance(rock1.transform.position, rock2.transform.position);
				if (dist < 0.25)
                {
					repulvector += rock1.transform.position - rock2.transform.position;
                }
			}
			repulvector.Normalize();
			rock1.GetComponent<Rigidbody>().AddForce(-repulvector * 30.0f);
        }
    }
}
