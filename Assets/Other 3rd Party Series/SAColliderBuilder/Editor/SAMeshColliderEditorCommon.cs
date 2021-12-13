//----------------------------------------------
// SABoneCollider
// Copyright (c) 2014 Stereoarts Nora
//----------------------------------------------
//#define _DEBUG_SINGLETHREAD

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using ShapeType						= SAColliderBuilderCommon.ShapeType;
using FitType						= SAColliderBuilderCommon.FitType;
using MeshType						= SAColliderBuilderCommon.MeshType;
using SliceMode						= SAColliderBuilderCommon.SliceMode;
using ElementType					= SAColliderBuilderCommon.ElementType;
using Bool3							= SAColliderBuilderCommon.Bool3;
using SplitMesh						= SAMeshColliderCommon.SplitMesh;
using SplitMode						= SAMeshColliderCommon.SplitMode;
using MeshCache						= SAMeshColliderEditorCommon.MeshCache;
using ReducerTask					= SAMeshColliderEditorCommon.ReducerTask;

using SimpleCountDownLatch			= SAColliderBuilderEditorCommon.SimpleCountDownLatch;

using ReducerProperty				= SAColliderBuilderCommon.ReducerProperty;
using ColliderProperty				= SAColliderBuilderCommon.ColliderProperty;
using RigidbodyProperty				= SAColliderBuilderCommon.RigidbodyProperty;

using SplitProperty					= SAMeshColliderCommon.SplitProperty;
using SAMeshColliderProperty		= SAMeshColliderCommon.SAMeshColliderProperty;
using SAMeshColliderBuilderProperty	= SAMeshColliderCommon.SAMeshColliderBuilderProperty;

public class SAMeshColliderEditorCommon
{
	public static MeshCache GetParentMeshCache( SAMeshCollider meshCollider )
	{
		if( meshCollider != null ) {
			Transform transform = meshCollider.gameObject.transform;
			for( ; transform != null; transform = transform.parent ) {
				MeshFilter meshFilter = transform.gameObject.GetComponent<MeshFilter>();
				if( meshFilter != null && meshFilter.sharedMesh != null ) {
					return new MeshCache(
						SAColliderBuilderEditorCommon.GetMesh( meshFilter ),
						SAColliderBuilderEditorCommon.GetMaterials( meshFilter ) );
				}
				
				SkinnedMeshRenderer skinnedMeshRnederer = transform.gameObject.GetComponent<SkinnedMeshRenderer>();
				if( skinnedMeshRnederer != null && skinnedMeshRnederer.sharedMesh != null ) {
					return new MeshCache(
						SAColliderBuilderEditorCommon.GetMesh( skinnedMeshRnederer ),
						SAColliderBuilderEditorCommon.GetMaterials( skinnedMeshRnederer ) );
				}

				if( SAColliderBuilderEditorCommon.IsRootTransform( transform ) ) {
					return null;
				}
			}
		}

		return null;
	}

	public static SAMeshCollider[] GetChildSAMeshColliders( GameObject parentGameObject )
	{
		if( parentGameObject == null ) {
			parentGameObject = null;
		}
		
		List<SAMeshCollider> meshColliders = new List<SAMeshCollider>();
		foreach( Transform transform in parentGameObject.transform ) {
			SAMeshCollider meshCollider = transform.gameObject.GetComponent<SAMeshCollider>();
			if( meshCollider != null ) {
				meshColliders.Add( meshCollider );
			}
		}
		
		return meshColliders.ToArray();
	}

	//----------------------------------------------------------------------------------------------------------------

	public static bool IsEqualSplitMesh( SplitMesh lhs, SplitMesh rhs )
	{
		if( lhs != null && rhs != null ) {
			if( lhs.subMeshCount == rhs.subMeshCount &&
			    lhs.subMesh == rhs.subMesh ) {
				// Check primitive
				if( lhs.triangle >= 0 && rhs.triangle >= 0 ) {
					if( lhs.triangleVertex == rhs.triangleVertex ) {
						// Nothing.
					} else {
						return false; // Not equal.
					}
				} else if( lhs.triangle < 0 && lhs.triangle < 0 ) {
					// Nothing.
				} else {
					return false; // Not equal.
				}
				
				int lhsVertexCount = (lhs.polygonVertices != null) ? lhs.polygonVertices.Length : 0;
				int rhsVertexCount = (rhs.polygonVertices != null) ? rhs.polygonVertices.Length : 0;
				if( lhsVertexCount != rhsVertexCount ) {
					return false;
				}
				
				// Compare vertices completely.
				for( int i = 0; i < lhsVertexCount; ++i ) {
					Vector3 vertex = lhs.polygonVertices[i];
					int j = 0;
					for( ;j < rhsVertexCount; ++j ) {
						if( vertex == rhs.polygonVertices[j] ) {
							break;
						}
					}
					if( j == rhsVertexCount ) {
						return false;
					}
				}
				
				return true;
			}
		}
		
		return false;
	}
	
	public static SAMeshCollider FindSAMeshCollider( SAMeshCollider[] meshColliders, SplitMesh splitMesh )
	{
		if( meshColliders != null && splitMesh != null ) {
			foreach( SAMeshCollider meshCollider in meshColliders ) {
				if( IsEqualSplitMesh( meshCollider.splitMesh, splitMesh ) ) {
					return meshCollider;
				}
			}
		}
		
		return null;
	}

	public static bool IsModifiedSAMeshColliderRecursively( SAMeshCollider meshCollider )
	{
		if( meshCollider != null ) {
			if( meshCollider.modified ) {
				return true;
			}

			foreach( Transform childTransform in meshCollider.gameObject.transform ) {
				if( IsModifiedSAMeshColliderRecursively( childTransform.gameObject.GetComponent< SAMeshCollider >() ) ) {
					return true;
				}
			}
		}

		return false;
	}
	
	//--------------------------------------------------------------------------------------------------------
	
	public class MeshCache
	{
		public class SubMeshCache
		{
			public int[] triangles;
			public Vector3[] triangleNormals;
		}
		
		SubMeshCache[] _subMeshes;
		Vector3[] _vertices;
		int[] _triangles;
		Vector3[] _triangleNormals;
		Material[] _materials;
		
		public Vector3[] vertices { get { return _vertices; } }
		public Material[] materials { get { return _materials; } }

		public int subMeshCount {
			get {
				return (_subMeshes != null) ? _subMeshes.Length : 0;
			}
		}

		public int[] triangles {
			get {
				return _triangles;
			}
		}

		public Vector3[] triangleNormals {
			get {
				return _triangleNormals;
			}
		}

		public int[] GetTriangles( int subMesh )
		{
			if( _subMeshes != null && subMesh < _subMeshes.Length ) {
				return _subMeshes[subMesh].triangles;
			}
			
			return null;
		}
		
