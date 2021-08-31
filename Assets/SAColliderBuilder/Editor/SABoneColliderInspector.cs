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

using ReducerProperty				= SAColliderBuilderCommon.ReducerProperty;
using ColliderProperty				= SAColliderBuilderCommon.ColliderProperty;
using RigidbodyProperty				= SAColliderBuilderCommon.RigidbodyProperty;

using BoneProperty					= SABoneColliderCommon.BoneProperty;
using SplitProperty					= SABoneColliderCommon.SplitProperty;
using SABoneColliderProperty		= SABoneColliderCommon.SABoneColliderProperty;
using SABoneColliderBuilderProperty	= SABoneColliderCommon.SABoneColliderBuilderProperty;

using ReducerOption					= SAColliderBuilderEditorCommon.ReducerOption;
using ColliderOption				= SAColliderBuilderEditorCommon.ColliderOption;
using BoneMeshCache					= SABoneColliderEditorCommon.BoneMeshCache;
using ReducerTask					= SABoneColliderEditorCommon.ReducerTask;

[CustomEditor(typeof(SABoneCollider))]
public class SABoneColliderInspector : Editor
{
	public override void OnInspectorGUI()
	{
		SABoneCollider boneCollider = (SABoneCollider)target;
		if( boneCollider.edittingBoneColliderProperty == null ) {
			if( boneCollider.boneColliderProperty != null ) {
				boneCollider.edittingBoneColliderProperty = boneCollider.boneColliderProperty.Copy();
			}
		}
		SABoneColliderProperty boneColliderProperty = boneCollider.edittingBoneColliderProperty;
		if( boneColliderProperty != null ) {
			SplitProperty splitProperty = boneColliderProperty.splitProperty;
			if( splitProperty != null ) {
				GUILayout.Label( "Split", EditorStyles.boldLabel );
				SABoneColliderEditorCommon.SplitInspectorGUI( splitProperty );
			}
			
			EditorGUILayout.Separator();
			GUILayout.Label( "Reducer", EditorStyles.boldLabel );
			SAColliderBuilderEditorCommon.ReducerInspectorGUI( boneColliderProperty.reducerProperty,
					ReducerOption.Advanced | ReducerOption.ColliderToChild );
			
			GUI.enabled = boneColliderProperty.reducerProperty.shapeType != ShapeType.None;
			EditorGUILayout.Separator();
			GUILayout.Label( "Collider", EditorStyles.boldLabel );
			SAColliderBuilderEditorCommon.ColliderInspectorGUI( boneColliderProperty.colliderProperty, ColliderOption.CreateAsset );
			EditorGUILayout.Separator();
			GUILayout.Label( "Rigidbody", EditorStyles.boldLabel );
			SAColliderBuilderEditorCommon.RigidbodyInspectorGUI( boneColliderProperty.rigidbodyProperty );
			GUI.enabled = true;
		}
		
		EditorGUILayout.Separator();

		if( boneColliderProperty != null ) {
			BoneProperty boneProperty = boneColliderProperty.boneProperty;
			if( boneProperty != null ) {
				boneProperty.recursivery = EditorGUILayout.Toggle( "Recursivery", boneProperty.recursivery );
			}
		}

		boneCollider.cleanupModified = EditorGUILayout.Toggle( "Cleanup Modified", boneCollider.cleanupModified );
		boneCollider.isDebug = EditorGUILayout.Toggle( "Is Debug", boneCollider.isDebug );
		
		EditorGUILayout.Separator();
		
		EditorGUILayout.BeginHorizontal();
		if( GUILayout.Button("Set Default") ) {
			if( boneCollider.defaultBoneColliderProperty != null ) {
				float beginTime = Time.realtimeSinceStartup;
				boneCollider.boneColliderProperty = boneCollider.defaultBoneColliderProperty.Copy();
				boneCollider.edittingBoneColliderProperty = null;
				Process( boneCollider );
				boneCollider.cleanupModified = false;
				boneCollider.isDebug = false;
				SABoneColliderEditorCommon.UnmarkManualProcessingToParent( boneCollider );
				boneCollider.ResetModifyName();
				float endTime = Time.realtimeSinceStartup;
				Debug.Log("Processed.[" + (endTime - beginTime) + " sec]" );
			}
		}
		GUILayout.FlexibleSpace();
		if( GUILayout.Button("Revert") ) {
			boneCollider.edittingBoneColliderProperty = null;
		}
		if( GUILayout.Button("Cleanup") ) {
			if( boneCollider.edittingBoneColliderProperty != null ) {
				boneCollider.boneColliderProperty = boneCollider.edittingBoneColliderProperty;
				boneCollider.edittingBoneColliderProperty = null;
				Cleanup( boneCollider );
				boneCollider.cleanupModified = false;
				boneCollider.isDebug = false;
				Debug.Log("Cleanuped.");
			}
		}
		if( GUILayout.Button("Process") ) {
			if( boneCollider.edittingBoneColliderProperty != null ) {
				boneCollider.boneColliderProperty = boneCollider.edittingBoneColliderProperty;
				boneCollider.edittingBoneColliderProperty = null;
				float beginTime = Time.realtimeSinceStartup;
				Process( boneCollider );
				boneCollider.cleanupModified = false;
				float endTime = Time.realtimeSinceStartup;
				Debug.Log("Processed.[" + (endTime - beginTime) + " sec]" );
			}
		}
		EditorGUILayout.EndHorizontal();
	}

