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

public class SAMeshColliderCommon
{
	public enum SplitMode
	{
		None,
		Material,
		Primitive,
		Polygon,
	}

	[System.Serializable]
	public class SplitMesh
	{
		public int subMeshCount; // for Mesh validation check
		public int subMesh = -1; // = materialIndex, disabled: -1
		
		public int triangle = -1; // = triangleIndex, disabled: -1
		public Vector3 triangleVertex; // for Triangle validation check
		
		public int[] polygonTriangles; // for Split by Polygon
		public Vector3[] polygonVertices; // for Split by Polygon
		
		[System.NonSerialized]
		public int[] triangles; // for Polygon Split.
		[System.NonSerialized]
		public Vector3[] vertices; // for Polygon Split.
		[System.NonSerialized]
		public Vector3[] triangleNormals; // for Polygon Split.
		
		public void PurgeTemporary()
		{
			this.triangles = null;
			this.vertices = null;
			this.triangleNormals = null;
		}
	}

	[System.Serializable]
	public class SplitProperty
	{
		public bool						splitMaterialEnabled		= true;
		public bool						splitPrimitiveEnabled		= true;
		public bool						splitPolygonNormalEnabled	= false;
		public float					splitPolygonNormalAngle		= 45.0f;
		
		public SplitProperty ShallowCopy()
		{
			return (SplitProperty)MemberwiseClone();
		}
	}

	[System.Serializable]
	public class SAMeshColliderProperty
	{
		public SplitProperty			splitProperty = new SplitProperty();
		public ReducerProperty			reducerProperty = new ReducerProperty();
		public ColliderProperty			colliderProperty = new ColliderProperty();
		public RigidbodyProperty		rigidbodyProperty = new RigidbodyProperty();
		public bool						modifyNameEnabled = true;

		public SAMeshColliderProperty Copy()
		{
			SAMeshColliderProperty r = new SAMeshColliderProperty();
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
	public class SAMeshColliderBuilderProperty
	{
		public SplitProperty			splitProperty = new SplitProperty();
		public ReducerProperty			reducerProperty = new ReducerProperty();
		public ColliderProperty			colliderProperty = new ColliderProperty();
		public RigidbodyProperty		rigidbodyProperty = new RigidbodyProperty();
		public bool						modifyNameEnabled = true;

		public SAMeshColliderBuilderProperty Copy()
		{
			SAMeshColliderBuilderProperty r = new SAMeshColliderBuilderProperty();
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
		
		public SAMeshColliderProperty ToSAMeshColliderProperty()
		{
			SAMeshColliderProperty r = new SAMeshColliderProperty();
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
