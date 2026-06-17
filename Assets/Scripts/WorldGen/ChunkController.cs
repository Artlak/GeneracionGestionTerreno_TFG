using AuxiliarClasses;
using System;
using System.Collections.Generic;
using UnityEngine;

public class ChunkController : MonoBehaviour
{
    public static ChunkObject[] chunkList { get; set; }

    public GameObject chunkAsset;

    [SerializeField] Transform player;
    [Range(5, 100)]
    public int radius = 5;
    [Range(2, 20)]
    public int maxLoDLevel = 5;
    public static int chunkSide { get; private set; } // Cantidad de chunks de lado a lado del mapa, se asigna desde el método CreateChunks al crear los chunks, se utiliza para gestionar los chunks y sus LoDs dentro del código chunkObject
    
    public static int heightMultiplier { get; set; } = 70;

    readonly int baseSize = 20;

    int chunkListSpawnCenter;

    int radiusDownCorner;
    int radiusUpCorner;
    int radiusZoneStart;
    int radiusZoneEnd;

    // Variables de posición del jugador para la gestión de LoDs según distancia
    int playerChunkX;
    int playerChunkZ;

    int lastChunkX;
    int lastChunkZ;

    int direction;
    bool canCheck = false; // Deshabilitado para que no ejecute el método Update, que se encargará de cargar los LoDs según la distancia a jugador luego de la primera carga

    // direcciones
    readonly int rightD = 0;
    readonly int leftD = 1;
    readonly int upD = 2;
    readonly int downD = 3;

    // Update is called once per frame
    void Update()
    {
        if (canCheck && CheckPosition())
        {
            Debug.Log("Cambio de chunk");
            LodLoader();
        }        
    }

    public void CreateChunks(WorldVertex[] worldVertices, int _chunkSide, int density) // Primera carga de LoDs con el centro o pos de jugador guardada como referencia
    {
        chunkSide = _chunkSide;
        chunkList = new ChunkObject[chunkSide * chunkSide]; // Inicializo la lista de chunks
        ChunkObject chunkAssetScript;
        
        int chunkSideVertices = baseSize * density + 1;
        int mapSideVertices = chunkSide * baseSize * density + 1;
        int chunkSideJump = chunkSideVertices - 1;

        chunkListSpawnCenter = chunkList.Length % 2 == 0 ? chunkList.Length / 2 - chunkSide / 2 : chunkList.Length / 2; // Chunk a considerar central, donde va a aparecer el jugador        
        player.position = new Vector3(chunkListSpawnCenter % chunkSide * baseSize, heightMultiplier + 2f, chunkListSpawnCenter / chunkSide * baseSize);
        for (int z = 0; z < chunkSide; z++)
        {
            for (int x = 0; x < chunkSide; x++)
            {
                WorldVertex[] _vertices = new WorldVertex[chunkSideVertices * chunkSideVertices];

                for (int cZ = 0; cZ < chunkSideVertices; cZ++) // Añade el rango de vértices correspondiente al chunk a la lista de vértices del chunk
                {   
                    for (int cX = 0; cX < chunkSideVertices; cX++)
                    {
                        int worldVertexIndex = z * mapSideVertices * chunkSideJump + x * chunkSideJump + cZ * mapSideVertices + cX; // Índice del mapa total de vértices

                        _vertices[cX + cZ * chunkSideVertices] = worldVertices[worldVertexIndex];   
                    }
                }                

                chunkAssetScript = Instantiate(chunkAsset, transform.position + new Vector3(x * baseSize, 0, z * baseSize), Quaternion.identity).GetComponent<ChunkObject>(); // Instancia el chunk en la posición correspondiente
                chunkAssetScript.density = density; // Asigna la densidad al chunk para que pueda calcular su malla correctamente (si la paso más tarde del data update = 0)
                chunkAssetScript.MeshInizialiciation(); // Crea la malla del chunk y asigna sus componentes MeshFilter y MeshCollider para su correcto funcionamiento a la hora de renderizar y colisionar con el chunk
                chunkAssetScript.DataUpdate(_vertices, chunkSideVertices, chunkSideVertices); // Actualiza los datos del chunk con la lista de vértices del chunk y sus dimensiones
                chunkAssetScript.indexPos = z * chunkSide + x; // Guarda la posición del chunk en la lista de chunks para su posterior gestión en la unión y separación de chunks                

                chunkList[x + z * chunkSide] = chunkAssetScript; // Añade el chunk a la lista de chunks para su posterior gestión
            }
        }

        ChunkFirstLoad(); // Carga de Lods según distancia a jugador
    }

