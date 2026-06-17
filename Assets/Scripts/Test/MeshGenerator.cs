using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MeshGenerator : MonoBehaviour
{
    Mesh mesh;

    Vector3[] vertices;
    int[] triangles;
    Color[] colors;

    public float rSec = .005f;

    public AnimationCurve heightCurve;

    // CAmbiar a size directamente, siempre igual y multiplos de 20, solo cambiar densidad (int)
    public int xSize = 20;
    public int zSize = 20;

    public int density = 1;

    private int totalSize;
    public int biomes;

    public float perlinZoom = .2f;    

    public float scale1 = 2f;
    public float amp1 = 2f;

    public float scale2 = 4f;
    public float amp2 = 4f;

    public float scale3 = 6f;
    public float amp3 = 6f;

    public Gradient gradient;

    private float minTerrainHeight;
    private float maxTerrainHeight;

    public int noiseWidth;
    public int noiseHeight;
    public int octaves;
    [Range(0f, 1f)]
    public float persistence;
    public float lacunarity;

    public int seed;
    public Vector2 offset;

    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        CreateShape();

        // float[,] noiseMap = Noise.GenerateNoiseMap();
    }

    private void Update()
    {
        ColorChange();

        //Condicion = Solo si  carga nuevo terreno o se altera el mapa (separar aplicacion de ruido a loos vértices de la generación, pero solo si se hacen pruebas, sino dejar tal cual y crear metodo que, al cargar nuevo terreno, guarde un nuevo elemento)
        UpdateMesh();
    }

    void CreateShape()
    {
        vertices = new Vector3[(xSize + 1) * (zSize + 1)];

        for (int i = 0, z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                Debug.Log(x + z);
                float y = CalculateNoise(x, z);
                Debug.Log(y);
                vertices[i] = new Vector3(x / density, y, z / density);

                if (y > maxTerrainHeight)
                    maxTerrainHeight = y;
                if (y < minTerrainHeight)
                    minTerrainHeight = y;

                i++;
            }
        }

        triangles = new int[xSize * zSize * 6];

        int vert = 0;
        int tris = 0;

        // Uso de triangle strips para mejorar la optimización en la generación de los triángulos
        for (int z = 0; z < zSize; ++z)
        {
            for (int x = 0; x < xSize; x++)
            {
                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + xSize + 1;
                triangles[tris + 2] = vert + 1;
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + xSize + 1;
                triangles[tris + 5] = vert + xSize + 2;

                vert++;
                tris += 6;
            }
            vert++;
        }
    }

    void ColorChange()
    {
        colors = new Color[vertices.Length];

        for (int i = 0, z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                float height = Mathf.InverseLerp(minTerrainHeight, maxTerrainHeight, vertices[i].y);
                colors[i] = gradient.Evaluate(height);
                i++;
            }
        }
    }

    void UpdateMesh()
    {
        mesh.Clear();

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        mesh.colors = colors;

        mesh.RecalculateNormals();
    }

    private float CalculateNoise(float x, float z)
    {
        float noise;

        noise = Mathf.PerlinNoise(x * perlinZoom, z * perlinZoom) * 5;
        Debug.Log(noise);
        noise += Mathf.PerlinNoise(x * amp1, z * amp1) * scale1;
        Debug.Log(noise);
        noise -= Mathf.PerlinNoise(x * amp2, z * amp2) * scale2;
        Debug.Log(noise);
        noise += Mathf.PerlinNoise(x * amp3, z * amp3) * scale3 * 2;

        Debug.Log(noise);
        return noise;
    }

    private void OnDrawGizmos()
    {
        if (vertices == null)
            return;

        for(int i = 0; i < vertices.Length; i++)
        {
            Gizmos.DrawSphere(vertices[i], .1f);
        }
    }
}
