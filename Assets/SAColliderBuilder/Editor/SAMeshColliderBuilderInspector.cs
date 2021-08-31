//----------------------------------------------
// SABoneCollider
// Copyright (c) 2014 Stereoarts Nora
//----------------------------------------------
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using ShapeType						= SAColliderBuilderCommon.ShapeType;
using MeshType						= SAColliderBuilderCommon.MeshType;
using SliceMode						= SAColliderBuilderCommon.SliceMode;
using ElementType					= SAColliderBuilderCommon.ElementType;
using SplitMesh						= SAMeshColliderCommon.SplitMesh;
using SplitMode						= SAMeshColliderCommon.SplitMode;
using MeshCache						= SAMeshColliderEditorCommon.MeshCache;
using ReducerTask					= SAMeshColliderEditorCommon.ReducerTask;

using ReducerProperty				= SAColliderBuilderCommon.ReducerProperty;
using ColliderProperty				= SAColliderBuilderCommon.ColliderProperty;
using RigidbodyProperty				= SAColliderBuilderCommon.RigidbodyProperty;

using ReducerOption					= SAColliderBuilderEditorCommon.ReducerOption;
using ColliderOption				= SAColliderBuilderEditorCommon.ColliderOption;
using SplitProperty					= SAMeshColliderCommon.SplitProperty;
using SAMeshColliderProperty		= SAMeshColliderCommon.SAMeshColliderProperty;
using SAMeshColliderBuilderProperty	= SAMeshColliderCommon.SAMeshColliderBuilderProperty;

