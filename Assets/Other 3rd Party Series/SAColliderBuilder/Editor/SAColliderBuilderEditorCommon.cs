//----------------------------------------------
// SABoneCollider
// Copyright (c) 2014 Stereoarts Nora
//----------------------------------------------
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

using ShapeType			= SAColliderBuilderCommon.ShapeType;
using FitType			= SAColliderBuilderCommon.FitType;
using MeshType			= SAColliderBuilderCommon.MeshType;
using SliceMode			= SAColliderBuilderCommon.SliceMode;
using ElementType		= SAColliderBuilderCommon.ElementType;
using ColliderToChild	= SAColliderBuilderCommon.ColliderToChild;

using ReducerProperty	= SAColliderBuilderCommon.ReducerProperty;
using ColliderProperty	= SAColliderBuilderCommon.ColliderProperty;
using RigidbodyProperty	= SAColliderBuilderCommon.RigidbodyProperty;

public class SAColliderBuilderEditorCommon
{
	public class SimpleCountDownLatch
	{
		readonly object _lock = new object();
		int _count;
		
		public SimpleCountDownLatch(int count)
		{
			_count = count;
		}
		
		public void Wait()
		{
			lock(_lock) {
				while( _count > 0 ) {
					System.Threading.Monitor.Wait(_lock);
				}
			}
		}
		
		public void Reset(int count)
		{
			_count = count;
		}
		
		public void CountDown()
		{
			lock(_lock) {
				if(--_count == 0) {
					System.Threading.Monitor.PulseAll(_lock);
				}
			}
		}
	}

	static void _GetSkinnedMeshRenderersInChildren( List<SkinnedMeshRenderer> skinnedMeshRenderers, GameObject go )
	{
		if( skinnedMeshRenderers != null && go != null ) {
			foreach( Transform childTransform in go.transform ) {
				if( childTransform.gameObject.GetComponent< Animator >() == null &&
					childTransform.gameObject.GetComponent< SAMeshColliderBuilder >() == null &&
				   	childTransform.gameObject.GetComponent< SABoneColliderBuilder >() == null ) { // Skip including child mesh.
					SkinnedMeshRenderer skinnedMeshRenderer = childTransform.gameObject.GetComponent< SkinnedMeshRenderer >();
					if( skinnedMeshRenderer != null ) {
						skinnedMeshRenderers.Add( skinnedMeshRenderer );
					}

					_GetSkinnedMeshRenderersInChildren( skinnedMeshRenderers, childTransform.gameObject );
				}
			}
		}
	}

	public static SkinnedMeshRenderer[] GetSkinnedMeshRenderers( GameObject go )
	{
		if( go == null ) {
			return null;
		}

		List<SkinnedMeshRenderer> skinnedMeshRenderers = new List<SkinnedMeshRenderer>();
		SkinnedMeshRenderer skinnedMeshRenderer = go.GetComponent< SkinnedMeshRenderer > ();
		if( skinnedMeshRenderer != null ) {
			skinnedMeshRenderers.Add( skinnedMeshRenderer );
		}

		_GetSkinnedMeshRenderersInChildren( skinnedMeshRenderers, go );
		return skinnedMeshRenderers.ToArray();
	}

	static void _GetMeshFiltersInChildren( List<MeshFilter> meshFilters, GameObject go )
	{
		if( meshFilters != null && go != null ) {
			foreach( Transform childTransform in go.transform ) {
				if( childTransform.gameObject.GetComponent< Animator >() == null &&
					childTransform.gameObject.GetComponent< SAMeshColliderBuilder >() == null &&
					childTransform.gameObject.GetComponent< SABoneColliderBuilder >() == null ) { // Skip including child mesh.
					MeshFilter meshFilter = childTransform.gameObject.GetComponent< MeshFilter >();
					if( meshFilter != null ) {
						meshFilters.Add( meshFilter );
					}
					
					_GetMeshFiltersInChildren( meshFilters, childTransform.gameObject );
				}
			}
		}
	}

