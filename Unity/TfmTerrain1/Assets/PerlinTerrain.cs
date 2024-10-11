using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class PerlinTerrain : MonoBehaviour
{
    public int width = 100;
    public int height = 100;

    public float noiseScale = 20f;
    public float heightMultiplier = 5f;
    public Vector2 noiseOffset;

    void Start()
    {
        GenerateTerrain();
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        Renderer objectRenderer = GetComponent<Renderer>();

        // Cambiamos el color del material al nuevo color
        if (objectRenderer != null)
        {
            objectRenderer.material.color = Color.gray;
        }
        this.gameObject.layer = LayerMask.NameToLayer("TERRENO");
        meshRenderer.receiveShadows = true;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
    }

    void GenerateTerrain()
    {
        Mesh mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        Vector3[] vertices = new Vector3[(width + 1) * (height + 1)];
        int[] triangles = new int[width * height * 6];

        for (int i = 0, z = 0; z <= height; z++)
        {
            for (int x = 0; x <= width; x++)
            {
                float y = Mathf.PerlinNoise((x + noiseOffset.x) / noiseScale, (z + noiseOffset.y) / noiseScale) * heightMultiplier;
                vertices[i] = new Vector3(x, y, z);
                i++;
            }
        }

        int vert = 0;
        int tris = 0;
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + width + 1;
                triangles[tris + 2] = vert + 1;
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + width + 1;
                triangles[tris + 5] = vert + width + 2;

                vert++;
                tris += 6;
            }
            vert++;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        // Actualiza el MeshCollider para que coincida con la nueva malla
        GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    void OnDrawGizmosSelected()
    {
        //if (GetComponent<MeshFilter>().mesh != null)
        //{
        //    Gizmos.color = Color.black;
        //    Gizmos.DrawWireMesh(GetComponent<MeshFilter>().mesh);
        //}
    }
}