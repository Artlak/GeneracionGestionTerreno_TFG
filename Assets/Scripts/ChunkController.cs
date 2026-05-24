using AuxiliarClasses;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkController : MonoBehaviour
{
    List<ChunkObject> chunkArray;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void ChunkDivision(WorldVertex[] worldVertices, int chunkSide) // Primera carga de LoDs con el centro o pos de jugador guardada como referencia
    {
        for (int i = 0; i < chunkSide; i++)
        {

        }
    }

    void PositionCheck() // Comprueba posición de jugador para cargar LoDs según distancia
    {

    }

    void ChunkRecalculation() // Carga de Lods según distancia a jugador
    {

    }

    void ChunkLoad() // Renderizado de los Chunks
    {

    }
}