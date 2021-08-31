//----------------------------------------------
// SABoneCollider
// Copyright (c) 2014 Stereoarts Nora
//----------------------------------------------
//#define _DEBUG_SINGLETHREAD

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;

using ShapeType						= SAColliderBuilderCommon.ShapeType;
using FitType						= SAColliderBuilderCommon.FitType;
using MeshType						= SAColliderBuilderCommon.MeshType;
using SliceMode						= SAColliderBuilderCommon.SliceMode;
using ElementType					= SAColliderBuilderCommon.ElementType;
using ColliderToChild				= SAColliderBuilderCommon.ColliderToChild;
using BoneWeightType				= SABoneColliderCommon.BoneWeightType;
using BoneTriangleExtent			= SABoneColliderCommon.BoneTriangleExtent;

using ReducerProperty				= SAColliderBuilderCommon.ReducerProperty;
using ColliderProperty				= SAColliderBuilderCommon.ColliderProperty;
using RigidbodyProperty				= SAColliderBuilderCommon.RigidbodyProperty;
using SimpleCountDownLatch			= SAColliderBuilderEditorCommon.SimpleCountDownLatch;

using BoneProperty					= SABoneColliderCommon.BoneProperty;
using SplitProperty					= SABoneColliderCommon.SplitProperty;
using SABoneColliderProperty		= SABoneColliderCommon.SABoneColliderProperty;
using SABoneColliderBuilderProperty	= SABoneColliderCommon.SABoneColliderBuilderProperty;

public class SABoneColliderEditorCommon
{
	/*
	public class ReducerParams
	{
	}
	*/

	public class ReducerResult
	{
		public bool						colliderToChild;
		public Quaternion				rotation = Quaternion.identity;
		public Vector3					center;
		public Vector3					boxA;
		public Vector3					boxB;
		public Vector3[]				vertices;
		public int[]					triangles;
	}

	public class ReducerTask
	{
		public SimpleCountDownLatch		simpleCountDownLatch;
		public SABoneCollider			boneCollider; // Don't access worker threads.
		public BoneMeshCache			boneMeshCache;
		public ReducerProperty			reducerProperty;
		public bool						colliderToChild;
		//public ReducerParams			reducerParams;
		public Vector3[]				vertices;
		public int[]					triangles;
		public ReducerResult			reducerResult;
	}

	public static void RegistReducerTask(
		List<ReducerTask> reducerTasks,
		SABoneCollider boneCollider,
		BoneMeshCache boneMeshCache )
	{
		if( reducerTasks == null || boneCollider == null || boneMeshCache == null ) {
			Debug.LogError("");
			return;
		}

		ReducerTask reducerTask = new ReducerTask();
		reducerTask.boneCollider = boneCollider;
		reducerTask.boneMeshCache = boneMeshCache;
		reducerTask.reducerProperty = boneCollider.reducerProperty;
		switch( boneCollider.reducerProperty.colliderToChild ) {
		case ColliderToChild.Off:
			reducerTask.colliderToChild = false;
			break;
		case ColliderToChild.On:
			reducerTask.colliderToChild = true;
			break;
		case ColliderToChild.Auto:
			reducerTask.colliderToChild = true;
			if( boneCollider.rigidbodyProperty != null ) {
				if( boneCollider.rigidbodyProperty.isCreate ) {
					reducerTask.colliderToChild = boneCollider.rigidbodyProperty.isKinematic;
				}
			}
			break;
		}

		BoneMeshCreator boneMeshCreator = new BoneMeshCreator();
		boneMeshCreator.Process( boneCollider, boneMeshCache );
		reducerTask.vertices = boneMeshCreator.boneVertices;
		reducerTask.triangles = boneMeshCreator.boneTriangles;

		reducerTasks.Add( reducerTask );
	}

	public static void ProcessReducerTask( object obj )
	{
		ReducerTask reducerTask = (ReducerTask)obj;
		try {
			ReduceBoneCollider( reducerTask );
		} catch( System.Exception e ) {
			Debug.LogError( e.ToString() );
		}
		if( reducerTask.simpleCountDownLatch != null ) {
			reducerTask.simpleCountDownLatch.CountDown();
		}
	}

