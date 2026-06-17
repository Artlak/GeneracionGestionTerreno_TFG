using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasController : MonoBehaviour
{
    public WorldGenerator worldGenerator;
    public ChunkController chunkController;

    // Textos Y Sliders
    public TextMeshProUGUI chunkSide;

    public TextMeshProUGUI density;

    public TextMeshProUGUI biomes;

    public TextMeshProUGUI radius;

    public TextMeshProUGUI lodLvl;

    public TextMeshProUGUI height;

    public Canvas canvas;

    public PlayerController playerController;

    public Button button;

    private void Start()
    {
        playerController.enabled = false;
    }

    public void SetChunkSide(float _chunkSide)
    {
        worldGenerator.chunksSide = (int) _chunkSide;
        chunkSide.text = ((int)_chunkSide).ToString();
    }

    public void SetDensity(float _density)
    {
        worldGenerator.density = (int) _density;
        density.text = ((int)_density).ToString();
    }

    public void SetBiomes(float _biomes)
    {
        worldGenerator.extraBiomesCount = (byte) _biomes;
        biomes.text = ((int)_biomes).ToString();
    }

    public void SetRadius(float _radius)
    {
        chunkController.radius = (int) _radius;
        radius.text = ((int)_radius).ToString();
    }

    public void SetlodLvl(float _lodLvl)
    {
        chunkController.maxLoDLevel = (int) _lodLvl;
        lodLvl.text = ((int)_lodLvl).ToString();
    }

    public void SetHeight(float _height)
    {
        ChunkController.heightMultiplier = (int) _height;
        height.text = ((int)_height).ToString();
    }

    public void SetSeed(string seed)
    {
        worldGenerator.seed = Int32.Parse(seed);
    }

    public void StartGeneration()
    {
        button.enabled = false;
        playerController.enabled = true;
        canvas.enabled = false;
        worldGenerator.StartGeneration();
    }
}
