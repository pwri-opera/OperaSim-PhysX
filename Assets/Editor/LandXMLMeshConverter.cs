using UnityEngine;
using System.Collections.Generic;

public class LandXMLMeshConverter
{
    public static GameObject CreateMeshFromSurface(Surface surface, Vector3 center)
    {
        // Create a new game object with mesh filter and renderer
        GameObject surfaceObject = new GameObject(surface.Name);
        MeshFilter meshFilter = surfaceObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = surfaceObject.AddComponent<MeshRenderer>();
        
        // Create mesh
        Mesh mesh = new Mesh();
        
        // Convert Point3D to Vector3
        Vector3[] vertices = new Vector3[surface.Points.Count];
        for (int i = 0; i < surface.Points.Count; i++)
        {
            Point3D point = surface.Points[i];
            // Convert to Unity's coordinate system (Z-up to Y-up)
            vertices[i] = new Vector3((float)point.X - center.x, (float)point.Z - center.z, (float)point.Y - center.y);
        }
        
        // Convert Face indices to triangles
        List<int> triangles = new List<int>();
        foreach (Face face in surface.Faces)
        {
            // Assuming faces are triangulated
            // If faces are not triangulated, you'll need to triangulate them
            if (face.VertexIndices.Count >= 3)
            {
                // Subtract 1 from indices because LandXML uses 1-based indexing
                // while Unity uses 0-based indexing
                triangles.Add(face.VertexIndices[0] - 1);
                triangles.Add(face.VertexIndices[1] - 1);
                triangles.Add(face.VertexIndices[2] - 1);
            }
        }
        
        // Assign vertices and triangles to mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles.ToArray();
        
        // Calculate normals
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        // Assign mesh to mesh filter
        //meshFilter.sharedMesh = mesh;
        meshFilter.mesh = mesh;
        
        // Assign default material
        meshRenderer.material = new Material(Shader.Find("Standard"));
        
        return surfaceObject;
    }
}