	public static ReducerResult PostfixReducerVertices( ReducerTask reducerTask, Vector3[] vertices, int[] triangles )
	{
		if( reducerTask == null || reducerTask.reducerProperty == null || vertices == null || triangles == null ) {
			Debug.LogError("");
			return null;
		}

		SAColliderBoxReducer reducer = new SAColliderBoxReducer();
		reducer.reduceMode = SAColliderBoxReducer.ReduceMode.Box;
		reducer.vertexList = vertices;
		reducer.optimizeRotation = reducerTask.reducerProperty.optimizeRotation;
		reducer.postfixTransform = false;
		reducer.Reduce();
		
		Quaternion reduceRotation = SAColliderBoxReducer.InversedRotation( reducer.reducedRotation );
		Vector3 reducedCenter = reducer.reducedCenter;
		
		Matrix4x4 transform = SAColliderBoxReducer._TranslateRotationMatrix( -reducedCenter, reduceRotation );
		
		Vector3[] transformVertices = (Vector3[])vertices.Clone();
		for( int i = 0; i < transformVertices.Length; ++i ) {
			transformVertices[i] = transform.MultiplyPoint3x4( transformVertices[i] );
		}
		
		if( reducerTask.reducerProperty.scale != Vector3.one ) {
			Vector3 scale = reducerTask.reducerProperty.scale;
			for( int i = 0; i < transformVertices.Length; ++i ) {
				transformVertices[i] = SAColliderBoxReducer.ScaledVector( transformVertices[i], scale );
			}
		}
		
		if( reducerTask.reducerProperty.offset != Vector3.zero ) {
			Vector3 offset = reducerTask.reducerProperty.offset;
			for( int i = 0; i < transformVertices.Length; ++i ) {
				transformVertices[i] += offset;
			}
		}

		ReducerResult reducerResult		= new ReducerResult();
		if( reducerTask.colliderToChild ) {
			reducerResult.rotation		= reducer.reducedRotation;
			reducerResult.center		= reducer.reducedCenter;
			reducerResult.boxA			= reducer.reducedBoxA;
			reducerResult.boxB			= reducer.reducedBoxB;
		} else {
			transform = transform.inverse;
			for( int i = 0; i < transformVertices.Length; ++i ) {
				transformVertices[i] = transform.MultiplyPoint3x4( transformVertices[i] );
			}
		}
		reducerResult.colliderToChild	= reducerTask.colliderToChild;
		reducerResult.vertices			= transformVertices;
		reducerResult.triangles			= triangles;
		return reducerResult;
	}
	
	public static ReducerResult MakeReduceMeshResult( ReducerTask reducerTask, Vector3[] vertices, int[] triangles )
	{
		SAColliderBuilderEditorCommon.CompactMesh compactMesh = SAColliderBuilderEditorCommon.MakeCompactMesh(
			vertices, triangles );
		if( compactMesh != null ) {
			return PostfixReducerVertices( reducerTask, compactMesh.vertices, compactMesh.triangles );
		} else {
			return PostfixReducerVertices( reducerTask, vertices, triangles );
		}
	}

	static void ReduceBoneCollider( ReducerTask reducerTask )
	{
		if( reducerTask == null || reducerTask.reducerProperty == null || reducerTask.boneMeshCache == null ) {
			Debug.LogError("");
			return;
		}

		if( reducerTask.vertices == null || reducerTask.triangles == null ) {
			return; // Nothing.
		}
		if( reducerTask.reducerProperty.shapeType == ShapeType.None ) {
			return; // Nothing.
		}

		ReducerProperty reducerProperty = reducerTask.reducerProperty;

#if false
		{
			ReducerResult reducerResult = new ReducerResult();
			reducerResult.vertices = reducerTask.vertices;
			reducerResult.triangles = reducerTask.triangles;
			reducerTask.reducerResult = reducerResult;
			return;
		}
#endif
		if( reducerProperty.shapeType == ShapeType.Mesh &&
		    reducerProperty.meshType == MeshType.Raw ) {
			reducerTask.reducerResult = MakeReduceMeshResult( reducerTask, reducerTask.vertices, reducerTask.triangles );
		} else if( reducerProperty.shapeType == ShapeType.Mesh &&
			  	   reducerProperty.meshType == MeshType.ConvexHull ) {

			Vector3[] vertices = reducerTask.vertices;
			int[] triangles = reducerTask.triangles;
			int maxTriangles = reducerProperty.maxTriangles;
			if( (triangles.Length / 3) <= maxTriangles ) {
				reducerTask.reducerResult = MakeReduceMeshResult( reducerTask, vertices, triangles );
				return;
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
				reducerTask.reducerResult = MakeReduceMeshResult( reducerTask, vertices, triangles );
				return;
			}
			
			if( hullResult.m_OutputVertices == null || hullResult.m_Indices == null ) {
				Debug.LogError("");
				reducerTask.reducerResult = MakeReduceMeshResult( reducerTask, vertices, triangles );
				return;
			}
			
			Vector3[] reducedVertices = new Vector3[hullResult.m_OutputVertices.Count];
			int[] reducesTriangles = new int[hullResult.m_Indices.Count];
			for( int i = 0; i < hullResult.m_OutputVertices.Count; ++i ) {
				reducedVertices[i] = hullResult.m_OutputVertices[i];
			}
			for( int i = 0; i < hullResult.m_Indices.Count; ++i ) {
				reducesTriangles[i] = hullResult.m_Indices[i];
			}
			
			reducerTask.reducerResult = MakeReduceMeshResult( reducerTask, reducedVertices, reducesTriangles );
			return;
		} else {
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

			SAColliderBoxReducer reducer = new SAColliderBoxReducer();
			reducer.reduceMode = (SAColliderBoxReducer.ReduceMode)reduceMode;
			reducer.vertexList = reducerTask.vertices;
			reducer.lineList = SAColliderBuilderEditorCommon.TriangleToLineIndices( reducerTask.triangles );
			reducer.scale = reducerProperty.scale;
			reducer.minThickness = reducerProperty.minThickness;
			reducer.offset = reducerProperty.offset;
			reducer.thicknessA = reducerProperty.thicknessA;
			reducer.thicknessB = reducerProperty.thicknessB;
			reducer.sliceCount = Mathf.Max( (reducerProperty.maxTriangles - 4) / 8, 1 );

			if( reducerTask.colliderToChild ) {
				reducer.postfixTransform = false;
				reducer.optimizeRotation = reducerProperty.optimizeRotation;
			} else {
				reducer.postfixTransform = true;
				reducer.rotation = Quaternion.identity; // Lock rotation to zero.
			}

			reducer.Reduce();

			ReducerResult reducerResult		= new ReducerResult();
			reducerResult.rotation			= reducer.reducedRotation;
			reducerResult.center			= reducer.reducedCenter;
			reducerResult.boxA				= reducer.reducedBoxA;
			reducerResult.boxB				= reducer.reducedBoxB;
			reducerResult.vertices			= reducer.reducedVertexList;
			reducerResult.triangles			= reducer.reducedIndexList;
			reducerResult.colliderToChild	= reducerTask.colliderToChild;
			reducerTask.reducerResult		= reducerResult;
		}
	}

