//----------------------------------------------
// SABoneCollider
// Copyright (c) 2014 Stereoarts Nora
//----------------------------------------------
using UnityEngine;

using SplitMesh = SAMeshColliderCommon.SplitMesh;
using SplitMode = SAMeshColliderCommon.SplitMode;
using SAMeshColliderProperty = SAMeshColliderCommon.SAMeshColliderProperty;

using SplitProperty = SAMeshColliderCommon.SplitProperty;
using ReducerProperty = SAColliderBuilderCommon.ReducerProperty;
using ColliderProperty = SAColliderBuilderCommon.ColliderProperty;
using RigidbodyProperty = SAColliderBuilderCommon.RigidbodyProperty;

public class SAMeshCollider : MonoBehaviour
{
	public SplitMesh				splitMesh = new SplitMesh();
	public SplitMode				splitMode = SplitMode.None;

	public SAMeshColliderProperty	meshColliderProperty = new SAMeshColliderProperty();

	public string					defaultName = "";
	public SAMeshColliderProperty	defaultMeshColliderProperty = new SAMeshColliderProperty();

	[System.NonSerialized]
	public SAMeshColliderProperty	edittingMeshCollidertProperty = null;

	public bool						modified = false;
	public bool						modifiedChildren = false;

	[System.NonSerialized]
	public bool						cleanupModified = false;
	[System.NonSerialized]
	public bool						isDebug = false;

	public SplitProperty splitProperty { get { return ( meshColliderProperty != null ) ? meshColliderProperty.splitProperty : null; } }
	public ReducerProperty reducerProperty { get { return ( meshColliderProperty != null ) ? meshColliderProperty.reducerProperty : null; } }
	public ColliderProperty colliderProperty { get { return ( meshColliderProperty != null ) ? meshColliderProperty.colliderProperty : null; } }
	public RigidbodyProperty rigidbodyProperty { get { return ( meshColliderProperty != null ) ? meshColliderProperty.rigidbodyProperty : null; } }

	public bool splitMaterialEnabled { get { return ( splitProperty != null ) ? splitProperty.splitMaterialEnabled : false; } }
	public bool splitPrimitiveEnabled { get { return ( splitProperty != null ) ? splitProperty.splitPrimitiveEnabled : false; } }
	public bool splitPolygonNormalEnabled { get { return ( splitProperty != null ) ? splitProperty.splitPolygonNormalEnabled : false; } }
	public float splitPolygonNormalAngle { get { return ( splitProperty != null ) ? splitProperty.splitPolygonNormalAngle : 0.0f; } }

	public bool modifyNameEnalbed { get { return (meshColliderProperty != null) ? meshColliderProperty.modifyNameEnabled : false; } }

	//----------------------------------------------------------------------------------------------------------------

	public void ChangeDefaultName( string defaultName )
	{
		bool isModifyName = _IsModifyName();
		this.defaultName = defaultName;
		if( this.modifyNameEnalbed ) {
			if( isModifyName ) {
				this.gameObject.name = _ComputeModifyName();
			}
		}
	}

	public void ChangeModified( bool modified )
	{
		bool isModifyName = _IsModifyName();
		this.modified = modified;
		if( this.modifyNameEnalbed ) {
			if( isModifyName ) {
				this.gameObject.name = _ComputeModifyName();
			}
		}
	}

	public void ChangeModifiedChildren( bool modifiedChildren )
	{
		bool isModifyName = _IsModifyName();
		this.modifiedChildren = modifiedChildren;
		if( this.modifyNameEnalbed ) {
			if( isModifyName ) {
				this.gameObject.name = _ComputeModifyName();
			}
		}
	}

	public void ResetModified()
	{
		bool isModifyName = _IsModifyName();
		this.modified = false;
		this.modifiedChildren = false;
		if( this.modifyNameEnalbed ) {
			if( isModifyName ) {
				this.gameObject.name = _ComputeModifyName();
			}
		}
	}

	public void ResetModifyName()
	{
		if( this.modifyNameEnalbed ) {
			this.gameObject.name = _ComputeModifyName();
		}
	}

	public string _ComputeModifyName()
	{
		if( this.modifyNameEnalbed ) {
			if( this.modified ) {
				if( string.IsNullOrEmpty(this.defaultName) ) {
					return "*";
				} else {
					return this.defaultName + "*";
				}
			}
			if( this.modifiedChildren ) {
				if( string.IsNullOrEmpty(this.defaultName) ) {
					return "+";
				} else {
					return this.defaultName + "+";
				}
			}
		}

		if( string.IsNullOrEmpty(this.defaultName) ) {
			return "";
		} else {
			return this.defaultName;
		}
	}

	public bool _IsModifyName()
	{
		if( this.modifyNameEnalbed ) {
			if( string.IsNullOrEmpty(this.gameObject.name) ) {
				return string.IsNullOrEmpty(_ComputeModifyName());
			} else {
				return this.gameObject.name == _ComputeModifyName();
			}
		}

		return false;
	}
}