    void ChunkFirstLoad() // Carga de Lods según distancia a jugador
    {
        player.position = new Vector3(chunkListSpawnCenter % chunkSide * baseSize, heightMultiplier + 2f, chunkListSpawnCenter / chunkSide * baseSize);
        Debug.Log(chunkSide + " Chunkside");        

        lastChunkX = chunkListSpawnCenter % chunkSide;
        lastChunkZ = chunkListSpawnCenter / chunkSide;

        int maxAllowedLoD = (int)Mathf.Log(Mathf.NextPowerOfTwo(chunkSide / 2 - radius - 1), 2) + 1; // LoD máximo permitido con el centro del radio en el centro del mapa. Aunque puede que al moverse el jugador pueda aumentar la simplicidad del LoD, no debería de importar lo suficiente si desde el centro no permite realizar esta operación.

        if (maxAllowedLoD < maxLoDLevel) // Asignamos el LoD máximo permitido al LoD máximo a cargar para evitar cargar LoDs demasiado simples y que afectarían a la zona dentro del radio. Solo si es menor el calculado al seleccionado.
        {
            maxLoDLevel = maxAllowedLoD;
        }        

        if (radius * 2 + 2 > chunkSide)
        {
            for (int i = 0; i < chunkList.Length; i++)
            {
                chunkList[i].LoadMesh();
            }
            player.position = new Vector3(lastChunkX * baseSize, heightMultiplier + 2f, lastChunkZ * baseSize);
        }
        else // Se encarga de decidir que chunks fusionar, activar los válidos y el update para comprobar la posición del jugador
        {
            radiusDownCorner = chunkListSpawnCenter - radius * chunkSide - radius; // Coord del primer chunk dentro del radio, se calcula restando al centro del spawn el número de chunks que hay en el radio multiplicado por el número de chunks que hay en cada línea, y restando el número de chunks que hay en el radio para tener en cuenta los chunks que se encuentran a la izquierda del centro del spawn
            radiusUpCorner = chunkListSpawnCenter + radius * chunkSide + radius; // Coord del último chunk dentro del radio, se calcula igual que la anterior variable pero restando el radio
            radiusZoneStart = (chunkListSpawnCenter - radius) % chunkSide; // Coordenada en X que marca el inicio de la zona del radio, se calcula restando al centro del spawn el número de chunks que hay en el radio multiplicado por el número de chunks que hay en cada línea, y dividiendo el resultado entre el número de chunks que hay en cada línea para pasar de coordenada 1D a coordenada 2D
            radiusZoneEnd = (chunkListSpawnCenter + radius) % chunkSide;// Coordenada en X que marca el fin de la zona del radio, se calcula al igual que la anterior variable pero restando

            for (int jump, i = 1; i < maxLoDLevel; i++) // Para saltar los chunks y líneas de chunks que no se van a usar es necesario comenzar por el valor 1 y acabar antes de llegar al vamor máximo. Para entenderlo: El LoD 2 se calcula a la primera vuelta, y los elementos a saltar siempre son el elemento en el que se encuentra + 2 elevado a LoD - 1.
            {
                jump = 1 << i; // Cantidad de chunks a saltar para cada LoD

                for (int z = 0; z < chunkSide; z += jump) // Se salta las líneas de chunks que no se van a usar para cargar los LoDs buscando solo desde las que puede realizar un LoD de menos calidad.
                {
                    for (int x = 0; x < chunkSide; x += jump)
                    {
                        int basePosition = x + z * chunkSide; // Primer chunk de los 4 a juntar
                        int sidePosition = x + jump + z * chunkSide; // Chunk dcha a juntar
                        int sideUpPosition = x + jump + ((z + jump) * chunkSide); // Chunk dcha y arriba a juntar

                        bool validLoDPosition = (sideUpPosition < radiusDownCorner || basePosition > radiusUpCorner) /* Se encuentra antes o después de la zona de radio */ || (sidePosition < radiusZoneStart + z * chunkSide /* Se encuentra a la izq de la zona de radio */ || basePosition > radiusZoneEnd + z * chunkSide) /* Se encuentra a la dcha de la zona de radio */; // Comprueba si se encuentra antes de llegar al primer elemento que pertenece al radio, después o a los bordes del radio

                        if (validLoDPosition) 
                        {
                            chunkList[x + z * chunkSide].ChunkFusion();
                        }
                    }
                }
            }

            for (int i = 0; i < chunkList.Length; i++)
            {
                chunkList[i].alreadyLoaded = true;
                if (chunkList[i].gameObject.activeSelf)
                {
                    chunkList[i].LoadMesh();
                }
            }

            // Mover jugador antes de activar carga de chunks de forma dinámica

            player.position = new Vector3(lastChunkX * baseSize, heightMultiplier + 2f, lastChunkZ * baseSize);

            canCheck = true; // Habilita el script para que se ejecute el método Update, que se encargará de cargar los LoDs según la distancia a jugador luego de la primera carga
        }
    }