	static void Process( SABoneCollider boneCollider )
	{
		if( boneCollider == null || boneCollider.colliderProperty == null ) {
			Debug.LogError("");
			return;
		}

		GameObject rootGameObject = SABoneColliderEditorCommon.GetSABoneColliderRootGameObject( boneCollider );
		if( rootGameObject == null ) {
			Debug.LogError("");
			return;
		}

		string collidersPath = null;
		if( boneCollider.colliderProperty.isCreateAsset ) {
			collidersPath = SABoneColliderEditorCommon.GetCollidersPath( rootGameObject );
			if( string.IsNullOrEmpty( collidersPath ) ) {
				Debug.LogWarning( "Not found collidersPath. Can't create asset." );
			}
		}
		HashSet<Transform> boneHashSet = SABoneColliderEditorCommon.GetBoneHashSet( rootGameObject );

		BoneMeshCache boneMeshCache = new BoneMeshCache();
		boneMeshCache.Process( rootGameObject );

		List<ReducerTask> reducerTasks = new List<ReducerTask>();

		SABoneColliderEditorCommon.CleanupSABoneCollider( boneCollider );
		SABoneColliderEditorCommon.RegistReducerTask( reducerTasks, boneCollider, boneMeshCache );

		SABoneColliderEditorCommon.MarkManualProcessingToParent( boneCollider );

		if( boneCollider.recursivery ) {
			foreach( Transform childTransform in boneCollider.gameObject.transform ) {
				if( SAColliderBuilderEditorCommon.IsRootTransform( childTransform ) ) {
					// Nothing.
				} else {
					_ProcessTransform( childTransform, reducerTasks, boneCollider, boneHashSet, boneMeshCache );
				}
			}
		}

		SABoneColliderEditorCommon.Reduce( reducerTasks, collidersPath, boneCollider.isDebug );
	}

	static void _ProcessTransform(
		Transform transform,
		List<ReducerTask> reducerTasks,
		SABoneCollider rootBoneCollider,
		HashSet<Transform> boneHashSet,
		BoneMeshCache boneMeshCache )
	{
		if( transform == null || reducerTasks == null || rootBoneCollider == null || boneHashSet == null || boneMeshCache == null ) {
			Debug.LogError("");
			return;
		}

		if( boneHashSet.Contains( transform ) ) {
			SABoneCollider boneCollider = transform.gameObject.GetComponent< SABoneCollider >();
			if( boneCollider != null ) {
				if( rootBoneCollider.cleanupModified || !boneCollider.modified ) {
					SABoneColliderEditorCommon.DestroySABoneCollider( boneCollider );
					boneCollider = null;
				}
				if( boneCollider != null && boneCollider.recursivery ) {
					return; // Skip modified children.
				}
			}
			
			if( boneCollider == null ) { // Don't overwrite modified.
				boneCollider = SABoneColliderEditorCommon.CreateSABoneCollider( transform.gameObject, rootBoneCollider );
				SABoneColliderEditorCommon.RegistReducerTask( reducerTasks, boneCollider, boneMeshCache );
			}
		}
		
		foreach( Transform childTransform in transform ) {
			if( SAColliderBuilderEditorCommon.IsRootTransform( childTransform ) ) {
				// Nothing.
			} else {
				_ProcessTransform( childTransform, reducerTasks, rootBoneCollider, boneHashSet, boneMeshCache );
			}
		}
	}

	static void Cleanup( SABoneCollider boneCollider )
	{
		if( boneCollider == null ) {
			Debug.LogError("");
			return;
		}

		GameObject rootGameObject = SABoneColliderEditorCommon.GetSABoneColliderRootGameObject( boneCollider );
		if( rootGameObject == null ) {
			Debug.LogError("");
			return;
		}

		HashSet<Transform> boneHashSet = SABoneColliderEditorCommon.GetBoneHashSet( rootGameObject );

		bool cleanupModified = boneCollider.cleanupModified;
		SABoneColliderEditorCommon.CleanupSABoneCollider( boneCollider );

		SABoneColliderEditorCommon.MarkManualProcessingToParent( boneCollider );

		if( boneCollider.recursivery ) {
			foreach( Transform childTransform in boneCollider.gameObject.transform ) {
				if( SAColliderBuilderEditorCommon.IsRootTransform( childTransform ) ) {
					// Nothing.
				} else {
					_CleanupTransform( childTransform, cleanupModified, boneHashSet );
				}
			}
		}
	}

	static void _CleanupTransform( Transform transform, bool cleanupModified, HashSet<Transform> boneHashSet )
	{
		if( transform == null ) {
			return;
		}

		if( boneHashSet.Contains( transform ) ) {
			SABoneCollider boneCollider = transform.gameObject.GetComponent<SABoneCollider>();
			if( boneCollider != null ) {
				if( cleanupModified || !boneCollider.modified ) {
					SABoneColliderEditorCommon.DestroySABoneCollider( boneCollider );
					boneCollider = null;
				}
				if( boneCollider != null && boneCollider.recursivery ) {
					return; // Don't clean manually processed.
				}
			}
		}

		foreach( Transform childTransform in transform ) {
			if( SAColliderBuilderEditorCommon.IsRootTransform( childTransform ) ) {
				// Nothing.
			} else {
				_CleanupTransform( childTransform, cleanupModified, boneHashSet );
			}
		}
	}
}