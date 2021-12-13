//----------------------------------------------
// SABoneCollider
// Copyright (c) 2014 Stereoarts Nora
//----------------------------------------------
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Bool3 = SAColliderBuilderCommon.Bool3;

public class SAColliderBoxReducer
{
	public enum ReduceMode {
		Box,
		BoxMesh,
		Mesh,
	}

	public enum SliceMode {
		Auto,
		X,
		Y,
		Z,
	}

	ReduceMode		_reduceMode			= ReduceMode.Mesh;
	SliceMode		_sliceMode			= SliceMode.Auto;
	Vector3			_minThickness		= Vector3.zero;
	Vector3[]		_vertexList			= null;
	bool[]			_usedVertexList		= null;
	int[]			_lineList			= null;
	Vector3			_center				= Vector3.zero;
	bool			_centerEnabled		= false;
	Quaternion		_rotation			= Quaternion.identity;
	bool			_rotationEnabled	= false;
	Bool3			_optimizeRotation	= new Bool3( true, true, true );
	Vector3			_scale				= Vector3.one;
	Vector3			_offset				= Vector3.zero;
	Vector3			_thicknessA			= Vector3.zero;
	Vector3			_thicknessB			= Vector3.zero;
	Vector3			_boundingBoxA		= Vector3.zero;
	Vector3			_boundingBoxB		= Vector3.zero;
	int				_sliceCount			= 31;
	int				_slicedDimention	= 0;
	Vector3[]		_slicedBoundingBoxA	= null;
	Vector3[]		_slicedBoundingBoxB	= null;
	Vector3[]		_slicedBoundingBoxC	= null;
	Vector3[]		_slicedBoundingBoxD	= null;
	Vector3[]		_slicedVertexList	= null;
	int[]			_slicedIndexList	= null;
	Vector3[]		_reducedVertexList	= null;
	int[]			_reducedIndexList	= null;
	Quaternion		_reducedRotation	= Quaternion.identity;
	Vector3			_reducedCenter		= Vector3.zero;
	Vector3			_reducedBoxA		= Vector3.zero;
	Vector3			_reducedBoxB		= Vector3.zero;
	bool			_postfixTransform	= true;
	
	public ReduceMode reduceMode { set { _reduceMode = value; } }
	public SliceMode sliceMode { set { _sliceMode = value; } }
	public int sliceCount { set { _sliceCount = value; } }
	public Vector3 minThickness { set { _minThickness = value; } }
	public Quaternion rotation { set { _rotationEnabled = true; _rotation = value; } }
	public Vector3 center { set { _centerEnabled = true; _center = value; } }
	public Bool3 optimizeRotation { set { _optimizeRotation = value; } }
	public Vector3 scale { set { _scale = value; } }
	public Vector3 offset { set { _offset = value; } }
	public Vector3 thicknessA { set { _thicknessA = value; } }
	public Vector3 thicknessB { set { _thicknessB = value; } }
	public Vector3[] vertexList { set { _vertexList = value; } }
	public int[] lineList { set { _lineList = value; } }
	public bool postfixTransform { set { _postfixTransform = value; } }
	public Vector3[] reducedVertexList { get { return _reducedVertexList; } }
	public int[] reducedIndexList { get { return _reducedIndexList; } }
	public Quaternion reducedRotation { get { return _reducedRotation; } }
	public Vector3 reducedCenter { get { return _reducedCenter; } }
	public Vector3 reducedBoxA { get { return _reducedBoxA; } }
	public Vector3 reducedBoxB { get { return _reducedBoxB; } }

	//----------------------------------------------------------------------------------------------------------------------------