	public static MeshFilter[] GetMeshFilters( GameObject go )
	{
		if( go == null ) {
			return null;
		}
		
		List<MeshFilter> meshFilters = new List<MeshFilter>();
		MeshFilter meshFilter = go.GetComponent< MeshFilter > ();
		if( meshFilter != null ) {
			meshFilters.Add( meshFilter );
		}
		
		_GetMeshFiltersInChildren( meshFilters, go );
		return meshFilters.ToArray();
	}

	public static List<Transform> GetChildTransformList( Transform parentTransform )
	{
		if( parentTransform != null ) {
			List<Transform> childTransformList = new List<Transform>();
			foreach( Transform childTransform in parentTransform ) {
				childTransformList.Add( childTransform );
			}
			return childTransformList;
		}
		return null;
	}

	//--------------------------------------------------------------------------------------------------------

	public static bool IsRootTransform( Transform transform )
	{
		if( transform != null ) {
			Animator animator = transform.gameObject.GetComponent<Animator>();
			SAMeshColliderBuilder meshColliderBuilder = transform.gameObject.GetComponent<SAMeshColliderBuilder>();
			SABoneColliderBuilder boneColliderBuilder = transform.gameObject.GetComponent<SABoneColliderBuilder>();
			if( animator != null || meshColliderBuilder != null || boneColliderBuilder != null ) {
				return true;
			}
		}
		
		return false;
	}

	//--------------------------------------------------------------------------------------------------------

	public static Mesh ProcessColliderMesh( string collidersPath, string fileName, Vector3[] vertices, int[] triangles )
	{
		if( !string.IsNullOrEmpty( collidersPath ) ) {
			string colliderPath = PathCombine( collidersPath, EscapeFileName( fileName ) );
			Mesh assetMesh = AssetDatabase.LoadAssetAtPath( colliderPath, typeof(Mesh) ) as Mesh;
			if( assetMesh != null ) {
				// Overwrite asset.
				assetMesh.triangles = new int[0];
				assetMesh.vertices = vertices;
				assetMesh.triangles = triangles;
				AssetDatabase.SaveAssets();
				return assetMesh;
			} else {
				// Create new asset.
				Mesh mesh = new Mesh();
				mesh.vertices = vertices;
				mesh.triangles = triangles;
				if( !System.IO.Directory.Exists( collidersPath ) ) {
					System.IO.Directory.CreateDirectory( collidersPath );
				}
				AssetDatabase.CreateAsset( mesh, colliderPath );
				return mesh;
			}
		} else {
			Mesh mesh = new Mesh();
			mesh.vertices = vertices;
			mesh.triangles = triangles;
			return mesh;
		}
	}

	public static string GetCollidersPath( string assetPath )
	{
		if( !string.IsNullOrEmpty( assetPath ) ) {
			assetPath = System.IO.Path.GetDirectoryName( assetPath );
			if( !string.IsNullOrEmpty( assetPath ) ) {
				return PathCombine( assetPath, "Colliders" );
			}
		}

		return null;
	}

	public static string EscapeFileName( string fileName )
	{
		if( fileName == null ) {
			return null;
		}
		System.Text.StringBuilder r = new System.Text.StringBuilder();
		for( int i = 0; i < fileName.Length; ++i ) {
			char c = fileName[i];
			if( c == '\t' ||
				c == '\\' ||
				c == '/'  ||
				c == ':'  ||
				c == '*'  ||
				c == '?'  ||
				c == '\"' ||
				c == '<'  ||
				c == '>'  ||
				c == '|' ) {
				r.Append( '_' );
			} else {
				if( c != '.' || i + 1 < fileName.Length ) {
					r.Append( c );
				} else {
					r.Append( '_' );
				}
			}
		}

		return r.ToString();
	}