		public Vector3[] GetTriangleNormals( int subMesh )
		{
			if( _subMeshes != null && subMesh < _subMeshes.Length ) {
				return _subMeshes[subMesh].triangleNormals;
			}
			
			return null;
		}
		
		public MeshCache( Mesh mesh, Material[] materials )
		{
			if( mesh == null ) {
				return;
			}

			_materials = materials;
			_vertices = mesh.vertices;
			int subMeshCount = mesh.subMeshCount;
			_subMeshes = new SubMeshCache[subMeshCount];
			int subMeshTriangleCount = 0;
			for( int subMesh = 0; subMesh < subMeshCount; ++subMesh ) {
				_subMeshes[subMesh] = new SubMeshCache();
				_subMeshes[subMesh].triangles = mesh.GetTriangles( subMesh );
				if( _subMeshes[subMesh].triangles != null ) {
					_subMeshes[subMesh].triangleNormals = new Vector3[_subMeshes[subMesh].triangles.Length / 3];
					subMeshTriangleCount += _subMeshes[subMesh].triangles.Length;
				}
			}
			
			ComputeNormals();
			//ComputeNormalsLegacy();

			// Concat all triangles.
			_triangles = new int[subMeshTriangleCount];
			_triangleNormals = new Vector3[subMeshTriangleCount / 3];
			int subMeshTriangleIndex = 0;
			for( int subMesh = 0; subMesh < subMeshCount; ++subMesh ) {
				if( _subMeshes[subMesh].triangles != null ) {
					System.Array.Copy( _subMeshes[subMesh].triangles, 0, _triangles, subMeshTriangleIndex, _subMeshes[subMesh].triangles.Length );
					System.Array.Copy( _subMeshes[subMesh].triangleNormals, 0, _triangleNormals, subMeshTriangleIndex / 3, _subMeshes[subMesh].triangleNormals.Length );
					subMeshTriangleIndex += _subMeshes[subMesh].triangles.Length;
				}
			}
		}
		
		public class ComputeNormalsTask
		{
			public MeshCache meshCache;
			public int subMesh;
			public int triangleBegin;
			public int triangleEnd;
			public SimpleCountDownLatch simpleCountDownLatch;
		}
		
		public void ComputeNormals()
		{
			if( _subMeshes != null ) {
				int prosessorCount = Mathf.Max(System.Environment.ProcessorCount, 1);
				SimpleCountDownLatch simpleCountDownLatch = new SimpleCountDownLatch( 0 );
				List<ComputeNormalsTask> tasks = new List<ComputeNormalsTask>();
				for( int subMesh = 0; subMesh < _subMeshes.Length; ++subMesh ) {
					int[] triangles = _subMeshes[subMesh].triangles;
					if( triangles != null ) {
						int triangleCount = triangles.Length;
						int processCount = Mathf.Min(prosessorCount, triangleCount / 3);
						if( processCount > 0 ) {
							int triangleBegin = 0;
							int processInTriangles = (((triangleCount / 3) + processCount - 1) / processCount) * 3;
							for( int i = 0; i < processCount; ++i ) {
								ComputeNormalsTask task = new ComputeNormalsTask();
								task.meshCache = this;
								task.simpleCountDownLatch = simpleCountDownLatch;
								task.subMesh = subMesh;
								task.triangleBegin = triangleBegin;
								task.triangleEnd = Mathf.Min(triangleBegin + processInTriangles, triangles.Length);
								tasks.Add(task);
								triangleBegin += processInTriangles;
							}
						}
					}
				}
				
				simpleCountDownLatch.Reset( tasks.Count );
				for( int task = 0; task < tasks.Count; ++task ) {
					System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(_ComputeNormals), tasks[task]);
				}
				simpleCountDownLatch.Wait();
			}
		}
		
		public static void _ComputeNormals( object obj )
		{
			ComputeNormalsTask task = (ComputeNormalsTask)obj;
			MeshCache meshCache = task.meshCache;
			Vector3[] vertices = meshCache._vertices;
			SubMeshCache[] subMeshes = meshCache._subMeshes;
			if( vertices != null && subMeshes != null ) {
				int[] triangles = subMeshes[task.subMesh].triangles;
				Vector3[] triangleNormals = subMeshes[task.subMesh].triangleNormals;
				if( triangles != null && triangleNormals != null ) {
					for( int triangle = task.triangleBegin; triangle < task.triangleEnd; triangle += 3 ) {
						Vector3 v0 = vertices[triangles[triangle + 0]];
						Vector3 v1 = vertices[triangles[triangle + 1]];
						Vector3 v2 = vertices[triangles[triangle + 2]];
						Vector3 n = Vector3.Cross( v1 - v0, v2 - v0 );
						float l = Mathf.Sqrt(n.sqrMagnitude);
						triangleNormals[triangle / 3] = (l > Mathf.Epsilon) ? (n / l) : (new Vector3(0,0,1));
					}
				}
			}
			if( task.simpleCountDownLatch != null ) {
				task.simpleCountDownLatch.CountDown();
			}
		}
		
