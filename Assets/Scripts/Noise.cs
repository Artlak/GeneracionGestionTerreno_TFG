using System;
using UnityEngine;

public static class Noise
{
    public static float[] GenerateNoiseMap(int chunksSide, int seed, float scale, int octaves, float persistence, float lacunarity /*Aumento de frecuencia*/, float heightLimit, bool resistance)
    {
        int sideVertex = 21 + chunksSide * 20;

        float[] noiseMap = new float[sideVertex * sideVertex];

        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffset = new Vector2[octaves]; // Cambio de posición de octavas
        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000); // Rango sin problemas (Con valores altos el resultado se repite)
            float offsetY = prng.Next(-100000, 100000);

            octaveOffset[i] = new Vector2(offsetX, offsetY);
        }

        if (scale <= 1f)
            scale = 1.1f;

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        float halfMap = sideVertex / 2f;

        for (int v = 0, y = 0; y < sideVertex; y++)
        {
            for (int x = 0; x < sideVertex; x++)
            {
                float amplitude = 1; // Cuanto altera la altura
                float frequency = 1; // Separación de los puntos
                float noiseHeight = 0; // Valor en un punto del ruido

                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = ((x - halfMap) / scale - octaveOffset[i].x) * frequency;
                    float sampleY = ((y - halfMap) / scale - octaveOffset[i].y) * frequency;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1; // Rango -1 a 1
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                if (noiseHeight > maxNoiseHeight)
                    maxNoiseHeight = noiseHeight;
                if (noiseHeight < minNoiseHeight)
                    minNoiseHeight = noiseHeight;

                noiseMap[v] = noiseHeight;

                v++;
            }
        }

        if (!resistance)
        {
            // Normalización altura con valores entre 0f y 1f
            for (int v = 0, y = 0; y < sideVertex; y++)
            {
                for (int x = 0; x < sideVertex; x++)
                {
                    noiseMap[v] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[v]);
                    if (noiseMap[v] > heightLimit)
                        noiseMap[v] = heightLimit;
                    v++;
                }
            }
        }
        else
        {
            // Normalización valores entre resistencia +- resistanceVar, que es heightLimit
            for (int v = 0, y = 0; y < sideVertex; y++)
            {
                for (int x = 0; x < sideVertex; x++)
                {
                    noiseMap[v] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[v]);
                    if (noiseMap[v] <= heightLimit)
                    {
                        noiseMap[v] = heightLimit;
                    }
                    else
                    {                        
                        if (heightLimit >= 0.8)
                        {
                            noiseMap[v] = 1f;
                        }
                        else
                        {
                            noiseMap[v] = heightLimit + 0.2f;
                        }
                    }

                    v++;
                }
            }
        }

        return noiseMap;
    }
}