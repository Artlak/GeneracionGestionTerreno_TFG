using AuxiliarClasses;
using System.Linq;
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
    int baseSide; // Lado de cada chunk teniendo en cuenta densidad
    int clusterSideX; // Cantidad de vertices de lado X
    int clusterSideZ; // Cantidad de vertices de lado Z
    int chunkSideX; // Cantidad de chunks de los que está compuesto de lado X
    int chunkSideZ; // Cantidad de chunks de los que está compuesto de lado Z
    public int lodLevel;

    public int density { get; set; }
    public bool alreadyLoaded { get; set; } = false;

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

        vertices = _vertices;

        clusterSideX = _clusterSideX;
        clusterSideZ = _clusterSideZ;
        
        baseSide = baseSize * density + 1;

        chunkSideX = (clusterSideX - 1) / (baseSize * density);
        chunkSideZ = (clusterSideZ - 1) / (baseSize * density);

        lodLevel = Mathf.Max(chunkSideX,chunkSideZ);

        if (alreadyLoaded)
        {
            LoadMesh();
        }
    }

    // Función de unión de chunks con distintas sobrecargas
    public void ChunkFusion() // Fusión cuadrada, se unen 4 chunks o clusters
    {        
        int indexX = indexPos + chunkSideX;
        int indexZ = indexPos + (chunkSideZ * ChunkController.chunkSide);
        int indexXZ = indexX + (chunkSideZ * ChunkController.chunkSide);
        WorldVertex[] auxVertices;

        bool chunkXIsValid = indexX < ChunkController.chunkList.Length && indexPos / ChunkController.chunkSide == indexX / ChunkController.chunkSide && ChunkController.chunkList[indexX].clusterSideZ == clusterSideZ; // Comprueba que es válido dentro del índice y que no se encuentra en la siguiente fila de altura
        bool chunkZIsValid = indexZ < ChunkController.chunkList.Length && ChunkController.chunkList[indexZ].clusterSideX == clusterSideX; // Comprueba que es válido dentro del índice y que se encuentra en la siguiente fila de altura
        bool chunkXZIsValid = indexXZ < ChunkController.chunkList.Length && indexZ / ChunkController.chunkSide == indexXZ / ChunkController.chunkSide && ChunkController.chunkList[indexXZ].clusterSideZ == ChunkController.chunkList[indexZ].clusterSideZ && ChunkController.chunkList[indexXZ].clusterSideX == ChunkController.chunkList[indexX].clusterSideX; // Comprueba que es válido dentro del índice y que se encuentra en la siguiente fila de altura

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

            // Bucles para añadir los vertices al actual

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

            DataUpdate(auxVertices, clusterSideX, clusterSideZ); // Actualiza datos chunkObject

            // Desactiva chunks fusionados
            clusterX.gameObject.SetActive(false);
            clusterZ.gameObject.SetActive(false);
            clusterXZ.gameObject.SetActive(false);
        }
        else if (chunkXIsValid) // Fusión Lateral
        {
            var clusterX = ChunkController.chunkList[indexX];
            clusterX.parentChunkIndex = indexPos;
            
            auxVertices = new WorldVertex[clusterSideZ * (clusterSideX + clusterX.clusterSideX - 1)];

            // Bucle para añadir los vertices al actual
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

            DataUpdate(auxVertices, clusterSideX, clusterSideZ); // Actualiza datos chunkObject

            // Desactiva chunk fusionado
            clusterX.gameObject.SetActive(false);
        }
        else if (chunkZIsValid) // Fusión Arriba
        {
            var clusterZ = ChunkController.chunkList[indexZ];
            clusterZ.parentChunkIndex = indexPos;

            auxVertices = new WorldVertex[clusterSideX * (clusterSideZ + (clusterZ.clusterSideZ - 1))];

            // Bucles para añadir los vertices al actual
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

            DataUpdate(auxVertices, clusterSideX, clusterSideZ); // Actualiza datos chunkObject                      

            // Desactiva chunks fusionados
            clusterZ.gameObject.SetActive(false);
        }
    }

    // Función de separación de chunks
    public void ChunkDivision() // Guarda los datos del chunks como el cuadrado de abajo a la izquierda y activa los que estaban unidos
    {
        WorldVertex[] auxVertices; // Matriz de los vertices que va a tenre el cluster / chunk actual

        int loDtoLoadX = Mathf.NextPowerOfTwo(chunkSideX) / 2 != 0 ? Mathf.NextPowerOfTwo(chunkSideX) / 2 : 0; // Calculo del valor LoD menor al actual si es posible
        int loDtoLoadZ = Mathf.NextPowerOfTwo(chunkSideZ) / 2 != 0 ? Mathf.NextPowerOfTwo(chunkSideZ) / 2 : 0; // Calculo del valor LoD menor al actual si es posible

        int xAxis;
        int zAxis;

        if (loDtoLoadX == loDtoLoadZ)
        {            
            xAxis = loDtoLoadX * baseSize * density + 1;
            zAxis = loDtoLoadZ * baseSize * density + 1;

            ChunkController.chunkList[indexPos + loDtoLoadX].gameObject.SetActive(true); // Activa chunk abajo dcha
            ChunkController.chunkList[indexPos + loDtoLoadX].LoadMesh();

            ChunkController.chunkList[indexPos + loDtoLoadZ * ChunkController.chunkSide].gameObject.SetActive(true); // Activa chunk arriba izq
            ChunkController.chunkList[indexPos + loDtoLoadZ * ChunkController.chunkSide].LoadMesh();

            ChunkController.chunkList[indexPos + loDtoLoadX + loDtoLoadZ * ChunkController.chunkSide].gameObject.SetActive(true); // Activa chunk arriba dcha
            ChunkController.chunkList[indexPos + loDtoLoadX + loDtoLoadZ * ChunkController.chunkSide].LoadMesh();

        }
        else if (loDtoLoadX > loDtoLoadZ)
        {
            xAxis = loDtoLoadX * baseSize * density + 1;
            zAxis = chunkSideZ * baseSize * density + 1;

            ChunkController.chunkList[indexPos + loDtoLoadX].gameObject.SetActive(true); // Activa chunk abajo dcha
            ChunkController.chunkList[indexPos + loDtoLoadX].LoadMesh();
        }
        else
        {
            xAxis = chunkSideX * baseSize * density + 1;
            zAxis = loDtoLoadZ * baseSize * density + 1;

            ChunkController.chunkList[indexPos + loDtoLoadZ * ChunkController.chunkSide].gameObject.SetActive(true); // Activa chunk arriba izq
            ChunkController.chunkList[indexPos + loDtoLoadZ * ChunkController.chunkSide].LoadMesh();
        }

        auxVertices = new WorldVertex[xAxis * zAxis];

        for (int v = 0, z = 0; z < zAxis; z++) // Añade los vertices del chunk base al nuevo array para su posterior asignación al chunk base. Siempre se encuentra en la esquina inferior izquierda
        {
            for (int x = 0; x < xAxis; x++)
            {
                auxVertices[v] = vertices[x + z * clusterSideX];
                v++;
            }
        }

        DataUpdate(auxVertices, xAxis, zAxis);
    }

    public void LoadMesh()
    {
        mesh.Clear();

        if (lodLevel == 1)
        {
            meshCollider.sharedMesh.Clear();
        }

        CreateShape();
        ColorChange();
        UpdateMesh();
    }

    void CreateShape()
    {
        meshVertices = new Vector3[baseSide * baseSide];

        float coordAdditionX = (float) (baseSize * chunkSideX) / (baseSide - 1); // Cuanto hay que añadir por vertice en las coord si damos por hecho que cada chunk base es de 20m * 20m // La formula (baseSide * chunkSide + 1) calcula cual sería el tamaño total de los chunks.
        float coordAdditionZ = (float) (baseSize * chunkSideZ) / (baseSide - 1);

        for (int v = 0, z = 0; z < baseSide; z++)
        {
            v = z * clusterSideX * chunkSideZ;

            for (int x = 0; x < baseSide; x++)
            {
                Vector3 vertexData = new Vector3(x * coordAdditionX, vertices[v].height * ChunkController.heightMultiplier, z * coordAdditionZ); // Vector de coordenadas creadas para corresponder el tamaño del chunk salvo la altura, que corresponde al mapa del mundo realizado con anterioridad

                meshVertices[x + z * baseSide] = vertexData;

                v += chunkSideX;
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
            v = z * clusterSideX * chunkSideZ;

            for (int x = 0; x < baseSide; x++)
            {
                colors[x + z * baseSide] = vertices[v].color;

                v += chunkSideX;
            }
        }
    }

    void UpdateMesh()
    {
        // Para suavizar el cabio a malla de menor detalle
        float yOffset = (lodLevel - 1) * -.2f;
        transform.position = new Vector3(transform.position.x, yOffset, transform.position.z);

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