    void LodLoader() // Según donde se haya movido el jugador, mapea la zona y escanea la zona alrrededor del radio para cargar o descargar los chunks que se encuentran alrrededor o dentro de la zona
    {
        int heigth;
        int side;

        int playerChunkListPosition = playerChunkX + playerChunkZ * chunkSide;

        // Comprobar si se encuentra pegado contra alguno de los bordes del mapa

        bool right = (playerChunkListPosition + radius) / chunkSide == playerChunkListPosition / chunkSide ? true : false;

        bool left = (playerChunkListPosition - radius) / chunkSide == playerChunkListPosition / chunkSide ? true : false;

        bool up = (playerChunkListPosition + radius * chunkSide) < chunkList.Length ? true : false;

        bool down = (playerChunkListPosition - radius * chunkSide) >= 0 ? true : false;

        if (right && left && up && down) // No se encuentra junto a ningún borde
        {
            heigth = 2 * radius + 1;
            side = 2 * radius + 1;
            radiusDownCorner = playerChunkListPosition - (radius * chunkSide) - radius;
            radiusUpCorner = playerChunkListPosition + (radius * chunkSide) + radius;
            radiusZoneStart = (playerChunkListPosition - radius) % chunkSide;
            radiusZoneEnd = (playerChunkListPosition + radius) % chunkSide;
        }
        else
        {
            // Calculo altura
            int heightUp = up ? radius : chunkSide - playerChunkListPosition / chunkSide - 1;

            int heightDown = down ? radius : playerChunkListPosition / chunkSide;

            heigth = heightUp + heightDown + 1;

            // Calculo lado
            int sideRight = right ? radius : chunkSide - playerChunkListPosition % chunkSide;

            int sideLeft = left ? radius : playerChunkListPosition % chunkSide - 1;

            side = sideRight + sideLeft + 1;

            radiusDownCorner = playerChunkListPosition - (heightDown * chunkSide) - sideLeft;
            radiusUpCorner = playerChunkListPosition + (heightUp * chunkSide) + sideRight;
            radiusZoneStart = (playerChunkListPosition + sideLeft) % chunkSide;
            radiusZoneEnd = (playerChunkListPosition + sideRight) % chunkSide;
        }

        int posOrNeg = 1;

        if (direction == rightD) posOrNeg = -1; // Si voy a la derecha, fusiono los de la izquierda (-X)
        else if (direction == leftD) posOrNeg = 1;  // Si voy a la izquierda, fusiono los de la derecha (+X)
        else if (direction == upD) posOrNeg = -1; // Si voy hacia arriba, fusiono los de abajo (-Z * chunkSide)
        else if (direction == downD) posOrNeg = 1;  // Si voy hacia abajo, fusiono los de arriba (+Z * chunkSide)

        if (direction == rightD || direction == leftD) // Revisa movimientos a izq y dcha
        {
            int rightCorner = radiusDownCorner + side;

            if (direction == rightD)
            {
                if (right)
                {
                    DivisionOperations(heigth, rightCorner, true); // Llama a funcion que realiza de forma automática las funciones de carga y descarga de chunks. Especifica del lateral
                }                

                if (left && (playerChunkListPosition - radius - 2) / chunkSide == playerChunkListPosition / chunkSide)
                {
                    SideOperations(heigth, radiusDownCorner, posOrNeg); // Llama a funcion que realiza de forma automática las funciones de carga y descarga de chunks. Especifica del lateral
                }                    
            }
            else if (direction == leftD)
            {
                if (left)
                {                    
                    DivisionOperations(heigth, radiusDownCorner, true); // LLama a funcion que realiza de forma automática las funciones de carga y descarga de chunks. Especifica del lateral
                }

                if (right && (playerChunkListPosition + radius + 2) / chunkSide == playerChunkListPosition / chunkSide)
                {                    
                    SideOperations(heigth, rightCorner, posOrNeg); // LLama a funcion que realiza de forma automática las funciones de carga y descarga de chunks. Especifica del lateral
                }
            }
        }
        else // Revisa movimientos arriba y abajo
        {
            int upCorner = radiusUpCorner - side;

            if (direction == upD)
            {
                if (up)
                {                    
                    DivisionOperations(side, upCorner, false); // LLama a funcion que realiza de forma automática las funciones de carga y descarga de chunks. Especifica de altura
                }

                if (down && (playerChunkListPosition - (radius - 2) * chunkSide) % chunkSide == playerChunkListPosition % chunkSide)
                {
                    HeightOperations(side, radiusDownCorner, posOrNeg); // LLama a funcion que realiza de forma automática las funciones de carga y descarga de chunks. Especifica de altura
                }
            }
            else if (direction == downD)
            {
                if (down)
                {
                    DivisionOperations(side, radiusDownCorner, false); // LLama a funcion que realiza de forma automática las funciones de carga y descarga de chunks. Especifica de altura
                }

                if (up && (playerChunkListPosition + (radius + 2) * chunkSide) % chunkSide == playerChunkListPosition % chunkSide)
                {
                    HeightOperations(side, upCorner, posOrNeg); // LLama a funcion que realiza de forma automática las funciones de carga y descarga de chunks. Especifica de altura
                }
            }
        }
    }

