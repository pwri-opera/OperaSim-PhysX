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

using SplitProperty					= SABoneColliderCommon.SplitProperty;
using SABoneColliderProperty		= SABoneColliderCommon.SABoneColliderProperty;
using SABoneColliderBuilderProperty	= SABoneColliderCommon.SABoneColliderBuilderProperty;

using ReducerOption					= SAColliderBuilderEditorCommon.ReducerOption;
using ColliderOption				= SAColliderBuilderEditorCommon.ColliderOption;
using ReducerTask					= SABoneColliderEditorCommon.ReducerTask;

[CustomEditor(typeof(SABoneColliderBuilder))]
public class SABoneColliderBuilderInspector : Editor
{
	public override void OnInspectorGUI()
	{
		SABoneColliderBuilder boneColliderBuilder = (SABoneColliderBuilder)target;
		if( boneColliderBuilder.edittingBoneColliderBuilderProperty == null ) {
			if( boneColliderBuilder.boneColliderBuilderProperty != null ) {
				boneColliderBuilder.edittingBoneColliderBuilderProperty = boneColliderBuilder.boneColliderBuilderProperty.Copy();
			}
		}
		SABoneColliderBuilderProperty boneColliderBuilderProperty = boneColliderBuilder.edittingBoneColliderBuilderProperty;
		if( boneColliderBuilderProperty != null ) {
			SplitProperty splitProperty = boneColliderBuilderProperty.splitProperty;
			if( splitProperty != null ) {
				GUILayout.Label( "Split", EditorStyles.boldLabel );
				SABoneColliderEditorCommon.SplitInspectorGUI( splitProperty );
			}
			
			EditorGUILayout.Separator();
			GUILayout.Label( "Reducer", EditorStyles.boldLabel );
			SAColliderBuilderEditorCommon.ReducerInspectorGUI( boneColliderBuilderProperty.reducerProperty,
			                                                  ReducerOption.ColliderToChild );
			
			GUI.enabled = boneColliderBuilderProperty.reducerProperty.shapeType != ShapeType.None;
			EditorGUILayout.Separator();
			GUILayout.Label( "Collider", EditorStyles.boldLabel );
			SAColliderBuilderEditorCommon.ColliderInspectorGUI( boneColliderBuilderProperty.colliderProperty, ColliderOption.CreateAsset );
			EditorGUILayout.Separator();
			GUILayout.Label( "Rigidbody", EditorStyles.boldLabel );
			SAColliderBuilderEditorCommon.RigidbodyInspectorGUI( boneColliderBuilderProperty.rigidbodyProperty );
			GUI.enabled = true;
		}
		
		EditorGUILayout.Separator();
		if( boneColliderBuilderProperty != null ) {
			boneColliderBuilderProperty.modifyNameEnabled = EditorGUILayout.Toggle( "Modify Name", boneColliderBuilderProperty.modifyNameEnabled );
		}
		boneColliderBuilder.cleanupModified = EditorGUILayout.Toggle( "Cleanup Modified", boneColliderBuilder.cleanupModified );
		boneColliderBuilder.isDebug = EditorGUILayout.Toggle( "Is Debug", boneColliderBuilder.isDebug );
		
		EditorGUILayout.Separator();
		
		EditorGUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if( GUILayout.Button("Revert") ) {
			boneColliderBuilder.edittingBoneColliderBuilderProperty = null;
		}
		if( GUILayout.Button("Cleanup") ) {
			if( boneColliderBuilder.edittingBoneColliderBuilderProperty != null ) {
				boneColliderBuilder.boneColliderBuilderProperty = boneColliderBuilder.edittingBoneColliderBuilderProperty;
				boneColliderBuilder.edittingBoneColliderBuilderProperty = null;
				Cleanup( boneColliderBuilder );
				boneColliderBuilder.cleanupModified = false;
				boneColliderBuilder.isDebug = false;
				Debug.Log("Cleanuped.");
			}
		}
		if( GUILayout.Button("Process") ) {
			if( boneColliderBuilder.edittingBoneColliderBuilderProperty != null ) {
				boneColliderBuilder.boneColliderBuilderProperty = boneColliderBuilder.edittingBoneColliderBuilderProperty;
				boneColliderBuilder.edittingBoneColliderBuilderProperty = null;
				float beginTime = Time.realtimeSinceStartup;
				Process( boneColliderBuilder );
				boneColliderBuilder.cleanupModified = false;
				boneColliderBuilder.isDebug = false;
				float endTime = Time.realtimeSinceStartup;
				Debug.Log("Processed.[" + (endTime - beginTime) + " sec]" );
			}
		}
		EditorGUILayout.EndHorizontal();
	}