	public void Reduce()
	{
		_usedVertexList = null;
		if( _vertexList != null && _lineList != null ) {
			_usedVertexList = new bool[_vertexList.Length];
			for( int i = 0; i < _lineList.Length; ++i ) {
				_usedVertexList[_lineList[i]] = true;
			}
		} else {
			_usedVertexList = new bool[_vertexList.Length];
			for( int i = 0; i < _usedVertexList.Length; ++i ) {
				_usedVertexList[i] = true;
			}
		}

		Vector3 minCenter = Vector3.zero;
		Vector3 minBoxA = Vector3.zero;
		Vector3 minBoxB = Vector3.zero;
		Vector3 minEular = Vector3.zero;

		_GetMinBoundingBoxAABB( ref minCenter, ref minBoxA, ref minBoxB, ref minEular );

		Matrix4x4 reducedTransform = Matrix4x4.identity;

		{
			_reducedCenter = minCenter;
			_reducedBoxA = minBoxA;
			_reducedBoxB = minBoxB;

			Quaternion reduceRotation = Quaternion.identity;
			if( _rotationEnabled ) {
				reduceRotation = InversedRotation( _rotation );
				_reducedRotation = _rotation;
			} else {
				reduceRotation = Quaternion.Euler( minEular );
				_reducedRotation = InversedRotation( reduceRotation );
			}
	
			if( _reduceMode == ReduceMode.Mesh || _reduceMode == ReduceMode.BoxMesh ) {
				Matrix4x4 reduceTransform = _TranslateRotationMatrix( -_reducedCenter, reduceRotation );
				_TransformVertexList( ref reduceTransform ); /* Adjust for Mesh. */
				reducedTransform = reduceTransform.inverse;
			}
		}

		if( _reduceMode == ReduceMode.Mesh ) {
			_boundingBoxA = minBoxA;
			_boundingBoxB = minBoxB;
			if( _MakeSlicedBoundingBoxAABB() ) {
				_MakeSlicedListFromBoundingBox();
			} else {
				_reduceMode = ReduceMode.BoxMesh;
			}
		}

		if( _reduceMode == ReduceMode.Box || _reduceMode == ReduceMode.BoxMesh ) {
			_ComputeMinThickness( ref minBoxA.x, ref minBoxB.x, _minThickness[0] );
			_ComputeMinThickness( ref minBoxA.y, ref minBoxB.y, _minThickness[1] );
			_ComputeMinThickness( ref minBoxA.z, ref minBoxB.z, _minThickness[2] );

			if( _scale != Vector3.one ) {
				minBoxA = ScaledVector( minBoxA, _scale );
				minBoxB = ScaledVector( minBoxB, _scale );
			}
			if( _thicknessA != Vector3.zero || _thicknessB != Vector3.zero ) {
				minBoxA += _thicknessA;
				minBoxB += _thicknessB;
			}
			if( _offset != Vector3.zero ) {
				minBoxA += _offset;
				minBoxB += _offset;
			}

			_reducedBoxA = minBoxA;
			_reducedBoxB = minBoxB;
			_boundingBoxA = minBoxA;
			_boundingBoxB = minBoxB;
			if( _reduceMode == ReduceMode.BoxMesh ) {
				_MakeSlicedListFromAABB( minBoxA, minBoxB );
			}
		}

		if( _reduceMode == ReduceMode.Mesh || _reduceMode == ReduceMode.BoxMesh ) {
			_MakeReducedListFromSlicedList();
			if( _postfixTransform ) {
				_TransformReducedList( ref reducedTransform );
			}
		}
	}

	//----------------------------------------------------------------------------------------------------------------------------

	static void _ComputeMinThickness( ref float boxA, ref float boxB, float minThickness )
	{
		float depth = Mathf.Abs(boxB - boxA);
		if( depth < minThickness ) {
			float center = (boxA + boxB) * 0.5f;
			if ( boxA <= boxB ) {
				boxA = center - minThickness * 0.5f;
				boxB = center + minThickness * 0.5f;
			} else {
				boxA = center + minThickness * 0.5f;
				boxB = center - minThickness * 0.5f;
			}
		}
	}

	//----------------------------------------------------------------------------------------------------------------------------

	Vector3 _GetBoundingBoxCenterAABB()
	{
		if( _vertexList == null || _usedVertexList == null ) {
			return Vector3.zero;
		}

		Vector3 boxA = Vector3.zero;
		Vector3 boxB = Vector3.zero;
		bool setAnything = false;
		for( int i = 0; i < _vertexList.Length; ++i ) {
			if( _usedVertexList[i] ) {
				if( !setAnything ) {
					setAnything = true;
					boxA = boxB = _vertexList[i];
				} else {
					boxA = Min( boxA, _vertexList[i] );
					boxB = Max( boxB, _vertexList[i] );
				}
			}
		}

		return (boxA + boxB) * 0.5f;
	}

	public static Matrix4x4 _RotationMatrix( Quaternion rotation )
	{
		return Matrix4x4.TRS( Vector3.zero, rotation, Vector3.one );
	}

	public static Matrix4x4 _TranslateRotationMatrix( Vector3 translate, Quaternion rotation )
	{
		Matrix4x4 translateTransform = Matrix4x4.identity;
		translateTransform.SetColumn( 3, new Vector4( translate.x, translate.y, translate.z, 1.0f ) );
		return Matrix4x4.TRS( Vector3.zero, rotation, Vector3.one ) * translateTransform;
	}

	public struct Euler
	{
		public int x, y, z;

		public Euler( int x, int y, int z )
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}

