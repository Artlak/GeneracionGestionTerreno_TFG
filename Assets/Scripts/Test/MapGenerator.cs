using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [Range(1, 200)]
    public int chunksSide;
    [Range(0f, 1f)]
    public float heightLimit;
    [Range(1, 16)]
    public int octaves;
    [Range(0f, 1f)]
    public float persistence;
    [Range(1f, 2f)]
    public float lacunarity;
    [Range(1.1f, 100f)]
    public float scale;

    public bool resistance;

    public int seed;

    public bool autoUpdate;

    public void GenerateMap()
    {
        float[] noiseMap = Noise.GenerateNoiseMap(chunksSide, seed, scale, octaves, persistence, lacunarity, heightLimit, resistance);

        MapDisplay display = FindObjectOfType<MapDisplay>();
        display.DrawTerrainMap(noiseMap);
    }

    private void OnValidate()
    {
        if (chunksSide < 1)
            chunksSide = 1;

        if (lacunarity < 1)
            lacunarity = 1;

        if (octaves < 1)
            octaves = 1;
    }
}
