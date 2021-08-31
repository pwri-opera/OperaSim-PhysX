//----------------------------------------------
// SABoneCollider
// Copyright (c) 2014 Stereoarts Nora
//----------------------------------------------
using UnityEngine;

using SABoneColliderBuilderProperty = SABoneColliderCommon.SABoneColliderBuilderProperty;

using BoneProperty = SABoneColliderCommon.BoneProperty;
using SplitProperty = SABoneColliderCommon.SplitProperty;
using ReducerProperty = SAColliderBuilderCommon.ReducerProperty;
using ColliderProperty = SAColliderBuilderCommon.ColliderProperty;
using RigidbodyProperty = SAColliderBuilderCommon.RigidbodyProperty;

using ShapeType = SAColliderBuilderCommon.ShapeType;
using MeshType = SAColliderBuilderCommon.MeshType;
using SliceMode = SAColliderBuilderCommon.SliceMode;

public class SABoneColliderBuilder : MonoBehaviour
{
	public SABoneColliderBuilderProperty	boneColliderBuilderProperty = new SABoneColliderBuilderProperty();
	
	[System.NonSerialized]
	public SABoneColliderBuilderProperty	edittingBoneColliderBuilderProperty = null;
	
	[System.NonSerialized]
	public bool								cleanupModified = false;
	[System.NonSerialized]
	public bool								isDebug = false;

	public SplitProperty splitProperty { get { return ( boneColliderBuilderProperty != null ) ? boneColliderBuilderProperty.splitProperty : null; } }
	public ReducerProperty reducerProperty { get { return ( boneColliderBuilderProperty != null ) ? boneColliderBuilderProperty.reducerProperty : null; } }
	public ColliderProperty colliderProperty { get { return ( boneColliderBuilderProperty != null ) ? boneColliderBuilderProperty.colliderProperty : null; } }
	public RigidbodyProperty rigidbodyProperty { get { return ( boneColliderBuilderProperty != null ) ? boneColliderBuilderProperty.rigidbodyProperty : null; } }
}
