//----------------------------------------------
// SABoneCollider
// Copyright (c) 2014 Stereoarts Nora
//----------------------------------------------
using UnityEngine;

using SABoneColliderProperty = SABoneColliderCommon.SABoneColliderProperty;

using BoneProperty = SABoneColliderCommon.BoneProperty;
using SplitProperty = SABoneColliderCommon.SplitProperty;
using ReducerProperty = SAColliderBuilderCommon.ReducerProperty;
using ColliderProperty = SAColliderBuilderCommon.ColliderProperty;
using RigidbodyProperty = SAColliderBuilderCommon.RigidbodyProperty;

using ShapeType = SAColliderBuilderCommon.ShapeType;
using MeshType = SAColliderBuilderCommon.MeshType;
using SliceMode = SAColliderBuilderCommon.SliceMode;

public class SABoneCollider : MonoBehaviour
{
	public SABoneColliderProperty	boneColliderProperty = new SABoneColliderProperty();

	public string					defaultName = "";
	public SABoneColliderProperty	defaultBoneColliderProperty = new SABoneColliderProperty();

	[System.NonSerialized]
	public SABoneColliderProperty	edittingBoneColliderProperty = null;

	public bool						modified = false;
	public bool						modifiedChildren = false;

	[System.NonSerialized]
	public bool						cleanupModified = false;
	[System.NonSerialized]
	public bool						isDebug = false;

	public BoneProperty boneProperty { get { return ( boneColliderProperty != null ) ? boneColliderProperty.boneProperty : null; } }
	public SplitProperty splitProperty { get { return ( boneColliderProperty != null ) ? boneColliderProperty.splitProperty : null; } }
	public ReducerProperty reducerProperty { get { return ( boneColliderProperty != null ) ? boneColliderProperty.reducerProperty : null; } }
	public ColliderProperty colliderProperty { get { return ( boneColliderProperty != null ) ? boneColliderProperty.colliderProperty : null; } }
	public RigidbodyProperty rigidbodyProperty { get { return ( boneColliderProperty != null ) ? boneColliderProperty.rigidbodyProperty : null; } }

	public bool recursivery { get { return (boneProperty != null) ? boneProperty.recursivery : false; } }
	public bool modifyNameEnalbed { get { return (boneColliderProperty != null) ? boneColliderProperty.modifyNameEnabled : false; } }

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