	public static Vector3 FuzzyZero( Vector3 v )
	{
		if( Mathf.Abs(v.x) <= 0.0001f ) { v.x = 0; }
		if( Mathf.Abs(v.y) <= 0.0001f ) { v.y = 0; }
		if( Mathf.Abs(v.z) <= 0.0001f ) { v.z = 0; }
		return v;
	}

	public static void CreateCollider( SABoneCollider boneCollider, ReducerResult reducerResult, string collidersPath, bool isDebug )
	{
		if( boneCollider == null ) {
			Debug.LogError("");
			return;
		}
		if( reducerResult == null ) {
			return; // Nothing.
		}
		ReducerProperty reducerProperty = boneCollider.reducerProperty;
		ColliderProperty colliderProperty = boneCollider.colliderProperty;
		if( reducerProperty == null || colliderProperty == null ) {
			Debug.LogError("");
			return;
		}
		if( reducerProperty.shapeType == ShapeType.None ) {
			return; // Nothing.
		}

		if( reducerProperty.shapeType == ShapeType.Mesh ) {
			if( reducerResult.vertices == null || reducerResult.vertices.Length == 0 ||
			    reducerResult.triangles == null || reducerResult.triangles.Length == 0 ) {
				return;
			}
		}

		GameObject gameObject = null;
		if( reducerResult.colliderToChild ) {
			gameObject = new GameObject("Coll." + boneCollider.defaultName);
			gameObject.AddComponent< SABoneColliderChild >();
			gameObject.transform.parent = boneCollider.gameObject.transform;
			gameObject.transform.localPosition = Vector3.zero;
			gameObject.transform.localRotation = Quaternion.identity;
			gameObject.transform.localScale = Vector3.one;
		} else {
			gameObject = boneCollider.gameObject;
		}

		if( reducerResult.colliderToChild ) {
			gameObject.transform.localPosition = reducerResult.center;
			gameObject.transform.localRotation = reducerResult.rotation;
			gameObject.transform.localScale = Vector3.one;
		}

		if( isDebug ) { // Logging.
			System.Text.StringBuilder str = new System.Text.StringBuilder();
			str.AppendLine( gameObject.name );
			SAColliderBuilderEditorCommon.DumpHierarchy( str, gameObject );
			Debug.Log( str.ToString() );
		}

		SAColliderBuilderEditorCommon.AddRigidbody( gameObject, boneCollider.rigidbodyProperty );

		if( reducerProperty.shapeType == ShapeType.Mesh ) {
			Mesh mesh = SAColliderBuilderEditorCommon.ProcessColliderMesh(
				collidersPath,
				boneCollider.defaultName + ".asset",
				reducerResult.vertices,
				reducerResult.triangles );
			MeshCollider collider = gameObject.AddComponent< MeshCollider >();
			collider.isTrigger = colliderProperty.isTrigger;
			collider.material = colliderProperty.material;
			if( colliderProperty.convex ) {
				if( reducerResult.triangles.Length <= 255 * 3 ) {
					collider.convex = true;
				} else {
					Debug.LogWarning( "Not convex mesh. " + boneCollider.defaultName );
				}
			}
			collider.sharedMesh = mesh;
			if( collider.bounds.size == Vector3.zero ) {
				System.Text.StringBuilder str = new System.Text.StringBuilder();
				str.AppendLine( "Zero bounds. " + boneCollider.defaultName );
				str.AppendLine( "Collider is too minimum. Please change Shape Type or Mesh Type." );
				SAColliderBuilderEditorCommon.DumpHierarchy( str, gameObject );
				Debug.LogWarning( str.ToString() );
			}
		} else {
			float sizeX = Mathf.Abs(reducerResult.boxB.x - reducerResult.boxA.x);
			float sizeY = Mathf.Abs(reducerResult.boxB.y - reducerResult.boxA.y);
			float sizeZ = Mathf.Abs(reducerResult.boxB.z - reducerResult.boxA.z);
			Vector3 center = FuzzyZero((reducerResult.boxA + reducerResult.boxB) / 2.0f);
			if( !reducerResult.colliderToChild ) {
				center += reducerResult.center;
			}
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

	public static void Reduce( List<ReducerTask> reducerTasks, string collidersPath, bool isDebug )
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
			CreateCollider(
				reducerTasks[i].boneCollider,
				reducerTasks[i].reducerResult,
				collidersPath,
				isDebug );
		}
	}

