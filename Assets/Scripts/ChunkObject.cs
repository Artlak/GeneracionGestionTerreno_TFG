using AuxiliarClasses;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ChunkObject : MonoBehaviour // Chunk and Cluster
{
    List<WorldVertex> vertices;

    public GameObject chunkAsset;

    // Añadido desde el asset
    Mesh mesh;
    [SerializeField] MeshCollider meshCollider;

    readonly int baseSize = 20; // Serían 21, pero para hacer - 1 y luego + 1 de nuevo lo dejo en 20
    int baseSide;
    int clusterSideX; // Cantidad de vertices de lado X
    int clusterSideZ; // Cantidad de vertices de lado Z
    int chunkSideX; // Cantidad de chunks de los que está compuesto de lado X
    int chunkSideZ; // Cantidad de chunks de los que está compuesto de lado Z
    int lodLevel;

    [Range(1, 5)]
    [SerializeField] int density;

    Vector3[] meshVertices;
    int[] triangles;
    Color[] colors;

    // Actualiza los datos del chunk, se comporta como un constructor
    public void DataUpdate(List<WorldVertex> _vertices, int _clusterSideX, int _clusterSideZ)
    {
        meshCollider.enabled = true;

        vertices = _vertices;
        baseSide = 21 + 20 * density;

        clusterSideX = _clusterSideX;
        clusterSideZ = _clusterSideZ;

        chunkSideX = (clusterSideX - 1) / 20;
        chunkSideZ = (clusterSideZ - 1) / 20;

        lodLevel = chunkSideX >= chunkSideZ ? chunkSideX : chunkSideZ;

        LoadMesh();
    }

    // Función de unión de chunks con distintas sobrecargas
    public void ChunkFusion(ChunkObject chunkObject0, ChunkObject chunkObject1, ChunkObject chunkObject2) // Fusión cuadrada, se unen 4 chunks o clusters
    {
        // Añadir cluster/chunk que se encuentra a la dcha
        
        for (int v = 0, i = 0; i < clusterSideZ; i++)
        {
            v += clusterSideX;

            vertices.InsertRange(v, chunkObject0.vertices.GetRange(i * chunkObject0.clusterSideX + 1, chunkObject0.clusterSideX - 1)); // Dentro del rango sse omite añadir el primero del nuevo chunk ya que es el mismo que el último de la fila de este
            
            v += chunkObject0.clusterSideX - 1;
        }

        clusterSideX += chunkObject0.clusterSideX - 1;

        for (int i = 1; i < chunkObject1.clusterSideZ; i++) // Comienza en 1 para saltarse la primera fila, ya que se repite, al igual que en el otro caso
        {
            vertices.AddRange(chunkObject1.vertices.GetRange(i * chunkObject1.clusterSideX, chunkObject1.clusterSideX));
            vertices.AddRange(chunkObject2.vertices.GetRange(i * chunkObject2.clusterSideX + 1, chunkObject2.clusterSideX - 1));
        }

        clusterSideZ += chunkObject1.clusterSideZ - 1;

        chunkSideX += chunkObject0.chunkSideX;
        chunkSideZ += chunkObject1.chunkSideZ;

        Destroy(chunkObject0.gameObject);
        Destroy(chunkObject1.gameObject);
        Destroy(chunkObject2.gameObject);

        LoadMesh();
    }

    public void ChunkFusion(ChunkObject chunkObject0, bool up) // Solo se unen dos chunks o clusters
    {
        if (!up)
        {
            for (int v = 0, i = 0; i < clusterSideZ; i++)
            {
                v += clusterSideX;

                vertices.InsertRange(v, chunkObject0.vertices.GetRange(i * chunkObject0.clusterSideX + 1, chunkObject0.clusterSideX - 1));

                v += chunkObject0.clusterSideX - 1;
            }

            clusterSideX += chunkObject0.clusterSideX - 1;
            chunkSideX += chunkObject0.chunkSideX;
        }
        else
        {
            for (int i = 1; i < chunkObject0.clusterSideZ; i++) 
            {
                vertices.AddRange(chunkObject0.vertices.GetRange(i * chunkObject0.clusterSideX, chunkObject0.clusterSideX));
            }

            clusterSideZ += chunkObject0.clusterSideZ - 1;
            chunkSideZ += chunkObject0.chunkSideZ;
        }

        Destroy(chunkObject0.gameObject);

        LoadMesh();
    }

    // Función de separación de chunks
    public void ChunkDivision()
    {
        List<WorldVertex> _vertices = new();
        List<WorldVertex> _vertices0 = new();
        ChunkObject chunkAssetScript;

        // Valores si son iguales
        int ownVertices = (baseSide - 1) * (chunkSideX - chunkSideX / 2) + 1;
        int extraVertices = (baseSide - 1) * (chunkSideX / 2) + 1;
        
        if (chunkSideX != chunkSideZ)
        {
            ownVertices = chunkSideX > chunkSideZ ? (baseSide - 1) * chunkSideZ + 1 : (baseSide - 1) * chunkSideX + 1;
        }

        if (chunkSideX != chunkSideZ)
        {
            extraVertices = chunkSideX > chunkSideZ ? (baseSide - 1) * (chunkSideX - chunkSideZ) + 1 : (baseSide - 1) * (chunkSideZ - chunkSideX) + 1;
        }        

        if (chunkSideX == chunkSideZ)
        {
            _vertices0.AddRange(vertices.GetRange(clusterSideX * ownVertices - extraVertices - 1 , extraVertices));
        }

        if (chunkSideX >= chunkSideZ)
        {
            for (int r = 1; r <= ownVertices; r++) // Chunk abajo dcha
            {
                _vertices.AddRange(vertices.GetRange(r * ownVertices - 1, extraVertices));            

                vertices.RemoveRange(r * ownVertices, extraVertices - 1);
            }

            chunkAssetScript = Instantiate(chunkAsset, transform.position + new Vector3((chunkSideX - chunkSideX / 2) * baseSize, 0, 0), Quaternion.identity).GetComponent<ChunkObject>();

            chunkAssetScript.DataUpdate(_vertices, extraVertices, ownVertices);

            _vertices.Clear();

        }

        if (chunkSideX <= chunkSideZ)
        {
            for (int r = 0; r < extraVertices; r++) // Chunk arriba
            {
                _vertices.AddRange(vertices.GetRange((ownVertices * ownVertices - ownVertices) + ownVertices * r + r, ownVertices));

                vertices.RemoveRange((ownVertices * ownVertices - ownVertices) + ownVertices * r + r, ownVertices - 1);
            }

            chunkAssetScript = Instantiate(chunkAsset, transform.position + new Vector3(0, 0, (chunkSideX - chunkSideX / 2) * baseSize), Quaternion.identity).GetComponent<ChunkObject>();

            chunkAssetScript.DataUpdate(_vertices, ownVertices, extraVertices);

            _vertices.Clear();
        }

        if (chunkSideX == chunkSideZ)
        {
            chunkAssetScript = Instantiate(chunkAsset, transform.position + new Vector3((chunkSideX - chunkSideX / 2) * baseSize, 0, (chunkSideX - chunkSideX / 2) * baseSize), Quaternion.identity).GetComponent<ChunkObject>();

            chunkAssetScript.DataUpdate(_vertices0, extraVertices, extraVertices);

            _vertices0.Clear();
        }

        DataUpdate(vertices, ownVertices, ownVertices);
    }

    public void LoadMesh()
    {
        mesh.Clear();
        meshCollider.sharedMesh.Clear();

        CreateShape();
        ColorChange();
        UpdateMesh();
    }

    void CreateShape()
    {
        meshVertices = new Vector3[baseSide * baseSide];

        float coordAdditionX = (float) (baseSize * chunkSideX + 1) / baseSide - 1; // Cuanto hay que añadir por vertice en las coord si damos por hecho que cada chunk base es de 20m * 20m // La formula (baseSide * chunkSideC + 1) calcula cual sería la cantidad de vertices con desidad = 1
        float coordAdditionZ = (float) (baseSize * chunkSideZ + 1) / baseSide - 1;

        for (int v = 0, z = 0; z < baseSide; z++)
        {
            v = z * clusterSideX;

            for (int x = 0; x < baseSide; x++)
            {
                meshVertices[v] = new Vector3(x * coordAdditionX + transform.position.x, vertices[v].height, z * coordAdditionZ + transform.position.z);

                v += chunkSideX;
            }
        }

        triangles = new int[(baseSide - 1) * (baseSide - 1) * 6];

        for (int vert = 0, tris = 0, z = 0; z < baseSide; ++z)
        {
            vert = z * baseSide;
            for (int x = 0; x < baseSide; x++)
            {
                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + baseSide + 1;
                triangles[tris + 2] = vert + 1;
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + baseSide + 1;
                triangles[tris + 5] = vert + baseSide + 2;

                vert++;
                tris += 6;
            }
        }
    }

    void ColorChange()
    {
        colors = new Color[baseSide * baseSide];

        for (int v = 0, z = 0; z < baseSide; z++)
        {
            for (int x = 0; x < baseSide; x++)
            {
                colors[v] = vertices[v].color;
                v++;
            }
        }
    }

    void UpdateMesh()
    {
        // Para suavizar el cabio a malla de menor detalle
        Vector3 offset = new Vector3(0, (0 - chunkSideX * 0.02f), 0);
        transform.position += offset;        

        mesh.vertices = meshVertices;
        mesh.triangles = triangles;

        mesh.colors = colors;

        mesh.RecalculateNormals();
        // Solo si está a pleno detalle
        if (lodLevel == 1)
        {
            meshCollider.sharedMesh = mesh;
            meshCollider.enabled = true;
        }
        
        //mesh.RecalculateBounds();
    }
}