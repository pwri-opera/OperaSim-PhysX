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

[CustomEditor(typeof(SAMeshCollider))]
public class SAMeshColliderInspector : Editor
{
	public override void OnInspectorGUI()
	{
		SAMeshCollider meshCollider = (SAMeshCollider)target;
		if( meshCollider.edittingMeshCollidertProperty == null ) {
			if( meshCollider.meshColliderProperty != null ) {
				meshCollider.edittingMeshCollidertProperty = meshCollider.meshColliderProperty.Copy();
			}
		}
		SAMeshColliderProperty meshColliderProperty = meshCollider.edittingMeshCollidertProperty;
		if( meshColliderProperty != null ) {
			SplitProperty splitProperty = meshColliderProperty.splitProperty;
			if( splitProperty != null ) {
				GUILayout.Label( "Split", EditorStyles.boldLabel );
				// Split Material
				GUI.enabled = ((int)meshCollider.splitMode <= (int)SplitMode.None);
				splitProperty.splitMaterialEnabled = EditorGUILayout.Toggle( "Split Material", splitProperty.splitMaterialEnabled );
				GUI.enabled = true;
				// Split Primitive
				GUI.enabled = ((int)meshCollider.splitMode <= (int)SplitMode.Material);
				splitProperty.splitPrimitiveEnabled = EditorGUILayout.Toggle( "Split Primitive", splitProperty.splitPrimitiveEnabled );
				GUI.enabled = true;
				// Split Polygon Normal
				EditorGUILayout.BeginHorizontal();
				GUI.enabled = ((int)meshCollider.splitMode <= (int)SplitMode.Primitive);
				splitProperty.splitPolygonNormalEnabled = EditorGUILayout.Toggle( "Split Polygon Normal", splitProperty.splitPolygonNormalEnabled );
				GUI.enabled = GUI.enabled && meshCollider.splitPolygonNormalEnabled;
				splitProperty.splitPolygonNormalAngle = EditorGUILayout.Slider( splitProperty.splitPolygonNormalAngle, 0.0f, 180.0f );
				GUI.enabled = true;
				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.Separator();
			GUILayout.Label( "Reducer", EditorStyles.boldLabel );
			SAColliderBuilderEditorCommon.ReducerInspectorGUI( meshColliderProperty.reducerProperty, ReducerOption.Advanced );

			GUI.enabled = meshColliderProperty.reducerProperty.shapeType != ShapeType.None;
			EditorGUILayout.Separator();
			GUILayout.Label( "Collider", EditorStyles.boldLabel );
			SAColliderBuilderEditorCommon.ColliderInspectorGUI( meshColliderProperty.colliderProperty, ColliderOption.None );
			EditorGUILayout.Separator();
			GUILayout.Label( "Rigidbody", EditorStyles.boldLabel );
			SAColliderBuilderEditorCommon.RigidbodyInspectorGUI( meshColliderProperty.rigidbodyProperty );
			GUI.enabled = true;
		}

		EditorGUILayout.Separator();
		meshCollider.cleanupModified = EditorGUILayout.Toggle( "Cleanup Modified", meshCollider.cleanupModified );
		meshCollider.isDebug = EditorGUILayout.Toggle( "Is Debug", meshCollider.isDebug );
		
		EditorGUILayout.Separator();
		
		EditorGUILayout.BeginHorizontal();
		if( GUILayout.Button("Set Default") ) {
			if( meshCollider.defaultMeshColliderProperty != null ) {
				float beginTime = Time.realtimeSinceStartup;
				meshCollider.meshColliderProperty = meshCollider.defaultMeshColliderProperty.Copy();
				meshCollider.edittingMeshCollidertProperty = null;
				Process( meshCollider );
				meshCollider.cleanupModified = false;
				SAMeshColliderEditorCommon.UnmarkManualProcessingToParent( meshCollider );
				meshCollider.ResetModifyName();
				float endTime = Time.realtimeSinceStartup;
				Debug.Log("Processed.[" + (endTime - beginTime) + " sec]" );
			}
		}
		GUILayout.FlexibleSpace();
		if( GUILayout.Button("Revert") ) {
			meshCollider.edittingMeshCollidertProperty = null;
		}
		if( GUILayout.Button("Cleanup") ) {
			if( meshCollider.edittingMeshCollidertProperty != null ) {
				meshCollider.meshColliderProperty = meshCollider.edittingMeshCollidertProperty;
				meshCollider.edittingMeshCollidertProperty = null;
				Cleanup( meshCollider );
				meshCollider.cleanupModified = false;
				meshCollider.isDebug = false;
				Debug.Log("Cleanuped.");
			}
		}
		if( GUILayout.Button("Process") ) {
			if( meshCollider.edittingMeshCollidertProperty != null ) {
				float beginTime = Time.realtimeSinceStartup;
				meshCollider.meshColliderProperty = meshCollider.edittingMeshCollidertProperty;
				meshCollider.edittingMeshCollidertProperty = null;
				Process( meshCollider );
				meshCollider.cleanupModified = false;
				meshCollider.isDebug = false;
				float endTime = Time.realtimeSinceStartup;
				Debug.Log("Processed.[" + (endTime - beginTime) + " sec]" );
			}
		}
		EditorGUILayout.EndHorizontal();
	}

	static void Process( SAMeshCollider meshCollider )
	{
		if( meshCollider == null ) {
			Debug.LogError("");
			return;
		}

		Cleanup( meshCollider );

		MeshCache meshCache = null;
		if( meshCollider.splitMode == SplitMode.None ||
		    meshCollider.splitMode == SplitMode.Material ||
			meshCollider.splitMode == SplitMode.Primitive ) {
			meshCache = SAMeshColliderEditorCommon.GetParentMeshCache( meshCollider );
			if( meshCache == null ) {
				Debug.LogError("Mesh not found:" + meshCollider.name);
				return;
			}
		}

		List<ReducerTask> reducerTasks = new List<ReducerTask>();

		switch( meshCollider.splitMode ) {
		case SplitMode.None:
			ProcessRoot( reducerTasks, meshCache, meshCollider );
			break;
		case SplitMode.Material:
			ProcessMaterial( reducerTasks, meshCache, meshCollider );
			break;
		case SplitMode.Primitive:
			ProcessPrimitive( reducerTasks, meshCache, meshCollider );
			break;
		case SplitMode.Polygon:
			ProcessPolygon( reducerTasks, meshCollider );
			break;
		}

		SAMeshColliderEditorCommon.Reduce( reducerTasks, meshCollider.isDebug );
	}

	static void ProcessRoot( List<ReducerTask> reducerTasks, MeshCache meshCache, SAMeshCollider meshCollider )
	{
		if( reducerTasks == null || meshCache == null || meshCollider == null ) {
			Debug.LogError("");
			return;
		}

		SplitMesh splitMesh = meshCollider.splitMesh;
		if( splitMesh == null ) {
			Debug.LogError("");
			return;
		}
		
		SAMeshColliderEditorCommon.MarkManualProcessingToParent( meshCollider );

		if( meshCollider.splitMaterialEnabled ) {
			SplitMaterial( reducerTasks, meshCache, meshCollider, meshCollider );
		} else if( meshCollider.splitPrimitiveEnabled ) {
			SplitPrimitive( reducerTasks, meshCache, meshCollider, meshCollider );
		} else if( meshCollider.splitPolygonNormalEnabled ) {
			SAMeshColliderEditorCommon.MakeSplitMeshTriangles( meshCache, splitMesh );
			SplitPolygon( reducerTasks, meshCache, meshCollider, meshCollider );
		} else {
			SAMeshColliderEditorCommon.MakeSplitMeshTriangles( meshCache, splitMesh );
			SAMeshColliderEditorCommon.RegistReducerTask( reducerTasks, meshCollider );
		}
	}

	static void SplitMaterial( List<ReducerTask> reducerTasks, MeshCache meshCache, SAMeshCollider parentMeshCollider, SAMeshCollider rootMeshCollider )
	{
		if( reducerTasks == null || meshCache == null || parentMeshCollider == null || rootMeshCollider == null ) {
			Debug.LogError("");
			return;
		}

		SplitMesh parentSplitMesh = parentMeshCollider.splitMesh;
		if( parentSplitMesh == null ) {
			Debug.LogError("");
			return;
		}
		
		SAMeshColliderEditorCommon.CleanupSelfSAMeshCollider( parentMeshCollider );
		
		SAMeshCollider[] existingMeshColliders = SAMeshColliderEditorCommon.GetChildSAMeshColliders( parentMeshCollider.gameObject );
		
		SplitMesh[] resplitMeshes = SAMeshColliderEditorCommon.MakeSplitMeshesByMaterial( meshCache );
		
		if( resplitMeshes == null || resplitMeshes.Length == 0 ) {
			return;
		}

		Material[] materials = meshCache.materials;

		for( int i = 0; i < resplitMeshes.Length; ++i ) {
			SplitMesh resplitMesh = resplitMeshes[i];
			SAMeshCollider existingMeshCollider = SAMeshColliderEditorCommon.FindSAMeshCollider( existingMeshColliders, resplitMesh );
			if( existingMeshCollider != null && existingMeshCollider.modified ) {
				continue;
			}

			string resplitMeshColliderName = SAMeshColliderEditorCommon.GetSAMeshColliderName_Material( materials, i );
			SAMeshCollider resplitMeshCollider = null;
			if( existingMeshCollider != null ) {
				resplitMeshCollider = existingMeshCollider;
				SAMeshColliderEditorCommon.SetupSAMeshCollider(
					parentMeshCollider,
					resplitMeshCollider,
					resplitMeshColliderName );
				resplitMesh = resplitMeshCollider.splitMesh;
			} else {
				resplitMeshCollider = SAMeshColliderEditorCommon.CreateSAMeshCollider(
					parentMeshCollider,
					resplitMeshColliderName,
					resplitMesh,
					SplitMode.Primitive );
			}

			if( resplitMeshCollider.splitPrimitiveEnabled ) {
				SplitPrimitive( reducerTasks, meshCache, resplitMeshCollider, rootMeshCollider );
			} else if( resplitMeshCollider.splitPolygonNormalEnabled ) {
				SAMeshColliderEditorCommon.MakeSplitMeshTriangles( meshCache, resplitMesh );
				SplitPolygon( reducerTasks, meshCache, resplitMeshCollider, rootMeshCollider );
			} else {
				SAMeshColliderEditorCommon.MakeSplitMeshTriangles( meshCache, resplitMesh );
				SAMeshColliderEditorCommon.RegistReducerTask( reducerTasks, resplitMeshCollider );
			}
		}
	}

	static void ProcessMaterial( List<ReducerTask> reducerTasks, MeshCache meshCache, SAMeshCollider meshCollider )
	{
		if( reducerTasks == null || meshCache == null || meshCollider == null ) {
			Debug.LogError("");
			return;
		}

		SplitMesh splitMesh = meshCollider.splitMesh;
		if( splitMesh == null ) {
			Debug.LogError("");
			return;
		}

		SAMeshColliderEditorCommon.MarkManualProcessingToParent( meshCollider );

		if( meshCollider.splitPrimitiveEnabled ) {
			SplitPrimitive( reducerTasks, meshCache, meshCollider, meshCollider );
		} else if( meshCollider.splitPolygonNormalEnabled ) {
			SAMeshColliderEditorCommon.MakeSplitMeshTriangles( meshCache, splitMesh );
			SplitPolygon( reducerTasks, meshCache, meshCollider, meshCollider );
		} else {
			SAMeshColliderEditorCommon.MakeSplitMeshTriangles( meshCache, splitMesh );
			SAMeshColliderEditorCommon.RegistReducerTask( reducerTasks, meshCollider );
		}
	}

	static void SplitPrimitive( List<ReducerTask> reducerTasks, MeshCache meshCache, SAMeshCollider parentMeshCollider, SAMeshCollider rootMeshCollider )
	{
		if( reducerTasks == null || meshCache == null || parentMeshCollider == null || rootMeshCollider == null ) {
			Debug.LogError("");
			return;
		}

		SplitMesh parentSplitMesh = parentMeshCollider.splitMesh;
		if( parentSplitMesh == null ) {
			Debug.LogError("");
			return;
		}

		SAMeshColliderEditorCommon.CleanupSelfSAMeshCollider( parentMeshCollider );
		
		SAMeshCollider[] existingMeshColliders = SAMeshColliderEditorCommon.GetChildSAMeshColliders( parentMeshCollider.gameObject );
		
		SplitMesh[] resplitMeshes = SAMeshColliderEditorCommon.MakeSplitMeshesByPrimitive( meshCache, parentSplitMesh );

		if( resplitMeshes == null || resplitMeshes.Length == 0 ) {
			return;
		}

		for( int i = 0; i < resplitMeshes.Length; ++i ) {
			SplitMesh resplitMesh = resplitMeshes[i];
			SAMeshCollider existingMeshCollider = SAMeshColliderEditorCommon.FindSAMeshCollider( existingMeshColliders, resplitMesh );
			if( existingMeshCollider != null && existingMeshCollider.modified ) {
				continue;
			}

			string resplitMeshColliderName = SAMeshColliderEditorCommon.GetSAMeshColliderName_Primitive( i );
			SAMeshCollider resplitMeshCollider = null;
			if( existingMeshCollider != null ) {
				resplitMeshCollider = existingMeshCollider;
				SAMeshColliderEditorCommon.SetupSAMeshCollider(
					parentMeshCollider,
					resplitMeshCollider,
					resplitMeshColliderName );
				resplitMesh = resplitMeshCollider.splitMesh;
			} else {
				resplitMeshCollider = SAMeshColliderEditorCommon.CreateSAMeshCollider(
					parentMeshCollider,
					resplitMeshColliderName,
					resplitMesh,
					SplitMode.Primitive );
			}

			if( resplitMeshCollider.splitPolygonNormalEnabled ) {
				SAMeshColliderEditorCommon.MakeSplitMeshTriangles( meshCache, resplitMesh );
				SplitPolygon( reducerTasks, meshCache, resplitMeshCollider, rootMeshCollider );
			} else {
				SAMeshColliderEditorCommon.MakeSplitMeshTriangles( meshCache, resplitMesh );
				SAMeshColliderEditorCommon.RegistReducerTask( reducerTasks, resplitMeshCollider );
			}
		}
	}

	static void ProcessPrimitive( List<ReducerTask> reducerTasks, MeshCache meshCache, SAMeshCollider meshCollider )
	{
		if( reducerTasks == null || meshCache == null || meshCollider == null ) {
			Debug.LogError("");
			return;
		}
		
		SplitMesh splitMesh = meshCollider.splitMesh;
		if( splitMesh == null ) {
			Debug.LogError("");
			return;
		}

		SAMeshColliderEditorCommon.MarkManualProcessingToParent( meshCollider );

		if( meshCollider.splitPolygonNormalEnabled ) {
			SAMeshColliderEditorCommon.MakeSplitMeshTriangles( meshCache, splitMesh );
			SplitPolygon( reducerTasks, meshCache, meshCollider, meshCollider );
		} else {
			SAMeshColliderEditorCommon.MakeSplitMeshTriangles( meshCache, splitMesh );
			SAMeshColliderEditorCommon.RegistReducerTask( reducerTasks, meshCollider );
		}
	}

	static void SplitPolygon( List<ReducerTask> reducerTasks, MeshCache meshCache, SAMeshCollider parentMeshCollider, SAMeshCollider rootMeshCollider )
	{
		if( reducerTasks == null || meshCache == null || parentMeshCollider == null || rootMeshCollider == null ) {
			Debug.LogError("");
			return;
		}

		SplitMesh parentSplitMesh = parentMeshCollider.splitMesh;
		if( parentSplitMesh == null ) {
			Debug.LogError("");
			return;
		}

		SAMeshColliderEditorCommon.CleanupSelfSAMeshCollider( parentMeshCollider );

		SAMeshCollider[] existingMeshColliders = SAMeshColliderEditorCommon.GetChildSAMeshColliders( parentMeshCollider.gameObject );

		SplitMesh[] resplitMeshes = SAMeshColliderEditorCommon.MakeSplitMeshesByPolygon( meshCache, parentSplitMesh, rootMeshCollider.splitPolygonNormalAngle );

		if( resplitMeshes == null || resplitMeshes.Length == 0 ) {
			return;
		}

		for( int i = 0; i < resplitMeshes.Length; ++i ) {
			SplitMesh resplitMesh = resplitMeshes[i];
			SAMeshCollider existingMeshCollider = SAMeshColliderEditorCommon.FindSAMeshCollider( existingMeshColliders, resplitMesh );
			if( existingMeshCollider != null && existingMeshCollider.modified ) {
				continue;
			}

			string resplitMeshColliderName = SAMeshColliderEditorCommon.GetSAMeshColliderName_Polygon( i );
			SAMeshCollider resplitMeshCollider = null;
			if( existingMeshCollider != null ) {
				resplitMeshCollider = existingMeshCollider;
				SAMeshColliderEditorCommon.SetupSAMeshCollider(
					parentMeshCollider,
					resplitMeshCollider,
					resplitMeshColliderName );
				resplitMesh = resplitMeshCollider.splitMesh;
			} else {
				resplitMeshCollider = SAMeshColliderEditorCommon.CreateSAMeshCollider(
					parentMeshCollider,
					resplitMeshColliderName,
					resplitMesh,
					SplitMode.Polygon );
			}

			SAMeshColliderEditorCommon.SalvageMeshByPolygon( resplitMesh );
			SAMeshColliderEditorCommon.RegistReducerTask( reducerTasks, resplitMeshCollider );
		}
	}

	static void ProcessPolygon( List<ReducerTask> reducerTasks, SAMeshCollider meshCollider )
	{
		if( reducerTasks == null || meshCollider == null ) {
			Debug.LogError("");
			return;
		}
		
		SplitMesh splitMesh = meshCollider.splitMesh;
		if( splitMesh == null ) {
			Debug.LogError("");
			return;
		}

		SAMeshColliderEditorCommon.MarkManualProcessingToParent( meshCollider );

		SAMeshColliderEditorCommon.SalvageMeshByPolygon( splitMesh );
		SAMeshColliderEditorCommon.RegistReducerTask( reducerTasks, meshCollider );
	}

	static void Cleanup( SAMeshCollider meshCollider )
	{
		if( meshCollider == null ) {
			Debug.LogError("");
			return;
		}

		SAMeshColliderEditorCommon.CleanupChildSAMeshColliders(
			meshCollider.gameObject,
			meshCollider.cleanupModified );

		SAMeshColliderEditorCommon.CleanupSelfSAMeshCollider( meshCollider );

		SAMeshColliderEditorCommon.MarkManualProcessingToParent( meshCollider );
	}
}