	static void Process( SABoneColliderBuilder boneColliderBuilder )
	{
		if( boneColliderBuilder == null || boneColliderBuilder.colliderProperty == null ) {
			Debug.LogError("");
			return;
		}

		string collidersPath = null;
		if( boneColliderBuilder.colliderProperty.isCreateAsset ) {
			collidersPath = SABoneColliderEditorCommon.GetCollidersPath( boneColliderBuilder.gameObject );
			if( string.IsNullOrEmpty( collidersPath ) ) {
				Debug.LogWarning( "Not found collidersPath. Can't create asset." );
			}
		}
		HashSet<Transform> boneHashSet = SABoneColliderEditorCommon.GetBoneHashSet( boneColliderBuilder.gameObject );

		SABoneColliderEditorCommon.BoneMeshCache boneMeshCache = new SABoneColliderEditorCommon.BoneMeshCache();
		boneMeshCache.Process( boneColliderBuilder.transform.gameObject );

		List<ReducerTask> reducerTasks = new List<ReducerTask>();
		foreach( Transform transform in boneColliderBuilder.transform ) {
			if( SAColliderBuilderEditorCommon.IsRootTransform( transform ) ) {
				// Nothing.
			} else {
				_ProcessTransform( transform, reducerTasks, boneColliderBuilder, boneHashSet, boneMeshCache );
			}
		}

		SABoneColliderEditorCommon.Reduce( reducerTasks, collidersPath, boneColliderBuilder.isDebug );
	}

	static void _ProcessTransform(
		Transform transform,
		List<ReducerTask> reducerTasks,
		SABoneColliderBuilder boneColliderBuilder,
		HashSet<Transform> boneHashSet,
		SABoneColliderEditorCommon.BoneMeshCache boneMeshCache )
	{
		if( transform == null || reducerTasks == null || boneColliderBuilder == null ||
		   	boneHashSet == null || boneMeshCache == null ) {
			return;
		}

		if( boneHashSet.Contains( transform ) ) {
			SABoneCollider boneCollider = transform.gameObject.GetComponent< SABoneCollider >();
			if( boneCollider != null ) {
				if( boneColliderBuilder.cleanupModified || !boneCollider.modified ) {
					SABoneColliderEditorCommon.DestroySABoneCollider( boneCollider );
					boneCollider = null;
				}
				if( boneCollider != null && boneCollider.recursivery ) {
					return; // Skip modified children.
				}
			}

			if( boneCollider == null ) { // Don't overwrite modified.
				boneCollider = SABoneColliderEditorCommon.CreateSABoneCollider( transform.gameObject, boneColliderBuilder );
				SABoneColliderEditorCommon.RegistReducerTask( reducerTasks, boneCollider, boneMeshCache );
			}
		}

		foreach( Transform childTransform in transform ) {
			if( SAColliderBuilderEditorCommon.IsRootTransform( childTransform ) ) {
				// Nothing.
			} else {
				_ProcessTransform( childTransform, reducerTasks, boneColliderBuilder, boneHashSet, boneMeshCache );
			}
		}
	}

	static void Cleanup( SABoneColliderBuilder boneColliderBuilder )
	{
		if( boneColliderBuilder == null ) {
			Debug.LogError("");
			return;
		}
		
		HashSet<Transform> boneHashSet = SABoneColliderEditorCommon.GetBoneHashSet( boneColliderBuilder.gameObject );
		foreach( Transform transform in boneColliderBuilder.transform ) {
			if( SAColliderBuilderEditorCommon.IsRootTransform( transform ) ) {
				// Nothing.
			} else {
				_CleanupTransform( transform, boneColliderBuilder, boneHashSet );
			}
		}
	}

	static void _CleanupTransform(
		Transform transform,
		SABoneColliderBuilder boneColliderBuilder,
		HashSet<Transform> boneHashSet )
	{
		if( transform == null || boneColliderBuilder == null || boneHashSet == null ) {
			return;
		}
		
		if( boneHashSet.Contains( transform ) ) {
			SABoneCollider boneCollider = transform.gameObject.GetComponent< SABoneCollider >();
			if( boneCollider != null ) {
				if( boneColliderBuilder.cleanupModified || !boneCollider.modified ) {
					SABoneColliderEditorCommon.DestroySABoneCollider( boneCollider );
					boneCollider = null;
				}
				if( boneCollider != null && boneCollider.recursivery ) {
					return;
				}
			}
		}
		
		foreach( Transform childTransform in transform ) {
			if( SAColliderBuilderEditorCommon.IsRootTransform( childTransform ) ) {
				// Nothing.
			} else {
				_CleanupTransform( childTransform, boneColliderBuilder, boneHashSet );
			}
		}
	}

	//--------------------------------------------------------------------------------------------------------------------------------
}
