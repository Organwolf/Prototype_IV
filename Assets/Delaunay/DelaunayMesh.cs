﻿using System.Collections.Generic;
using UnityEngine;
using TriangleNet.Geometry;
using TriangleNet.Topology;

public class DelaunayMesh : MonoBehaviour {
    // Maximum size of the terrain.
    public int xsize = 50;
    public int ysize = 50;

    // Minimum distance the poisson-disc-sampled points are from each other.
    public float minPointRadius = 4.0f;

    // Number of random points to generate.
    public int randomPoints = 3;

    // Triangles in each chunk.
    public int trianglesInChunk = 20000;

    // Perlin noise parameters
    public float elevationScale = 100.0f;
    public float sampleSize = 1.0f;
    public int octaves = 8;
    public float frequencyBase = 2;
    public float persistence = 1.1f;

    // Detail mesh parameters
    public Transform detailMesh;
    public int detailMeshesToGenerate = 50;

    // Prefab which is generated for each chunk of the mesh.
    public Transform chunkPrefab = null;

    // Elevations at each point in the mesh
    private List<float> elevations;
    
    // Fast triangle querier for arbitrary points
    private TriangleBin bin;

    // The delaunay mesh
    private TriangleNet.Mesh mesh = null;

    void Start()
    {
        Generate(0);
    }

    public virtual void Generate(float fixedElevation) {

        elevations = new List<float>();

        Polygon polygon = new Polygon();

        polygon.Add(new Vertex(1, 1));
        polygon.Add(new Vertex(2, 2));
        polygon.Add(new Vertex(0, 3));
        polygon.Add(new Vertex(3, 1));
        polygon.Add(new Vertex(5, 2));
        polygon.Add(new Vertex(7, 3));
        polygon.Add(new Vertex(3, 4));

        GameObject obj0 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obj0.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        obj0.transform.localPosition = new Vector3(1, 0, 1);
        GameObject obj1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obj1.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        obj1.transform.localPosition = new Vector3(2, 0,2);
        GameObject obj2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obj2.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        obj2.transform.localPosition = new Vector3(0, 0, 3);
        GameObject obj3 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obj3.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        obj3.transform.localPosition = new Vector3(3, 0, 1);
        GameObject obj4 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obj4.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        obj4.transform.localPosition = new Vector3(5, 0, 2);
        GameObject obj5 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obj5.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        obj5.transform.localPosition = new Vector3(7, 0, 3);
        GameObject obj6 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obj6.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        obj6.transform.localPosition = new Vector3(3, 0, 4);

        TriangleNet.Meshing.ConstraintOptions options = new TriangleNet.Meshing.ConstraintOptions() { ConformingDelaunay = false };
        mesh = (TriangleNet.Mesh)polygon.Triangulate(options);

        Debug.Log(mesh.Vertices.Count);

        // Sample perlin noise to get elevations
        foreach (Vertex vert in mesh.Vertices) {
           Debug.Log(vert.ToString());
           elevations.Add(fixedElevation);
        }
        MakeMesh();
    }
    