	//----------------------------------------------------------------------------------------------------------------------------

	public static string GetAssetPath( GameObject gameObject )
	{
		if( gameObject != null ) {
			Animator animator = gameObject.GetComponent<Animator>();
			if( animator != null ) {
				string assetPath = AssetDatabase.GetAssetPath( animator.avatar );
				if( !string.IsNullOrEmpty( assetPath ) ) {
					return assetPath;
				}
			}
			SkinnedMeshRenderer[] skinnedMeshRenderers = SAColliderBuilderEditorCommon.GetSkinnedMeshRenderers( gameObject );
			if( skinnedMeshRenderers != null ) {
				foreach( SkinnedMeshRenderer skinnedMeshRenderer in skinnedMeshRenderers ) {
					if( skinnedMeshRenderer != null ) {
						string assetPath = AssetDatabase.GetAssetPath( skinnedMeshRenderer.sharedMesh );
						if( !string.IsNullOrEmpty( assetPath ) ) {
							return assetPath;
						}
					}
				}
			}
		}

		return null;
	}

	public static string GetCollidersPath( GameObject gameObject )
	{
		return SAColliderBuilderEditorCommon.GetCollidersPath( GetAssetPath( gameObject ) );
	}

	public static HashSet<Transform> GetBoneHashSet( GameObject gameObject )
	{
		if( gameObject != null ) {
			HashSet<Transform> boneHashSet = new HashSet<Transform>();
			SkinnedMeshRenderer[] skinnedMeshRenderers = SAColliderBuilderEditorCommon.GetSkinnedMeshRenderers( gameObject );
			if( skinnedMeshRenderers != null ) {
				foreach( SkinnedMeshRenderer skinnedMeshRenderer in skinnedMeshRenderers ) {
					foreach( Transform bone in skinnedMeshRenderer.bones ) {
						boneHashSet.Add( bone );
					}
				}
			}
			return boneHashSet;
		}
		return null;
	}
	
	//----------------------------------------------------------------------------------------------------------------------------

	public static bool IsModifiedChildren( Transform parentTransform )
	{
		if( parentTransform != null ) {
			foreach( Transform childTransform in parentTransform ) {
				SABoneCollider boneCollider = childTransform.gameObject.GetComponent<SABoneCollider>();
				if( boneCollider != null ) {
					if( boneCollider.modified || boneCollider.modifiedChildren ) {
						return true;
					}
				}
			}
		}
		
		return false;
	}

	public static void MarkManualProcessingToParent( SABoneCollider boneCollider )
	{
		if( boneCollider == null ) {
			Debug.LogError("");
			return;
		}
		
		boneCollider.ChangeModified( true );
		
		Transform transform = boneCollider.transform.parent;
		for( ; transform != null; transform = transform.parent ) {
			boneCollider = transform.gameObject.GetComponent<SABoneCollider>();
			if( boneCollider == null ) {
				return;
			}
			
			boneCollider.ChangeModifiedChildren( true );
			
			if( SAColliderBuilderEditorCommon.IsRootTransform( transform ) ) {
				return;
			}
		}
	}

	public static void UnmarkManualProcessingToParent( SABoneCollider boneCollider )
	{
		if( boneCollider == null ) {
			Debug.LogError("");
			return;
		}
		
		boneCollider.ChangeModified( false );
		boneCollider.ChangeModifiedChildren( IsModifiedChildren( boneCollider.transform ) );
		
		Transform transform = boneCollider.transform.parent;
		for( ; transform != null; transform = transform.parent ) {
			boneCollider = transform.gameObject.GetComponent<SABoneCollider>();
			if( boneCollider == null ) {
				return;
			}
			
			if( boneCollider.modified ) {
				// Nothing.
			} else if( boneCollider.modifiedChildren ) {
				// Check children.
				boneCollider.ChangeModifiedChildren( IsModifiedChildren( transform ) );
			}
			
			if( SAColliderBuilderEditorCommon.IsRootTransform( transform ) ) {
				return;
			}
		}
	}

	//----------------------------------------------------------------------------------------------------------------------------

	public static GameObject GetSABoneColliderRootGameObject( SABoneCollider boneCollider )
	{
		if( boneCollider == null ) {
			return null;
		}

		Transform parentTransform = boneCollider.transform.parent;
		if( parentTransform != null ) {
			SABoneCollider rootBoneCollider = null;
			while( parentTransform != null ) {
				if( parentTransform.gameObject.GetComponent< Animator >() != null ||
				    parentTransform.gameObject.GetComponent< SABoneColliderBuilder >() != null ) {
					return parentTransform.gameObject;
				}
				SABoneCollider parentBoneCollider = parentTransform.gameObject.GetComponent< SABoneCollider >();
				if( parentBoneCollider != null ) {
					rootBoneCollider = parentBoneCollider;
				}
				parentTransform = parentTransform.parent;
			}
			if( rootBoneCollider != null ) {
				return rootBoneCollider.gameObject;
			}
		}

		return boneCollider.gameObject;
	}