    void HeightOperations(int _side, int _corner, int _posOrNeg) // Operación que mapea zonas altas y bajar alrrededor del radio para comprobar si se pueden unir en LoDs mayores
    {
        for (int x = 0; x < _side; x++)
        {
            int baseChunkIndex = _corner + x; // Coordenada base
            if (baseChunkIndex < 0 || baseChunkIndex >= chunkList.Length || chunkList[baseChunkIndex] == null) continue; // Coprobar si es válida

            for (int loDlvl = 1; loDlvl < maxLoDLevel; loDlvl++)
            {
                int currentLoDlvl = 1 << loDlvl;

                int targetChunkIndex = baseChunkIndex + (currentLoDlvl * _posOrNeg * chunkSide);
                if (targetChunkIndex < 0 || targetChunkIndex >= chunkList.Length || chunkList[targetChunkIndex] == null) break; // Coprobar si es válida

                // Valores simplificados a eje x y z
                int originX = chunkList[targetChunkIndex].indexPos % chunkSide;
                int originZ = chunkList[targetChunkIndex].indexPos / chunkSide;

                if (originX % currentLoDlvl != 0 || originZ % currentLoDlvl != 0) break; // Coprobar si es válida en este nivel de lod

                if (chunkList[targetChunkIndex].gameObject.activeSelf && chunkList[targetChunkIndex].lodLevel < currentLoDlvl)
                {
                    chunkList[targetChunkIndex].ChunkFusion(); // Fusion de chunks si es valido
                }
                else
                {
                    break;
                }
            }
        }
    }

