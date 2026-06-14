using AuxiliarClasses;
using JetBrains.Annotations;
using System.Collections.Generic;
using System.Drawing;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class ChunkController : MonoBehaviour
{
    public static ChunkObject[] chunkList { get; set; }

    public GameObject chunkAsset;

    [SerializeField] Transform player;
    [Range(5, 100)]
    public int radius = 5;
    [Range(2, 10)]
    public int maxLoDLevel = 5;
    public static int chunkSide { get; private set; } // Cantidad de chunks de lado a lado del mapa, se asigna desde el método CreateChunks al crear los chunks, se utiliza para gestionar los chunks y sus LoDs dentro del código chunkObject
    
    public static int heightMultiplier { get; private set; } = 70;

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

    // direcciones
    readonly int rightD = 0;
    readonly int leftD = 1;
    readonly int upD = 2;
    readonly int downD = 3;

    void Start()
    {
        enabled = false; // Deshabilitado para que no ejecute el método Update, que se encargará de cargar los LoDs según la distancia a jugador luego de la primera carga
    }

    // Update is called once per frame
    void Update()
    {
        if (CheckPosition())
        {
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
        chunkListSpawnCenter = chunkList.Length % 2 == 0 ? chunkList.Length / 2 - chunkSide / 2 : chunkList.Length / 2; // Chunk a considerar central, donde va a aparecer el jugador

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

            // Mover jugador antes de activar carga de chunks de forma dinámica

            player.position = new Vector3(lastChunkX * baseSize, 100, lastChunkZ * baseSize);

            enabled = true; // Habilita el script para que se ejecute el método Update, que se encargará de cargar los LoDs según la distancia a jugador luego de la primera carga
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

        int posOrNeg = direction % 2 == 0 ? -1 : 0; // Esta variable se usa en las funciones de escaneo y cambio de los LoDs en la parte en la que debe comprobar hacia que dirección tiene que comprobar.
        if (direction == rightD || direction == leftD) // Revisa movimientos a izq y dcha
        {
            int rightCorner = radiusDownCorner + side - 1;

            if (direction == rightD)
            {
                if (right)
                {
                    SideOperations(heigth, rightCorner, !right, posOrNeg); // Llama a funcion que realiza de forma automática las funciones de carga y descarga de chunks. Especifica del lateral
                }                

                if (left && (playerChunkListPosition - radius - 2) / chunkSide == playerChunkListPosition / chunkSide)
                {
                    SideOperations(heigth, radiusDownCorner, right, posOrNeg); // Llama a funcion que realiza de forma automática las funciones de carga y descarga de chunks. Especifica del lateral
                }                    
            }
            else if (direction == leftD)
            {
                if (left)
                {                    
                    SideOperations(heigth, radiusDownCorner, !left, posOrNeg); // LLama a funcion que realiza de forma automática las funciones de carga y descarga de chunks. Especifica del lateral
                }

                if (right && (playerChunkListPosition + radius + 2) / chunkSide == playerChunkListPosition / chunkSide)
                {                    
                    SideOperations(heigth, rightCorner, left, posOrNeg); // LLama a funcion que realiza de forma automática las funciones de carga y descarga de chunks. Especifica del lateral
                }
            }
        }
        else // Revisa movimientos arriba y abajo
        {
            int upCorner = radiusUpCorner - side + 1;

            if (direction == upD)
            {
                if (up)
                {                    
                    HeightOperations(side, upCorner, !up, posOrNeg); // LLama a funcion que realiza de forma automática las funciones de carga y descarga de chunks. Especifica de altura
                }

                if (down && (playerChunkListPosition - (radius - 2) * chunkSide) % chunkSide == playerChunkListPosition % chunkSide)
                {
                    HeightOperations(side, radiusDownCorner, up, posOrNeg); // LLama a funcion que realiza de forma automática las funciones de carga y descarga de chunks. Especifica de altura
                }
            }
            else if (direction == downD)
            {
                if (down)
                {
                    HeightOperations(side, radiusDownCorner, !left, posOrNeg); // LLama a funcion que realiza de forma automática las funciones de carga y descarga de chunks. Especifica de altura
                }

                if (up && (playerChunkListPosition + (radius + 2) * chunkSide) % chunkSide == playerChunkListPosition % chunkSide)
                {
                    HeightOperations(side, upCorner, left, posOrNeg); // LLama a funcion que realiza de forma automática las funciones de carga y descarga de chunks. Especifica de altura
                }
            }
        }
    }

    void HeightOperations(int _side, int _corner, bool _addLod, int _posOrNeg) // Realiza un bucle que sive tanto para izquierda como derecha que revisa una fila entera y devuelve a la máxima calidad los elementos con LoD aplicado.
    {
        if (_addLod) // Comprueba si es para bajar el nivel de detalle o simplemente para dividir
        {
            int loDlvl = 1; // Carga de loD a comprobar
            int currentLoDlvl = 1 << loDlvl; // Me permite conocer el multiplo de 2 actual que usaré para realizar varias operaciones y comparaciones. Se actualiza al final del bucle.
            int baseSide = _corner % chunkSide;
            bool stillSameColumn = true; // Variable usada para comprobar si se puede seguir comprobando la columna o no quedan más elementos que comprobar para crear mayores Lods

            while (stillSameColumn && loDlvl < maxLoDLevel) // Comprobación de altura coincidente y si supera el max LoD permitido
            {
                for (int vertexSide, v = 0, x = 0; x < _side; x++)
                {
                    vertexSide = x + baseSide; // Altura. La guardo para mayor legibilidad en el código a la hora de comprobar si es multiplo de alguna potencia del LoDActual
                    v = _corner + x + (currentLoDlvl * _posOrNeg) * chunkSide; // Vertices de la nueva fila a comprobar

                    if (chunkList[v].gameObject.activeSelf && vertexSide % currentLoDlvl == 0 && (_corner + (currentLoDlvl * _posOrNeg) * chunkSide) % currentLoDlvl == 0 && chunkList[v].lodLevel < currentLoDlvl)
                    {
                        chunkList[v].ChunkFusion();
                        x += currentLoDlvl - 1; // Para saltar elementos que ya se sabe que van a ser nulos o que no se pueden unir.
                    }
                    else if (x > currentLoDlvl && chunkList[v].gameObject.activeSelf)
                    {
                        return;
                    }
                }
                loDlvl++; // Aumento de LoD para siguiente vuelta
                currentLoDlvl = 1 << loDlvl; // Actualización de variable
                stillSameColumn = (_corner + (currentLoDlvl * _posOrNeg) * chunkSide) % chunkSide == baseSide; // Comprobación de que sigue siendo válido el bucle. Uso de la variable posOrNeg para saber en que dirección continuar la búsqueda de chunks para LoDs
            }
        }
        else
        {
            Stack<int> indexList = new Stack<int>(); // Pila usada para encontrar origen de LoD padre e ir cargando en orden los LoDs de mayor calidad hasta tener en máxima calidad el chunk indicado

            for (int v = 0, x = 0; x < _side; x++)
            {
                v = _corner + x;
                indexList.Push(v);
                do
                {
                    if (chunkList[v].gameObject.activeSelf)
                    {
                        if (chunkList[v].lodLevel != 1) // Solo si necesita ser dividido
                        {
                            v = indexList.Pop();
                            chunkList[v].ChunkDivision();
                        }
                        else
                        {
                            indexList.Pop();
                        }
                    }
                    else
                    {
                        indexList.Push(chunkList[v].parentChunkIndex);
                        v = chunkList[v].parentChunkIndex;
                    }
                } while (indexList.Count > 0);
            }
        }
    }

    void SideOperations(int _height, int _corner, bool _addLod, int _posOrNeg) // Realiza un bucle que sive tanto para izquierda como derecha que revisa una columna entera y devuelve a la máxima calidad los elementos con LoD aplicado 
    {
        if (_addLod) // Comprueba si es para bajar el nivel de detalle o simplemente para dividir
        {
            int loDlvl = 1; // Carga de loD a comprobar
            int currentLoDlvl = 1 << loDlvl; // Me permite conocer el multiplo de 2 actual que usaré para realizar varias operaciones y comparaciones. Se actualiza al final del bucle.
            int baseHeight = _corner / chunkSide;
            bool stillSameRow = true; // Variable usada para comprobar si se puede seguir comprobando la fila o no quedan más elementos que comprobar para crear mayores Lods

            while (stillSameRow && loDlvl < maxLoDLevel) // Comprobación de altura coincidente y si supera el max LoD permitido
            {
                for (int vertexHeight, v = 0, z = 0; z < _height; z++)
                {
                    vertexHeight = z * chunkSide; // Altura. La guardo para mayor legibilidad en el ´código a la hora de comprobar si es multiplo de alguna potencia del LoDActual
                    v = _corner + (currentLoDlvl * _posOrNeg) + z * chunkSide; // Vertices de la nueva columna a comprobar
                    
                    if (chunkList[v].gameObject.activeSelf && vertexHeight % currentLoDlvl == 0 && (_corner + (currentLoDlvl * _posOrNeg)) % currentLoDlvl == 0 && chunkList[v].lodLevel < currentLoDlvl)
                    {
                        chunkList[v].ChunkFusion();
                        z += currentLoDlvl - 1; // Para saltar elementos que ya se sabe que van a ser nulos o que no se pueden unir.
                    }
                    else if (z > currentLoDlvl && chunkList[v].gameObject.activeSelf)
                    {
                        return;
                    }
                }
                loDlvl++; // Aumento de LoD para siguiente vuelta
                currentLoDlvl = 1 << loDlvl; // Actualización de variable
                stillSameRow = (_corner + (currentLoDlvl * _posOrNeg)) / chunkSide == baseHeight; // Comprobación de que sigue siendo válido el bucle. Uso de la variable posOrNeg para saber en que dirección continuar la búsqueda de chunks para LoDs
            }            
        }
        else
        {
            Stack<int> indexList = new Stack<int>(); // Pila usada para encontrar origen de LoD padre e ir cargando en orden los LoDs de mayor calidad hasta tener en máxima calidad el chunk indicado

            for (int v = 0, z = 0; z < _height; z++)
            {
                v = _corner + z * chunkSide;
                indexList.Push(v);

                do
                {
                    if (chunkList[v].gameObject.activeSelf)
                    {
                        if (chunkList[v].lodLevel != 1) // Solo si necesita ser dividido
                        {
                            v = indexList.Pop();
                            chunkList[v].ChunkDivision();
                        }
                        else
                        {
                            indexList.Pop();
                        }
                    }
                    else
                    {
                        indexList.Push(chunkList[v].parentChunkIndex);
                        v = chunkList[v].parentChunkIndex;
                    }
                } while (indexList.Count > 0);                
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