	static SABoneCollider _CreateSABoneCollider( GameObject gameObject, SABoneColliderProperty boneColliderProperty )
	{
		if( gameObject == null || boneColliderProperty == null ) {
			Debug.LogError("");
			return null;
		}
		
		SABoneCollider boneCollider = gameObject.GetComponent< SABoneCollider >();
		if( boneCollider == null ) {
			boneCollider = gameObject.AddComponent< SABoneCollider >();
		}

		boneCollider.boneColliderProperty = boneColliderProperty;
		boneCollider.defaultName = gameObject.name;
		return boneCollider;
	}

	public static SABoneCollider CreateSABoneCollider( GameObject gameObject, SABoneColliderBuilder boneColliderBuilder )
	{
		if( boneColliderBuilder == null || boneColliderBuilder.boneColliderBuilderProperty == null ) {
			Debug.LogError("");
			return null;
		}
		return _CreateSABoneCollider( gameObject, boneColliderBuilder.boneColliderBuilderProperty.ToSABoneColliderProperty() );
	}

	public static SABoneCollider CreateSABoneCollider( GameObject gameObject, SABoneCollider parentBoneCollider )
	{
		if( parentBoneCollider == null || parentBoneCollider.boneColliderProperty == null ) {
			Debug.LogError("");
			return null;
		}
		return _CreateSABoneCollider( gameObject, parentBoneCollider.boneColliderProperty.Copy() );
	}

	public static void CleanupSABoneCollider( SABoneCollider boneCollider )
	{
		if( boneCollider != null ) {
			List<GameObject> kinematicChildren = new List<GameObject>();
			foreach( Transform childTransform in boneCollider.transform ) {
				if( childTransform.GetComponent<SABoneColliderChild>() != null ) {
					kinematicChildren.Add( childTransform.gameObject );
				}
			}
			if( kinematicChildren.Count > 0 ) {
				for( int i = 0; i < kinematicChildren.Count; ++i ) {
					GameObject.DestroyImmediate( kinematicChildren[i] );
				}
			} else {
				if( boneCollider.reducerProperty != null && boneCollider.reducerProperty.shapeType != ShapeType.None ) {
					Collider[] colliders = boneCollider.gameObject.GetComponents<Collider>();
					if( colliders != null ) {
						for( int i = 0; i < colliders.Length; ++i ) {
							MonoBehaviour.DestroyImmediate( colliders[i] );
						}
					}
					if( boneCollider.rigidbodyProperty != null && boneCollider.rigidbodyProperty.isCreate ) {
						Rigidbody rigidbody = boneCollider.gameObject.GetComponent<Rigidbody>();
						if( rigidbody != null ) {
							MonoBehaviour.DestroyImmediate( rigidbody );
						}
					}
				}
			}
		}
	}

	public static void DestroySABoneCollider( SABoneCollider boneCollider )
	{
		if( boneCollider != null ) {
			CleanupSABoneCollider( boneCollider );
			boneCollider.ResetModified();
			MonoBehaviour.DestroyImmediate( boneCollider );
		}
	}

	//----------------------------------------------------------------------------------------------------------------------------

	public static void SplitInspectorGUI( SplitProperty splitProperty )
	{
		if( splitProperty == null ) {
			return;
		}

		EditorGUILayout.BeginHorizontal();
		splitProperty.boneWeightType = (BoneWeightType)EditorGUILayout.EnumPopup( "Bone Weight", splitProperty.boneWeightType );
		if( splitProperty.boneWeightType == BoneWeightType.Bone2 ) {
			int v = EditorGUILayout.IntField( splitProperty.boneWeight2 );
			v = Mathf.Clamp( v, 0, 100 );
			splitProperty.boneWeight2 = v;
			if( v == 100 ) {
				splitProperty.boneWeight3 = 100;
				splitProperty.boneWeight4 = 100;
			} if( v == 0 ) {
				splitProperty.boneWeight3 = 0;
				splitProperty.boneWeight4 = 0;
			} else {
				double r = (double)(100 - v) / (double)v;
				splitProperty.boneWeight3 = Mathf.Max( (int)(100.0 / (1.0 + r + r)), 1 );
				splitProperty.boneWeight4 = Mathf.Max( (int)(100.0 / (1.0 + r + r + r)), 1 );
			}
		} else {
			splitProperty.boneWeight2 = EditorGUILayout.IntField( splitProperty.boneWeight2 );
			splitProperty.boneWeight3 = EditorGUILayout.IntField( splitProperty.boneWeight3 );
			splitProperty.boneWeight4 = EditorGUILayout.IntField( splitProperty.boneWeight4 );
		}
		EditorGUILayout.EndHorizontal();
		splitProperty.greaterBoneWeight = EditorGUILayout.Toggle( "Greater Bone Weight", splitProperty.greaterBoneWeight );
		splitProperty.boneTriangleExtent = (BoneTriangleExtent)EditorGUILayout.EnumPopup( "Bone Triangle Extent", splitProperty.boneTriangleExtent );
	}

	//----------------------------------------------------------------------------------------------------------------------------