[CustomEditor(typeof(SAMeshColliderBuilder))]
public class SAMeshColliderBuilderInspector : Editor
{
	public override void OnInspectorGUI()
	{
		SAMeshColliderBuilder meshColliderBuilder = (SAMeshColliderBuilder)target;
		if( meshColliderBuilder.edittingMeshColliderBuilderProperty == null ) {
			if( meshColliderBuilder.meshColliderBuilderProperty != null ) {
				meshColliderBuilder.edittingMeshColliderBuilderProperty = meshColliderBuilder.meshColliderBuilderProperty.Copy();
			}
		}
		SAMeshColliderBuilderProperty meshColliderBuilderProperty = meshColliderBuilder.edittingMeshColliderBuilderProperty;
		if( meshColliderBuilderProperty != null ) {
			SplitProperty splitProperty = meshColliderBuilderProperty.splitProperty;
			if( splitProperty != null ) {
				GUILayout.Label( "Split", EditorStyles.boldLabel );
				// Split Material
				splitProperty.splitMaterialEnabled = EditorGUILayout.Toggle( "Split Material", splitProperty.splitMaterialEnabled );
				// Split Primitive
				splitProperty.splitPrimitiveEnabled = EditorGUILayout.Toggle( "Split Primitive", splitProperty.splitPrimitiveEnabled );
				// Split Polygon Normal
				EditorGUILayout.BeginHorizontal();
				splitProperty.splitPolygonNormalEnabled = EditorGUILayout.Toggle( "Split Polygon Normal", splitProperty.splitPolygonNormalEnabled );
				GUI.enabled = splitProperty.splitPolygonNormalEnabled;
				splitProperty.splitPolygonNormalAngle = EditorGUILayout.Slider( splitProperty.splitPolygonNormalAngle, 0.0f, 180.0f );
				GUI.enabled = true;
				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.Separator();
			GUILayout.Label( "Reducer", EditorStyles.boldLabel );
			SAColliderBuilderEditorCommon.ReducerInspectorGUI( meshColliderBuilderProperty.reducerProperty, ReducerOption.None );

			GUI.enabled = meshColliderBuilderProperty.reducerProperty.shapeType != ShapeType.None;
			EditorGUILayout.Separator();
			GUILayout.Label( "Collider", EditorStyles.boldLabel );
			SAColliderBuilderEditorCommon.ColliderInspectorGUI( meshColliderBuilderProperty.colliderProperty, ColliderOption.None );
			EditorGUILayout.Separator();
			GUILayout.Label( "Rigidbody", EditorStyles.boldLabel );
			SAColliderBuilderEditorCommon.RigidbodyInspectorGUI( meshColliderBuilderProperty.rigidbodyProperty );
			GUI.enabled = true;
		}

		EditorGUILayout.Separator();
		if( meshColliderBuilderProperty != null ) {
			meshColliderBuilderProperty.modifyNameEnabled = EditorGUILayout.Toggle( "Modify Name", meshColliderBuilderProperty.modifyNameEnabled );
		}
		meshColliderBuilder.cleanupModified = EditorGUILayout.Toggle( "Cleanup Modified", meshColliderBuilder.cleanupModified );
		meshColliderBuilder.isDebug = EditorGUILayout.Toggle( "Is Debug", meshColliderBuilder.isDebug );

		EditorGUILayout.Separator();

		EditorGUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if( GUILayout.Button("Revert") ) {
			meshColliderBuilder.edittingMeshColliderBuilderProperty = null;
		}
		if( GUILayout.Button("Cleanup") ) {
			if( meshColliderBuilder.edittingMeshColliderBuilderProperty != null ) {
				meshColliderBuilder.meshColliderBuilderProperty = meshColliderBuilder.edittingMeshColliderBuilderProperty;
				meshColliderBuilder.edittingMeshColliderBuilderProperty = null;
				Cleanup( meshColliderBuilder );
				meshColliderBuilder.cleanupModified = false;
				meshColliderBuilder.isDebug = false;
				Debug.Log("Cleanuped.");
			}
		}
		if( GUILayout.Button("Process") ) {
			if( meshColliderBuilder.edittingMeshColliderBuilderProperty != null ) {
				meshColliderBuilder.meshColliderBuilderProperty = meshColliderBuilder.edittingMeshColliderBuilderProperty;
				meshColliderBuilder.edittingMeshColliderBuilderProperty = null;
				float beginTime = Time.realtimeSinceStartup;
				Process( meshColliderBuilder );
				meshColliderBuilder.cleanupModified = false;
				meshColliderBuilder.isDebug = false;
				float endTime = Time.realtimeSinceStartup;
				Debug.Log("Processed.[" + (endTime - beginTime) + " sec]" );
			}
		}
		EditorGUILayout.EndHorizontal();
	}

	static void Process( SAMeshColliderBuilder meshColliderBuilder )
	{
		if( meshColliderBuilder == null ) {
			Debug.LogError("");
			return;
		}

		MeshFilter[] meshFilters = SAColliderBuilderEditorCommon.GetMeshFilters( meshColliderBuilder.gameObject );
		SkinnedMeshRenderer[] skinnedMeshRenderers = SAColliderBuilderEditorCommon.GetSkinnedMeshRenderers( meshColliderBuilder.gameObject );

		if( ( meshFilters == null || meshFilters.Length == 0 ) && ( skinnedMeshRenderers == null || skinnedMeshRenderers.Length == 0 ) ) {
			Debug.LogError( "Nothing MeshFilter/SkinnedMeshRenderer. Skip Processing." );
			return;
		}
		
		List<ReducerTask> reducerTasks = new List<ReducerTask>();

		if( meshFilters != null ) {
			foreach( MeshFilter meshFilter in meshFilters ) {
				Mesh mesh = SAColliderBuilderEditorCommon.GetMesh( meshFilter );
				Material[] materials = SAColliderBuilderEditorCommon.GetMaterials( meshFilter );
				MeshCache meshCahce = new MeshCache( mesh, materials );
				SAMeshColliderEditorCommon.CleanupChildSAMeshColliders( meshFilter.gameObject, meshColliderBuilder.cleanupModified );
				ProcessRoot( reducerTasks, meshCahce, meshColliderBuilder, meshFilter.gameObject );
			}
		}

		if( skinnedMeshRenderers != null ) {
			foreach( SkinnedMeshRenderer skinnedMeshRenderer in skinnedMeshRenderers ) {
				Mesh mesh = SAColliderBuilderEditorCommon.GetMesh( skinnedMeshRenderer );
				Material[] materials = SAColliderBuilderEditorCommon.GetMaterials( skinnedMeshRenderer );
				MeshCache meshCahce = new MeshCache( mesh, materials );
				SAMeshColliderEditorCommon.CleanupChildSAMeshColliders( skinnedMeshRenderer.gameObject, meshColliderBuilder.cleanupModified );
				ProcessRoot( reducerTasks, meshCahce, meshColliderBuilder, skinnedMeshRenderer.gameObject );
			}
		}

		SAMeshColliderEditorCommon.Reduce( reducerTasks, meshColliderBuilder.isDebug );
	}

	static void Cleanup( SAMeshColliderBuilder meshColliderBuilder )
	{
		if( meshColliderBuilder == null ) {
			Debug.LogError("");
			return;
		}
		
		MeshFilter[] meshFilters = SAColliderBuilderEditorCommon.GetMeshFilters( meshColliderBuilder.gameObject );
		SkinnedMeshRenderer[] skinnedMeshRenderers = SAColliderBuilderEditorCommon.GetSkinnedMeshRenderers( meshColliderBuilder.gameObject );
		
		if( ( meshFilters == null || meshFilters.Length == 0 ) && ( skinnedMeshRenderers == null || skinnedMeshRenderers.Length == 0 ) ) {
			Debug.LogError( "Nothing MeshFilter/SkinnedMeshRenderer. Skip Cleanuping." );
			return;
		}

		if( meshFilters != null ) {
			foreach( MeshFilter meshFilter in meshFilters ) {
				SAMeshColliderEditorCommon.CleanupChildSAMeshColliders( meshFilter.gameObject, meshColliderBuilder.cleanupModified );
			}
		}
		
		if( skinnedMeshRenderers != null ) {
			foreach( SkinnedMeshRenderer skinnedMeshRenderer in skinnedMeshRenderers ) {
				SAMeshColliderEditorCommon.CleanupChildSAMeshColliders( skinnedMeshRenderer.gameObject, meshColliderBuilder.cleanupModified );
			}
		}
	}

	static void ProcessRoot(
		List<ReducerTask> reducerTasks,
		MeshCache meshCache,
		SAMeshColliderBuilder meshColliderBuilder,
		GameObject parentGameObject )
	{
		if( reducerTasks == null || meshCache == null || meshColliderBuilder == null || parentGameObject == null ) {
			Debug.LogError("");
			return;
		}

		SplitMesh resplitMesh = SAMeshColliderEditorCommon.MakeRootSplitMesh( meshCache );
		if( resplitMesh == null ) {
			return;
		}

		if( meshColliderBuilder.splitMaterialEnabled ) {
			ProcessMaterial( reducerTasks, meshCache, meshColliderBuilder, parentGameObject );
		} else if( meshColliderBuilder.splitPrimitiveEnabled ) {
			ProcessPrimitive( reducerTasks, meshCache, meshColliderBuilder, parentGameObject, resplitMesh );
		} else if( meshColliderBuilder.splitPolygonNormalEnabled ) {
			SAMeshColliderEditorCommon.MakeSplitMeshTriangles( meshCache, resplitMesh );
			ProcessPolygon( reducerTasks, meshCache, meshColliderBuilder, parentGameObject, resplitMesh );
		} else {
			SAMeshCollider[] existingMeshColliders = SAMeshColliderEditorCommon.GetChildSAMeshColliders( parentGameObject );
			SAMeshCollider existingMeshCollider = SAMeshColliderEditorCommon.FindSAMeshCollider( existingMeshColliders, resplitMesh );
			if( existingMeshCollider != null && existingMeshCollider.modified ) {
				return; // Not overwrite modified SAMeshCollider.
			}

			string resplitMeshColliderName = SAMeshColliderEditorCommon.GetSAMeshColliderName_Root( parentGameObject );
			SAMeshCollider resplitMeshCollider = null;
			if( existingMeshCollider != null ) {
				resplitMeshCollider = existingMeshCollider;
				SAMeshColliderEditorCommon.SetupSAMeshCollider(
					meshColliderBuilder,
					resplitMeshCollider,
					resplitMeshColliderName );
				resplitMesh = resplitMeshCollider.splitMesh;
			} else {
				resplitMeshCollider = SAMeshColliderEditorCommon.CreateSAMeshCollider(
					meshColliderBuilder,
					parentGameObject,
					resplitMeshColliderName,
					resplitMesh,
					SplitMode.None );
			}

			SAMeshColliderEditorCommon.MakeSplitMeshTriangles( meshCache, resplitMesh );
			SAMeshColliderEditorCommon.RegistReducerTask( reducerTasks, resplitMeshCollider );
		}
	}

	static void ProcessMaterial(
		List<ReducerTask> reducerTasks,
		MeshCache meshCache,
		SAMeshColliderBuilder meshColliderBuilder,
		GameObject parentGameObject )
	{
		if( reducerTasks == null || meshCache == null || meshColliderBuilder == null || parentGameObject == null ) {
			Debug.LogError("");
			return;
		}

		SplitMesh[] resplitMeshes = SAMeshColliderEditorCommon.MakeSplitMeshesByMaterial( meshCache );
		if( resplitMeshes == null || resplitMeshes.Length == 0 ) {
			return;
		}

		SAMeshCollider[] existingMeshColliders = SAMeshColliderEditorCommon.GetChildSAMeshColliders( parentGameObject );

		Material[] materials = meshCache.materials;

		for( int i = 0; i < resplitMeshes.Length; ++i ) {
			SplitMesh resplitMesh = resplitMeshes[i];
			SAMeshCollider existingMeshCollider = SAMeshColliderEditorCommon.FindSAMeshCollider( existingMeshColliders, resplitMesh );
			if( existingMeshCollider != null && existingMeshCollider.modified ) {
				continue; // Not overwrite modified SAMeshCollider.
			}

			string resplitMeshColliderName = SAMeshColliderEditorCommon.GetSAMeshColliderName_Material( materials, i );
			SAMeshCollider resplitMeshCollider = null;
			if( existingMeshCollider != null ) {
				resplitMeshCollider = existingMeshCollider;
				SAMeshColliderEditorCommon.SetupSAMeshCollider(
					meshColliderBuilder,
					resplitMeshCollider,
					resplitMeshColliderName );
				resplitMesh = resplitMeshCollider.splitMesh;
			} else {
				resplitMeshCollider = SAMeshColliderEditorCommon.CreateSAMeshCollider(
					meshColliderBuilder,
					parentGameObject,
					resplitMeshColliderName,
					resplitMesh,
					SplitMode.Material );
			}

			if( resplitMeshCollider.splitPrimitiveEnabled ) {
				ProcessPrimitive( reducerTasks, meshCache, meshColliderBuilder, resplitMeshCollider.gameObject, resplitMeshCollider.splitMesh );
			} else if( resplitMeshCollider.splitPolygonNormalEnabled ) {
				SAMeshColliderEditorCommon.MakeSplitMeshTriangles( meshCache, resplitMesh );
				ProcessPolygon( reducerTasks, meshCache, meshColliderBuilder, resplitMeshCollider.gameObject, resplitMeshCollider.splitMesh );
			} else {
				SAMeshColliderEditorCommon.MakeSplitMeshTriangles( meshCache, resplitMesh );
				SAMeshColliderEditorCommon.RegistReducerTask( reducerTasks, resplitMeshCollider );
			}
		}
	}

	static void ProcessPrimitive(
		List<ReducerTask> reducerTasks,
		MeshCache meshCache,
		SAMeshColliderBuilder meshColliderBuilder,
		GameObject parentGameObject,
		SplitMesh parentSplitMesh )
	{
		if( reducerTasks == null || meshCache == null || meshColliderBuilder == null || parentGameObject == null || parentSplitMesh == null ) {
			Debug.LogError("");
			return;
		}

		SplitMesh[] resplitMeshes = SAMeshColliderEditorCommon.MakeSplitMeshesByPrimitive( meshCache, parentSplitMesh );
		if( resplitMeshes == null || resplitMeshes.Length == 0 ) {
			return;
		}

		SAMeshCollider[] existingMeshColliders = SAMeshColliderEditorCommon.GetChildSAMeshColliders( parentGameObject );

		for( int i = 0; i < resplitMeshes.Length; ++i ) {
			SplitMesh resplitMesh = resplitMeshes[i];
			SAMeshCollider existingMeshCollider = SAMeshColliderEditorCommon.FindSAMeshCollider( existingMeshColliders, resplitMesh );
			if( existingMeshCollider != null && existingMeshCollider.modified ) {
				continue; // Not overwrite modified SAMeshCollider.
			}

			string resplitMeshColliderName = SAMeshColliderEditorCommon.GetSAMeshColliderName_Primitive( i );
			SAMeshCollider resplitMeshCollider = null;
			if( existingMeshCollider != null ) {
				resplitMeshCollider = existingMeshCollider;
				SAMeshColliderEditorCommon.SetupSAMeshCollider(
					meshColliderBuilder,
					resplitMeshCollider,
					resplitMeshColliderName );
				resplitMesh = resplitMeshCollider.splitMesh;
			} else {
				resplitMeshCollider = SAMeshColliderEditorCommon.CreateSAMeshCollider(
					meshColliderBuilder,
					parentGameObject,
					resplitMeshColliderName,
					resplitMesh,
					SplitMode.Primitive );
			}
			
			if( resplitMeshCollider.splitPolygonNormalEnabled ) {
				SAMeshColliderEditorCommon.MakeSplitMeshTriangles( meshCache, resplitMesh );
				ProcessPolygon( reducerTasks, meshCache, meshColliderBuilder, resplitMeshCollider.gameObject, resplitMeshCollider.splitMesh );
			} else {
				SAMeshColliderEditorCommon.MakeSplitMeshTriangles( meshCache, resplitMesh );
				SAMeshColliderEditorCommon.RegistReducerTask( reducerTasks, resplitMeshCollider );
			}
		}
	}

	static void ProcessPolygon(
		List<ReducerTask> reducerTasks,
		MeshCache meshCache,
		SAMeshColliderBuilder meshColliderBuilder,
		GameObject parentGameObject,
		SplitMesh parentSplitMesh )
	{
		if( reducerTasks == null || meshCache == null || meshColliderBuilder == null || parentGameObject == null || parentSplitMesh == null ) {
			Debug.LogError("");
			return;
		}

		if( !meshColliderBuilder.splitPolygonNormalEnabled ) {
			return;
		}

		SplitMesh[] resplitMeshes = SAMeshColliderEditorCommon.MakeSplitMeshesByPolygon( meshCache, parentSplitMesh, meshColliderBuilder.splitPolygonNormalAngle );
		if( resplitMeshes == null || resplitMeshes.Length == 0 ) {
			return;
		}

		SAMeshCollider[] existingMeshColliders = SAMeshColliderEditorCommon.GetChildSAMeshColliders( parentGameObject );

		for( int i = 0; i < resplitMeshes.Length; ++i ) {
			SplitMesh resplitMesh = resplitMeshes[i];
			SAMeshCollider existingMeshCollider = SAMeshColliderEditorCommon.FindSAMeshCollider( existingMeshColliders, resplitMesh );
			if( existingMeshCollider != null && existingMeshCollider.modified ) {
				continue; // Not overwrite modified SAMeshCollider.
			}

			string resplitMeshColliderName = SAMeshColliderEditorCommon.GetSAMeshColliderName_Polygon( i );
			SAMeshCollider resplitMeshCollider = null;
			if( existingMeshCollider != null ) {
				resplitMeshCollider = existingMeshCollider;
				SAMeshColliderEditorCommon.SetupSAMeshCollider(
					meshColliderBuilder,
					resplitMeshCollider,
					resplitMeshColliderName );
				resplitMesh = resplitMeshCollider.splitMesh;
			} else {
				resplitMeshCollider = SAMeshColliderEditorCommon.CreateSAMeshCollider(
					meshColliderBuilder,
					parentGameObject,
					resplitMeshColliderName,
					resplitMesh,
					SplitMode.Polygon );
			}
			
			SAMeshColliderEditorCommon.SalvageMeshByPolygon( resplitMesh );
			SAMeshColliderEditorCommon.RegistReducerTask( reducerTasks, resplitMeshCollider );
		}
	}
}