    public void MakeMesh() {
        IEnumerator<Triangle> triangleEnumerator = mesh.Triangles.GetEnumerator();

        for (int chunkStart = 0; chunkStart < mesh.Triangles.Count; chunkStart += trianglesInChunk) {
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();

            int chunkEnd = chunkStart + trianglesInChunk;
            for (int i = chunkStart; i < chunkEnd; i++)
            {
                if (!triangleEnumerator.MoveNext())
                {
                    break;
                }

                Triangle triangle = triangleEnumerator.Current;

                // For the triangles to be right-side up, they need
                // to be wound in the opposite direction
                Vector3 v0 = GetPoint3D(triangle.vertices[2].id);
                Vector3 v1 = GetPoint3D(triangle.vertices[1].id);
                Vector3 v2 = GetPoint3D(triangle.vertices[0].id);

                triangles.Add(vertices.Count);
                triangles.Add(vertices.Count + 1);
                triangles.Add(vertices.Count + 2);

                vertices.Add(v0);
                vertices.Add(v1);
                vertices.Add(v2);

                Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0);
                normals.Add(normal);
                normals.Add(normal);
                normals.Add(normal);

                uvs.Add(new Vector2(0.0f, 0.0f));
                uvs.Add(new Vector2(0.0f, 0.0f));
                uvs.Add(new Vector2(0.0f, 0.0f));
            }
            // CHUNK LOGIC - material

            Mesh chunkMesh = new Mesh();
            chunkMesh.vertices = vertices.ToArray();
            chunkMesh.uv = uvs.ToArray();
            chunkMesh.triangles = triangles.ToArray();
            chunkMesh.normals = normals.ToArray();

            // CHUNK LOGIC - init prefab
            Transform chunk = Instantiate<Transform>(chunkPrefab, transform.position, transform.rotation);
            chunk.GetComponent<MeshFilter>().mesh = chunkMesh;
            chunk.GetComponent<MeshCollider>().sharedMesh = chunkMesh;
            chunk.transform.parent = transform;
            // CHUNK LOGIC - init prefab
        }
    }

    /* Returns a point's local coordinates. */
    public Vector3 GetPoint3D(int index) {
        Vertex vertex = mesh.vertices[index];
        float elevation = elevations[index];
        return new Vector3((float)vertex.x, elevation, (float)vertex.y);
    }
    
    /* Returns the triangle containing the given point. If no triangle was found, then null is returned.
       The list will contain exactly three point indices. */
    public List<int> GetTriangleContainingPoint(Vector2 point) {
        Triangle triangle = bin.getTriangleForPoint(new Point(point.x, point.y));
        if (triangle == null) {
            return null;
        }

        return new List<int>(new int[] { triangle.vertices[0].id, triangle.vertices[1].id, triangle.vertices[2].id });
    }

    /* Returns a pretty good approximation of the height at a given point in worldspace */
    public float GetElevation(float x, float y) {
        if (x < 0 || x > xsize ||
                y < 0 || y > ysize) {
            return 0.0f;
        }

        Vector2 point = new Vector2(x, y);
        List<int> triangle = GetTriangleContainingPoint(point);

        if (triangle == null) {
            // This can happen sometimes because the triangulation does not actually fit entirely within the bounds of the grid;
            // not great error handling, but let's return an invalid value
            return float.MinValue;
        }

        Vector3 p0 = GetPoint3D(triangle[0]);
        Vector3 p1 = GetPoint3D(triangle[1]);
        Vector3 p2 = GetPoint3D(triangle[2]);

        Vector3 normal = Vector3.Cross(p0 - p1, p1 - p2).normalized;
        float elevation = p0.y + (normal.x * (p0.x - x) + normal.z * (p0.z - y)) / normal.y;

        return elevation;
    }

    /* Scatters detail meshes within the bounds of the terrain. */
    public void ScatterDetailMeshes() {
        for (int i = 0; i < detailMeshesToGenerate; i++)
        {
            // Obtain a random position
            float x = Random.Range(0, xsize);
            float z = Random.Range(0, ysize);
            float elevation = GetElevation(x, z);
            Vector3 position = new Vector3(x, elevation, z);

            if (elevation == float.MinValue) {
                // Value returned when we couldn't find a triangle, just skip this one
                continue;
            }

            // We always want the mesh to remain upright, so only vary the rotation in the x-z plane
            float angle = Random.Range(0, 360.0f);
            Quaternion randomRotation = Quaternion.AngleAxis(angle, Vector3.up);

            Instantiate<Transform>(detailMesh, position, randomRotation, this.transform);
        }
    }

    public void OnDrawGizmos() {
        if (mesh == null) {
            // Probably in the editor
            return;
        }

        Gizmos.color = Color.red;
        foreach (Edge edge in mesh.Edges) {
            Vertex v0 = mesh.vertices[edge.P0];
            Vertex v1 = mesh.vertices[edge.P1];
            Vector3 p0 = new Vector3((float)v0.x, 0.0f, (float)v0.y);
            Vector3 p1 = new Vector3((float)v1.x, 0.0f, (float)v1.y);
            Gizmos.DrawLine(p0, p1);
        }
    }
}