		public void ComputeNormalsLegacy()
		{
			if( _subMeshes != null ) {
				for( int subMesh = 0; subMesh < _subMeshes.Length; ++subMesh ) {
					int[] triangles = _subMeshes[subMesh].triangles;
					Vector3[] triangleNormals = _subMeshes[subMesh].triangleNormals;
					if( triangles != null && triangleNormals != null ) {
						for( int triangle = 0; triangle < triangles.Length; triangle += 3 ) {
							Vector3 v0 = vertices[triangles[triangle + 0]];
							Vector3 v1 = vertices[triangles[triangle + 1]];
							Vector3 v2 = vertices[triangles[triangle + 2]];
							Vector3 n = Vector3.Cross( v1 - v0, v2 - v0 );
							float l = Mathf.Sqrt(n.sqrMagnitude);
							triangleNormals[triangle / 3] = (l > Mathf.Epsilon) ? (n / l) : (new Vector3(0,0,1));
						}
					}
				}
			}
		}
	}
	
	//--------------------------------------------------------------------------------------------------------

	public static SplitMesh MakeRootSplitMesh( MeshCache meshCache )
	{
		if( meshCache == null ) {
			Debug.LogError("");
			return null;
		}

		return new SplitMesh();
	}

	public static SplitMesh[] MakeSplitMeshesByMaterial( MeshCache meshCache )
	{
		if( meshCache == null ) {
			Debug.LogError("");
			return null;
		}
		
		Vector3[] vertices = meshCache.vertices;
		if( vertices == null ) {
			Debug.LogError("");
			return null;
		}
		
		int subMeshCount = meshCache.subMeshCount;
		List<SplitMesh> splitMeshes = new List<SplitMesh>();
		
		for( int subMesh = 0; subMesh < subMeshCount; ++subMesh ) {
			int[] triangles = meshCache.GetTriangles( subMesh );
			if( triangles != null ) {
				SplitMesh splitMesh = new SplitMesh();
				splitMesh.subMeshCount = meshCache.subMeshCount;
				splitMesh.subMesh = subMesh;
				splitMeshes.Add( splitMesh );
			}
		}
		
		return splitMeshes.ToArray();
	}
	
	public static SplitMesh[] MakeSplitMeshesByPrimitive( MeshCache meshCache, SplitMesh splitMesh )
	{
		if( meshCache == null || splitMesh == null ) {
			Debug.LogError("");
			return null;
		}
		
		if( !ValidateSplitMesh( meshCache, splitMesh ) ) {
			Debug.LogError("");
			return null;
		}
		
		if( splitMesh.subMesh >= meshCache.subMeshCount ) {
			Debug.LogError("");
			return null;
		}
		
		Vector3[] vertices = meshCache.vertices;
		if( vertices == null ) {
			Debug.LogError("");
			return null;
		}
		
		List<SplitMesh> resplitMeshes = new List<SplitMesh>();
		HashSet<Vector3> usedVertices = new HashSet<Vector3>();

		int[] triangles = null;
		if( splitMesh.subMesh < 0 ) {
			triangles = meshCache.triangles;
		} else {
			triangles = meshCache.GetTriangles( splitMesh.subMesh );
		}
		if( triangles != null ) {
			bool[] usedTriangles = new bool[triangles.Length / 3];
			for( int triangle = 0; triangle < triangles.Length; triangle += 3 ) {
				if( !usedTriangles[triangle / 3] ) {
					usedTriangles[triangle / 3] = true;
					usedVertices.Clear();
					usedVertices.Add( vertices[triangles[triangle + 0]] );
					usedVertices.Add( vertices[triangles[triangle + 1]] );
					usedVertices.Add( vertices[triangles[triangle + 2]] );
					
					SplitMesh resplitMesh = new SplitMesh();
					resplitMesh.subMeshCount = meshCache.subMeshCount;
					resplitMesh.subMesh = splitMesh.subMesh;
					resplitMesh.triangle = triangle;
					resplitMesh.triangleVertex = vertices[triangles[triangle + 0]];
					resplitMeshes.Add( resplitMesh );
					
					for(;;) {
						bool findAnything = false;
						for( int searchTriangle = triangle + 3; searchTriangle < triangles.Length; searchTriangle += 3 ) {
							if( !usedTriangles[searchTriangle / 3] ) {
								int vertexIndex0 = triangles[searchTriangle + 0];
								int vertexIndex1 = triangles[searchTriangle + 1];
								int vertexIndex2 = triangles[searchTriangle + 2];
								if( usedVertices.Contains( vertices[vertexIndex0] ) ||
								    usedVertices.Contains( vertices[vertexIndex1] ) ||
								    usedVertices.Contains( vertices[vertexIndex2] ) ) {
									usedTriangles[searchTriangle / 3] = true;
									usedVertices.Add( vertices[vertexIndex0] );
									usedVertices.Add( vertices[vertexIndex1] );
									usedVertices.Add( vertices[vertexIndex2] );
									findAnything = true;
								}
							}
						}
						if( !findAnything ) {
							break;
						}
					}
				}
			}
		}

		return resplitMeshes.ToArray();
	}
	
	//--------------------------------------------------------------------------------------------------------
	
	public static bool ValidateSplitMesh( MeshCache meshCache, SplitMesh splitMesh )
	{
		if( meshCache == null || splitMesh == null ) {
			Debug.LogError("");
			return false;
		}
		if( splitMesh.subMesh >= splitMesh.subMeshCount ) {
			Debug.LogError("");
			return false;
		}

		if( splitMesh.subMesh >= 0 ) {
			if( meshCache.subMeshCount != splitMesh.subMeshCount ) {
				return false;
			}
		}
		
		if( splitMesh.triangle >= 0 ) {
			Vector3[] vertices = meshCache.vertices;
			int[] triangles = null;

			if( splitMesh.subMesh < 0 ) {
				triangles = meshCache.triangles;
			} else {
				triangles = meshCache.GetTriangles( splitMesh.subMesh );
			}

			if( vertices == null || triangles == null ) {
				return false;
			}
			
			if( splitMesh.triangle < triangles.Length &&
			    vertices[triangles[splitMesh.triangle]] == splitMesh.triangleVertex ) {
				return true;
			}
			
			// Salvage triangle.
			for( int i = 0; i < triangles.Length; ++i ) {
				if( vertices[triangles[i]] == splitMesh.triangleVertex ) {
					splitMesh.triangle = i;
					return true;
				}
			}
			
			return false;
		} else {
			return true;
		}
	}
	
	public static bool MakeSplitMeshTriangles( MeshCache meshCache, SplitMesh splitMesh )
	{
		if( splitMesh == null ) {
			Debug.LogError("");
			return false;
		}
		
		splitMesh.triangles = null;
		splitMesh.vertices = null;
		splitMesh.triangleNormals = null;
		
		if( meshCache == null ) {
			Debug.LogError("");
			return false;
		}
		if( !ValidateSplitMesh( meshCache, splitMesh ) ) {
			Debug.LogError("");
			return false;
		}
		if( splitMesh.subMesh >= meshCache.subMeshCount ) {
			Debug.LogError("");
			return false;
		}
		
		Vector3[] vertices = meshCache.vertices;
		int[] triangles = null;
		Vector3[] triangleNormals = null;
		if( splitMesh.subMesh < 0 ) {
			triangles = meshCache.triangles;
			triangleNormals = meshCache.triangleNormals;
		} else {
			triangles = meshCache.GetTriangles( splitMesh.subMesh );
			triangleNormals = meshCache.GetTriangleNormals( splitMesh.subMesh );
		}
		if( vertices == null || triangles == null || triangleNormals == null ) {
			Debug.LogError("");
			return false;
		}
		
		if( splitMesh.triangle >= 0 ) {
			List<int> splitTriangles = new List<int>( triangles.Length );
			List<Vector3> splitTriangleNormals = new List<Vector3>( triangles.Length / 3 );
			
			HashSet<Vector3> usedVertices = new HashSet<Vector3>();
			bool[] usedTriangles = new bool[triangles.Length / 3];
			usedTriangles[splitMesh.triangle / 3] = true;
			usedVertices.Add( vertices[triangles[splitMesh.triangle + 0]] );
			usedVertices.Add( vertices[triangles[splitMesh.triangle + 1]] );
			usedVertices.Add( vertices[triangles[splitMesh.triangle + 2]] );
			
			for(;;) {
				bool findAnything = false;
				for( int searchTriangle = splitMesh.triangle + 3; searchTriangle < triangles.Length; searchTriangle += 3 ) {
					if( !usedTriangles[searchTriangle / 3] ) {
						int vertexIndex0 = triangles[searchTriangle + 0];
						int vertexIndex1 = triangles[searchTriangle + 1];
						int vertexIndex2 = triangles[searchTriangle + 2];
						if( usedVertices.Contains( vertices[vertexIndex0] ) ||
						   usedVertices.Contains( vertices[vertexIndex1] ) ||
						   usedVertices.Contains( vertices[vertexIndex2] ) ) {
							usedTriangles[searchTriangle / 3] = true;
							usedVertices.Add( vertices[vertexIndex0] );
							usedVertices.Add( vertices[vertexIndex1] );
							usedVertices.Add( vertices[vertexIndex2] );
							findAnything = true;
						}
					}
				}
				if( !findAnything ) {
					break;
				}
			}
			for( int triangle = 0; triangle < triangles.Length; triangle += 3 ) {
				if( usedTriangles[triangle / 3] ) {
					splitTriangles.Add( triangles[triangle + 0] );
					splitTriangles.Add( triangles[triangle + 1] );
					splitTriangles.Add( triangles[triangle + 2] );
					splitTriangleNormals.Add( triangleNormals[triangle / 3] );
				}
			}
			
			splitMesh.vertices = vertices;
			splitMesh.triangles = splitTriangles.ToArray();
			splitMesh.triangleNormals = splitTriangleNormals.ToArray();
		} else {
			splitMesh.vertices = vertices;
			splitMesh.triangles = triangles;
			splitMesh.triangleNormals = triangleNormals;
		}
		
		return true;
	}
	
	// for splitPolygonNormalEnabled
	public static void SalvageMeshByPolygon( SplitMesh splitMesh )
	{
		if( splitMesh == null || splitMesh.polygonVertices == null || splitMesh.polygonTriangles == null ) {
			Debug.LogError("");
			return;
		}
		
		splitMesh.vertices = splitMesh.polygonVertices;
		splitMesh.triangles = splitMesh.polygonTriangles;
	}
	
	// for splitPolygonNormalEnabled
	public static SplitMesh[] MakeSplitMeshesByPolygon( MeshCache meshCache, SplitMesh splitMesh, float splitPolygonNormalAngle )
	{
		if( meshCache == null || splitMesh == null ) {
			return null;
		}
		if( !ValidateSplitMesh( meshCache, splitMesh ) ) {
			return null;
		}
		
		float dotTolerance = Mathf.Cos( splitPolygonNormalAngle * Mathf.Deg2Rad );
		
		Vector3[] vertices = splitMesh.vertices;
		int[] triangles = splitMesh.triangles;
		Vector3[] triangleNormals = splitMesh.triangleNormals;
		if( vertices == null || triangles == null || triangleNormals == null ) {
			Debug.LogError("");
			return null;
		}
		
		HashSet<Vector3> usedTriangleNormals = new HashSet<Vector3>();
		bool[] usedTrianglesAll = new bool[triangles.Length / 3];
		bool[] usedTriangles = new bool[triangles.Length / 3];
		
		List<SplitMesh> resplitMeshes = new List<SplitMesh>();
		HashSet<Vector3> usedVertices = new HashSet<Vector3>();
		
		for( int triangle = 0; triangle < triangles.Length; triangle += 3 ) {
			if( !usedTrianglesAll[triangle / 3] ) {
				for( int i = 0; i < usedTriangles.Length; ++i ) {
					usedTriangles[i] = false;
				}
				usedTrianglesAll[triangle / 3] = true;
				usedTriangles[triangle / 3] = true;
				usedTriangleNormals.Clear();
				usedTriangleNormals.Add( triangleNormals[triangle / 3] );
				usedVertices.Clear();
				usedVertices.Add( vertices[triangles[triangle + 0]] );
				usedVertices.Add( vertices[triangles[triangle + 1]] );
				usedVertices.Add( vertices[triangles[triangle + 2]] );
				
				SplitMesh resplitMesh		= new SplitMesh();
				resplitMesh.subMeshCount	= splitMesh.subMeshCount;
				resplitMesh.subMesh			= splitMesh.subMesh;
				resplitMesh.triangle		= splitMesh.triangle;
				
				for(;;) {
					bool findAnything = false;
					for( int searchTriangle = triangle + 3; searchTriangle < triangles.Length; searchTriangle += 3 ) {
						if( !usedTrianglesAll[searchTriangle / 3] ) {
							Vector3 v0 = vertices[triangles[searchTriangle + 0]];
							Vector3 v1 = vertices[triangles[searchTriangle + 1]];
							Vector3 v2 = vertices[triangles[searchTriangle + 2]];
							if( usedVertices.Contains( v0 ) ||
							    usedVertices.Contains( v1 ) ||
							    usedVertices.Contains( v2 ) ) {
								Vector3 searchTriangleNormal = triangleNormals[searchTriangle / 3];
								bool dontMatchAnything = false;
								// memo: Check for all contained triangleNormals.
								foreach( Vector3 usedTriangleNormal in usedTriangleNormals ) {
									float dot = Vector3.Dot( usedTriangleNormal, searchTriangleNormal );
									if( dot < dotTolerance ) {
										dontMatchAnything = true;
										break;
									}
								}
								// memo: If all matched, add new triangleNormals.
								if( !dontMatchAnything ) {
									usedTrianglesAll[searchTriangle / 3] = true;
									usedTriangles[searchTriangle / 3] = true;
									usedTriangleNormals.Add( searchTriangleNormal );
									usedVertices.Add( v0 );
									usedVertices.Add( v1 );
									usedVertices.Add( v2 );
									findAnything = true;
								}
							}
						}
					}
					if( !findAnything ) {
						break;
					}
				}

				// FIX: Contain back faces.
				for( int searchTriangle = 0; searchTriangle < triangles.Length; searchTriangle += 3 ) {
					if( !usedTrianglesAll[searchTriangle / 3] ) {
						if( usedVertices.Contains( vertices[triangles[searchTriangle + 0]] ) &&
						    usedVertices.Contains( vertices[triangles[searchTriangle + 1]] ) &&
						    usedVertices.Contains( vertices[triangles[searchTriangle + 2]] ) ) {
							usedTrianglesAll[searchTriangle / 3] = true;
							usedTriangles[searchTriangle / 3] = true;
						}
					}
				}

				// Create triangles.
				List<int> rebuildTriangles = new List<int>( triangles.Length );
				List<Vector3> rebuildTriangleNormals = new List<Vector3>( triangles.Length / 3 );
				for( int searchTriangle = 0; searchTriangle < triangles.Length; searchTriangle += 3 ) {
					if( usedTriangles[searchTriangle / 3] ) {
						rebuildTriangles.Add( triangles[searchTriangle + 0] );
						rebuildTriangles.Add( triangles[searchTriangle + 1] );
						rebuildTriangles.Add( triangles[searchTriangle + 2] );
						rebuildTriangleNormals.Add( triangleNormals[searchTriangle / 3] );
					}
				}
				
				resplitMesh.vertices = vertices;
				resplitMesh.triangles = rebuildTriangles.ToArray();
				resplitMesh.triangleNormals = rebuildTriangleNormals.ToArray();
				SAColliderBuilderEditorCommon.CompactMesh compactMesh = SAColliderBuilderEditorCommon.MakeCompactMesh( resplitMesh.vertices, resplitMesh.triangles );
				if( compactMesh != null ) {
					resplitMesh.polygonVertices = compactMesh.vertices;
					resplitMesh.polygonTriangles = compactMesh.triangles;
				}
				resplitMeshes.Add( resplitMesh );
			}
		}
		
		return resplitMeshes.ToArray();
	}
	
	//--------------------------------------------------------------------------------------------------------

	public static void MarkManualProcessingToParent( SAMeshCollider meshCollider )
	{
		if( meshCollider == null ) {
			Debug.LogError("");
			return;
		}

		meshCollider.ChangeModified( true );

		Transform transform = meshCollider.transform.parent;
		for( ; transform != null; transform = transform.parent ) {
			meshCollider = transform.gameObject.GetComponent<SAMeshCollider>();
			if( meshCollider == null ) {
				return;
			}

			meshCollider.ChangeModifiedChildren( true );

			if( SAColliderBuilderEditorCommon.IsRootTransform( transform ) ) {
				return;
			}
		}
	}

	public static bool IsModifiedChildren( Transform parentTransform )
	{
		if( parentTransform != null ) {
			foreach( Transform childTransform in parentTransform ) {
				SAMeshCollider meshCollider = childTransform.gameObject.GetComponent<SAMeshCollider>();
				if( meshCollider != null ) {
					if( meshCollider.modified || meshCollider.modifiedChildren ) {
						return true;
					}
				}
			}
		}

		return false;
	}

	public static void UnmarkManualProcessingToParent( SAMeshCollider meshCollider )
	{
		if( meshCollider == null ) {
			Debug.LogError("");
			return;
		}
		
		meshCollider.ChangeModified( false );
		meshCollider.ChangeModifiedChildren( IsModifiedChildren( meshCollider.transform ) );
		
		Transform transform = meshCollider.transform.parent;
		for( ; transform != null; transform = transform.parent ) {
			meshCollider = transform.gameObject.GetComponent<SAMeshCollider>();
			if( meshCollider == null ) {
				return;
			}

			if( meshCollider.modified ) {
				// Nothing.
			} else if( meshCollider.modifiedChildren ) {
				// Check children.
				meshCollider.ChangeModifiedChildren( IsModifiedChildren( transform ) );
			}

			if( SAColliderBuilderEditorCommon.IsRootTransform( transform ) ) {
				return;
			}
		}
	}

	public static void CleanupSelfSAMeshCollider( SAMeshCollider meshCollider )
	{
		if( meshCollider == null ) {
			Debug.LogError("");
			return;
		}

		Collider[] colliders = meshCollider.gameObject.GetComponents<Collider>();
		if( colliders != null ) {
			for( int i = 0; i < colliders.Length; ++i ) {
				MonoBehaviour.DestroyImmediate( colliders[i] );
			}
		}

		Rigidbody rigidbody = meshCollider.gameObject.GetComponent<Rigidbody>();
		if( rigidbody != null ) {
			MonoBehaviour.DestroyImmediate( rigidbody );
		}
	}

	public static int CleanupChildSAMeshColliders( GameObject parentGameObject, bool cleanupModified )
	{
		if( parentGameObject == null ) {
			Debug.LogError("");
			return -1; // Nothing children.
		}
		
		List<Transform> childTransformList = SAColliderBuilderEditorCommon.GetChildTransformList( parentGameObject.transform );
		if( childTransformList == null ) {
			return -1; // Nothing children.
		}

		int r = -1; // Nothing children.
		for( int i = 0; i < childTransformList.Count; ++i ) {
			Transform childTransform = childTransformList[i];
			if( childTransform != null ) {
				SAMeshCollider meshCollider = childTransform.gameObject.GetComponent< SAMeshCollider >();
				if( meshCollider != null ) {
					if( !meshCollider.modified ) {
						CleanupSelfSAMeshCollider( meshCollider );
					}
					if( cleanupModified || !meshCollider.modified ) {
						int r2 = CleanupChildSAMeshColliders( childTransform.gameObject, cleanupModified );
						if( r2 == -1 ) {
							GameObject.DestroyImmediate( childTransform.gameObject ); // Destroy gameObject.
						} else if( r2 == 0 ) {
							meshCollider.ResetModified();
							MonoBehaviour.DestroyImmediate( meshCollider );
							if( r == -1 ) {
								r = 0; // Existing Non-SAMeshCollider child.
							}
						} else {
							r = 1; // Existing SAMeshCollider child.
						}
					} else {
						r = 1; // Existing SAMeshCollider child.
					}
				} else {
					if( r == -1 ) {
						r = 0; // Existing Non-SAMeshCollider child.
					}
				}
			}
		}

		return r;
	}

	//--------------------------------------------------------------------------------------------------------

	public static void SetupSAMeshCollider(
		SAMeshCollider parentMeshCollider,
		SAMeshCollider meshCollider,
		string defaultName )
	{
		if( parentMeshCollider == null || meshCollider == null || defaultName == null ) {
			Debug.LogError("");
			return;
		}

		meshCollider.ChangeDefaultName( defaultName );

		if( parentMeshCollider.meshColliderProperty != null ) {
			meshCollider.meshColliderProperty = parentMeshCollider.meshColliderProperty.Copy();
			meshCollider.defaultMeshColliderProperty = meshCollider.meshColliderProperty.Copy();
		}
	}

	public static SAMeshCollider CreateSAMeshCollider(
		SAMeshCollider parentMeshCollider,
		string gameObjectName,
		SplitMesh splitMesh,
		SplitMode splitMode )
	{
		if( parentMeshCollider == null || gameObjectName == null || splitMesh == null ) {
			Debug.LogError("");
			return null;
		}

		GameObject gameObject				= new GameObject( gameObjectName );
		gameObject.transform.parent			= parentMeshCollider.gameObject.transform;
		gameObject.transform.localPosition	= Vector3.zero;
		gameObject.transform.localRotation	= Quaternion.identity;
		gameObject.transform.localScale		= Vector3.one;
		SAMeshCollider meshCollider			= gameObject.AddComponent<SAMeshCollider>();
		
		meshCollider.splitMesh				= splitMesh;
		meshCollider.splitMode				= splitMode;

		meshCollider.defaultName			= gameObjectName;
		meshCollider.ResetModifyName();

		if( parentMeshCollider.meshColliderProperty != null ) {
			meshCollider.meshColliderProperty = parentMeshCollider.meshColliderProperty.Copy();
			meshCollider.defaultMeshColliderProperty = meshCollider.meshColliderProperty.Copy();
		}

		return meshCollider;
	}

	//--------------------------------------------------------------------------------------------------------

	public static string GetSAMeshColliderName_Root( GameObject parentGameObject )
	{
		if( parentGameObject != null && !string.IsNullOrEmpty(parentGameObject.name) ) {
			return parentGameObject.name;
		}

		return "Root";
	}

	public static string GetSAMeshColliderName_Material( Material[] materials, int index )
	{
		if( materials != null && index < materials.Length && !string.IsNullOrEmpty(materials[index].name) ) {
			return materials[index].name;
		}

		return "Mesh." + index.ToString("D8");
	}

	public static string GetSAMeshColliderName_Primitive( int index )
	{
		return "Prim." + index.ToString("D8");
	}

	public static string GetSAMeshColliderName_Polygon( int index )
	{
		return "Poly." + index.ToString("D8");
	}

	//--------------------------------------------------------------------------------------------------------

	public static void SetupSAMeshCollider(
		SAMeshColliderBuilder meshColliderBuilder,
		SAMeshCollider meshCollider,
		string defaultName )
	{
		if( meshColliderBuilder == null || meshCollider == null || defaultName == null ) {
			Debug.LogError("");
			return;
		}

		meshCollider.ChangeDefaultName( defaultName );

		if( meshColliderBuilder.meshColliderBuilderProperty != null ) {
			meshCollider.meshColliderProperty = meshColliderBuilder.meshColliderBuilderProperty.ToSAMeshColliderProperty();
			meshCollider.defaultMeshColliderProperty = meshCollider.meshColliderProperty.Copy();
		}
	}

	public static SAMeshCollider CreateSAMeshCollider(
		SAMeshColliderBuilder meshColliderBuilder,
		GameObject parentGameObject,
		string defaultName,
		SplitMesh splitMesh,
		SplitMode splitMode )
	{
		if( meshColliderBuilder == null || parentGameObject == null || defaultName == null || splitMesh == null ) {
			Debug.LogError("");
			return null;
		}
		
		GameObject gameObject							= new GameObject( defaultName );
		gameObject.transform.parent						= parentGameObject.transform;
		gameObject.transform.localPosition				= Vector3.zero;
		gameObject.transform.localRotation				= Quaternion.identity;
		gameObject.transform.localScale					= Vector3.one;
		SAMeshCollider meshCollider						= gameObject.AddComponent<SAMeshCollider>();
		
		meshCollider.splitMesh							= splitMesh;
		meshCollider.splitMode							= splitMode;

		meshCollider.defaultName						= defaultName;

		if( meshColliderBuilder.meshColliderBuilderProperty != null ) {
			meshCollider.meshColliderProperty = meshColliderBuilder.meshColliderBuilderProperty.ToSAMeshColliderProperty();
			meshCollider.defaultMeshColliderProperty = meshCollider.meshColliderProperty.Copy();
		}

		return meshCollider;
	}

	public class ReducerTask
	{
		public SimpleCountDownLatch		simpleCountDownLatch;
		public SAMeshCollider			meshCollider; // Don't access worker threads.
		public SplitMesh				splitMesh;
		public ReducerProperty			reducerProperty;
		public ReducerParams			reducerParams;
		public ReducerResult			reducerResult;
	}
	
	public static void RegistReducerTask( List<ReducerTask> reducerTasks, SAMeshCollider meshCollider )
	{
		if( reducerTasks == null || meshCollider == null ) {
			Debug.LogError("");
			return;
		}
		
		SplitMesh splitMesh = meshCollider.splitMesh;
		ReducerProperty reducerProperty = meshCollider.reducerProperty;
		if( splitMesh == null || reducerProperty == null ) {
			Debug.LogError("");
			return;
		}
		
		ReducerTask reducerTask = new ReducerTask();
		
		reducerTask.meshCollider = meshCollider;
		reducerTask.splitMesh = splitMesh;
		reducerTask.reducerProperty = reducerProperty;

		int reduceMode = 0;
		if( reducerProperty.shapeType == ShapeType.Mesh ) {
			if( reducerProperty.meshType == MeshType.ConvexBoxes ) {
				reduceMode = (int)SAColliderBoxReducer.ReduceMode.Mesh;
			} else {
				reduceMode = (int)SAColliderBoxReducer.ReduceMode.BoxMesh;
			}
		} else {
			reduceMode = (int)SAColliderBoxReducer.ReduceMode.Box;
		}
		
		ReducerParams reducerParams		= new ReducerParams();
		reducerParams.reduceMode		= reduceMode;
		reducerParams.reducerProperty	= reducerProperty;
		reducerTask.reducerParams		= reducerParams;
		reducerTasks.Add( reducerTask );
	}
	
	public static void ProcessReducerTask( object obj )
	{
		ReducerTask reducerTask = (ReducerTask)obj;
		try {
			ReduceMeshCollider( reducerTask );
		} catch( System.Exception e ) {
			Debug.LogError( e.ToString() );
		}
		if( reducerTask.simpleCountDownLatch != null ) {
			reducerTask.simpleCountDownLatch.CountDown();
		}
	}

	public static void ReduceMeshCollider( ReducerTask reducerTask )
	{
		if( reducerTask == null ||
		    reducerTask.splitMesh == null ||
		    reducerTask.reducerProperty == null ) {
			Debug.LogError("");
			return;
		}

		if( reducerTask.reducerProperty.shapeType == ShapeType.None ) {
			return; // Nothing.
		}

		if( reducerTask.reducerParams == null ) {
			Debug.LogError("");
			return;
		}
		
		reducerTask.reducerParams.vertices = reducerTask.splitMesh.vertices;
		reducerTask.reducerParams.triangles = reducerTask.splitMesh.triangles;
		reducerTask.reducerParams.lines = SAColliderBuilderEditorCommon.TriangleToLineIndices( reducerTask.splitMesh.triangles );
		reducerTask.reducerResult = Reduce( reducerTask.reducerParams );
	}
	
	//----------------------------------------------------------------------------------------------------------------
	
	public static Vector3 FuzzyZero( Vector3 v )
	{
		if( Mathf.Abs(v.x) <= 0.0001f ) { v.x = 0; }
		if( Mathf.Abs(v.y) <= 0.0001f ) { v.y = 0; }
		if( Mathf.Abs(v.z) <= 0.0001f ) { v.z = 0; }
		return v;
	}

	public static void CreateCollider(
		SAMeshCollider meshCollider,
		ReducerResult reducerResult,
		bool isDebug)
	{
		if( meshCollider == null ) {
			Debug.LogError("");
			return;
		}

		ReducerProperty reducerProperty = meshCollider.reducerProperty;
		ColliderProperty colliderProperty = meshCollider.colliderProperty;
		RigidbodyProperty rigidbodyProperty = meshCollider.rigidbodyProperty;
		if( reducerProperty == null || colliderProperty == null || rigidbodyProperty == null ) {
			Debug.LogError("");
			return;
		}

		if( reducerProperty.shapeType == ShapeType.None ) {
			return; // Nothing.
		}
		
		if( reducerResult == null ) {
			Debug.LogError("");
			return;
		}

		SAColliderBuilderEditorCommon.AddRigidbody( meshCollider.gameObject, rigidbodyProperty );

		GameObject gameObject = meshCollider.gameObject;

		gameObject.transform.localPosition = reducerResult.center;
		gameObject.transform.localRotation = reducerResult.rotation;
		gameObject.transform.localScale = Vector3.one;

		if( isDebug ) { // Logging.
			System.Text.StringBuilder str = new System.Text.StringBuilder();
			str.AppendLine( gameObject.name );
			SAColliderBuilderEditorCommon.DumpHierarchy( str, gameObject );
			Debug.Log( str.ToString() );
		}

		if( reducerProperty.shapeType == ShapeType.Mesh ) {
			Mesh mesh = new Mesh();
			mesh.vertices = reducerResult.vertices;
			mesh.triangles = reducerResult.triangles;
			MeshCollider collider = gameObject.AddComponent< MeshCollider >();
			collider.isTrigger = colliderProperty.isTrigger;
			collider.material = colliderProperty.material;
			if( colliderProperty.convex ) {
				if( reducerResult.triangles.Length <= 255 * 3 ) {
					collider.convex = true;
				} else {
					Debug.LogWarning( "Not convex mesh. " + meshCollider.gameObject.name );
				}
			}
			collider.sharedMesh = mesh;
			if( collider.bounds.size == Vector3.zero ) {
				System.Text.StringBuilder str = new System.Text.StringBuilder();
				str.AppendLine( "Zero bounds. " + meshCollider.gameObject.name );
				str.AppendLine( "Collider is too minimum. Please change Shape Type or Mesh Type." );
				SAColliderBuilderEditorCommon.DumpHierarchy( str, gameObject );
				Debug.LogWarning( str.ToString() );
			}
		} else {
			float sizeX = Mathf.Abs(reducerResult.boxB.x - reducerResult.boxA.x);
			float sizeY = Mathf.Abs(reducerResult.boxB.y - reducerResult.boxA.y);
			float sizeZ = Mathf.Abs(reducerResult.boxB.z - reducerResult.boxA.z);
			Vector3 center = FuzzyZero((reducerResult.boxA + reducerResult.boxB) / 2.0f);
			if( reducerProperty.shapeType == ShapeType.Box ) {
				BoxCollider boxCollider = gameObject.AddComponent< BoxCollider >();
				boxCollider.center = center;
				boxCollider.size = new Vector3( sizeX, sizeY, sizeZ );
				boxCollider.isTrigger = colliderProperty.isTrigger;
				boxCollider.material = colliderProperty.material;
			}
			if( reducerProperty.shapeType == ShapeType.Capsule ) {
				CapsuleCollider capsuleCollider = gameObject.AddComponent< CapsuleCollider >();
				capsuleCollider.center = center;
				if( sizeX > sizeY && sizeX > sizeZ ) { // X
					capsuleCollider.direction = 0;
					if( reducerProperty.fitType == FitType.Inner ) {
						capsuleCollider.radius = Mathf.Min( sizeY, sizeZ ) / 2.0f;
					} else {
						capsuleCollider.radius = Mathf.Max( sizeY, sizeZ ) / 2.0f;
					}
					capsuleCollider.height = sizeX;
				} else if( sizeY > sizeX && sizeY > sizeZ ) { // Y
					capsuleCollider.direction = 1;
					if( reducerProperty.fitType == FitType.Inner ) {
						capsuleCollider.radius = Mathf.Min( sizeX, sizeZ ) / 2.0f;
					} else {
						capsuleCollider.radius = Mathf.Max( sizeX, sizeZ ) / 2.0f;
					}
					capsuleCollider.height = sizeY;
				} else { // Z
					capsuleCollider.direction = 2;
					if( reducerProperty.fitType == FitType.Inner ) {
						capsuleCollider.radius = Mathf.Min( sizeX, sizeY ) / 2.0f;
					} else {
						capsuleCollider.radius = Mathf.Max( sizeX, sizeY ) / 2.0f;
					}
					capsuleCollider.height = sizeZ;
				}
				capsuleCollider.isTrigger = colliderProperty.isTrigger;
				capsuleCollider.material = colliderProperty.material;
			}
			if( reducerProperty.shapeType == ShapeType.Sphere ) {
				SphereCollider sphereCollider = gameObject.AddComponent< SphereCollider >();
				sphereCollider.center = center;
				if( reducerProperty.fitType == FitType.Inner ) {
					sphereCollider.radius = Mathf.Min( Mathf.Min( sizeX, sizeY ), sizeZ ) / 2.0f;
				} else {
					sphereCollider.radius = Mathf.Max( Mathf.Max( sizeX, sizeY ), sizeZ ) / 2.0f;
				}
				sphereCollider.isTrigger = colliderProperty.isTrigger;
				sphereCollider.material = colliderProperty.material;
			}
		}
	}
	
	//----------------------------------------------------------------------------------------------------------------
	
	public class ReducerParams
	{
		public int reduceMode;
		public Vector3[] vertices;
		public int[] triangles;
		public int[] lines;
		public ReducerProperty reducerProperty;
	}
	
	public class ReducerResult
	{
		public Quaternion rotation = Quaternion.identity;
		public Vector3 center;
		public Vector3 boxA;
		public Vector3 boxB;
		public Vector3[] vertices;
		public int[] triangles;
	}

	public static ReducerResult PostfixReducerVertices( ReducerParams reducerParams, Vector3[] vertices, int[] triangles )
	{
		if( reducerParams == null || reducerParams.reducerProperty == null || vertices == null || triangles == null ) {
			return null;
		}

		SAColliderBoxReducer reducer = new SAColliderBoxReducer();
		reducer.reduceMode = SAColliderBoxReducer.ReduceMode.Box;
		reducer.vertexList = vertices;
		reducer.optimizeRotation = reducerParams.reducerProperty.optimizeRotation;
		reducer.postfixTransform = false;
		reducer.Reduce();

		Quaternion reduceRotation = SAColliderBoxReducer.InversedRotation( reducer.reducedRotation );
		Vector3 reducedCenter = reducer.reducedCenter;

		Matrix4x4 transform = SAColliderBoxReducer._TranslateRotationMatrix( -reducedCenter, reduceRotation );

		Vector3[] transformVertices = (Vector3[])vertices.Clone();
		for( int i = 0; i < transformVertices.Length; ++i ) {
			transformVertices[i] = transform.MultiplyPoint3x4( transformVertices[i] );
		}

		if( reducerParams.reducerProperty.scale != Vector3.one ) {
			Vector3 scale = reducerParams.reducerProperty.scale;
			for( int i = 0; i < transformVertices.Length; ++i ) {
				transformVertices[i] = SAColliderBoxReducer.ScaledVector( transformVertices[i], scale );
			}
		}

		if( reducerParams.reducerProperty.offset != Vector3.zero ) {
			Vector3 offset = reducerParams.reducerProperty.offset;
			for( int i = 0; i < transformVertices.Length; ++i ) {
				transformVertices[i] += offset;
			}
		}

		ReducerResult reducerResult = new ReducerResult();
		reducerResult.rotation		= reducer.reducedRotation;
		reducerResult.center		= reducer.reducedCenter;
		reducerResult.vertices		= transformVertices;
		reducerResult.triangles		= triangles;
		return reducerResult;
	}

	public static ReducerResult MakeReduceMeshResult( ReducerParams reducerParams, Vector3[] vertices, int[] triangles )
	{
		SAColliderBuilderEditorCommon.CompactMesh compactMesh = SAColliderBuilderEditorCommon.MakeCompactMesh(
			vertices, triangles );
		if( compactMesh != null ) {
			return PostfixReducerVertices( reducerParams, compactMesh.vertices, compactMesh.triangles );
		} else {
			return PostfixReducerVertices( reducerParams, vertices, triangles );
		}
	}

	public static ReducerResult Reduce( ReducerParams reducerParams )
	{
		if( reducerParams == null || reducerParams.reducerProperty == null ) {
			Debug.LogError("");
			return null;
		}

		if( reducerParams.reducerProperty.shapeType == ShapeType.Mesh &&
		    reducerParams.reducerProperty.meshType == MeshType.Raw ) {
			return MakeReduceMeshResult( reducerParams, reducerParams.vertices, reducerParams.triangles );
		} else if(	reducerParams.reducerProperty.shapeType == ShapeType.Mesh &&
					reducerParams.reducerProperty.meshType == MeshType.ConvexHull ) {
			Vector3[] vertices = reducerParams.vertices;
			int[] triangles = reducerParams.triangles;
			if( vertices == null || triangles == null ) {
				Debug.LogError("");
				return null;
			}
			
			int maxTriangles = reducerParams.reducerProperty.maxTriangles;
			
			if( (triangles.Length / 3) <= maxTriangles ) {
				return MakeReduceMeshResult( reducerParams, vertices, triangles );
			}
			
			SAColliderHullReducer.HullDesc hullDesc = new SAColliderHullReducer.HullDesc();
			
			hullDesc.mFlags = SAColliderHullReducer.HullFlag.QF_TRIANGLES;
			hullDesc.mVcount = triangles.Length;
			for( int i = 0; i < triangles.Length; ++i ) {
				hullDesc.mVertices.Add( vertices[triangles[i]] );
			}
			
			hullDesc.mMaxVertices = Mathf.Max( (maxTriangles * 3 + 12) / 6, 2 );
			
			SAColliderHullReducer.HullResult hullResult = new SAColliderHullReducer.HullResult();
			SAColliderHullReducer.HullLibrary hullLibrary = new SAColliderHullReducer.HullLibrary();
			
			SAColliderHullReducer.HullError hullError = hullLibrary.CreateConvexHull( hullDesc, hullResult );
			if( hullError == SAColliderHullReducer.HullError.QE_FAIL ) {
				return MakeReduceMeshResult( reducerParams, vertices, triangles );
			}
			
			if( hullResult.m_OutputVertices == null || hullResult.m_Indices == null ) {
				Debug.LogError("");
				return MakeReduceMeshResult( reducerParams, vertices, triangles );
			}
			
			Vector3[] reducedVertices = new Vector3[hullResult.m_OutputVertices.Count];
			int[] reducesTriangles = new int[hullResult.m_Indices.Count];
			for( int i = 0; i < hullResult.m_OutputVertices.Count; ++i ) {
				reducedVertices[i] = hullResult.m_OutputVertices[i];
			}
			for( int i = 0; i < hullResult.m_Indices.Count; ++i ) {
				reducesTriangles[i] = hullResult.m_Indices[i];
			}
			
			return MakeReduceMeshResult( reducerParams, reducedVertices, reducesTriangles );
		} else {
			ReducerProperty reducerProperty = reducerParams.reducerProperty;
			
			SAColliderBoxReducer reducer = new SAColliderBoxReducer();
			reducer.reduceMode			= (SAColliderBoxReducer.ReduceMode)reducerParams.reduceMode;
			reducer.vertexList			= reducerParams.vertices;
			reducer.lineList			= reducerParams.lines;
			
			reducer.sliceCount			= Mathf.Max( (reducerProperty.maxTriangles - 4) / 8, 1 );
			
			reducer.sliceMode			= (SAColliderBoxReducer.SliceMode)(int)reducerProperty.sliceMode;
			reducer.minThickness		= reducerProperty.minThickness;
			reducer.optimizeRotation	= reducerProperty.optimizeRotation;
			reducer.scale				= reducerProperty.scale;
			reducer.offset				= reducerProperty.offset;
			reducer.thicknessA			= reducerProperty.thicknessA;
			reducer.thicknessB			= reducerProperty.thicknessB;
			reducer.postfixTransform	= false;
			
			reducer.Reduce();
			
			ReducerResult reducerResult	= new ReducerResult();
			reducerResult.rotation		= reducer.reducedRotation;
			reducerResult.center		= reducer.reducedCenter;
			reducerResult.boxA			= reducer.reducedBoxA;
			reducerResult.boxB			= reducer.reducedBoxB;
			reducerResult.vertices		= reducer.reducedVertexList;
			reducerResult.triangles		= reducer.reducedIndexList;
			return reducerResult;
		}
	}

	public static void Reduce( List<ReducerTask> reducerTasks, bool isDebug )
	{
		if( reducerTasks == null ) {
			Debug.LogError("");
			return;
		}

		SimpleCountDownLatch simpleCountDownLatch = new SimpleCountDownLatch( reducerTasks.Count );
		#if _DEBUG_SINGLETHREAD
		for( int i = 0; i < reducerTasks.Count; ++i ) {
			reducerTasks[i].simpleCountDownLatch = simpleCountDownLatch;
			ProcessReducerTask(reducerTasks[i]);
		}
		#else
		for( int i = 0; i < reducerTasks.Count; ++i ) {
			reducerTasks[i].simpleCountDownLatch = simpleCountDownLatch;
			System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(
				ProcessReducerTask), reducerTasks[i]);
		}
		simpleCountDownLatch.Wait();
		#endif

		for( int i = 0; i < reducerTasks.Count; ++i ) {
			SAMeshColliderEditorCommon.CreateCollider(
				reducerTasks[i].meshCollider,
				reducerTasks[i].reducerResult,
				isDebug );
			
			if( reducerTasks[i].splitMesh != null ) {
				reducerTasks[i].splitMesh.PurgeTemporary();
			}
		}
	}
}