	public static string PathCombine( string pathA, string pathB )
	{
		//return Path.Combine( pathA, pathB );
		if( string.IsNullOrEmpty( pathA ) ) {
			return pathB;
		}
		if( string.IsNullOrEmpty( pathB ) ) {
			return pathA;
		}
		
		System.Text.StringBuilder str = new System.Text.StringBuilder();
		for( int i = 0; i < pathA.Length; ++i ) {
			if( pathA[i] == '\\' ) {
				str.Append( '/' );
			} else if( pathA[i] == '.' ) {
				if( i + 1 < pathA.Length && ( pathA[i + 1] == '/' || pathA[i + 1] == '\\' ) ) {
					++i;
				} else {
					str.Append( pathA[i] );
				}
			} else {
				str.Append( pathA[i] );
			}
		}
		
		if( pathA[pathA.Length - 1] != '/' && pathA[pathA.Length - 1] != '\\' ) {
			str.Append( '/' );
		}
		
		for( int i = 0; i < pathB.Length; ++i ) {
			if( pathB[i] == '\\' ) {
				str.Append( '/' );
			} else if( pathB[i] == '.' ) {
				if( i + 1 < pathB.Length && ( pathB[i + 1] == '/' || pathB[i + 1] == '\\' ) ) {
					++i;
				} else {
					str.Append( pathB[i] );
				}
			} else {
				str.Append( pathB[i] );
			}
		}
		
		return str.ToString();
	}

	public static Mesh GetMesh( MeshFilter meshFilter )
	{
		if( meshFilter != null ) {
			return meshFilter.sharedMesh;
		}
		return null;
	}

	public static Mesh GetMesh( SkinnedMeshRenderer skinnedMeshRenderer )
	{
		if( skinnedMeshRenderer != null ) {
			return skinnedMeshRenderer.sharedMesh;
		}
		return null;
	}

	public static Material[] GetMaterials( MeshFilter meshFilter )
	{
		if( meshFilter != null ) {
			MeshRenderer meshRenderer = meshFilter.gameObject.GetComponent<MeshRenderer>();
			if( meshRenderer != null ) {
				return meshRenderer.sharedMaterials;
			}
		}
		return null;
	}

	public static Material[] GetMaterials( SkinnedMeshRenderer skinnedMeshRenderer )
	{
		if( skinnedMeshRenderer != null ) {
			return skinnedMeshRenderer.sharedMaterials;
		}
		return null;
	}

	//--------------------------------------------------------------------------------------------------------

	public class CompactMesh
	{
		public Vector3[] vertices;
		public int[] triangles;

		public CompactMesh( Vector3[] vertices, int[] triangles )
		{
			this.vertices = vertices;
			this.triangles = triangles;
		}
	}
	
	public static CompactMesh MakeCompactMesh( Vector3[] vertices, int[] triangles )
	{
		if( vertices == null || triangles == null ) {
			return null;
		}
		
		bool[] usedVertices = new bool[vertices.Length];
		for( int i = 0; i < triangles.Length; ++i ) {
			usedVertices[triangles[i]] = true;
		}
		int compactLength = 0;
		for( int i = 0; i < usedVertices.Length; ++i ) {
			if( usedVertices[i] ) {
				++compactLength;
			}
		}

		if( compactLength == vertices.Length ) {
			return new CompactMesh( vertices, triangles );
		}

		int compactIndex = 0;
		Vector3[] compactVertices = new Vector3[compactLength];
		int[] indexConverter = new int[usedVertices.Length];
		for( int i = 0; i < usedVertices.Length; ++i ) {
			indexConverter[i] = compactIndex;
			if( usedVertices[i] ) {
				compactVertices[compactIndex] = vertices[i];
				++compactIndex;
			}
		}
		
		int[] compactTriangles = new int[triangles.Length];
		for( int i = 0; i < triangles.Length; ++i ) {
			compactTriangles[i] = indexConverter[triangles[i]];
		}
		
		return new CompactMesh( compactVertices, compactTriangles );
	}

	//--------------------------------------------------------------------------------------------------------

