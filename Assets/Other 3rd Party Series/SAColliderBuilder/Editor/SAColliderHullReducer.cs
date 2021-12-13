/*
 * 
 * * C# / XNA  port of Bullet (c) 2011 Mark Neale <xexuxjy@hotmail.com>
 
Stan Melax Convex Hull Computation
Copyright (c) 2003-2006 Stan Melax http://www.melax.com/

This software is provided 'as-is', without any express or implied warranty.
In no event will the authors be held liable for any damages arising from the use of this software.
Permission is granted to anyone to use this software for any purpose, 
including commercial applications, and to alter it and redistribute it freely, 
subject to the following restrictions:

1. The origin of this software must not be misrepresented; you must not claim that you wrote the original software. If you use this software in a product, an acknowledgment in the product documentation would be appreciated but is not required.
2. Altered source versions must be plainly marked as such, and must not be misrepresented as being the original software.
3. This notice may not be removed or altered from any source distribution.
*/

/*
 * BulletXNA for Unity modified for SABoneCollider
 * Copyright (c) 2014 Stereoarts Nora

 - Change namespace BulletXNA.LinearMath to SAColliderHullReducer
 - Change IndexedVector3 to UnityEngine.Vector3
 - Change IndexedVector4 to UnityEngine.Vector4
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;

//namespace BulletXNA.LinearMath
namespace SAColliderHullReducer
{
	using IndexedVector3 = UnityEngine.Vector3;
	using IndexedVector4 = UnityEngine.Vector4;

	public class MathUtil
	{
		public static readonly float SIMD_RADS_PER_DEG = UnityEngine.Mathf.Deg2Rad;
		public static readonly float BT_LARGE_FLOAT = 1e18f;
		public static readonly IndexedVector3 MAX_VECTOR = new IndexedVector3(BT_LARGE_FLOAT,BT_LARGE_FLOAT,BT_LARGE_FLOAT);
		public static readonly IndexedVector3 MIN_VECTOR = new IndexedVector3(-BT_LARGE_FLOAT,-BT_LARGE_FLOAT,-BT_LARGE_FLOAT);

		public static void VectorMin(ref IndexedVector3 input, ref IndexedVector3 output)
		{
			output.x = UnityEngine.Mathf.Min(input.x, output.x);
			output.y = UnityEngine.Mathf.Min(input.y, output.y);
			output.z = UnityEngine.Mathf.Min(input.z, output.z);
		}

		public static void VectorMin(IndexedVector3 input, ref IndexedVector3 output)
		{
			VectorMin( ref input, ref output );
		}

		public static void VectorMax(ref IndexedVector3 input, ref IndexedVector3 output)
		{
			output.x = UnityEngine.Mathf.Max(input.x, output.x);
			output.y = UnityEngine.Mathf.Max(input.y, output.y);
			output.z = UnityEngine.Mathf.Max(input.z, output.z);
		}

		public static void VectorMax(IndexedVector3 input, ref IndexedVector3 output)
		{
			VectorMax( ref input, ref output );
		}
	}

	public class Math
	{
		public static float Sin( float v ) { return UnityEngine.Mathf.Sin( v ); }
		public static float Cos( float v ) { return UnityEngine.Mathf.Cos( v ); }
		public static float Abs( float v ) { return UnityEngine.Mathf.Abs( v ); }

		public static IndexedVector4 ToVector4( IndexedVector3 xyz, float w )
		{
			return new IndexedVector4( xyz.x, xyz.y, xyz.z, w );
		}
	}

	static class Extensions
	{
		public static float Length(this IndexedVector3 v)
		{
			return UnityEngine.Mathf.Sqrt(v.sqrMagnitude);
		}

		public static float LengthSquared(this IndexedVector3 v)
		{
			return v.sqrMagnitude;
		}

		public static IndexedVector3 Absolute(this IndexedVector3 v)
		{
			return new IndexedVector3(
				UnityEngine.Mathf.Abs(v.x),
				UnityEngine.Mathf.Abs(v.y),
				UnityEngine.Mathf.Abs(v.z));
		}

		public static IndexedVector3 ToVector3(this IndexedVector4 v)
		{
			return new IndexedVector3( v.x, v.y, v.z );
		}

		public static IndexedVector3 Normalized(this IndexedVector3 v)
		{
			return v.normalized;
		}
	}

	//#include <string.h>
	
	//#include "btConvexHull.h"
	//#include "LinearMath/List.h"
	//#include "LinearMath/btMinMax.h"
	//#include "LinearMath/IndexedVector3.h"
	
	public class ConvexHull
	{
	}
	
	
	
	//template <class T>
	//void Swap(T &a,T &b)
	//{
	//    T tmp = a;
	//    a=b;
	//    b=tmp;
	//}
	
	
	//----------------------------------
	
	public class int3  
	{
		public int x,y,z;
		public int3()
		{
		}
		public int3(int _x,int _y, int _z)
		{
			x=_x;
			y=_y;
			z=_z;
		}
		
		public override bool Equals(object obj)
		{
			int3 b =(int3)obj;
			if(x != b.x || y != b.y || z != b.z)
			{
				return false;
			}
			return true;
		}
		
		public int At(int index)
		{
			if(index == 0) return x;
			if(index == 1) return y;
			if(index == 2) return z;
			return -1;
		}
		
		public void At(int index,int value)
		{
			if (index == 0) x = value;
			else if (index == 1) y = value;
			else if (index == 2) z = value;
		}
		
		public override int GetHashCode()
		{
			return x + y + z;
		}
	}
	
	public class int4
	{
		public int x,y,z,w;
		public int4(){}
		public int4(int _x,int _y, int _z,int _w){x=_x;y=_y;z=_z;w=_w;}
		public int At(int index)
		{
			if(index == 0) return x;
			if(index == 1) return y;
			if(index == 2) return z;
			if(index == 3) return w;
			return -1;
		}
		
		public void At(int index,int value)
		{
			if(index == 0)
			{
				x = value;
			}
			else if(index == 1)
			{
				y = value;
			}
			else if(index == 2)
			{
				z = value ;
			}
			else if(index == 3)
			{
				w = value;
			}
		}
	}
	
	//------- IndexedVector4 ----------
	
	
	//public static IndexedVector4 PlaneFlip(IndexedVector4 &IndexedVector4){return IndexedVector4(-IndexedVector4.normal,-IndexedVector4.dist);}
	//inline int operator==( IndexedVector4 &a, IndexedVector4 &b ) { return (a.normal==b.normal && a.dist==b.dist); }
	//inline int coplanar( IndexedVector4 &a, IndexedVector4 &b ) { return (a==b || a==PlaneFlip(b)); }
	
	
	//--------- Utility Functions ------
	
	//IndexedVector3  PlaneLineIntersection(IndexedVector4 IndexedVector4, IndexedVector3 p0, IndexedVector3 p1);
	//IndexedVector3  PlaneProject(IndexedVector4 IndexedVector4, IndexedVector3 point);
	
	//IndexedVector3  ThreePlaneIntersection(IndexedVector4 p0,IndexedVector4 p1, IndexedVector4 p2);
	
	//float   DistanceBetweenLines(ref IndexedVector3 ustart, ref IndexedVector3 udir, ref IndexedVector3 vstart, ref IndexedVector3 vdir, IndexedVector3 *upoint=NULL, IndexedVector3 *vpoint=NULL);
	//IndexedVector3  TriNormal(ref IndexedVector3 v0, ref IndexedVector3 v1, ref IndexedVector3 v2);
	//IndexedVector3  NormalOf(IndexedVector3 *vert, int n);
	
	
	[Flags]
	public enum PlaneIntersectType
	{
		COPLANAR = 0, //(0)
		UNDER = 1,  //(1)
		OVER = 2,  //(2)
		SPLIT = 3 //(OVER|UNDER)
	}
	
	public enum HullError
	{
		QE_OK,            // success!
		QE_FAIL           // failed.
	}
	
	//typedef ConvexH::HalfEdge HalfEdge;
	
	
	public class ConvexH 
	{
		public class HalfEdge
		{
			public short ea;         // the other half of the edge (index into edges list)
			public byte v;  // the vertex at the start of this edge (index into vertices list)
			public byte p;  // the facet on which this edge lies (index into facets list)
			public HalfEdge(){}
			HalfEdge(short _ea,byte _v, byte _p)
			{
				ea = _ea;
				v = _v;
				p = _p;
			}
		}
		ConvexH()
		{
		}
		//public IList<IndexedVector3> vertices = new ObjectArray<IndexedVector3>();
		//public IList<HalfEdge> edges = new ObjectArray<HalfEdge>();
		//public IList<IndexedVector4> facets = new ObjectArray<IndexedVector4>();
		public IList<IndexedVector3> vertices = new List<IndexedVector3>();
		public IList<HalfEdge> edges = new List<HalfEdge>();
		public IList<IndexedVector4> facets = new List<IndexedVector4>();

		public ConvexH(int vertices_size,int edges_size,int facets_size)
		{
			//vertices.Capacity = vertices_size;
			//edges.Capacity = edges_size;
			//facets.Capacity = facets_size;
		}
	}
	
	
	
	
	
	public class VertFlag
	{
		public byte planetest;
		public byte junk;
		public byte undermap;
		public byte overmap;
	}
	
	public class EdgeFlag 
	{
		public byte planetest;
		public byte fixes;
		public short undermap;
		public short overmap;
	}
	
	public class PlaneFlag
	{
		public byte undermap;
		public byte overmap;
	}
	
	public class Coplanar
	{
		public ushort ea;
		public byte v0;
		public byte v1;
	}
	
	public class HullResult
	{
		public HullResult()
		{
			mPolygons = true;
			mNumOutputVertices = 0;
			mNumFaces = 0;
			mNumIndices = 0;
		}
		public bool                    mPolygons;                  // true if indices represents polygons, false indices are triangles
		public int            mNumOutputVertices;         // number of vertices in the output hull
		public IList<IndexedVector3>	m_OutputVertices = new List<IndexedVector3>();            // array of vertices
		public int            mNumFaces;                  // the number of faces produced
		public int            mNumIndices;                // the total number of indices
		public IList<int>    m_Indices = new List<int>();                   // pointer to indices.
		
		// If triangles, then indices are array indexes into the vertex list.
		// If polygons, indices are in the form (number of points in face) (p1, p2, p3, ..) etc..
	}
	
	
	public class PHullResult
	{
		public PHullResult()
		{
			mVcount = 0;
			mIndexCount = 0;
			mFaceCount = 0;
		}
		
		public int mVcount;
		public int mIndexCount;
		public int mFaceCount;
		public IList<IndexedVector3> mVertices = new List<IndexedVector3>();
		public IList<int> m_Indices = new List<int>();
	}
	
	[Flags]
	public enum HullFlag
	{
		QF_TRIANGLES         = (1<<0),             // report results as triangles, not polygons.
		QF_REVERSE_ORDER     = (1<<1),             // reverse order of the triangle indices.
		QF_DEFAULT           = QF_TRIANGLES
	}
	
	public class HullDesc
	{
		public HullDesc()
		{
			mFlags          = HullFlag.QF_DEFAULT;
			mVcount         = 0;
			//mVertices       = 0;
			//mVertexStride   = sizeof(btVector3);
			mNormalEpsilon  = 0.001f;
			mMaxVertices	= 4096; // maximum number of points to be considered for a convex hull.
			mMaxFaces	= 4096;
		}
		
		public HullDesc(HullFlag flag,
		                int vcount,
		                IList<IndexedVector3> vertices)
		{
			mFlags          = flag;
			mVcount         = vcount;
			mVertices       = vertices;
			//mVertexStride   = stride;
			mNormalEpsilon  = 0.001f;
			mMaxVertices    = 4096;
		}
		
		public bool HasHullFlag(HullFlag flag)
		{
			return ( (mFlags & flag) != 0 );
		}
		
		void SetHullFlag(HullFlag flag)
		{
			mFlags|=flag;
		}
		
		void ClearHullFlag(HullFlag flag)
		{
			mFlags&=~flag;
		}
		
		public HullFlag mFlags;           // flags to use when generating the convex hull.
		public int mVcount;          // number of vertices in the input point cloud
		public IList<IndexedVector3> mVertices = new List<IndexedVector3>();        // the array of vertices.
		public int mVertexStride;    // the stride of each vertex, in bytes.
		public float mNormalEpsilon;   // the epsilon for removing duplicates.  This is a normalized value, if normalized bit is on.
		public int mMaxVertices;     // maximum number of vertices to be considered for the hull!
		public int mMaxFaces;
	}
	
	
	
	
	
	
	
	//int operator ==(int3 &a,int3 &b);
	//int operator ==(int3 &a,int3 &b) 
	//{
	//    for(int i=0;i<3;i++) 
	//    {
	//        if(a[i]!=b[i]) return 0;
	//    }
	//    return 1;
	//}
	
	
	//int above(IndexedVector3* vertices,int3& t, ref IndexedVector3 p, float epsilon);
	
	public class HullTriangle : int3
	{
		public int3 n;
		public int id;
		public int vmax;
		public float rise;
		
		public HullTriangle(int a,int b,int c):base(a,b,c)
		{
			n = new int3(-1,-1,-1);
			vmax=-1;
			rise = 0.0f;
		}
		
		public int Neib(int a,int b)
		{
			int er=-1;
			int i;
			for(i=0;i<3;i++) 
			{
				int i1=(i+1)%3;
				int i2=(i+2)%3;
				if((At(i)==a && At(i1)==b)) return n.At(i2);
				if((At(i)==b && At(i1)==a)) return n.At(i2);
			}
			Debug.Assert(false);
			return er;
		}
		
		public void Neib(int a, int b,int value)
		{
			//int er = -1;
			int i;
			for (i = 0; i < 3; i++)
			{
				int i1 = (i + 1) % 3;
				int i2 = (i + 2) % 3;
				if ((At(i) == a && At(i1) == b))
				{
					n.At(i2, value);
					break;
				}
				if ((At(i) == b && At(i1) == a))
				{
					n.At(i2,value);
					break;
				}
			}
		}
		
	}
	
	public class HullLibrary
	{
		public static int MaxDirFiltered(IList<IndexedVector3> p,int count,ref IndexedVector3 dir,IList<int> allow)
		{
			Debug.Assert(count != 0);
			int m=-1;
			for (int i = 0; i < count; i++)
			{
				if (allow[i] != 0)
				{
					if (m == -1 || IndexedVector3.Dot(p[i], dir) > IndexedVector3.Dot(p[m], dir))
						m = i;
				}
			}
			Debug.Assert(m!=-1);
			return m;
		} 
		
		public static IndexedVector3 Orth(ref IndexedVector3 v)
		{
			IndexedVector3 a = IndexedVector3.Cross(v,new IndexedVector3(0,0,1));
			IndexedVector3 b = IndexedVector3.Cross(v,new IndexedVector3(0,1,0));
			if (a.Length() > b.Length())
			{
				return IndexedVector3.Normalize(a);
			} 
			else 
			{
				return IndexedVector3.Normalize(b);
			}
		}
		public static int MaxDirSterId(IList<IndexedVector3> p, int count, IndexedVector3 dir, IList<int> allow)
		{
			return MaxDirSterId(p, count, ref dir, allow);
		}
		
		public static int MaxDirSterId(IList<IndexedVector3> p, int count, ref IndexedVector3 dir, IList<int> allow)
		{
			int m = -1;
			while (m == -1)
			{
				m = MaxDirFiltered(p, count, ref dir, allow);
				if (allow[m] == 3) return m;
				IndexedVector3 u = Orth(ref dir);
				IndexedVector3 v = IndexedVector3.Cross(u, dir);
				int ma = -1;
				for (float x = 0.0f; x <= 360.0f; x += 45.0f)
				{
					float s = (float)Math.Sin(MathUtil.SIMD_RADS_PER_DEG * (x));
					float c = (float)Math.Cos(MathUtil.SIMD_RADS_PER_DEG * (x));
					IndexedVector3 adjustedDir = dir + (u * s + v * c) * 0.025f;
					int mb = MaxDirFiltered(p, count, ref adjustedDir, allow);
					if (ma == m && mb == m)
					{
						allow[m] = 3;
						return m;
					}
					if (ma != -1 && ma != mb)  // Yuck - this is really ugly
					{
						int mc = ma;
						for (float xx = x - 40.0f; xx <= x; xx += 5.0f)
						{
							float s1 = (float)Math.Sin(MathUtil.SIMD_RADS_PER_DEG * (xx));
							float c1 = (float)Math.Cos(MathUtil.SIMD_RADS_PER_DEG * (xx));
							IndexedVector3 adjustedDir2 = dir + (u * s1 + v * c1) * 0.025f;
							int md = MaxDirFiltered(p, count, ref adjustedDir2, allow);
							if (mc == m && md == m)
							{
								allow[m] = 3;
								return m;
							}
							mc = md;
						}
					}
					ma = mb;
				}
				allow[m] = 0;
				m = -1;
			}
			Debug.Assert(false);
			return m;
		}
		
		public bool Above(IList<IndexedVector3> vertices, int3 t, IndexedVector3 p, float epsilon)
		{
			return Above(vertices, t, ref p, epsilon);
		}
		
		public bool Above(IList<IndexedVector3> vertices, int3 t, ref IndexedVector3 p, float epsilon)
		{
			IndexedVector3 n = HullLibrary.TriNormal(vertices[t.At(0)], vertices[t.At(1)], vertices[t.At(2)]);
			return (IndexedVector3.Dot(n, p - vertices[t.At(0)]) > epsilon); // EPSILON???
		}
		
		public bool HasEdge(int3 t, int a, int b)
		{
			for (int i = 0; i < 3; i++)
			{
				int i1 = (i + 1) % 3;
				if (t.At(i) == a && t.At(i1) == b) return true;
			}
			return false;
		}
		
		public bool HasVert(int3 t, int v)
		{
			return (t.At(0) == v || t.At(1) == v || t.At(2) == v);
		}
		
		public int ShareEdge(int3 a, int3 b)
		{
			int i;
			for (i = 0; i < 3; i++)
			{
				int i1 = (i + 1) % 3;
				if (HasEdge(a, b.At(i1), b.At(i))) return 1;
			}
			return 0;
		}
		
		public void B2bFix(HullTriangle s, HullTriangle t)
		{
			for (int i = 0; i < 3; i++)
			{
				int i1 = (i + 1) % 3;
				int i2 = (i + 2) % 3;
				int a = s.At(i1);
				int b = s.At(i2);
				Debug.Assert(m_tris[s.Neib(a, b)].Neib(b, a) == s.id);
				Debug.Assert(m_tris[t.Neib(a, b)].Neib(b, a) == t.id);
				m_tris[s.Neib(a, b)].Neib(b, a, t.Neib(b, a));
				m_tris[t.Neib(b, a)].Neib(a, b,s.Neib(a, b));
			}
		}
		
		void RemoveB2b(HullTriangle s, HullTriangle t)
		{
			B2bFix(s, t);
			DeAllocateTriangle(s);
			
			DeAllocateTriangle(t);
		}
		
		void CheckIt(HullTriangle t)
		{
			//(void)t;
			
			Debug.Assert(m_tris[t.id] == t);
			for (int i = 0; i < 3; i++)
			{
				int i1 = (i + 1) % 3;
				int i2 = (i + 2) % 3;
				int a = t.At(i1);
				int b = t.At(i2);
				
				// release compile fix
				//(void)i1;
				//(void)i2;
				//(void)a;
				//(void)b;
				
				Debug.Assert(a != b);
				Debug.Assert(m_tris[t.n.At(i)].Neib(b, a) == t.id);
			}
		}
		
		public HullTriangle AllocateTriangle(int a, int b, int c)
		{
			HullTriangle tr = new HullTriangle(a, b, c);
			tr.id = m_tris.Count;
			m_tris.Add(tr);
			return tr;
		}
		
		public void DeAllocateTriangle(HullTriangle tri)
		{
			Debug.Assert(m_tris[tri.id] == tri);
			m_tris[tri.id] = null;
		}
		
		
		public void Extrude(HullTriangle t0, int v)
		{
			int3 t = t0;
			int n = m_tris.Count;
			HullTriangle ta = AllocateTriangle(v, t.At(1), t.At(2));
			ta.n = new int3(t0.n.At(0), n + 1, n + 2);
			m_tris[t0.n.At(0)].Neib(t.At(1), t.At(2), n + 0);
			HullTriangle tb = AllocateTriangle(v, t.At(2), t.At(0));
			tb.n = new int3(t0.n.At(1), n + 2, n + 0);
			m_tris[t0.n.At(1)].Neib(t.At(2), t.At(0), n + 1);
			HullTriangle tc = AllocateTriangle(v, t.At(0), t.At(1));
			tc.n = new int3(t0.n.At(2), n + 0, n + 1);
			m_tris[t0.n.At(2)].Neib(t.At(0), t.At(1), n + 2);
			CheckIt(ta);
			CheckIt(tb);
			CheckIt(tc);
			if (HasVert(m_tris[ta.n.At(0)], v))
			{
				RemoveB2b(ta, m_tris[ta.n.At(0)]);
			}
			if (HasVert(m_tris[tb.n.At(0)], v))
			{
				RemoveB2b(tb, m_tris[tb.n.At(0)]);
			}
			if (HasVert(m_tris[tc.n.At(0)], v))
			{
				RemoveB2b(tc, m_tris[tc.n.At(0)]);
			}
			DeAllocateTriangle(t0);
		}
		
		public HullTriangle Extrudable(float epsilon)
		{
			HullTriangle t = null;
			for (int i = 0; i < m_tris.Count; i++)
			{
				if (t == null || (m_tris[i] != null && t.rise < m_tris[i].rise))
				{
					t = m_tris[i];
				}
			}
			return (t.rise > epsilon) ? t : null;
		}
		
		public int4 FindSimplex(IList<IndexedVector3> verts, int verts_count, IList<int> allow)
		{
			IndexedVector3[] basis = new IndexedVector3[3];
			basis[0] = new IndexedVector3(0.01f, 0.02f, 1.0f);
			int p0 = HullLibrary.MaxDirSterId(verts, verts_count, basis[0], allow);
			int p1 = HullLibrary.MaxDirSterId(verts, verts_count, -basis[0], allow);
			basis[0] = verts[p0] - verts[p1];
			if (p0 == p1 || basis[0] == /*IndexedVector3.Zero*/ IndexedVector3.zero)
			{
				return new int4(-1, -1, -1, -1);
			}
			basis[1] = IndexedVector3.Cross(new IndexedVector3(1f, 0.02f, 0f), basis[0]);
			basis[2] = IndexedVector3.Cross(new IndexedVector3(-0.02f, 1f, 0f), basis[0]);
			if (basis[1].Length() > basis[2].Length())
			{
				basis[1].Normalize();
			}
			else
			{
				basis[1] = basis[2];
				basis[1].Normalize();
			}
			int p2 = MaxDirSterId(verts, verts_count, basis[1], allow);
			if (p2 == p0 || p2 == p1)
			{
				p2 = MaxDirSterId(verts, verts_count, -basis[1], allow);
			}
			if (p2 == p0 || p2 == p1)
			{
				return new int4(-1, -1, -1, -1);
			}
			basis[1] = verts[p2] - verts[p0];
			basis[2] = IndexedVector3.Normalize(IndexedVector3.Cross(basis[1], basis[0]));
			int p3 = MaxDirSterId(verts, verts_count, basis[2], allow);
			if (p3 == p0 || p3 == p1 || p3 == p2)
			{
				p3 = MaxDirSterId(verts, verts_count, -basis[2], allow);
			}
			if (p3 == p0 || p3 == p1 || p3 == p2)
			{
				return new int4(-1, -1, -1, -1);
			}
			Debug.Assert(!(p0 == p1 || p0 == p2 || p0 == p3 || p1 == p2 || p1 == p3 || p2 == p3));
			if (IndexedVector3.Dot(verts[p3] - verts[p0], IndexedVector3.Cross(verts[p1] - verts[p0], verts[p2] - verts[p0])) < 0)
			{
				// Swap
				int temp = p2;
				p2 = p3;
				p3 = temp;
			}
			return new int4(p0, p1, p2, p3);
		}
		
		public int CalcHullGen(IList<IndexedVector3> verts, int verts_count, int vlimit)
		{
			if(verts_count <4) return 0;
			if(vlimit==0)
			{
				vlimit=1000000000;
			}
			IndexedVector3 bmin = MathUtil.MAX_VECTOR;
			IndexedVector3 bmax = MathUtil.MIN_VECTOR;
			
			IList<int> isextreme = new List<int>(verts_count);
			IList<int> allow = new List<int>(verts_count);
			
			for(int j=0;j<verts_count;j++) 
			{
				allow.Add(1);
				isextreme.Add(0);
				MathUtil.VectorMin(verts[j], ref bmin);
				MathUtil.VectorMax(verts[j], ref bmax);
			}
			float epsilon = (bmax-bmin).Length() * 0.001f;
			Debug.Assert (epsilon != 0.0);
			
			int4 p = FindSimplex(verts,verts_count,allow);
			if(p.x==-1) return 0; // simplex failed
			
			IndexedVector3 center = (verts[p.At(0)]+verts[p.At(1)]+verts[p.At(2)]+verts[p.At(3)]) / 4.0f;  // a valid interior point
			HullTriangle t0 = AllocateTriangle(p.At(2),p.At(3),p.At(1)); t0.n= new int3(2,3,1);
			HullTriangle t1 = AllocateTriangle(p.At(3),p.At(2),p.At(0)); t1.n= new int3(3,2,0);
			HullTriangle t2 = AllocateTriangle(p.At(0),p.At(1),p.At(3)); t2.n= new int3(0,1,3);
			HullTriangle t3 = AllocateTriangle(p.At(1),p.At(0),p.At(2)); t3.n= new int3(1,0,2);
			isextreme[p.At(0)]=isextreme[p.At(1)]=isextreme[p.At(2)]=isextreme[p.At(3)]=1;
			CheckIt(t0);CheckIt(t1);CheckIt(t2);CheckIt(t3);
			
			for(int j=0;j<m_tris.Count;j++)
			{
				HullTriangle t=m_tris[j];
				Debug.Assert(t != null);
				Debug.Assert(t.vmax<0);
				IndexedVector3 n=HullLibrary.TriNormal(verts[t.At(0)],verts[t.At(1)],verts[t.At(2)]);
				t.vmax = MaxDirSterId(verts,verts_count,n,allow);
				t.rise = IndexedVector3.Dot(n,verts[t.vmax]-verts[t.At(0)]);
			}
			HullTriangle te = null;
			vlimit-=4;
			while(vlimit >0 && ((te=Extrudable(epsilon)) != null))
			{
				//int3 ti=te;
				int v=te.vmax;
				Debug.Assert(v != -1);
				Debug.Assert(isextreme[v] == 0);  // wtf we've already done this vertex
				isextreme[v]=1;
				//if(v==p0 || v==p1 || v==p2 || v==p3) continue; // done these already
				int j=m_tris.Count;
				while(j-- > 0) 
				{
					if(m_tris[j] == null) 
					{
						continue;
					}
					int3 t=m_tris[j];
					if(Above(verts,t,verts[v],0.01f*epsilon)) 
					{
						Extrude(m_tris[j],v);
					}
				}
				// now check for those degenerate cases where we have a flipped triangle or a really skinny triangle
				j=m_tris.Count;
				while(j-- > 0)
				{
					if(m_tris[j] == null)
					{
						continue;
					}
					if(!HasVert(m_tris[j],v)) 
					{
						break;
					}
					int3 nt=m_tris[j];
					if(Above(verts,nt,center,0.01f*epsilon)  || IndexedVector3.Cross(verts[nt.At(1)]-verts[nt.At(0)],verts[nt.At(2)]-verts[nt.At(1)]).Length()< epsilon*epsilon*0.1f )
					{
						HullTriangle nb = m_tris[m_tris[j].n.At(0)];
						Debug.Assert(nb != null);
						Debug.Assert(!HasVert(nb,v));
						Debug.Assert(nb.id<j);
						Extrude(nb,v);
						j=m_tris.Count; 
					}
				} 
				j=m_tris.Count;
				while(j-- != 0)
				{
					HullTriangle t=m_tris[j];
					if(t == null)
					{
						continue;
					}
					if(t.vmax>=0) 
					{
						break;
					}
					IndexedVector3 n=TriNormal(verts[t.At(0)],verts[t.At(1)],verts[t.At(2)]);
					t.vmax = MaxDirSterId(verts,verts_count,n,allow);
					if(isextreme[t.vmax] != 0) 
					{
						t.vmax=-1; // already done that vertex - algorithm needs to be able to terminate.
					}
					else
					{
						t.rise = IndexedVector3.Dot(n,verts[t.vmax]-verts[t.At(0)]);
					}
				}
				vlimit --;
			}
			return 1;
		}
		
		public int CalcHull(IList<IndexedVector3> verts, int verts_count, IList<int> tris_out, ref int tris_count, int vlimit)
		{
			int rc = CalcHullGen(verts, verts_count, vlimit);
			if (rc == 0)
			{
				return 0;
			}
			IList<int> ts = new List<int>();
			int i;
			
			for (i = 0; i < m_tris.Count; i++)
			{
				if (m_tris[i] != null)
				{
					for (int j = 0; j < 3; j++)
					{
						ts.Add((m_tris[i]).At(j));
					}
					DeAllocateTriangle(m_tris[i]);
				}
			}
			tris_count = ts.Count / 3;
			// FIXME
			tris_out.Clear();
			
			for (i = 0; i < ts.Count; i++)
			{
				tris_out.Add(ts[i]);
			}
			m_tris.Clear();
			
			return 1;
		}
		
		
		public bool ComputeHull(int vcount, IList<IndexedVector3> vertices, PHullResult result, int vlimit)
		{
			
			int tris_count = 0;
			int ret = CalcHull(vertices, vcount, result.m_Indices, ref tris_count, vlimit);
			if (ret == 0)
			{
				return false;
			}
			result.mIndexCount = (int)(tris_count * 3);
			result.mFaceCount = (int)tris_count;
			result.mVertices = vertices;
			result.mVcount = (int)vcount;
			return true;
			
		}
		
		
		public void ReleaseHull(PHullResult result)
		{
			if (result.m_Indices.Count != 0)
			{
				result.m_Indices.Clear();
			}
			
			result.mVcount = 0;
			result.mIndexCount = 0;
			result.mVertices.Clear();
		}
		
		
		//*********************************************************************
		//*********************************************************************
		//********  HullLib header
		//*********************************************************************
		//*********************************************************************
		
		//*********************************************************************
		//*********************************************************************
		//********  HullLib implementation
		//*********************************************************************
		//*********************************************************************
		
		public HullError CreateConvexHull(HullDesc desc,           // describes the input request
		                                  HullResult result)         // contains the resulst
		{
			HullError ret = HullError.QE_FAIL;
			
			PHullResult hr = new PHullResult();
			
			int vcount = desc.mVcount;
			if (vcount < 8)
			{
				vcount = 8;
			}
			
			IList<IndexedVector3> vertexSource = new List<IndexedVector3>((int)vcount);
			for (int i = 0; i < vcount; ++i)
			{
				vertexSource.Add(/*IndexedVector3.Zero*/IndexedVector3.zero);
			}
			
#if false
			IndexedVector3 scale = new IndexedVector3(1);
#else
			IndexedVector3 scale = IndexedVector3.one;
#endif

			int ovcount = 0;
			
			bool ok = CleanupVertices(desc.mVcount, desc.mVertices, desc.mVertexStride, ref ovcount, vertexSource, desc.mNormalEpsilon, ref scale); // normalize point cloud, remove duplicates!
			
			if (ok)
			{
				//		if ( 1 ) // scale vertices back to their original size.
				{
					for (int i = 0; i < ovcount; i++)
					{
						IndexedVector3 v = vertexSource[i];
						//v.X *= scale.X;
						//v.Y *= scale.Y;
						//v.Z *= scale.Z;
						v.x *= scale.x;
						v.y *= scale.y;
						v.z *= scale.z;
						vertexSource[i] = v;
					}
				}
				
				ok = ComputeHull(ovcount, vertexSource, hr, desc.mMaxVertices);
				
				if (ok)
				{
					
					// re-index triangle mesh so it refers to only used vertices, rebuild a new vertex table.
#if false
					IList<IndexedVector3> vertexScratch = new ObjectArray<IndexedVector3>((int)hr.mVcount);
#else
					IList<IndexedVector3> vertexScratch = new List<IndexedVector3>((int)hr.mVcount);
					for( int i = 0; i < hr.mVcount; ++i ) {
						vertexScratch.Add( IndexedVector3.zero );
					}
#endif

					BringOutYourDead(hr.mVertices, hr.mVcount, vertexScratch, ref ovcount, hr.m_Indices, hr.mIndexCount);
					
					ret = HullError.QE_OK;
					
					if (desc.HasHullFlag(HullFlag.QF_TRIANGLES)) // if he wants the results as triangle!
					{
						result.mPolygons = false;
						result.mNumOutputVertices = ovcount;
						//result.m_OutputVertices.resize(ovcount);
						result.m_OutputVertices.Clear() ;
						result.mNumFaces = hr.mFaceCount;
						result.mNumIndices = hr.mIndexCount;
						
						//result.m_Indices.resize(hr.mIndexCount);
						result.m_Indices.Clear();
						
						for (int i = 0; i < ovcount; ++i)
						{
							result.m_OutputVertices.Add(vertexScratch[i]);
						}
						//memcpy(&result.m_OutputVertices[0], &vertexScratch[0], sizeof(IndexedVector3) * ovcount);
						
						if (desc.HasHullFlag(HullFlag.QF_REVERSE_ORDER))
						{
							
							IList<int> source = hr.m_Indices;
							IList<int> dest = result.m_Indices;
							
							for (int i = 0; i < hr.mFaceCount; i++)
							{
								int index = (i * 3);
								//dest[index + 0] = source[index + 2];
								//dest[index + 1] = source[index + 1];
								//dest[index + 2] = source[index + 0];
								dest.Add(source[index + 2]);
								dest.Add(source[index + 1]);
								dest.Add(source[index + 0]);
							}
						}
						else
						{
							for (int i = 0; i < hr.mIndexCount; ++i)
							{
								//result.m_Indices[i] = hr.m_Indices[i];
								result.m_Indices.Add(hr.m_Indices[i]);
							}
							//memcpy(&result.m_Indices[0], &hr.m_Indices[0], sizeof(int) * hr.mIndexCount);
						}
					}
					else
					{
						result.mPolygons = true;
						result.mNumOutputVertices = ovcount;
						//result.m_OutputVertices.resize(ovcount);
						result.m_OutputVertices.Clear();
						result.mNumFaces = hr.mFaceCount;
						result.mNumIndices = hr.mIndexCount + hr.mFaceCount;
						//result.m_Indices.resize(result.mNumIndices);
						result.m_Indices.Clear();
						
						for (int i = 0; i < ovcount; ++i)
						{
							result.m_OutputVertices.Add(vertexScratch[i]);
						}
						//memcpy(&result.m_OutputVertices[0], &vertexScratch[0], sizeof(IndexedVector3) * ovcount);
						
						//				if ( 1 )
						{
							IList<int> source = hr.m_Indices;
							IList<int> dest = result.m_Indices;
							
							for (int i = 0; i < hr.mFaceCount; i++)
							{
								//int destIndex = (i * 4);
								int srcIndex = (i * 3);
								dest[0] = 3;
								if (desc.HasHullFlag(HullFlag.QF_REVERSE_ORDER))
								{
									dest.Add(source[srcIndex+2]);
									dest.Add(source[srcIndex + 1]);
									dest.Add(source[srcIndex + 0]);
								}
								else
								{
									dest.Add(source[srcIndex + 0]);
									dest.Add(source[srcIndex + 1]);
									dest.Add(source[srcIndex + 2]);
								}
							}
						}
					}
					ReleaseHull(hr);
				}
			}
			
			return ret;
		}
		
		
		
		public HullError ReleaseResult(HullResult result) // release memory allocated for this result, we are done with it.
		{
			if (result.m_OutputVertices.Count != 0)
			{
				result.mNumOutputVertices = 0;
				result.m_OutputVertices.Clear();
			}
			if (result.m_Indices.Count != 0)
			{
				result.mNumIndices = 0;
				result.m_Indices.Clear();
			}
			return HullError.QE_OK;
		}
		
		
		public static void AddPoint(ref int vcount, IList<IndexedVector3> p, float x, float y, float z)
		{
			// XXX, might be broken
			IndexedVector3 dest = p[vcount];
#if false
			dest.X = x;
			dest.Y = y;
			dest.Z = z;
#else
			dest.Set(x,y,z);
#endif
			p[vcount] = dest;
			vcount++;
		}
		
		public float GetDist(float px, float py, float pz, ref IndexedVector3 p2)
		{
#if false
			float dx = px - p2.X;
			float dy = py - p2.Y;
			float dz = pz - p2.Z;
			
			return dx * dx + dy * dy + dz * dz;
#else
			return (new IndexedVector3(px,py,pz) - p2).sqrMagnitude;
#endif
		}
		
		
		
		public bool CleanupVertices(int svcount,
		                            IList<IndexedVector3> svertices,
		                            int stride,
		                            ref int vcount,       // output number of vertices
		                            IList<IndexedVector3> vertices,                 // location to store the results.
		                            float normalepsilon,
		                            ref IndexedVector3 scale)
		{
			if ( svcount == 0 ) 
			{
				return false;
			}
			
			m_vertexIndexMapping.Clear();
			
			vcount = 0;
			
			IndexedVector3 recip = new IndexedVector3();

#if false
			scale = new IndexedVector3(1);
#else
			scale = IndexedVector3.one;
#endif

			IndexedVector3 bmin = MathUtil.MAX_VECTOR;
			IndexedVector3 bmax = MathUtil.MIN_VECTOR;
			
			//char *vtx = (char *) svertices;
			
			//	if ( 1 )
			{
				for (int i=0; i<svcount; i++)
				{
					IndexedVector3 p = svertices[i];
					MathUtil.VectorMin(ref p,ref bmin);
					MathUtil.VectorMax(ref p,ref bmax);
					svertices[i] = p;
				}
			}
			
			IndexedVector3 diff = bmax - bmin;
			
			IndexedVector3 center = diff * 0.5f;
			center += bmin;
			if ( diff.x < EPSILON || diff.y < EPSILON || diff.z < EPSILON || svcount < 3 )
			{
				
				float len = float.MaxValue;
				
				if ( diff.x > EPSILON && diff.x < len ) len = diff.x;
				if ( diff.y > EPSILON && diff.y < len ) len = diff.y;
				if ( diff.z > EPSILON && diff.z < len ) len = diff.z;
				
				if ( len == float.MaxValue)
				{
#if false
					diff = new IndexedVector3(0.01f);
#else
					diff = new IndexedVector3(0.01f,0.01f,0.01f);
#endif
				}
				else
				{
					if ( diff.x < EPSILON ) diff.x = len * 0.05f; // 1/5th the shortest non-zero edge.
					if ( diff.y < EPSILON ) diff.y = len * 0.05f;
					if ( diff.z < EPSILON ) diff.z = len * 0.05f;
				}
				
				float x1 = center.x - diff.x;
				float x2 = center.x + diff.x;
				
				float y1 = center.y - diff.y;
				float y2 = center.y + diff.y;
				
				float z1 = center.z - diff.z;
				float z2 = center.z + diff.z;
				
				AddPoint(ref vcount,vertices,x1,y1,z1);
				AddPoint(ref vcount, vertices, x2, y1, z1);
				AddPoint(ref vcount, vertices, x2, y2, z1);
				AddPoint(ref vcount, vertices, x1, y2, z1);
				AddPoint(ref vcount, vertices, x1, y1, z2);
				AddPoint(ref vcount, vertices, x2, y1, z2);
				AddPoint(ref vcount, vertices, x2, y2, z2);
				AddPoint(ref vcount, vertices, x1, y2, z2);
				
				return true; // return cube
				
				
			}
			else
			{
				if ( scale.LengthSquared() > 0 )
				{
					scale = diff;
					//scale.Value.X = dx;
					//scale.Value.Y = dy;
					//scale.Value.Z = dz;
					
					recip.x = 1 / diff.x;
					recip.y = 1 / diff.y;
					recip.z = 1 / diff.z;
					
					//recip[0] = 1 / dx;
					//recip[1] = 1 / dy;
					//recip[2] = 1 / dz;

#if false
					center = center * recip;
#else
					center.x = center.x * recip.x;
					center.y = center.y * recip.y;
					center.z = center.z * recip.z;
#endif
					
					//center.X*=recip[0];
					//center.Y*=recip[1];
					//center.Z*=recip[2];
					
				}
				
			}
			
			//vtx = (char *) svertices;
			
			for (int i=0; i<svcount; i++)
			{
				IndexedVector3 p = svertices[i];
				
				
				if ( scale.LengthSquared() > 0 )
				{
					//p.Normalize();
					p.x *= recip.x;
					p.y *= recip.y;
					p.z *= recip.z;
					svertices[i] = p;
				}
				
				//		if ( 1 )
				{
					int j = 0;
					for (j=0; j<vcount; j++)
					{
						/// XXX might be broken
						IndexedVector3 v = vertices[j];
						
						IndexedVector3 temp = v - p;
						
						IndexedVector3 absTemp = temp.Absolute();
						
						if (absTemp.x < normalepsilon && absTemp.y < normalepsilon && absTemp.z < normalepsilon)
						{
							// ok, it is close enough to the old one
							// now let us see if it is further from the center of the point cloud than the one we already recorded.
							// in which case we keep this one instead.
							
							float dist1 = (p - center).LengthSquared();
							float dist2 = (v-center).LengthSquared();
							
							if ( dist1 > dist2 )
							{
								vertices[j] = p;
							}
							
							break;
						}
					}
					
					if ( j == vcount )
					{
						vertices[vcount] = p;
						vcount++;
					}
					m_vertexIndexMapping.Add(j);
				}
			}
			
			// ok..now make sure we didn't prune so many vertices it is now invalid.
			//	if ( 1 )
			{
				float[] bmin2= new float[]{  float.MaxValue,  float.MaxValue,  float.MaxValue };
				float[] bmax2 = new float[] { float.MinValue, float.MinValue, float.MinValue };
				
				for (int i=0; i<vcount; i++)
				{
					IndexedVector3 p = vertices[i];
					if(p.x < bmin2[0]) bmin2[0] = p.x;
					if (p.x > bmax2[0]) bmax2[0] = p.x;
					if (p.y < bmin2[1]) bmin2[1] = p.y;
					if (p.y > bmax2[1]) bmax2[1] = p.y;
					if (p.z < bmin2[2]) bmin2[2] = p.z;
					if (p.z > bmax2[2]) bmax2[2] = p.z;
					
				}
				
				float dx2 = bmax2[0] - bmin2[0];
				float dy2 = bmax2[1] - bmin2[1];
				float dz2 = bmax2[2] - bmin2[2];
				
				if ( dx2 < EPSILON || dy2 < EPSILON || dz2 < EPSILON || vcount < 3)
				{
					float cx = dx2*0.5f + bmin2[0];
					float cy = dy2*0.5f + bmin2[1];
					float cz = dz2*0.5f + bmin2[2];
					
					float len = float.MaxValue;
					
					if ( dx2 >= EPSILON && dx2 < len ) len = dx2;
					if ( dy2 >= EPSILON && dy2 < len ) len = dy2;
					if ( dz2 >= EPSILON && dz2 < len ) len = dz2;
					
					if ( len == float.MaxValue )
					{
						dx2 = dy2 = dz2 = 0.01f; // one centimeter
					}
					else
					{
						if ( dx2 < EPSILON ) dx2 = len * 0.05f; // 1/5th the shortest non-zero edge.
						if ( dy2 < EPSILON ) dy2 = len * 0.05f;
						if ( dz2 < EPSILON ) dz2 = len * 0.05f;
					}
					
					float x1 = cx - dx2;
					float x2 = cx + dx2;
					
					float y1 = cy - dy2;
					float y2 = cy + dy2;
					
					float z1 = cz - dz2;
					float z2 = cz + dz2;
					
					vcount = 0; // add box
					
					AddPoint(ref vcount,vertices,x1,y1,z1);
					AddPoint(ref vcount, vertices, x2, y1, z1);
					AddPoint(ref vcount, vertices, x2, y2, z1);
					AddPoint(ref vcount, vertices, x1, y2, z1);
					AddPoint(ref vcount, vertices, x1, y1, z2);
					AddPoint(ref vcount, vertices, x2, y1, z2);
					AddPoint(ref vcount, vertices, x2, y2, z2);
					AddPoint(ref vcount, vertices, x1, y2, z2);
					
					return true;
				}
			}
			
			return true;
		}
		
		public void BringOutYourDead(IList<IndexedVector3> verts, int vcount, IList<IndexedVector3> overts, ref int ocount, IList<int> indices, int indexcount)
		{
#if false
			IList<int> tmpIndices = new ObjectArray<int>(m_vertexIndexMapping.Count);
#else
			IList<int> tmpIndices = new List<int>(m_vertexIndexMapping.Count);
			for(int i = 0; i < m_vertexIndexMapping.Count; ++i) {
				tmpIndices.Add(0);
			}
#endif

			for (int i = 0; i < m_vertexIndexMapping.Count; i++)
			{
				tmpIndices[i] = m_vertexIndexMapping[i];
			}

#if false
			IList<int> usedIndices = new ObjectArray<int>((int)vcount);
#else
			IList<int> usedIndices = new List<int>((int)vcount);
			for( int i = 0; i < vcount; ++i ) {
				usedIndices.Add(0);
			}
#endif
			//usedIndices.resize(static_cast<int>(vcount));
			//memset(&usedIndices[0], 0, sizeof(int) * vcount);
			
			ocount = 0;
			
			for (int i = 0; i < indexcount; i++)
			{
				int v = indices[i]; // original array index

				Debug.Assert(v >= 0 && v < vcount);
				
				if (usedIndices[(int)v]!=0) // if already remapped
				{
					indices[i] = usedIndices[(int)v] - 1; // index to new array
				}
				else
				{
					indices[i] = ocount;      // new index mapping
					
					//overts[ocount][0] = verts[(int)v][0]; // copy old vert to new vert array
					//overts[ocount][1] = verts[(int)v][1];
					//overts[ocount][2] = verts[(int)v][2];
					
					overts[(int)ocount] = verts[(int)v];
					for (int k = 0; k < m_vertexIndexMapping.Count; k++)
					{
						if (tmpIndices[k] == v)
						{
							m_vertexIndexMapping[k] = ocount;
						}
					}
					
					ocount++; // increment output vert count
					
					Debug.Assert(ocount >= 0 && ocount <= vcount);
					
					usedIndices[(int)v] = ocount; // assign new index remapping
				}
			}
		}
		public static IndexedVector3 ThreePlaneIntersection(IndexedVector4 p0, IndexedVector4 p1, IndexedVector4 p2)
		{
			IndexedVector3 N1 = p0.ToVector3();
			IndexedVector3 N2 = p1.ToVector3();
			IndexedVector3 N3 = p2.ToVector3();
			
			IndexedVector3 n2n3 = IndexedVector3.Cross(N2, N3);
			IndexedVector3 n3n1 = IndexedVector3.Cross(N3, N1);
			IndexedVector3 n1n2 = IndexedVector3.Cross(N1, N2);
			
			float quotient = IndexedVector3.Dot(N1, n2n3);
			
			Debug.Assert(Math.Abs(quotient) > 0.000001f);
			
			quotient = -1.0f / quotient;
			n2n3 *= p0.w;
			n3n1 *= p1.w;
			n1n2 *= p2.w;
			
			IndexedVector3 potentialVertex = n2n3;
			potentialVertex += n3n1;
			potentialVertex += n1n2;
			potentialVertex *= quotient;
			
			IndexedVector3 result = potentialVertex;
			return result;
		}
		
		
		public static IndexedVector3 PlaneLineIntersection(ref IndexedVector4 plane, ref IndexedVector3 p0, ref IndexedVector3 p1)
		{
			// returns the point where the line p0-p1 intersects the IndexedVector4 n&
			IndexedVector3 dif = p1 - p0;
			float dn = IndexedVector3.Dot(plane.ToVector3(), dif);
			float t = -(plane.w + IndexedVector3.Dot(plane.ToVector3(), p0)) / dn;
			return p0 + (dif * t);
		}
		
		public static IndexedVector3 PlaneProject(ref IndexedVector4 plane, ref IndexedVector3 point)
		{
			return point - plane.ToVector3() * (IndexedVector3.Dot(point, plane.ToVector3()) + plane.w);
		}
		
		public static IndexedVector3 TriNormal(IndexedVector3 v0, IndexedVector3 v1, IndexedVector3 v2)
		{
			return TriNormal(ref v0, ref v1, ref v2);
		}
		
		public static IndexedVector3 TriNormal(ref IndexedVector3 v0, ref IndexedVector3 v1, ref IndexedVector3 v2)
		{
			// return the normal of the triangle
			// inscribed by v0, v1, and v2
			IndexedVector3 cp = IndexedVector3.Cross(v1 - v0, v2 - v1);
			float m = cp.Length();
			if (m == 0)
			{
				return new IndexedVector3(1, 0, 0);
			}
			return cp * (1.0f / m);
		}
		
		
		public static float DistanceBetweenLines(ref IndexedVector3 ustart, ref IndexedVector3 udir, ref IndexedVector3 vstart, ref IndexedVector3 vdir, ref IndexedVector3? upoint, ref IndexedVector3? vpoint)
		{
			IndexedVector3 cp = IndexedVector3.Cross(udir, vdir);
			cp.Normalize();
			
			float distu = -IndexedVector3.Dot(cp, ustart);
			float distv = -IndexedVector3.Dot(cp, vstart);
			float dist = (float)Math.Abs(distu - distv);
			if (upoint.HasValue)
			{
#if false
				IndexedVector4 plane = new IndexedVector4(IndexedVector3.Cross(vdir, cp).Normalized(),0);
#else
				IndexedVector4 plane = Math.ToVector4(IndexedVector3.Cross(vdir, cp).Normalized(),0);
#endif
				plane.w = -IndexedVector3.Dot(plane.ToVector3(), vstart);
				IndexedVector3 a = ustart + udir;
				upoint = PlaneLineIntersection(ref plane, ref ustart, ref a);
			}
			if (vpoint.HasValue)
			{
#if false
				IndexedVector4 plane = new IndexedVector4(IndexedVector3.Cross(udir, cp).Normalized(), 0);
#else
				IndexedVector4 plane = Math.ToVector4(IndexedVector3.Cross(udir, cp).Normalized(), 0);
#endif
				plane.w = -IndexedVector3.Dot(plane.ToVector3(), ustart);
				IndexedVector3 a = vstart + vdir;
				vpoint = PlaneLineIntersection(ref plane, ref vstart, ref a);
			}
			return dist;
		}
		
		public static PlaneIntersectType PlaneTest(ref IndexedVector4 p, ref IndexedVector3 v)
		{
			float planetestepsilon = 0.0001f;
			float a = IndexedVector3.Dot(v, p.ToVector3()) + p.w;
			PlaneIntersectType flag = (a > planetestepsilon) ? PlaneIntersectType.OVER : ((a < -planetestepsilon) ? PlaneIntersectType.UNDER : PlaneIntersectType.COPLANAR);
			return flag;
		}
		
		public static PlaneIntersectType SplitTest(ConvexH convex, ref IndexedVector4 IndexedVector4)
		{
			PlaneIntersectType flag = PlaneIntersectType.COPLANAR;
			for (int i = 0; i < convex.vertices.Count; i++)
			{
				IndexedVector3 vtx = convex.vertices[i];
				flag |= PlaneTest(ref IndexedVector4, ref vtx);
			}
			return flag;
		}
		
		
		
		public const float PAPERWIDTH = 0.001f;
		public const float planetestepsilon = PAPERWIDTH;
		
		
		public IList<HullTriangle> m_tris = new List<HullTriangle>();
		public IList<int> m_vertexIndexMapping = new List<int>();
		public const float EPSILON = 0.000001f; /* close enough to consider two floating point numbers to be 'the same'. */
		
	}
}	