	public class BoneMeshCache
	{
		int				_meshVertexCount;
		int				_meshTriangleCount;
		int				_meshBoneCount;
		Matrix4x4[]		_meshBindPoses;
		Transform[]		_meshBones;
		BoneWeight[]	_meshBoneWeights;
		Vector3[]		_meshVertices;
		int[]			_meshTriangles;
		
		// for Work
		bool[]			_targetBones;
		bool[]			_targetVertices;
		bool[]			_passedVertices;
		bool[]			_processedVertices;
		int[]			_redirectIndices;
		int[]			_boneIndices;

		public int meshVetexCount			{ get { return _meshVertexCount; } }
		public int meshTriangleCount		{ get { return _meshTriangleCount; } }
		public int meshBoneCount			{ get { return _meshBoneCount; } }
		public Matrix4x4[] meshBindPoses	{ get { return _meshBindPoses; } }
		public Transform[] meshBones		{ get { return _meshBones; } }
		public BoneWeight[] meshBoneWeights	{ get { return _meshBoneWeights; } }
		public Vector3[] meshVertices		{ get { return _meshVertices; } }
		public int[] meshTriangles			{ get { return _meshTriangles; } }
		
		// for Work
		public bool[] targetBones			{ get { return _targetBones; } }
		public bool[] targetVertices		{ get { return _targetVertices; } }
		public bool[] passedVertices		{ get { return _passedVertices; } }
		public bool[] processedVertices		{ get { return _processedVertices; } }
		public int[] redirectIndices		{ get { return _redirectIndices; } }
		public int[] boneIndices			{ get { return _boneIndices; } }

		public void Process( GameObject gameObject )
		{
			SkinnedMeshRenderer[] skinnedMeshRenderers = SAColliderBuilderEditorCommon.GetSkinnedMeshRenderers( gameObject );
			if( skinnedMeshRenderers == null || skinnedMeshRenderers.Length == 0 ) {
				Debug.LogError( "Not found SkinnedMeshRenderer." );
				return;
			}
			
			foreach( var skinnedMeshRenderer in skinnedMeshRenderers ) {
				if( skinnedMeshRenderer.bones != null ) {
					BoneWeight[] boneWeights = skinnedMeshRenderer.sharedMesh.boneWeights;
					if( boneWeights == null || boneWeights.Length == 0 ) {
						continue;
					}
					this._meshBoneCount += skinnedMeshRenderer.bones.Length;
					this._meshVertexCount += skinnedMeshRenderer.sharedMesh.vertexCount;
					this._meshTriangleCount += skinnedMeshRenderer.sharedMesh.triangles.Length;
				}
			}
			
			this._meshBones				= new Transform[this._meshBoneCount];
			this._meshBindPoses			= new Matrix4x4[this._meshBoneCount];
			this._meshBoneWeights		= new BoneWeight[this._meshVertexCount];
			this._meshVertices			= new Vector3[this._meshVertexCount];
			this._meshTriangles			= new int[this._meshTriangleCount];
			
			// for Work
			this._targetBones			= new bool[this._meshBoneCount];
			this._targetVertices		= new bool[this._meshVertexCount];
			this._passedVertices		= new bool[this._meshVertexCount];
			this._processedVertices		= new bool[this._meshVertexCount];
			this._redirectIndices		= new int[this._meshVertexCount];
			this._boneIndices			= new int[this._meshVertexCount];
			for( int i = 0; i < this._boneIndices.Length; ++i ) {
				this._boneIndices[i] = -1;
			}
			
			int meshRendererBoneIndex = 0;
			int meshRendererVertexIndex = 0;
			int meshRendererTriangleIndex = 0;
			foreach( var skinnedMeshRenderer in skinnedMeshRenderers ) {
				if( skinnedMeshRenderer.bones != null ) {
					Transform[]		bones		= skinnedMeshRenderer.bones;
					Matrix4x4[]		bindPoses	= skinnedMeshRenderer.sharedMesh.bindposes;
					BoneWeight[]	boneWeights	= skinnedMeshRenderer.sharedMesh.boneWeights;
					Vector3[]		vertices	= skinnedMeshRenderer.sharedMesh.vertices;
					int[]			triangles	= skinnedMeshRenderer.sharedMesh.triangles;
					if( boneWeights == null || boneWeights.Length == 0 ) {
						continue;
					}
					for( int i = 0; i < bones.Length; ++i ) {
						this._meshBones[meshRendererBoneIndex + i] = bones[i];
						this._meshBindPoses[meshRendererBoneIndex + i] = bindPoses[i];
					}
					for( int i = 0; i < vertices.Length; ++i ) {
						this._meshVertices[meshRendererVertexIndex + i] = vertices[i];
						BoneWeight boneWeight = boneWeights[i];
						if( boneWeight.boneIndex0 >= 0 ) boneWeight.boneIndex0 += meshRendererBoneIndex;
						if( boneWeight.boneIndex1 >= 0 ) boneWeight.boneIndex1 += meshRendererBoneIndex;
						if( boneWeight.boneIndex2 >= 0 ) boneWeight.boneIndex2 += meshRendererBoneIndex;
						if( boneWeight.boneIndex3 >= 0 ) boneWeight.boneIndex3 += meshRendererBoneIndex;
						this._meshBoneWeights[meshRendererVertexIndex + i] = boneWeight;
					}
					for( int i = 0; i < triangles.Length; ++i ) {
						this._meshTriangles[meshRendererTriangleIndex + i] = triangles[i] + meshRendererVertexIndex;
					}
					
					meshRendererBoneIndex		+= bones.Length;
					meshRendererVertexIndex		+= vertices.Length;
					meshRendererTriangleIndex	+= triangles.Length;
				}
			}
		}
		
