using AuxiliarClasses;
using UnityEngine;

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
    
    public static int heightMultiplier { get; private set; } = 10;

    int baseSize = 20;

    int radiusDownCorner;
    int radiusUpCorner;
    int radiusZoneStart;
    int radiusZoneEnd;

    // Start is called before the first frame update
    void Start()
    {
        enabled = false; // Deshabilitado para que no ejecute el método Update, que se encargará de cargar los LoDs según la distancia a jugador luego de la primera carga
    }

    // Update is called once per frame
    void Update()
    {
        
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

                for (int cZ = 0; cZ < chunkSideVertices; cZ++)
                {   
                    for (int cX = 0; cX < chunkSideVertices; cX++)
                    {
                        int worldVertexIndex = z * mapSideVertices * chunkSideJump + x * chunkSideJump + cZ * mapSideVertices + cX;

                        _vertices[cX + cZ * chunkSideVertices] = worldVertices[worldVertexIndex]; // Añade el rango de vértices correspondiente al chunk a la lista de vértices del chunk
                    }                        
                }

                chunkAssetScript = Instantiate(chunkAsset, transform.position + new Vector3(x * baseSize, 0, z * baseSize), Quaternion.identity).GetComponent<ChunkObject>(); // Instancia el chunk en la posición correspondiente
                chunkAssetScript.MeshInizialiciation(); // Crea la malla del chunk y asigna sus componentes MeshFilter y MeshCollider para su correcto funcionamiento a la hora de renderizar y colisionar con el chunk
                chunkAssetScript.DataUpdate(_vertices, chunkSideVertices, chunkSideVertices); // Actualiza los datos del chunk con la lista de vértices del chunk y sus dimensiones
                chunkAssetScript.indexPos = z * chunkSide + x; // Guarda la posición del chunk en la lista de chunks para su posterior gestión en la unión y separación de chunks
                chunkAssetScript.density = density;

                chunkList[x + z * chunkSide] = chunkAssetScript; // Añade el chunk a la lista de chunks para su posterior gestión
            }
        }

        ChunkFirstLoad(); // Carga de Lods según distancia a jugador
    }

    void ChunkFirstLoad() // Carga de Lods según distancia a jugador
    {
        int chunkListSpawnCenter = chunkList.Length % 2 == 0 ? chunkList.Length / 2 - chunkSide / 2 + 1 : chunkList.Length - chunkList.Length / 2;

        int maxAllowedLoD = (int)Mathf.Log(Mathf.NextPowerOfTwo(chunkSide / 2 - radius - 1), 2) + 1; // LoD máximo permitido con el centro del radio en el centro del mapa. Aunque puede que al moverse el jugador pueda aumentar la simplicidad del LoD, no debería de importar lo suficiente si desde el centro no permite realizar esta operación.

        if (maxAllowedLoD < maxLoDLevel) // Asignamos el LoD máximo permitido al LoD máximo a cargar para evitar cargar LoDs demasiado simples y que afectarían a la zona dentro del radio. Solo si es menor el calculado al seleccionado.
        {
            maxLoDLevel = maxAllowedLoD;
        }        

        if (radius * 2 + 1 > chunkSide)
        {
            for (int i = 0; i < chunkList.Length; i++)
            {
                chunkList[i].LoadMesh();
            }
        }
        else
        {
            radiusDownCorner = chunkListSpawnCenter - radius * chunkSide - radius;
            radiusUpCorner = chunkListSpawnCenter + radius * chunkSide + radius;
            radiusZoneStart = (chunkListSpawnCenter - radius * chunkSide) / chunkSide; // Coordenada en X o Z del primer chunk que se encuentra dentro del radio
            radiusZoneEnd = (chunkListSpawnCenter + radius * chunkSide) / chunkSide;

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

            foreach (ChunkObject chunk in chunkList) // Si los chunks están activados activar render
            {
                if (chunk.gameObject.activeSelf)
                {
                    chunk.LoadMesh();
                }
            }

            enabled = true; // Habilita el script para que se ejecute el método Update, que se encargará de cargar los LoDs según la distancia a jugador luego de la primera carga
        }
    }

    void LoDCheck() // Comprueba posición de jugador para cargar LoDs según distancia
    {

    }


    void ChunkLoad() // Renderizado de los Chunks
    {

    }
}