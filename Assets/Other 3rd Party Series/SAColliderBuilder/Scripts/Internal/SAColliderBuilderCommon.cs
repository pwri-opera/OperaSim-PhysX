//----------------------------------------------
// SABoneCollider
// Copyright (c) 2014 Stereoarts Nora
//----------------------------------------------
using UnityEngine;

public class SAColliderBuilderCommon
{
	public enum ShapeType
	{
		None,
		Mesh,
		Box,
		Capsule,
		Sphere,
	}

	public enum FitType // for Capsule, Sphere
	{
		Outer,
		Inner,
	}

	public enum MeshType
	{
		Raw,
		ConvexBoxes,
		ConvexHull,
		Box,
	}
	
	public enum SliceMode
	{
		Auto,
		X,
		Y,
		Z,
	}

	public enum ElementType
	{
		X,
		XYZ,
	}

	[System.Serializable]
	public struct Bool3
	{
		public bool x;
		public bool y;
		public bool z;

		public Bool3( bool x, bool y, bool z )
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}

		public void SetValue( bool x, bool y, bool z )
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}
	}

	public enum ColliderToChild
	{
		Auto,
		On,
		Off,
	}

	//----------------------------------------------------------------------------------------------------------------

	[System.Serializable]
	public class ReducerProperty
	{
		public ShapeType				shapeType					= ShapeType.Box;
		public FitType					fitType						= FitType.Outer;
		public MeshType					meshType					= MeshType.Box;
		public int						maxTriangles				= 255;
		public SliceMode				sliceMode					= SliceMode.Auto;
		public Vector3					scale						= Vector3.one;
		public ElementType				scaleElementType			= ElementType.X;
		public Vector3					minThickness				= new Vector3( 0.01f, 0.01f, 0.01f );
		public ElementType				minThicknessElementType		= ElementType.X;
		public Bool3					optimizeRotation			= new Bool3( true, true, true );
		public ElementType				optimizeRotationElementType	= ElementType.X;
		public ColliderToChild			colliderToChild				= ColliderToChild.Auto;

		public Vector3					offset						= Vector3.zero;
		public Vector3					thicknessA					= Vector3.zero;
		public Vector3					thicknessB					= Vector3.zero;
		
		public bool						viewAdvanced				= false;

		public ReducerProperty ShallowCopy()
		{
			return (ReducerProperty)MemberwiseClone();
		}
	}
	
	[System.Serializable]
	public class ColliderProperty
	{
		public bool						convex						= true;
		public bool						isTrigger					= false;
		public PhysicMaterial			material					= null;
		public bool						isCreateAsset				= false;

		public ColliderProperty ShallowCopy()
		{
			return (ColliderProperty)MemberwiseClone();
		}
	}
	
	[System.Serializable]
	public class RigidbodyProperty
	{
		public float					mass						= 1.0f;
		public float					drag						= 0.0f;
		public float					angularDrag					= 0.05f;
		public bool						isKinematic					= true; // Unity default: false
		public bool						useGravity					= false; // Unity default: true
		public RigidbodyInterpolation	interpolation				= RigidbodyInterpolation.None;
		public CollisionDetectionMode	collisionDetectionMode		= CollisionDetectionMode.Discrete;
		
		public bool						isCreate					= true;
		public bool						viewAdvanced				= false;
		
		public RigidbodyProperty ShallowCopy()
		{
			return (RigidbodyProperty)MemberwiseClone();
		}
	}
}