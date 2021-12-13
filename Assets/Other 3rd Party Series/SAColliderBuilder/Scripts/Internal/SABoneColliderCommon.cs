//----------------------------------------------
// SABoneCollider
// Copyright (c) 2014 Stereoarts Nora
//----------------------------------------------
using UnityEngine;

using ShapeType = SAColliderBuilderCommon.ShapeType;
using MeshType = SAColliderBuilderCommon.MeshType;
using SliceMode = SAColliderBuilderCommon.SliceMode;
using ElementType = SAColliderBuilderCommon.ElementType;
using ReducerProperty = SAColliderBuilderCommon.ReducerProperty;
using ColliderProperty = SAColliderBuilderCommon.ColliderProperty;
using RigidbodyProperty = SAColliderBuilderCommon.RigidbodyProperty;

public class SABoneColliderCommon
{
	public enum BoneWeightType
	{
		Bone2,
		Bone4,
	}

	public enum BoneTriangleExtent
	{
		Disable,
		Vertex2,
		Vertex1,
	}

	[System.Serializable]
	public class BoneProperty
	{
		public bool						recursivery = false;
	
		public BoneProperty ShallowCopy()
		{
			return (BoneProperty)MemberwiseClone();
		}
	}

	[System.Serializable]
	public class SplitProperty
	{
		public BoneWeightType			boneWeightType = BoneWeightType.Bone2;
		public int						boneWeight2 = 50;
		public int						boneWeight3 = 33;
		public int						boneWeight4 = 25;
		public bool						greaterBoneWeight = true;
		public BoneTriangleExtent		boneTriangleExtent = BoneTriangleExtent.Vertex2;

		public SplitProperty ShallowCopy()
		{
			return (SplitProperty)MemberwiseClone();
		}
	}

	[System.Serializable]
	public class SABoneColliderProperty
	{
		public BoneProperty				boneProperty = new BoneProperty();
		public SplitProperty			splitProperty = new SplitProperty();
		public ReducerProperty			reducerProperty = new ReducerProperty();
		public ColliderProperty			colliderProperty = new ColliderProperty();
		public RigidbodyProperty		rigidbodyProperty = new RigidbodyProperty();
		public bool						modifyNameEnabled = false;

		public SABoneColliderProperty Copy()
		{
			SABoneColliderProperty r = new SABoneColliderProperty();
			if( this.boneProperty != null )
				r.boneProperty = this.boneProperty.ShallowCopy();
			if( this.splitProperty != null )
				r.splitProperty = this.splitProperty.ShallowCopy();
			if( this.reducerProperty != null )
				r.reducerProperty = this.reducerProperty.ShallowCopy();
			if( this.colliderProperty != null )
				r.colliderProperty = this.colliderProperty.ShallowCopy();
			if( this.rigidbodyProperty != null )
				r.rigidbodyProperty = this.rigidbodyProperty.ShallowCopy();
			
			r.modifyNameEnabled = this.modifyNameEnabled;
			return r;
		}
	}
	
	[System.Serializable]
	public class SABoneColliderBuilderProperty
	{
		public SplitProperty			splitProperty = new SplitProperty();
		public ReducerProperty			reducerProperty = new ReducerProperty();
		public ColliderProperty			colliderProperty = new ColliderProperty();
		public RigidbodyProperty		rigidbodyProperty = new RigidbodyProperty();
		public bool						modifyNameEnabled = false;

		public SABoneColliderBuilderProperty Copy()
		{
			SABoneColliderBuilderProperty r = new SABoneColliderBuilderProperty();
			if( this.splitProperty != null )
				r.splitProperty = this.splitProperty.ShallowCopy();
			if( this.reducerProperty != null )
				r.reducerProperty = this.reducerProperty.ShallowCopy();
			if( this.colliderProperty != null )
				r.colliderProperty = this.colliderProperty.ShallowCopy();
			if( this.rigidbodyProperty != null )
				r.rigidbodyProperty = this.rigidbodyProperty.ShallowCopy();

			r.modifyNameEnabled = this.modifyNameEnabled;
			return r;
		}
		
		public SABoneColliderProperty ToSABoneColliderProperty()
		{
			SABoneColliderProperty r = new SABoneColliderProperty();
			if( this.splitProperty != null )
				r.splitProperty = this.splitProperty.ShallowCopy();
			if( this.reducerProperty != null )
				r.reducerProperty = this.reducerProperty.ShallowCopy();
			if( this.colliderProperty != null )
				r.colliderProperty = this.colliderProperty.ShallowCopy();
			if( this.rigidbodyProperty != null )
				r.rigidbodyProperty = this.rigidbodyProperty.ShallowCopy();
			
			r.modifyNameEnabled = this.modifyNameEnabled;
			return r;
		}
	}
}