		public void SetValue( int x, int y, int z )
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}
	}

	public struct MinBounding
	{
		public Vector3 boxA;
		public Vector3 boxB;
		public Euler euler;
		public float volume;
		public bool setted;

		public void Set( Vector3 boxA, Vector3 boxB, Euler euler, float volume )
		{
			this.boxA = boxA;
			this.boxB = boxB;
			this.euler = euler;
			this.volume = volume;
			this.setted = true;
		}

		public void Set( ref MinBounding minBounding )
		{
			this.boxA = minBounding.boxA;
			this.boxB = minBounding.boxB;
			this.euler = minBounding.euler;
			this.volume = minBounding.volume;
			this.setted = minBounding.setted;
		}

		public void Contain( Vector3 boxA, Vector3 boxB, Euler euler, float volume )
		{
			if( !this.setted || this.volume > volume ) {
				Set( boxA, boxB, euler, volume );
			}
		}

		public void Contain( ref MinBounding minBounding )
		{
			if( !this.setted || this.volume > minBounding.volume ) {
				Set( ref minBounding );
			}
		}
	}

	public struct SharedMinBounding
	{
		public MinBounding minBounding;

		public void Contain( ref MinBounding minBounding )
		{
			this.minBounding.Contain( ref minBounding );
		}
	}

	static void _ProcessBoundingBoxAABB(
		ref SharedMinBounding sharedMinBounding,
		Vector3[] vertices,
		bool[] usedVertices,
		Vector3 minCenter,
		Euler beginEuler,
		Euler endEuler,
		int stepEuler )
	{
		if( vertices == null || usedVertices == null ) {
			return;
		}

		Matrix4x4 transform = Matrix4x4.identity;
		MinBounding minBounding = new MinBounding();
		for( int rz = beginEuler.z; rz < endEuler.z; rz += stepEuler ) {
			for( int ry = beginEuler.y; ry < endEuler.y; ry += stepEuler ) {
				for( int rx = beginEuler.x; rx < endEuler.x; rx += stepEuler ) {
					transform.SetTRS( Vector3.zero, Quaternion.Euler( rx, ry, rz ), Vector3.one );
					Vector3 tempBoxA = Vector3.zero, tempBoxB = Vector3.zero;
					_GetBoundingBoxAABB( vertices, usedVertices, ref tempBoxA, ref tempBoxB, ref minCenter, ref transform );
					Vector3 v = ( tempBoxB - tempBoxA );
					float tempVolume = _GetVolume( v );
					Euler tempEuler = new Euler( rx, ry, rz );
					minBounding.Contain( tempBoxA, tempBoxB, tempEuler, tempVolume );
				}
			}
		}
		
		sharedMinBounding.Contain( ref minBounding );
	}

	static void _GetBoundingBoxAABB( Vector3[] vertices, bool[] usedVertices, ref Vector3 boxA, ref Vector3 boxB, ref Vector3 minCenter, ref Matrix4x4 transform )
	{
		boxA = Vector3.zero;
		boxB = Vector3.zero;
		bool setAnything = false;
		for( int i = 0; i < vertices.Length; ++i ) {
			if( usedVertices[i] ) {
				Vector3 v = transform.MultiplyPoint3x4( vertices[i] - minCenter );
				if( !setAnything ) {
					setAnything = true;
					boxA = boxB = v;
				} else {
					boxA = Min( boxA, v );
					boxB = Max( boxB, v );
				}
			}
		}
	}

	void _GetBoundingBoxAABB( ref Vector3 boxA, ref Vector3 boxB, ref Vector3 minCenter, ref Matrix4x4 transform )
	{
		boxA = Vector3.zero;
		boxB = Vector3.zero;
		if( _vertexList != null && _usedVertexList != null ) {
			_GetBoundingBoxAABB( _vertexList, _usedVertexList, ref boxA, ref boxB, ref minCenter, ref transform );
		}
	}

	void _GetMinBoundingBoxAABB( ref Vector3 minCenter, ref Vector3 minBoxA, ref Vector3 minBoxB, ref Vector3 minEular )
	{
		if( _centerEnabled ) {
			minCenter = _center;
		} else {
			minCenter = _GetBoundingBoxCenterAABB();
		}
		//minCenter = Vector3.zero;
		
		if( _rotationEnabled ) {
			minBoxA = Vector3.zero;
			minBoxB = Vector3.zero;
			minEular = Vector3.zero;
			Matrix4x4 transform = _RotationMatrix( InversedRotation( _rotation ) );
			_GetBoundingBoxAABB( ref minBoxA, ref minBoxB, ref minCenter, ref transform );
			return;
		}

		#if false
		{
			minBoxA = Vector3.zero;
			minBoxB = Vector3.zero;
			minEular = Vector3.zero;
			Matrix4x4 transform = Matrix4x4.identity;
			_GetBoundingBoxAABB( ref minBoxA, ref minBoxB, ref minCenter, ref transform );
		}
		#else
		int stepEuler = 20;
		int stepEuler2 = 5;
		int stepEuler3 = 1;

		SharedMinBounding sharedMinBounding = new SharedMinBounding();

		{
			Euler beginEuler = new Euler( 0, 0, 0 );
			Euler endEuler = new Euler( 180, 180, 180 );
			if( !_optimizeRotation.x ) { beginEuler.x = 0; endEuler.x = 1; }
			if( !_optimizeRotation.y ) { beginEuler.y = 0; endEuler.y = 1; }
			if( !_optimizeRotation.z ) { beginEuler.z = 0; endEuler.z = 1; }

			_ProcessBoundingBoxAABB(
				ref sharedMinBounding,
				_vertexList,
				_usedVertexList,
				minCenter,
				beginEuler,
				endEuler,
				stepEuler );
		}

		{
			int fx = sharedMinBounding.minBounding.euler.x;
			int fy = sharedMinBounding.minBounding.euler.y;
			int fz = sharedMinBounding.minBounding.euler.z;
			Euler beginEuler = new Euler( fx - stepEuler, fy - stepEuler, fz - stepEuler );
			Euler endEuler = new Euler( fx + stepEuler, fy + stepEuler, fz + stepEuler );
			if( !_optimizeRotation.x ) { beginEuler.x = 0; endEuler.x = 1; }
			if( !_optimizeRotation.y ) { beginEuler.y = 0; endEuler.y = 1; }
			if( !_optimizeRotation.z ) { beginEuler.z = 0; endEuler.z = 1; }

			_ProcessBoundingBoxAABB(
				ref sharedMinBounding,
				_vertexList,
				_usedVertexList,
				minCenter,
				beginEuler,
				endEuler,
				stepEuler2 );
		}

		{
			int fx = sharedMinBounding.minBounding.euler.x;
			int fy = sharedMinBounding.minBounding.euler.y;
			int fz = sharedMinBounding.minBounding.euler.z;
			Euler beginEuler = new Euler( fx - stepEuler2, fy - stepEuler2, fz - stepEuler2 );
			Euler endEuler = new Euler( fx + stepEuler2, fy + stepEuler2, fz + stepEuler2 );
			if( !_optimizeRotation.x ) { beginEuler.x = 0; endEuler.x = 1; }
			if( !_optimizeRotation.y ) { beginEuler.y = 0; endEuler.y = 1; }
			if( !_optimizeRotation.z ) { beginEuler.z = 0; endEuler.z = 1; }

			_ProcessBoundingBoxAABB(
				ref sharedMinBounding,
				_vertexList,
				_usedVertexList,
				minCenter,
				beginEuler,
				endEuler,
				stepEuler3 );
		}

		Euler euler = sharedMinBounding.minBounding.euler;
		minBoxA = sharedMinBounding.minBounding.boxA;
		minBoxB = sharedMinBounding.minBounding.boxB;
		minEular = new Vector3( (float)euler.x, (float)euler.y, (float)euler.z );
		#endif
	}

	bool _MakeSlicedBoundingBoxAABB()
	{
		int SliceCount = _sliceCount;
		float f_SliceCount = (float)_sliceCount;
		
		int minimumDim = -1;
		float minimumVolume = 0.0f;
		Vector3[] minimumBoxA = null;
		Vector3[] minimumBoxB = null;

		/* memo: Choose minimum box in 3 direction. */
		for( int i = 0; i < 3; ++i ) {
			/* Compute AABB in 31 dividing box. */
			List<Vector3> tempBoxA = new List<Vector3>( SliceCount );
			List<Vector3> tempBoxB = new List<Vector3>( SliceCount );
			switch( i ) {
			case 0: // X
				if( minimumDim < 0 || _sliceMode == SliceMode.Auto || _sliceMode == SliceMode.X ) {
					Matrix4x4 transform = Matrix4x4.TRS( Vector3.zero, Quaternion.Euler( 45.0f, 0.0f, 0.0f ), Vector3.one ); /* Right Hand(Left Rotation) */

					// Limit search range
					Vector3 boundingBoxA = _boundingBoxA;
					Vector3 boundingBoxB = _boundingBoxB;
					boundingBoxA[0] += _thicknessA[0];
					boundingBoxB[0] += _thicknessB[0];

					float minX = boundingBoxA.x;
					float stepX = (boundingBoxB.x - boundingBoxA.x) / f_SliceCount;
					float tempVolume = 0.0f;
					for( int n = 0; n < SliceCount; ++n ) {
						float maxX = minX + stepX;
						Vector3 boxA = Vector3.zero, boxB = Vector3.zero;
						if( _GetBoundingBoxAABB( 0, ref boxA, ref boxB, minX, maxX, ref transform ) ) {
							boxA.x = minX;
							boxB.x = maxX;
							tempBoxA.Add( boxA );
							tempBoxB.Add( boxB );
							tempVolume += _GetBoxVolume( boxA, boxB );
						}
						minX = maxX;
					}
					if( tempVolume > Mathf.Epsilon ) {
						minimumDim = 0;
						minimumVolume = tempVolume;
						minimumBoxA = tempBoxA.ToArray();
						minimumBoxB = tempBoxB.ToArray();
					}
				}
				break;
			case 1: // Y
				if( minimumDim < 0 || _sliceMode == SliceMode.Auto || _sliceMode == SliceMode.Y ) {
					Matrix4x4 transform = Matrix4x4.TRS( Vector3.zero, Quaternion.Euler( 0.0f, 45.0f, 0.0f ), Vector3.one ); /* Right Hand(Left Rotation) */

					// Limit search range
					Vector3 boundingBoxA = _boundingBoxA;
					Vector3 boundingBoxB = _boundingBoxB;
					boundingBoxA[1] += _thicknessA[1];
					boundingBoxB[1] += _thicknessB[1];

					float minY = boundingBoxA.y;
					float stepY = (boundingBoxB.y - boundingBoxA.y) / f_SliceCount;
					float tempVolume = 0.0f;
					for( int n = 0; n < SliceCount; ++n ) {
						float maxY = minY + stepY;
						Vector3 boxA = Vector3.zero, boxB = Vector3.zero;
						if( _GetBoundingBoxAABB( 1, ref boxA, ref boxB, minY, maxY, ref transform ) ) {
							boxA.y = minY;
							boxB.y = maxY;
							tempBoxA.Add( boxA );
							tempBoxB.Add( boxB );
							tempVolume += _GetBoxVolume( boxA, boxB );
						}
						minY = maxY;
					}
					if( tempVolume > Mathf.Epsilon ) {
						if( _sliceMode == SliceMode.Y || minimumVolume > tempVolume ) {
							minimumDim = 1;
							minimumVolume = tempVolume;
							minimumBoxA = tempBoxA.ToArray();
							minimumBoxB = tempBoxB.ToArray();
						}
					}
				}
				break;
			case 2: // Z
				if( minimumDim < 0 || _sliceMode == SliceMode.Auto || _sliceMode == SliceMode.Z ) {
					Matrix4x4 transform = Matrix4x4.TRS( Vector3.zero, Quaternion.Euler( 0.0f, 0.0f, 45.0f ), Vector3.one ); /* Right Hand(Left Rotation) */

					// Limit search range
					Vector3 boundingBoxA = _boundingBoxA;
					Vector3 boundingBoxB = _boundingBoxB;
					boundingBoxA[2] += _thicknessA[2];
					boundingBoxB[2] += _thicknessB[2];

					float minZ = boundingBoxA.z;
					float stepZ = (boundingBoxB.z - boundingBoxA.z) / f_SliceCount;
					float tempVolume = 0.0f;
					for( int n = 0; n < SliceCount; ++n ) {
						float maxZ = minZ + stepZ;
						Vector3 boxA = Vector3.zero, boxB = Vector3.zero;
						if( _GetBoundingBoxAABB( 2, ref boxA, ref boxB, minZ, maxZ, ref transform ) ) {
							boxA.z = minZ;
							boxB.z = maxZ;
							tempBoxA.Add( boxA );
							tempBoxB.Add( boxB );
							tempVolume += _GetBoxVolume( boxA, boxB );
						}
						minZ = maxZ;
					}
					if( tempVolume > Mathf.Epsilon ) {
						if( _sliceMode == SliceMode.Z || minimumVolume > tempVolume ) {
							minimumDim = 2;
							minimumVolume = tempVolume;
							minimumBoxA = tempBoxA.ToArray();
							minimumBoxB = tempBoxB.ToArray();
						}
					}
				}
				break;
			}
		}

		if( minimumDim < 0 || minimumBoxA == null || minimumBoxA.Length == 0 ) {
			return false;
		}

		Vector3 thicknessA = _thicknessA; 
		Vector3 thicknessB = _thicknessB;
		thicknessA[minimumDim] = 0;
		thicknessB[minimumDim] = 0;

		if( _minThickness != Vector3.zero ) {
			for( int i = 0; i < minimumBoxA.Length; ++i ) {
				if( minimumDim != 0 ) {
					_ComputeMinThickness( ref minimumBoxA[i].x, ref minimumBoxB[i].x, _minThickness.x );
				}
				if( minimumDim != 1 ) {
					_ComputeMinThickness( ref minimumBoxA[i].y, ref minimumBoxB[i].y, _minThickness.y );
				}
				if( minimumDim != 2 ) {
					_ComputeMinThickness( ref minimumBoxA[i].z, ref minimumBoxB[i].z, _minThickness.z );
				}
			}

			int end = minimumBoxA.Length - 1;
			if( minimumDim == 0 ) {
				_ComputeMinThickness( ref minimumBoxA[0].x, ref minimumBoxB[end].x, _minThickness.x );
			}
			if( minimumDim == 1 ) {
				_ComputeMinThickness( ref minimumBoxA[0].y, ref minimumBoxB[end].y, _minThickness.y );
			}
			if( minimumDim == 2 ) {
				_ComputeMinThickness( ref minimumBoxA[0].z, ref minimumBoxB[end].z, _minThickness.z );
			}
		}
		
		if( _scale != Vector3.one ) {
			for( int i = 0; i < minimumBoxA.Length; ++i ) {
				minimumBoxA[i] = ScaledVector( minimumBoxA[i], _scale );
				minimumBoxB[i] = ScaledVector( minimumBoxB[i], _scale );
			}
		}

		if( thicknessA != Vector3.zero || thicknessB != Vector3.zero ) {
			for( int i = 0; i < minimumBoxA.Length; ++i ) {
				minimumBoxA[i] += thicknessA;
				minimumBoxB[i] += thicknessB;
			}
		}
		if( _offset != Vector3.zero ) {
			for( int i = 0; i < minimumBoxA.Length; ++i ) {
				minimumBoxA[i] += _offset;
				minimumBoxB[i] += _offset;
			}
		}

		{
			BoxCollector boxCollector = new BoxCollector();
			boxCollector.Collect( minimumBoxA );
			boxCollector.Collect( minimumBoxB );
			if( boxCollector.isAnything ) {
				_reducedBoxA = boxCollector.boxA;
				_reducedBoxB = boxCollector.boxB;
			}
		}

		_slicedDimention = minimumDim;
		_slicedBoundingBoxA = minimumBoxA.Clone() as Vector3[];
		_slicedBoundingBoxB = minimumBoxB.Clone() as Vector3[];
		_slicedBoundingBoxC = minimumBoxA.Clone() as Vector3[];
		_slicedBoundingBoxD = minimumBoxB.Clone() as Vector3[];

		/* AB ... Plane of begin CD ... Plane of end */
		/* AB / CD are diagonal planes. AB / CD planes are parallel. */
		/* Adjacent CD/AB is equal minimumDim(X,Y,Z). */
		/* Adjacent CD/AB is equal after Combine optimized. */
		for( int i = 0; i < minimumBoxA.Length; ++i ) {
			_slicedBoundingBoxB[i][minimumDim] = _slicedBoundingBoxA[i][minimumDim];
			_slicedBoundingBoxC[i][minimumDim] = _slicedBoundingBoxD[i][minimumDim];
		}
		/* Combine optimized.(Optimized vertices, exclude begin/end plane.) */
		for( int i = 1; i < minimumBoxA.Length; ++i ) {
			/* Average VertexA. */
			Vector3 boxA1 = minimumBoxA[i];
			Vector3 boxA0 = minimumBoxA[i - 1];
			boxA0[minimumDim] = boxA1[minimumDim];
			Vector3 boxAM = (boxA1 + boxA0) / 2.0f;
			/* Copy arrangemented vertexA. */
			_slicedBoundingBoxA[i] = boxAM;
			_slicedBoundingBoxC[i - 1] = boxAM;
			/* Average VertexB */
			Vector3 boxB0 = minimumBoxB[i - 1];
			Vector3 boxB1 = minimumBoxB[i];
			boxB1[minimumDim] = boxB0[minimumDim];
			Vector3 boxBM = (boxB1 + boxB0) / 2.0f;
			/* Copy arrangemented vertexB. */
			_slicedBoundingBoxB[i] = boxBM;
			_slicedBoundingBoxD[i - 1] = boxBM;
		}

		return true;
	}
	
	bool _GetBoundingBoxAABB( int DIM_, ref Vector3 boxA, ref Vector3 boxB, float minV, float maxV, ref Matrix4x4 innerTransform )
	{
		BoxCollector boxCollector = new BoxCollector();
		//BoxCollector boxCollector2 = new BoxCollector(); // for Inner

		if( _lineList != null ) {
			for( int i = 0, count = (int)_lineList.Length / 2 * 2; i < count; i += 2 ) {
				//assert( (int)_lineList[i + 0] < _vertexList.size() && (int)_lineList[i + 1] < _vertexList.size() );
				Vector3 vertex0 = _vertexList[_lineList[i + 0]];
				Vector3 vertex1 = _vertexList[_lineList[i + 1]];
				if( vertex0[DIM_] > vertex1[DIM_] ) {
					_Swap( ref vertex0, ref vertex1 );
				}
				if( vertex0[DIM_] >= maxV || vertex1[DIM_] <= minV ) {
					// Out of range.
				} else {
					if( vertex0[DIM_] < minV ) {  /* Begin point smaller than minV */
						if( !_FuzzyZero( vertex0[DIM_] - vertex1[DIM_] ) ) { /* Overlap check( Check for 0 divide ) */
							/* Compute cross point in target area(minV) */
							Vector3 modVertex0 = ( vertex0 + (vertex1 - vertex0) * (minV - vertex0[DIM_]) / (vertex1[DIM_] - vertex0[DIM_]) );
							boxCollector.Collect( modVertex0 );
						}
					} else {
						boxCollector.Collect( vertex0 );
					}
					if( vertex1[DIM_] > maxV ) { /* End point bigger than maxV */
						if( !_FuzzyZero( vertex0[DIM_] - vertex1[DIM_] ) ) { /* Overlap check( Check for 0 divide ) */
							/* Compute cross point in target area(maxV) */
							Vector3 modVertex1 = ( vertex1 + (vertex0 - vertex1) * (vertex1[DIM_] - maxV) / (vertex1[DIM_] - vertex0[DIM_]) );
							boxCollector.Collect( modVertex1 );
						}
					} else {
						boxCollector.Collect( vertex1 );
					}
				}
			}
		}
		
		boxA = boxCollector.boxA;
		boxB = boxCollector.boxB;
		if( boxA == boxB ) {
			return false;
		}
		boxA[DIM_] = minV;
		boxB[DIM_] = maxV;
		return true;
	}

	void _MakeSlicedListFromAABB( Vector3 boxA, Vector3 boxB )
	{
		Vector3[] vertices = new Vector3[] {
			new Vector3( boxA.x, boxA.y, boxA.z ),
			new Vector3( boxA.x, boxB.y, boxA.z ),
			new Vector3( boxB.x, boxB.y, boxA.z ),
			new Vector3( boxB.x, boxA.y, boxA.z ),
			new Vector3( boxA.x, boxA.y, boxB.z ),
			new Vector3( boxA.x, boxB.y, boxB.z ),
			new Vector3( boxB.x, boxB.y, boxB.z ),
			new Vector3( boxB.x, boxA.y, boxB.z ),
		};
		
		int[] indices = new int[] {
			0, 1, 2, 3, /* Front side */
			3, 2, 6, 7, /* Right side */
			4, 5, 1, 0, /* Left side */
			1, 5, 6, 2, /* Upper side */
			4, 0, 3, 7, /* Lower side */
			7, 6, 5, 4, /* Back side */
		};
		
		_slicedVertexList = vertices;
		_slicedIndexList = indices;
	}

	void _MakeReducedListFromAABB( Vector3 boxA, Vector3 boxB )
	{
		Vector3[] vertices = new Vector3[] {
			new Vector3( boxA.x, boxA.y, boxA.z ),
			new Vector3( boxA.x, boxB.y, boxA.z ),
			new Vector3( boxB.x, boxB.y, boxA.z ),
			new Vector3( boxB.x, boxA.y, boxA.z ),
			new Vector3( boxA.x, boxA.y, boxB.z ),
			new Vector3( boxA.x, boxB.y, boxB.z ),
			new Vector3( boxB.x, boxB.y, boxB.z ),
			new Vector3( boxB.x, boxA.y, boxB.z ),
		};
		
		int[] indices = new int[] {
			0, 1, 2, /* Front(1) */
			2, 3, 0, /* Front(2) */
			3, 2, 6, /* Right(1) */
			6, 7, 3, /* Right(2) */
			4, 5, 1, /* Left(1) */
			1, 0, 4, /* Left(2) */
			1, 5, 6, /* Upper(1) */
			6, 2, 1, /* Upper(2) */
			4, 0, 3, /* Lower(1) */
			3, 7, 4, /* Lower(2) */
			7, 6, 5, /* Back(1) */
			5, 4, 7, /* Back(2) */
		};
		
		_reducedVertexList = vertices;
		_reducedIndexList = indices;
	}

	static void _AddSurface( List<int> indexList, int[] indices, int ptr, int ofst )
	{
		for( int i = 0; i < 4; ++i ) {
			indexList.Add( indices[i + ptr] + ofst );
		}
	}

	void _MakeSlicedListFromBoundingBox()
	{
		if( _slicedBoundingBoxA == null ) {
			Debug.LogError("");
			return;
		}

		List<Vector3> slicedVertexList = new List<Vector3>( 8 * _slicedBoundingBoxA.Length );
		List<int> slicedIndexList = new List<int>( 24 * _slicedBoundingBoxA.Length );

		/* Add Front/Back plane has LeftDown/LeftUp/RightUp/RightDown vertices. */
		for( int n = 0; n < _slicedBoundingBoxA.Length; ++n ) {
			Vector3 boxA = _slicedBoundingBoxA[n];
			Vector3 boxB = _slicedBoundingBoxB[n];
			Vector3 boxC = _slicedBoundingBoxC[n];
			Vector3 boxD = _slicedBoundingBoxD[n];
			switch( _slicedDimention ) {
			case 0: // X
				{
					Vector3[] vertices = new Vector3[] {
						new Vector3( boxA.x, boxA.y, boxA.z ),
						new Vector3( boxA.x, boxB.y, boxA.z ),
						new Vector3( boxD.x, boxD.y, boxC.z ),
						new Vector3( boxC.x, boxC.y, boxC.z ),
						new Vector3( boxA.x, boxA.y, boxB.z ),
						new Vector3( boxA.x, boxB.y, boxB.z ),
						new Vector3( boxD.x, boxD.y, boxD.z ),
						new Vector3( boxC.x, boxC.y, boxD.z ),
					};
					for( int i = 0; i < vertices.Length; ++i ) {
						slicedVertexList.Add( vertices[i] );
					}
				}
				break;
			case 1: // Y
				{
					Vector3[] vertices = new Vector3[] {
						new Vector3( boxA.x, boxA.y, boxA.z ),
						new Vector3( boxC.x, boxC.y, boxC.z ),
						new Vector3( boxD.x, boxC.y, boxC.z ),
						new Vector3( boxB.x, boxB.y, boxA.z ),
						new Vector3( boxA.x, boxA.y, boxB.z ),
						new Vector3( boxC.x, boxC.y, boxD.z ),
						new Vector3( boxD.x, boxC.y, boxD.z ),
						new Vector3( boxB.x, boxB.y, boxB.z ),
					};
					for( int i = 0; i < vertices.Length; ++i ) {
						slicedVertexList.Add( vertices[i] );
					}
				}
				break;
			case 2: // Z
				{
					Vector3[] vertices = new Vector3[] {
						new Vector3( boxA.x, boxA.y, boxA.z ),
						new Vector3( boxA.x, boxB.y, boxA.z ),
						new Vector3( boxB.x, boxB.y, boxA.z ),
						new Vector3( boxB.x, boxA.y, boxA.z ),
						new Vector3( boxC.x, boxC.y, boxD.z ),
						new Vector3( boxC.x, boxD.y, boxD.z ),
						new Vector3( boxD.x, boxD.y, boxD.z ),
						new Vector3( boxD.x, boxC.y, boxD.z ),
					};
					for( int i = 0; i < vertices.Length; ++i ) {
						slicedVertexList.Add( vertices[i] );
					}
				}
				break;
			}
		}

		int[] indices = new int[] {
			0, 1, 2, 3, /* Front */
			3, 2, 6, 7, /* Right */
			4, 5, 1, 0, /* Left */
			1, 5, 6, 2, /* Upper */
			4, 0, 3, 7, /* Lower */
			7, 6, 5, 4, /* Back */
		};

		switch( _slicedDimention ) {
		case 0: // X
			for( int n = 0, ofst = 0; n < _slicedBoundingBoxA.Length; ++n, ofst += 8 ) {
				_AddSurface( slicedIndexList, indices, 4 * 0, ofst );
				_AddSurface( slicedIndexList, indices, 4 * 3, ofst );
				_AddSurface( slicedIndexList, indices, 4 * 4, ofst );
				_AddSurface( slicedIndexList, indices, 4 * 5, ofst );
				/* Close left side */
				if( n == 0 ) {
					_AddSurface( slicedIndexList, indices, 4 * 2, ofst );
				}
				/* Close right side */
				if( n + 1 == _slicedBoundingBoxA.Length ) {
					_AddSurface( slicedIndexList, indices, 4 * 1, ofst );
				}
			}
			break;
		case 1: // Y
			for( int n = 0, ofst = 0; n < _slicedBoundingBoxA.Length; ++n, ofst += 8 ) {
				_AddSurface( slicedIndexList, indices, 4 * 0, ofst );
				_AddSurface( slicedIndexList, indices, 4 * 1, ofst );
				_AddSurface( slicedIndexList, indices, 4 * 2, ofst );
				_AddSurface( slicedIndexList, indices, 4 * 5, ofst );
				if( n == 0 ) {
					_AddSurface( slicedIndexList, indices, 4 * 4, ofst );
				}
				if( n + 1 == _slicedBoundingBoxA.Length ) {
					_AddSurface( slicedIndexList, indices, 4 * 3, ofst );
				}
			}
			break;
		case 2: // Z
			for( int n = 0, ofst = 0; n < _slicedBoundingBoxA.Length; ++n, ofst += 8 ) {
				_AddSurface( slicedIndexList, indices, 4 * 1, ofst );
				_AddSurface( slicedIndexList, indices, 4 * 2, ofst );
				_AddSurface( slicedIndexList, indices, 4 * 3, ofst );
				_AddSurface( slicedIndexList, indices, 4 * 4, ofst );
				if( n == 0 ) {
					_AddSurface( slicedIndexList, indices, 4 * 0, ofst );
				}
				if( n + 1 == _slicedBoundingBoxA.Length ) {
					_AddSurface( slicedIndexList, indices, 4 * 5, ofst );
				}
			}
			break;
		}

		_slicedVertexList = slicedVertexList.ToArray();
		_slicedIndexList = slicedIndexList.ToArray();
	}

	void _MakeReducedListFromSlicedList()
	{
		if( _slicedVertexList == null || _slicedIndexList == null ) {
			Debug.LogError("");
			return;
		}

		_reducedVertexList = _slicedVertexList.Clone() as Vector3[];
		_reducedIndexList = new int[_slicedIndexList.Length / 4 * 6];

		for( int i = 0, r = 0; i < _slicedIndexList.Length / 4 * 4; i += 4, r += 6 ) {
			int i0 = _slicedIndexList[i + 0];
			int i1 = _slicedIndexList[i + 1];
			int i2 = _slicedIndexList[i + 2];
			int i3 = _slicedIndexList[i + 3];
			_reducedIndexList[r + 0] = i0;
			_reducedIndexList[r + 1] = i1;
			_reducedIndexList[r + 2] = i2;
			_reducedIndexList[r + 3] = i2;
			_reducedIndexList[r + 4] = i3;
			_reducedIndexList[r + 5] = i0;
		}
	}

	void _TransformVertexList( ref Matrix4x4 transform )
	{
		if( _vertexList != null && _usedVertexList != null ) {
			Vector3[] vertexList = new Vector3[_vertexList.Length];
			for( int i = 0; i < _vertexList.Length; ++i ) {
				if( _usedVertexList[i] ) {
					vertexList[i] = transform.MultiplyPoint3x4( _vertexList[i] );
				}
			}
			_vertexList = vertexList; // memo: Override vertexList.
		} else {
			Debug.LogError("");
		}
	}

	void _TransformReducedList( ref Matrix4x4 transform )
	{
		if( _reducedVertexList != null ) {
			for( int i = 0; i < _reducedVertexList.Length; ++i ) {
				_reducedVertexList[i] = transform.MultiplyPoint3x4( _reducedVertexList[i] );
			}
		} else {
			Debug.LogError("");
		}
	}

	//----------------------------------------------------------------------------------------------------------------------------

	class BoxCollector
	{
		public bool isAnything = false;
		public Vector3 boxA = Vector3.zero;
		public Vector3 boxB = Vector3.zero;

		public void Collect( Vector3 vertex )
		{
			if( !isAnything ) {
				isAnything = true;
				boxA = boxB = vertex;
			} else {
				boxA = Min( boxA, vertex );
				boxB = Max( boxB, vertex );
			}
		}
		
		public void Collect( Vector3[] vertexArray )
		{
			if( vertexArray != null ) {
				for( int i = 0; i < vertexArray.Length; ++i ) {
					Collect( vertexArray[i] );
				}
			}
		}
	}

	static void _Swap( ref Vector3 a, ref Vector3 b )
	{
		Vector3 t = a;
		a = b;
		b = t;
	}

	static bool _FuzzyZero( float a )
	{
		return Mathf.Abs( a ) <= Mathf.Epsilon;
	}

	static float _GetVolume( Vector3 v )
	{
		return Mathf.Abs( v.x * v.y * v.z );
	}

	static float _GetBoxVolume( Vector3 boxA, Vector3 boxB )
	{
		Vector3 t = ( boxB - boxA );
		return Mathf.Abs( t.x * t.y * t.z );
	}

	public static Vector3 Min( Vector3 a, Vector3 b )
	{
		return new Vector3(
			Mathf.Min( a.x, b.x ),
			Mathf.Min( a.y, b.y ),
			Mathf.Min( a.z, b.z ) );
	}

	public static Vector3 Max( Vector3 a, Vector3 b )
	{
		return new Vector3(
			Mathf.Max( a.x, b.x ),
			Mathf.Max( a.y, b.y ),
			Mathf.Max( a.z, b.z ) );
	}

	public static Vector3 ScaledVector( Vector3 v, Vector3 s )
	{
		return new Vector3( v.x * s.x, v.y * s.y, v.z * s.z );
	}

	public static Quaternion InversedRotation( Quaternion q )
	{
		return new Quaternion( -q.x, -q.y, -q.z, q.w );
	}
}