	static ulong _MakeLineKey( int index0, int index1 )
	{
		if( index0 < index1 ) {
			return (ulong)(uint)index0 | ((ulong)(uint)index1 << 32);
		} else {
			return (ulong)(uint)index1 | ((ulong)(uint)index0 << 32);
		}
	}
	
	public static int[] TriangleToLineIndices( int[] triangles )
	{
		if( triangles == null || triangles.Length == 0 ) {
			return null;
		}
		
		HashSet<ulong> lineKeys = new HashSet<ulong>();
		for( int t = 0; t < triangles.Length; t += 3 ) {
			lineKeys.Add( _MakeLineKey( triangles[t + 0], triangles[t + 1] ) );
			lineKeys.Add( _MakeLineKey( triangles[t + 1], triangles[t + 2] ) );
			lineKeys.Add( _MakeLineKey( triangles[t + 2], triangles[t + 0] ) );
		}
		
		List<int> lines = new List<int>(lineKeys.Count * 2);
		foreach( ulong lineKey in lineKeys ) {
			int index0 = unchecked( (int)(uint)lineKey );
			int index1 = unchecked( (int)(uint)(lineKey >> 32) );
			lines.Add( index0 );
			lines.Add( index1 );
		}
		
		return lines.ToArray();
	}

	//--------------------------------------------------------------------------------------------------------

	public static void AddRigidbody( GameObject gameObject, RigidbodyProperty rigidbodyProperty )
	{
		if( gameObject == null || rigidbodyProperty == null ) {
			Debug.LogError("");
			return;
		}

		if( rigidbodyProperty.isCreate ) {
			Rigidbody rigodbody = gameObject.AddComponent< Rigidbody >();
			if( rigodbody == null ) {
				rigodbody = gameObject.GetComponent< Rigidbody >();
			}
			if( rigodbody != null ) {
				rigodbody.mass = rigidbodyProperty.mass;
				rigodbody.drag = rigidbodyProperty.drag;
				rigodbody.angularDrag = rigidbodyProperty.angularDrag;
				rigodbody.isKinematic = rigidbodyProperty.isKinematic;
				rigodbody.useGravity = rigidbodyProperty.useGravity;
				rigodbody.interpolation = rigidbodyProperty.interpolation;
				rigodbody.collisionDetectionMode = rigidbodyProperty.collisionDetectionMode;
			}
		}
	}

	//--------------------------------------------------------------------------------------------------------

	[System.Flags]
	public enum ReducerOption
	{
		None				= 0,
		ColliderToChild		= 0x01,
		Advanced			= 0x02,
	}