		public void CleanWork()
		{
			Array.Clear( this._targetBones,			0, this._targetBones.Length );
			Array.Clear( this._targetVertices,		0, this._targetVertices.Length );
			Array.Clear( this._passedVertices,		0, this._passedVertices.Length );
			Array.Clear( this._processedVertices,	0, this._processedVertices.Length );
			Array.Clear( this._redirectIndices,		0, this._redirectIndices.Length );
			for( int i = 0; i < this._boneIndices.Length; ++i ) {
				this._boneIndices[i] = -1;
			}
		}
	}

	private class BoneMeshCreator
	{
		SABoneCollider	_boneCollider;
		BoneMeshCache	_boneMeshCache;
		
		int				_meshVertexCount;
		int				_meshBoneCount;
		Matrix4x4[]		_meshBindPoses;
		Transform[]		_meshBones;
		BoneWeight[]	_meshBoneWeights;
		Vector3[]		_meshVertices;
		int[]			_meshTriangles;
		bool[]			_targetBones;
		bool[]			_processedVertex;

		Vector3[]		_boneVertices;
		int[]			_boneTriangles;

		public Vector3[] boneVertices	{ get { return _boneVertices; } }
		public int[] boneTriangles		{ get { return _boneTriangles; } }

		public bool Process( SABoneCollider boneCollider, BoneMeshCache boneMeshCache )
		{
			if( boneCollider == null || boneMeshCache == null ) {
				return false;
			}

			boneMeshCache.CleanWork();

			if( boneMeshCache.meshBoneCount == 0 ||
			    boneMeshCache.meshVetexCount == 0 || 
			    boneMeshCache.meshTriangleCount == 0 ) {
				return false;
			}

			this._boneCollider		= boneCollider;
			this._boneMeshCache		= boneMeshCache;
			
			this._meshVertexCount	= boneMeshCache.meshVetexCount;
			this._meshBoneCount		= boneMeshCache.meshBoneCount;
			this._meshBones			= boneMeshCache.meshBones;
			this._meshBindPoses		= boneMeshCache.meshBindPoses;
			this._meshBoneWeights	= boneMeshCache.meshBoneWeights;
			this._meshVertices		= boneMeshCache.meshVertices;
			this._meshTriangles		= boneMeshCache.meshTriangles;
			this._targetBones		= boneMeshCache.targetBones;
			this._processedVertex	= boneMeshCache.processedVertices;

			return _Process();
		}

		void _RebuildTargetBones( Transform boneTransform )
		{
			for( int i = 0; i < this._meshBoneCount; ++i ) {
				this._targetBones[i] = (this._meshBones[i] == boneTransform);
			}
		}

