using AuxiliarClasses;
using System.Collections.Generic;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
    Biome biome; // Se vacía luego de la creación de los biomas en el mapa, ya que es bastante pesado. Cuantos más biomas mayor;

    WorldVertex[] worldVertices; // Más rápido que lista u arrayList. Como no se va a alterar la mejor opción es array

    // Variables necesarias para la creacción del mundo

    [Range(1, 500)]
    public int chunksSide = 30; // Los Chunks, salvo que se altere la densidad, siempre serán de 21*21

    [Range(1, 5)]
    public int density = 1; // Multiplicador del tamaño del cuadrado que son los chunks Chunks (20). Media entre alturas de vertices para llenar huecos que no cubre el heightMap.

    public int seed = 0;

    [Range(0, 255)]
    public byte extraBiomesCount = 0;

    [SerializeField] Gradient defaultBiomeColor;

    public ChunkController chunkController;

    int verticesSide;
    int verticesSideAux;

    public void StartGeneration()
    {
        Cursor.lockState = CursorLockMode.Locked;

        verticesSide = chunksSide * 20 + 1; // 20 porque salvo la línea extra que todos usan del anterior, usan 20 propios.

        verticesSideAux = verticesSide + (verticesSide - 1) * (density - 1); // Los elementos extra se calculan con en número de vértices totales - 1 por la densidad. chunksSide * 20 * density + 1 también sería una operación válida para calcular todos los elementos laterales;

        worldVertices = new WorldVertex[verticesSideAux * verticesSideAux];

        GenerateWorldMap();
    }

    void GenerateWorldMap()
    {
        System.Random prng = new System.Random(seed);

        biome = new Biome(seed, defaultBiomeColor, chunksSide);

        VertexListCreator();

        if (extraBiomesCount != 0) // Si hay biomas extra
        {
            ExtraBiomesAssignator(prng);
        }

        if (density > 1) // Asignación de valores a vértices extra
        {
            ExtraVerticesAssignator();
        }

        Debug.Log("Mapa de vertices creado");

        chunkController.CreateChunks(worldVertices, chunksSide, density);

        worldVertices = null; // Limpieza de la variable para liberar memoria, ya que es bastante pesada y no se va a usar más

        
    }

    void VertexListCreator() // Añade la cantidad de vertices necesarios al arrayList
    {
        // Dividido en ifs para no tener que repetirlo cada vez que tiene que generar los vértices extra
        if (density == 1)
        {
            Debug.Log(biome.heightMap.Length);
            Debug.Log(worldVertices.Length);
            for (int i = 0; i < biome.heightMap.Length; i++)
            {
                WorldVertex auxVertex = new();
                auxVertex.height = biome.heightMap[i];
                auxVertex.resistance = biome.resistanceMap[i];
                auxVertex.color = Biome.ColorByResistance(biome.biomeColor.Evaluate(auxVertex.height), auxVertex.resistance);

                worldVertices[i] = auxVertex;
            }
        }
        else
        {
            for (int v = 0, y = 0; y < verticesSide; y++) // Coord Y
            {
                for (int x = 0; x < verticesSide; x++) // Coord X
                {
                    WorldVertex auxVertex = new();
                    auxVertex.height = biome.heightMap[x + y * verticesSide];
                    auxVertex.resistance = biome.resistanceMap[x + y * verticesSide];
                    auxVertex.color = Biome.ColorByResistance(biome.biomeColor.Evaluate(auxVertex.height), auxVertex.resistance);

                    worldVertices[v] = auxVertex;
                    v++;

                    if (x < verticesSide - 1) // Generación de vertices extra intercalados
                    {                        
                        for (int d = 1; d < density; d++)
                        {
                            WorldVertex auxVertex0 = new();
                            worldVertices[v] = auxVertex0;
                            v++;
                        }
                    }
                }

                if (y < verticesSide - 1)
                {
                    for (int d = 1; d < density; d++)
                    {
                        for (int e = 0; e < verticesSideAux; e++) // Generación de linea de puntos extra completa
                        {
                            WorldVertex auxVertex = new();
                            worldVertices[v] = auxVertex;
                            v++;
                        }

                    }
                }
            }
        }
    }

    void ExtraBiomesAssignator(System.Random prng)
    {
        for (int size, zones, b = 0; b < extraBiomesCount; b++) // size es el tamaño del bioma (aunque varíe ligeramente) zones comprende la cantidad de zonas del bioma totales
        {
            biome = new Biome(prng.Next(), chunksSide);
            size = chunksSide * prng.Next(1, 2); // Valores arbitrarios que a mi parecer permiten la personalización necesaria incluso con valores mínimos o muy altos
            zones = chunksSide / 2 == 0 ? 1 : chunksSide / 2; // Por si es un único chunk

            for (int sizeX, sizeY, startPos, a = 0; a < zones; a++)
            {
                startPos = prng.Next(verticesSide * verticesSide - 1);

                // Comprobación de que no se pasa de largo ni de ancho
                sizeX = Mathf.Abs((startPos % verticesSide) - verticesSide) - 1 >= size ? size : Mathf.Abs((startPos % verticesSide) - verticesSide) - 1;
                sizeY = Mathf.Abs((startPos / verticesSide) - verticesSide) - 1 >= size ? size : Mathf.Abs((startPos / verticesSide) - verticesSide) - 1;


                int indexStartPos = startPos;

                if (density > 1) // Si la densidad es mayor a 1, el index real cambiará radicalmente, por lo que hay que calcular que punto concide con cada punto. Para no aplicar la formula cada vez, solo calculo el primer punto y luego recorro la lista como lo haría de normal
                {
                    int height = startPos / verticesSide;
                    indexStartPos = startPos * density + height * verticesSideAux * density;
                }

                for (int currentPos, currentIndexPos, y = 0; y < sizeY; y++)
                {
                    currentPos = startPos + verticesSide * y;
                    currentIndexPos = startPos + verticesSideAux * density * y;

                    for (int x = 0; x < sizeX; x++)
                    {
                        WorldVertex auxVertex = new();
                        currentPos ++;
                        currentIndexPos += density;
                        auxVertex.height = biome.heightMap[currentPos];
                        auxVertex.resistance = biome.resistanceMap[currentPos];
                        auxVertex.resistance = biome.resistanceMap[currentPos];
                        auxVertex.color = Biome.ColorByResistance(biome.biomeColor.Evaluate(auxVertex.height), auxVertex.resistance);
                        worldVertices[currentIndexPos] = auxVertex;
                    }
                }
            }
        }

        biome.Clear();
    }

    void ExtraVerticesAssignator()
    {
        float hIncrement = 0;
        float rIncrement = 0; //Variables que guardan el incremento de altura y resistencia

        int densityVertexIndex = verticesSideAux - (density - 1); // Index para el correcto acabado del bucle

        for (int y = 0; y < densityVertexIndex; y += density)
        {
            for (int x = 0; x < densityVertexIndex; x += density)
            {
                // Usa los vertices que se encuentran arriba y abajo para calcular la interpolación

                hIncrement = (worldVertices[x + y * verticesSideAux + density].height - worldVertices[x + y * verticesSideAux].height) / density;
                rIncrement = (worldVertices[x + y * verticesSideAux + density].resistance - worldVertices[x + y * verticesSideAux].resistance) / density;

                for (int a = 1; a < density; a++)
                {
                    worldVertices[x + a + y * verticesSideAux].height = worldVertices[x + y * verticesSideAux].height + hIncrement * a; // El cálculo es la altura del último punto + incremento que es x parte del total para llegar al siguiente punto donde x es la densidad
                    worldVertices[x + a + y * verticesSideAux].resistance = worldVertices[x + y * verticesSideAux].resistance + rIncrement * a;
                    worldVertices[x + a + y * verticesSideAux].color = Color.Lerp(worldVertices[x + y * verticesSideAux + density].color, worldVertices[x + y * verticesSideAux].color, .5f); // Interpoación de color entre los elementos que rodean el vértice
                }
            }

            if (y > 0)
            {
                for (int a = density - 1; a > 0; a--) // Aplica la interpolación a las filas enteras
                {
                    for (int x = 0; x < verticesSideAux; x++)
                    {
                        hIncrement = (worldVertices[x + (y - density) * verticesSideAux].height - worldVertices[x + y * verticesSideAux].height) / density;
                        rIncrement = (worldVertices[x + (y - density) * verticesSideAux].resistance - worldVertices[x + y * verticesSideAux].resistance) / density;

                        worldVertices[x + (y - a) * verticesSideAux].height = worldVertices[x + y * verticesSideAux].height + hIncrement * a;
                        worldVertices[x + (y - a) * verticesSideAux].resistance = worldVertices[x + y * verticesSideAux].resistance + rIncrement * a;
                        worldVertices[x + (y - a) * verticesSideAux].color = Color.Lerp(worldVertices[x + (y - density) * verticesSideAux].color, worldVertices[x + y * verticesSideAux].color, .5f);
                    }
                }
            }
        }
    }
}