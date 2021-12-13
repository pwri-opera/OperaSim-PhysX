//----------------------------------------------
// SABoneCollider
// Copyright (c) 2014 Stereoarts Nora
//----------------------------------------------
using UnityEngine;

using SAMeshColliderBuilderProperty = SAMeshColliderCommon.SAMeshColliderBuilderProperty;

using SplitProperty = SAMeshColliderCommon.SplitProperty;
using ReducerProperty = SAColliderBuilderCommon.ReducerProperty;
using ColliderProperty = SAColliderBuilderCommon.ColliderProperty;
using RigidbodyProperty = SAColliderBuilderCommon.RigidbodyProperty;

public class SAMeshColliderBuilder : MonoBehaviour
{
	public SAMeshColliderBuilderProperty	meshColliderBuilderProperty = new SAMeshColliderBuilderProperty();

	[System.NonSerialized]
	public SAMeshColliderBuilderProperty	edittingMeshColliderBuilderProperty = null;

	[System.NonSerialized]
	public bool								cleanupModified = false;
	[System.NonSerialized]
	public bool								isDebug = false;

	public SplitProperty splitProperty { get { return ( meshColliderBuilderProperty != null ) ? meshColliderBuilderProperty.splitProperty : null; } }
	public ReducerProperty reducerProperty { get { return ( meshColliderBuilderProperty != null ) ? meshColliderBuilderProperty.reducerProperty : null; } }
	public ColliderProperty colliderProperty { get { return ( meshColliderBuilderProperty != null ) ? meshColliderBuilderProperty.colliderProperty : null; } }
	public RigidbodyProperty rigidbodyProperty { get { return ( meshColliderBuilderProperty != null ) ? meshColliderBuilderProperty.rigidbodyProperty : null; } }
	
	public bool splitMaterialEnabled { get { return ( splitProperty != null ) ? splitProperty.splitMaterialEnabled : false; } }
	public bool splitPrimitiveEnabled { get { return ( splitProperty != null ) ? splitProperty.splitPrimitiveEnabled : false; } }
	public bool splitPolygonNormalEnabled { get { return ( splitProperty != null ) ? splitProperty.splitPolygonNormalEnabled : false; } }
	public float splitPolygonNormalAngle { get { return ( splitProperty != null ) ? splitProperty.splitPolygonNormalAngle : 0.0f; } }
}
