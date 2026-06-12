using AuxiliarClasses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class ChunkObject : MonoBehaviour // Chunk and Cluster
{
    WorldVertex[] vertices;

    public GameObject chunkAsset;

    Mesh mesh;
    MeshFilter meshFilter;

    MeshCollider meshCollider;

    public int indexPos { get; set; } // Posición del chunk en la lista de chunks, se asigna desde el ChunkController al crear el chunk, se utiliza para gestionar los chunks y sus LoDs
    public int parentChunkIndex { get; set; } // Índice del chunk del que se compone el cluster, se asigna desde el método ChunkFusion al fusionar los chunks, se utiliza para gestionar lod LoDs una vez fusionados los chunks, para separarlos de la forma correcta después.

    readonly int baseSize = 20; // Serían 21, pero para hacer - 1 y luego + 1 de nuevo lo dejo en 20
    int baseSide;
    int clusterSideX; // Cantidad de vertices de lado X
    int clusterSideZ; // Cantidad de vertices de lado Z
    int chunkSideX; // Cantidad de chunks de los que está compuesto de lado X
    int chunkSideZ; // Cantidad de chunks de los que está compuesto de lado Z
    [SerializeField] int lodLevel;
    int lodOffset; // Cantidad de elementos a sumar en el índice para acceder a la posición de los chunks de los que se compone

    public int density { get; set; }

    Vector3[] meshVertices;
    int[] triangles;
    Color[] colors;

    public void MeshInizialiciation()
    {
        mesh = new Mesh();
        meshCollider = GetComponent<MeshCollider>();

        GetComponent<MeshFilter>().mesh = mesh;        
        meshCollider.sharedMesh = mesh;
    }

    // Actualiza los datos del chunk, se comporta como un constructor
    public void DataUpdate(WorldVertex[] _vertices, int _clusterSideX, int _clusterSideZ)
    {
        meshCollider.enabled = false;

        vertices = _vertices.ToArray();

        clusterSideX = _clusterSideX;
        clusterSideZ = _clusterSideZ;
        
        baseSide = 20 * density + 1;

        chunkSideX = (clusterSideX - 1) / (baseSize * density);
        chunkSideZ = (clusterSideZ - 1) / (baseSize * density);

        lodLevel = chunkSideX >= chunkSideZ ? chunkSideX : chunkSideZ;

        lodOffset = lodLevel - lodLevel / 2;
    }

    // Función de unión de chunks con distintas sobrecargas
    public void ChunkFusion() // Fusión cuadrada, se unen 4 chunks o clusters
    {
        int indexX = indexPos + chunkSideX;
        int indexZ = indexPos + (chunkSideZ * ChunkController.chunkSide);
        int indexXZ = indexX + (chunkSideZ * ChunkController.chunkSide);
        WorldVertex[] auxVertices;

        bool chunkXIsValid = indexX < ChunkController.chunkList.Length && indexPos / ChunkController.chunkSide == indexX / ChunkController.chunkSide; // Comprueba que es válido dentro del índice y que no se encuentra en la siguiente fila de altura
        bool chunkZIsValid = indexZ < ChunkController.chunkList.Length; // Comprueba que es válido dentro del índice y que se encuentra en la siguiente fila de altura
        bool chunkXZIsValid = indexXZ < ChunkController.chunkList.Length && indexZ / ChunkController.chunkSide == indexXZ / ChunkController.chunkSide; // Comprueba que es válido dentro del índice y que se encuentra en la siguiente fila de altura

        if (chunkXIsValid && chunkZIsValid && chunkXZIsValid) // Añade los 3 cluster al cluster base
        {
            ChunkObject clusterX = ChunkController.chunkList[indexX];
            ChunkObject clusterZ = ChunkController.chunkList[indexZ];
            ChunkObject clusterXZ = ChunkController.chunkList[indexXZ];

            // Asigna el índice del chunk del que se componen a los clusters para gestionar los LoDs una vez fusionados, para separarlos de la forma correcta después.
            clusterX.parentChunkIndex = indexPos;
            clusterZ.parentChunkIndex = indexPos;
            clusterXZ.parentChunkIndex = indexPos;

            // El nuevo array de vertices se crea con el tamaño necesario para contener los vertices del cluster base y los nuevos vertices que se añaden al fusionar, teniendo en cuenta que se repiten la fila y columna de unión
            auxVertices = new WorldVertex[(clusterSideX + clusterX.clusterSideX - 1) * (clusterSideZ + clusterZ.clusterSideZ - 1)];
            int v = 0;

            for (int z = 0; z < clusterSideZ; z++)
            {
                for (int x = 0; x < clusterSideX; x++)
                {
                    auxVertices[v] = vertices[x + z * clusterSideX];
                    v++;
                }

                for (int i = 1; i < clusterX.clusterSideX; i++) // Comienza en 1 para saltarse la primera columna, ya que se repite, al igual que en el otro caso
                {
                    auxVertices[v] = clusterX.vertices[z * clusterX.clusterSideX + i];
                    v++;
                }
            }

            clusterSideX += clusterX.clusterSideX - 1;

            for (int z = 1; z < clusterZ.clusterSideZ; z++) // Comienza en 1 para saltarse la primera fila, ya que se repite
            {
                for (int x = 0; x < clusterZ.clusterSideX; x++)
                {
                    auxVertices[v] = clusterZ.vertices[x + z * clusterZ.clusterSideX];
                    v++;
                }

                for (int i = 1; i < clusterXZ.clusterSideX; i++) // Comienza en 1 para saltarse la primera columna, ya que se repite
                {
                    auxVertices[v] = clusterXZ.vertices[z * clusterXZ.clusterSideX + i];
                    v++;
                }
            }

            clusterSideZ += clusterZ.clusterSideZ - 1;

            DataUpdate(auxVertices, clusterSideX, clusterSideZ);

            clusterX.gameObject.SetActive(false);
            clusterZ.gameObject.SetActive(false);
            clusterXZ.gameObject.SetActive(false);
        }
        else if (chunkXIsValid)
        {
            var clusterX = ChunkController.chunkList[indexX];
            clusterX.parentChunkIndex = indexPos;
            
            auxVertices = new WorldVertex[clusterSideZ * (clusterSideX + clusterX.clusterSideX - 1)];
            
            for (int v = 0, z = 0; z < clusterSideZ; z++)
            {
                for (int x = 0; x < clusterSideX; x++)
                {
                    auxVertices[v] = vertices[x + z * clusterSideX];
                    v++;
                }

                for (int i = 1; i < clusterX.clusterSideX; i++) // Comienza en 1 para saltarse la primera columna, ya que se repite, al igual que en el otro caso
                {
                    auxVertices[v] = clusterX.vertices[z * clusterX.clusterSideX + i];
                    v++;
                }
            }

            clusterSideX += clusterX.clusterSideX - 1;

            DataUpdate(auxVertices, clusterSideX, clusterSideZ);

            clusterX.gameObject.SetActive(false);
        }
        else if (chunkZIsValid)
        {
            var clusterZ = ChunkController.chunkList[indexZ];
            clusterZ.parentChunkIndex = indexPos;

            auxVertices = new WorldVertex[clusterSideX * (clusterSideZ + (clusterZ.clusterSideZ - 1))];
            int v = 0;
            for (int i = 0; i < vertices.Length; i++) // Introduce los vertices del cluster actual en el nuevo array directamente ya que mantienen la misma posición debido a que los nuevos vértices se añaden al final
            {
                auxVertices[v] = vertices[i];
                v++;
            }
            for (int z = 1; z < clusterZ.clusterSideZ; z++) // Comienza en 1 para saltarse la primera fila, ya que se repite, al igual que en el otro caso
            {
                for (int x = 0; x < clusterZ.clusterSideX; x++)
                {
                    auxVertices[v] = clusterZ.vertices[x + z * clusterZ.clusterSideX];
                    v++;
                }   
            }

            clusterSideZ += clusterZ.clusterSideZ - 1;

            DataUpdate(auxVertices, clusterSideX, clusterSideZ);

            clusterZ.gameObject.SetActive(false);
        }        

        // LoadMesh();
    }

    // Función de separación de chunks
    public void ChunkDivision() // Guarda los datos del chunks como el cuadrado de abajo a la izquierda y activa los que estaban unidos
    {
        // Valores si son iguales
        int ownVertices = (baseSide - 1) * (chunkSideX - chunkSideX / 2) + 1;

        if (chunkSideX != chunkSideZ)
        {
            ownVertices = chunkSideX > chunkSideZ ? (baseSide - 1) * chunkSideZ + 1 : (baseSide - 1) * chunkSideX + 1;
        }

        WorldVertex[] auxVertices = new WorldVertex[ownVertices * ownVertices];

        for (int v = 0, z = 0; z < ownVertices; z++) // Añade los vertices del chunk base al nuevo array para su posterior asignación al chunk base. Siempre se encuentra en la esquina inferior izquierda
        {
            for (int x = 0; x < ownVertices; x++)
            {
                auxVertices[v] = vertices[x + z * clusterSideX];
                v++;
            }
        }

        if (chunkSideX == chunkSideZ)
        {
            ChunkController.chunkList[indexPos + lodOffset].gameObject.SetActive(true); // Activa chunk abajo dcha
            ChunkController.chunkList[indexPos + lodOffset * ChunkController.chunkSide].gameObject.SetActive(true); // Activa chunk arriba izq
            ChunkController.chunkList[indexPos + lodOffset + lodOffset * ChunkController.chunkSide].gameObject.SetActive(true); // Activa chunk arriba dcha            
        }
        else if (chunkSideX >= chunkSideZ)
        {

            ChunkController.chunkList[indexPos + lodOffset].gameObject.SetActive(true); // Chunk abajo dcha

        }
        else
        {
            ChunkController.chunkList[indexPos + lodOffset * ChunkController.chunkSide].gameObject.SetActive(true); // Chunk arriba izq
        }

        DataUpdate(auxVertices, ownVertices, ownVertices);

        LoadMesh();
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

        float coordAdditionX = (float) (baseSize * chunkSideX) / (baseSide - 1); // Cuanto hay que añadir por vertice en las coord si damos por hecho que cada chunk base es de 20m * 20m // La formula (baseSide * chunkSide + 1) calcula cual sería el tamaño total de los chunks.
        float coordAdditionZ = (float) (baseSize * chunkSideZ) / (baseSide - 1);

        //Debug.Log("ChunkSideX " + chunkSideX + " ChunkSideZ " + chunkSideZ);
        //Debug.Log("LoD " + lodLevel);
        //Debug.Log("Vertices Length: " + vertices.Length);

        //for (int i = 0; i < vertices.Length; i++)
        //{
        //    if (vertices[i] == null)
        //    {
        //        Debug.Log("Vertex in " + i + " position: null");
        //    }
        //}        

        for (int v = 0, z = 0; z < baseSide; z++)
        {
            v = z * clusterSideX * chunkSideZ * density;

            for (int x = 0; x < baseSide; x++)
            {
                //Debug.Log("Vertex " + v + ": ");
                Vector3 vertexData = new Vector3(x * coordAdditionX, vertices[v].height * ChunkController.heightMultiplier, z * coordAdditionZ); // Vector de coordenadas creadas para corresponder el tamaño del chunk salvo la altura, que corresponde al mapa del mundo realizado con anterioridad

                meshVertices[x + z * baseSide] = vertexData;

                v += chunkSideX * density;
            }
        }

        triangles = new int[(baseSide - 1) * (baseSide - 1) * 6];

        for (int vert = 0, tris = 0, z = 0; z < baseSide - 1; ++z)
        {
            vert = z * baseSide;
            for (int x = 0; x < baseSide - 1; x++)
            {
                triangles[tris] = vert;
                triangles[tris + 1] = vert + baseSide;
                triangles[tris + 2] = vert + 1;
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + baseSide;
                triangles[tris + 5] = vert + baseSide + 1;

                vert++;
                tris += 6;
            }
        }
    }

    void ColorChange()
    {
        colors = new Color[meshVertices.Length];

        for (int v = 0, z = 0; z < baseSide; z++)
        {
            v = z * clusterSideX * chunkSideZ * density;

            for (int x = 0; x < baseSide; x++)
            {
                colors[x + z * baseSide] = vertices[v].color;

                v += chunkSideX * density;
            }
        }
    }

    void UpdateMesh()
    {
        // Para suavizar el cabio a malla de menor detalle
        Vector3 offset = new Vector3(0, chunkSideX * 0.02f, 0);
        transform.position -= offset;        

        mesh.vertices = meshVertices;
        mesh.triangles = triangles;

        mesh.colors = colors;

        mesh.RecalculateNormals();
        // Solo si está a pleno detalle
        if (lodLevel == 1)
        {
            meshCollider.enabled = true;
        }
        
        //mesh.RecalculateBounds();
    }
}