    void SideOperations(int _height, int _corner, int _posOrNeg) // Operación que mapea zonas laterales alrrededor del radio para comprobar si se pueden unir en LoDs mayores
    {
        for (int z = 0; z < _height; z++)
        {
            int baseChunkIndex = _corner + (z * chunkSide); // Coordenada base
            if (baseChunkIndex < 0 || baseChunkIndex >= chunkList.Length || chunkList[baseChunkIndex] == null) continue; // Coprobar si es válida

            int baseRow = baseChunkIndex / chunkSide; // Altura de linea

            for (int loDlvl = 1; loDlvl < maxLoDLevel; loDlvl++) // Comprobamos si alcanzó máximo de comprobaciones de carga de LoDs
            {
                int currentLoDlvl = 1 << loDlvl; // Asignamos para mayor claridad

                int targetChunkIndex = baseChunkIndex + (currentLoDlvl * _posOrNeg); // Chunk a comprobar si permite LoD

                // Comprobaciones de seguridad por si no es válido
                if (targetChunkIndex < 0 || targetChunkIndex >= chunkList.Length || chunkList[targetChunkIndex] == null) break;
                if (chunkList[targetChunkIndex].indexPos / chunkSide != baseRow) break;
                
                // Valores simplificados a eje x y z
                int originX = chunkList[targetChunkIndex].indexPos % chunkSide;
                int originZ = chunkList[targetChunkIndex].indexPos / chunkSide;

                if (originX % currentLoDlvl != 0 || originZ % currentLoDlvl != 0) break; // Comprobamos si se encuentra en una linea y columna que permita fuison y si permite el tipo de fusion adecuado

                if (chunkList[targetChunkIndex].gameObject.activeSelf && chunkList[targetChunkIndex].lodLevel < currentLoDlvl)
                {
                    chunkList[targetChunkIndex].ChunkFusion(); // Unimos chunks
                }
                else
                {
                    break; // Salimos de la línea
                }
            }
        }
    }

    void DivisionOperations(int _side, int _corner, bool lateral)
    {
        Stack<int> indexList = new Stack<int>(); // Pila usada para encontrar origen de LoD padre e ir cargando en orden los LoDs de mayor calidad hasta tener en máxima calidad el chunk indicado

        for (int v = 0, i = 0; i < _side; i++)
        {
            v = lateral ? _corner + i * chunkSide : _corner + i;

            if (chunkList[v].gameObject.activeSelf && chunkList[v].lodLevel == 1) continue;

            int currentIndex = v;
            while (currentIndex >= 0 && currentIndex < chunkList.Length && !chunkList[currentIndex].gameObject.activeSelf)
            {
                indexList.Push(currentIndex);
                currentIndex = chunkList[currentIndex].parentChunkIndex; // Viaje hacia arriba
            }

            // Si el ancestro que encontramos encendido necesita dividirse, se mete a la cola
            if (currentIndex >= 0 && currentIndex < chunkList.Length && chunkList[currentIndex].lodLevel != 1)
            {
                indexList.Push(currentIndex);
            }

            // Operaciones de división
            while (indexList.Count > 0)
            {
                int chunkParaDividir = indexList.Pop();

                if (chunkList[chunkParaDividir].lodLevel != 1)
                {
                    chunkList[chunkParaDividir].ChunkDivision();
                }
            }
        }
    }
    bool CheckPosition() // Comprobar si hace falta cargar y descargar LoDs
    {
        // Por si acaso en un futuro decido cambiarlo a que el 0,0,0 sea el centro del mapa
        playerChunkX = Mathf.FloorToInt(player.position.x / baseSize);
        playerChunkZ = Mathf.FloorToInt(player.position.z / baseSize);

        if (playerChunkX < 0 || playerChunkX > chunkSide - 1)
        {
            playerChunkX = lastChunkX;
            player.position = new Vector3(lastChunkX * baseSize, player.position.y, player.position.z);
            return false;
        }

        if (playerChunkZ < 0 || playerChunkZ > chunkSide - 1)
        {
            playerChunkZ = lastChunkZ;
            player.position = new Vector3(player.position.x, player.position.y, lastChunkZ * baseSize);
            return false;
        }

        if (playerChunkX != lastChunkX || playerChunkZ != lastChunkZ) // Si el jugador ha cambiado de chunk, se comprueba si es necesario cargar o descargar algún LoD
        {
            // Dirección del movimiento del jugador, 0 = derecha, 1 = izquierda, 2 = arriba, 3 = abajo
            if (playerChunkX > lastChunkX)
            {
                direction = 0;
            }
            else if (playerChunkX < lastChunkX)
            {
                direction = 1;
            }
            else if (playerChunkZ > lastChunkZ)
            {
                direction = 2;
            }
            else if (playerChunkZ < lastChunkZ)
            {
                direction = 3;
            }

            lastChunkX = playerChunkX;
            lastChunkZ = playerChunkZ;

            return true;
        }

        return false;
    }
}