	public static void ReducerInspectorGUI( ReducerProperty reducerProperty, ReducerOption reducerOption )
	{
		if( reducerProperty == null ) {
			return;
		}

		bool enabled = GUI.enabled;

		bool shapeEnabled = (reducerProperty.shapeType != ShapeType.None);
		bool thicknessEnables = true;
		if( reducerProperty.shapeType == ShapeType.None ) {
			thicknessEnables = false;
		} else if( reducerProperty.shapeType == ShapeType.Mesh ) {
			if( reducerProperty.meshType == MeshType.Raw ||
			    reducerProperty.meshType == MeshType.ConvexHull ) {
				thicknessEnables = false;
			}
		}

		reducerProperty.shapeType = (ShapeType)EditorGUILayout.EnumPopup( "Shape Type", reducerProperty.shapeType );

		GUI.enabled = enabled && ( reducerProperty.shapeType == ShapeType.Capsule ||
		                          reducerProperty.shapeType == ShapeType.Sphere );
		reducerProperty.fitType = (FitType)EditorGUILayout.EnumPopup( "Fit Type", reducerProperty.fitType );

		GUI.enabled = enabled && reducerProperty.shapeType == ShapeType.Mesh;
		reducerProperty.meshType = (MeshType)EditorGUILayout.EnumPopup( "Mesh Type", reducerProperty.meshType );

		GUI.enabled = enabled && reducerProperty.shapeType == ShapeType.Mesh &&
			(reducerProperty.meshType == MeshType.ConvexBoxes || reducerProperty.meshType == MeshType.ConvexHull);
		reducerProperty.maxTriangles = EditorGUILayout.IntSlider( "Max Triangles", reducerProperty.maxTriangles, 1, 255 );

		GUI.enabled = enabled && reducerProperty.shapeType == ShapeType.Mesh
								&& reducerProperty.meshType == MeshType.ConvexBoxes;
		reducerProperty.sliceMode = (SliceMode)EditorGUILayout.EnumPopup( "Slice Mode", reducerProperty.sliceMode );

		GUI.enabled = enabled;

		EditorGUILayout.BeginHorizontal();
		GUI.enabled = enabled && shapeEnabled;
		reducerProperty.scaleElementType = (ElementType)EditorGUILayout.EnumPopup( "Scale", reducerProperty.scaleElementType );
		if( reducerProperty.scaleElementType == ElementType.X ) {
			float v = EditorGUILayout.FloatField( reducerProperty.scale.x );
			reducerProperty.scale.x = v;
			reducerProperty.scale.y = v;
			reducerProperty.scale.z = v;
		} else {
			reducerProperty.scale.x = EditorGUILayout.FloatField( reducerProperty.scale.x );
			reducerProperty.scale.y = EditorGUILayout.FloatField( reducerProperty.scale.y );
			reducerProperty.scale.z = EditorGUILayout.FloatField( reducerProperty.scale.z );
		}
		GUI.enabled = enabled;
		EditorGUILayout.EndHorizontal();
		
		EditorGUILayout.BeginHorizontal();
		GUI.enabled = enabled && thicknessEnables;
		reducerProperty.minThicknessElementType = (ElementType)EditorGUILayout.EnumPopup( "Min Thickness", reducerProperty.minThicknessElementType );
		if( reducerProperty.minThicknessElementType == ElementType.X ) {
			float v = EditorGUILayout.FloatField( reducerProperty.minThickness.x );
			reducerProperty.minThickness.x = v;
			reducerProperty.minThickness.y = v;
			reducerProperty.minThickness.z = v;
		} else {
			reducerProperty.minThickness.x = EditorGUILayout.FloatField( reducerProperty.minThickness.x );
			reducerProperty.minThickness.y = EditorGUILayout.FloatField( reducerProperty.minThickness.y );
			reducerProperty.minThickness.z = EditorGUILayout.FloatField( reducerProperty.minThickness.z );
		}
		GUI.enabled = enabled;
		EditorGUILayout.EndHorizontal();
		
		EditorGUILayout.BeginHorizontal();
		GUI.enabled = enabled && shapeEnabled;
		reducerProperty.optimizeRotationElementType = (ElementType)EditorGUILayout.EnumPopup( "Optimize Rotation", reducerProperty.optimizeRotationElementType );
		if( reducerProperty.optimizeRotationElementType == ElementType.X ) {
			bool flag = GUILayout.Toggle( reducerProperty.optimizeRotation.x, "" );
			reducerProperty.optimizeRotation.x = flag;
			reducerProperty.optimizeRotation.y = flag;
			reducerProperty.optimizeRotation.z = flag;
		} else {
			reducerProperty.optimizeRotation.x = GUILayout.Toggle( reducerProperty.optimizeRotation.x,"X" );
			reducerProperty.optimizeRotation.y = GUILayout.Toggle( reducerProperty.optimizeRotation.y,"Y" );
			reducerProperty.optimizeRotation.z = GUILayout.Toggle( reducerProperty.optimizeRotation.z,"Z" );
		}
		GUILayout.FlexibleSpace();
		GUI.enabled = enabled;
		EditorGUILayout.EndHorizontal();
		if( (reducerOption & ReducerOption.ColliderToChild) == ReducerOption.ColliderToChild ) {
			reducerProperty.colliderToChild = (ColliderToChild)EditorGUILayout.EnumPopup( "Collider To Child", reducerProperty.colliderToChild );
		}

		if( (reducerOption & ReducerOption.Advanced) == ReducerOption.Advanced ) {
			reducerProperty.viewAdvanced = EditorGUILayout.Foldout( reducerProperty.viewAdvanced, "Advanced" );
			if( reducerProperty.viewAdvanced ) {
				GUI.enabled = enabled && shapeEnabled;
				reducerProperty.offset = EditorGUILayout.Vector3Field( "Offset", reducerProperty.offset );
				GUI.enabled = enabled && thicknessEnables;
				reducerProperty.thicknessA = EditorGUILayout.Vector3Field( "ThicknessA", reducerProperty.thicknessA );
				reducerProperty.thicknessB = EditorGUILayout.Vector3Field( "ThicknessB", reducerProperty.thicknessB );
				GUI.enabled = enabled;
			}
		}

		GUI.enabled = enabled;
	}