		bool _Process()
		{
			SplitProperty splitProperty = this._boneCollider.splitProperty;
			if( splitProperty == null ) {
				Debug.LogError("");
				return false;
			}

			float[] weights = new float[5] { 0.0f, 0.0f,
				(float)splitProperty.boneWeight2 * 0.01f,
				(float)splitProperty.boneWeight3 * 0.01f,
				(float)splitProperty.boneWeight4 * 0.01f };

			_RebuildTargetBones( this._boneCollider.transform );

			float[] boneWeightArray = new float[4];
			int[] boneIndexArray = new int[4];

			bool isGreaterBoneWeight = splitProperty.greaterBoneWeight;

			int passedVertexCount = 0;
			bool[] targetVertex = this._boneMeshCache.targetVertices;
			int[] boneIndices = this._boneMeshCache.boneIndices;
			BoneWeight[] boneWeights = this._meshBoneWeights;
			for( int i = 0; i < this._meshVertexCount; ++i ) {
				if( _processedVertex[i] == false ) {
					BoneWeight boneWeight = boneWeights[i];
					boneWeightArray[0] = boneWeight.weight0;
					boneWeightArray[1] = boneWeight.weight1;
					boneWeightArray[2] = boneWeight.weight2;
					boneWeightArray[3] = boneWeight.weight3;
					boneIndexArray[0] = boneWeight.boneIndex0;
					boneIndexArray[1] = boneWeight.boneIndex1;
					boneIndexArray[2] = boneWeight.boneIndex2;
					boneIndexArray[3] = boneWeight.boneIndex3;
					
					int boneCount = 0;
					int targetBoneIndex = -1;
					for( int n = 0; n < 4; ++n ) {
						if( boneIndexArray[n] >= 0 && boneWeightArray[n] > 0.0f ) {
							++boneCount;
						}
					}
					for( int n = 0; n < 4; ++n ) {
						if( boneIndexArray[n] >= 0 &&
						   	this._targetBones[boneIndexArray[n]] &&
						   	boneWeightArray[n] > weights[boneCount] ) {
							targetBoneIndex = boneIndexArray[n];
						}
					}
					if( isGreaterBoneWeight ) {
						if( targetBoneIndex == -1 ) {
							int greaterBoneIndex = -1;
							float greaterBoneWeight = 0.0f;
							bool greaterBoneIsTarget = false;
							for( int n = 0; n < 4; ++n ) {
								if( boneIndexArray[n] >= 0 ) {
									if( boneWeightArray[n] > greaterBoneWeight ) {
										greaterBoneIndex = boneIndexArray[n];
										greaterBoneWeight = boneWeightArray[n];
										greaterBoneIsTarget = this._targetBones[greaterBoneIndex];
									} else if( boneWeightArray[n] == greaterBoneWeight && !greaterBoneIsTarget ) {
										greaterBoneIndex = boneIndexArray[n];
										greaterBoneWeight = boneWeightArray[n];
										greaterBoneIsTarget = this._targetBones[greaterBoneIndex];
									}
								}
							}
							if( greaterBoneIsTarget ) {
								targetBoneIndex = greaterBoneIndex;
							}
						}
					}
					if( targetBoneIndex != -1 ) {
						boneIndices[i] = targetBoneIndex;
						targetVertex[i] = true;
						_processedVertex[i] = true;
						++passedVertexCount;
					}
				}
			}

			if( passedVertexCount == 0 ) {
				return false;
			}

			int[] triangles = this._meshTriangles;

			if( splitProperty.boneTriangleExtent != BoneTriangleExtent.Disable ) {
				int extentVertexCount = 0;
				if( splitProperty.boneTriangleExtent == BoneTriangleExtent.Vertex1 ) {
					extentVertexCount = 1;
				}
				if( splitProperty.boneTriangleExtent == BoneTriangleExtent.Vertex2 ) {
					extentVertexCount = 2;
				}
				for( int i = 0; i + 2 < triangles.Length; i += 3 ) {
					int index0 = triangles[i + 0];
					int index1 = triangles[i + 1];
					int index2 = triangles[i + 2];
					int targetVertexCount = 0;
					bool pv0 = _processedVertex[index0];
					bool pv1 = _processedVertex[index1];
					bool pv2 = _processedVertex[index2];
					if( pv0 ) ++targetVertexCount;
					if( pv1 ) ++targetVertexCount;
					if( pv2 ) ++targetVertexCount;
					if( targetVertexCount != 3 && targetVertexCount >= extentVertexCount ) {
						int replicateBoneIndex = -1;
						int boneIndex0 = boneIndices[index0];
						int boneIndex1 = boneIndices[index1];
						int boneIndex2 = boneIndices[index2];
						if( boneIndex0 != -1 && pv0 && replicateBoneIndex == -1 ) replicateBoneIndex = boneIndex0;
						if( boneIndex1 != -1 && pv1 && replicateBoneIndex == -1 ) replicateBoneIndex = boneIndex1;
						if( boneIndex2 != -1 && pv2 && replicateBoneIndex == -1 ) replicateBoneIndex = boneIndex2;
						if( boneIndex0 == -1 ) boneIndices[index0] = replicateBoneIndex;
						if( boneIndex1 == -1 ) boneIndices[index1] = replicateBoneIndex;
						if( boneIndex2 == -1 ) boneIndices[index2] = replicateBoneIndex;
						targetVertex[index0] = true;
						targetVertex[index1] = true;
						targetVertex[index2] = true;
					}
				}
				for( int i = 0; i < this._meshVertexCount; ++i ) {
					_processedVertex[i] |= targetVertex[i];
				}
			}

			bool[] passedVertex = this._boneMeshCache.passedVertices;
			List<int> passedTriangles = new List<int>();
			for( int i = 0; i + 2 < triangles.Length; i += 3 ) {
				int index0 = triangles[i + 0];
				int index1 = triangles[i + 1];
				int index2 = triangles[i + 2];
				if( targetVertex[index0] && targetVertex[index1] && targetVertex[index2] ) {
					passedTriangles.Add( index0 );
					passedTriangles.Add( index1 );
					passedTriangles.Add( index2 );
					passedVertex[index0] = true;
					passedVertex[index1] = true;
					passedVertex[index2] = true;
				}
			}
			
			if( passedTriangles.Count == 0 ) {
				return false;
			}
			
			int remakeVertexCount = 0;
			for( int i = 0; i < this._meshVertexCount; ++i ) {
				if( passedVertex[i] ) {
					++remakeVertexCount;
				}
			}
			
			Vector3[] remakeVertices = new Vector3[remakeVertexCount];
			int[] redirectIndex = this._boneMeshCache.redirectIndices;
			for( int i = 0, index = 0; i < this._meshVertexCount; ++i ) {
				if( passedVertex[i] ) {
					Matrix4x4 matrix = _meshBindPoses[boneIndices[i]];
					Vector3 v = this._meshVertices[i];
					v = matrix.MultiplyPoint( v );
					remakeVertices[index] = v;
					redirectIndex[i] = index;
					++index;
				}
			}
			
			for( int i = 0; i < passedTriangles.Count; ++i ) {
				passedTriangles[i] = redirectIndex[passedTriangles[i]];
			}

			_boneVertices = remakeVertices;
			_boneTriangles = passedTriangles.ToArray();
			return true;
		}
	}
}
