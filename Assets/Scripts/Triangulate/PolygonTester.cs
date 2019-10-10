using UnityEngine;

public class PolygonTester : MonoBehaviour
{
    void Start()
    {
        // Create Vector2 vertices
        Vector2[] vertices2D = new Vector2[] {
            new Vector2(1,1),
            new Vector2(2,2),
            new Vector2(0,3),
            new Vector2(3,1),
            new Vector2(5,2),
            new Vector2(7,3),
        };

        // Use the triangulator to get indices for creating triangles
        Triangulator tr = new Triangulator(vertices2D);
        int[] indices = tr.Triangulate();

        // Create the Vector3 vertices
        Vector3[] vertices = new Vector3[vertices2D.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = new Vector3(vertices2D[i].x, 0, vertices2D[i].y);
        }

        Debug.Log(vertices.Length);

        // Create the mesh
        Mesh msh = new Mesh();
        msh.vertices = vertices;
        msh.triangles = indices;
        msh.RecalculateNormals();
        msh.RecalculateBounds();

        // Set up game object with mesh;
        gameObject.AddComponent(typeof(MeshRenderer));
        MeshFilter filter = gameObject.AddComponent(typeof(MeshFilter)) as MeshFilter;
        filter.mesh = msh;
    }
}