	[System.Flags]
	public enum ColliderOption
	{
		None				= 0,
		CreateAsset			= 0x01,
	}

	public static void ColliderInspectorGUI( ColliderProperty colliderProperty, ColliderOption colliderOption )
	{
		if( colliderProperty == null ) {
			return;
		}

		colliderProperty.convex = EditorGUILayout.Toggle( "Convex", colliderProperty.convex );
		colliderProperty.isTrigger = EditorGUILayout.Toggle( "Is Trigger", colliderProperty.isTrigger );
		colliderProperty.material = EditorGUILayout.ObjectField( "Physics Material", colliderProperty.material, typeof(PhysicMaterial), false ) as PhysicMaterial;
		if( (colliderOption & ColliderOption.CreateAsset) != ColliderOption.None ) {
			colliderProperty.isCreateAsset = EditorGUILayout.Toggle( "Create Asset(4Prefab)", colliderProperty.isCreateAsset );
		}
	}

	public static void RigidbodyInspectorGUI( RigidbodyProperty rigidbodyProperty )
	{
		if( rigidbodyProperty == null ) {
			return;
		}

		bool enabled = GUI.enabled;
		rigidbodyProperty.isCreate = EditorGUILayout.Toggle( "Is Create", rigidbodyProperty.isCreate );
		GUI.enabled = enabled && rigidbodyProperty.isCreate;
		rigidbodyProperty.isKinematic = EditorGUILayout.Toggle( "Is Kinematic", rigidbodyProperty.isKinematic );
		rigidbodyProperty.viewAdvanced = EditorGUILayout.Foldout( rigidbodyProperty.viewAdvanced, "Advanced" );
		if( rigidbodyProperty.viewAdvanced ) {
			rigidbodyProperty.mass = EditorGUILayout.FloatField( "Mass", rigidbodyProperty.mass );
			rigidbodyProperty.drag = EditorGUILayout.FloatField( "Drag", rigidbodyProperty.mass );
			rigidbodyProperty.angularDrag = EditorGUILayout.FloatField( "Angular Drag", rigidbodyProperty.mass );
			rigidbodyProperty.useGravity = EditorGUILayout.Toggle( "Use Gravity", rigidbodyProperty.useGravity );
			rigidbodyProperty.interpolation = (RigidbodyInterpolation)EditorGUILayout.EnumPopup( "Interpolation", rigidbodyProperty.interpolation );
			rigidbodyProperty.collisionDetectionMode = (CollisionDetectionMode)EditorGUILayout.EnumPopup( "Collision Detection Mode", rigidbodyProperty.collisionDetectionMode );
		}
		GUI.enabled = enabled;
	}

	//--------------------------------------------------------------------------------------------------------

	static void _DumpHierarchy( System.Text.StringBuilder str, Transform trn )
	{
		if( str != null && trn != null ) {
			_DumpHierarchy( str, trn.parent );
			str.Append( trn.name + " / " );
		}
	}

	public static void DumpHierarchy( System.Text.StringBuilder str, GameObject go )
	{
		if( str != null && go != null ) {
			str.Append( "[Hierarchy] " );
			_DumpHierarchy( str, go.transform.parent );
			str.AppendLine( go.name );
		}
	}
}
