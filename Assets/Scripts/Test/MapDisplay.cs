using UnityEngine;

public class MapDisplay : MonoBehaviour
{
    public Renderer textureRender;

    public void DrawTerrainMap(float[] noiseMap)
    {
        int size = (int) Mathf.Sqrt(noiseMap.Length);

        Texture2D texture = new Texture2D(size, size);

        Color[] colorMap = new Color[size * size];
        for (int v = 0, y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                colorMap[y * size + x] = Color.Lerp(Color.black, Color.white, noiseMap[v]);
                v++;
            }
        }
        texture.SetPixels(colorMap);
        texture.Apply();

        textureRender.sharedMaterial.mainTexture = texture;
        textureRender.transform.localScale = new Vector3(size, 1, size);
    }
}