﻿using UnityEngine;

public class MapGenerator : MonoBehaviour {

    public enum DrawMode {
        Noise,
        ColorMap
    };

    public DrawMode drawMode;

	public int mapWidth;
    public int mapHeight;
    public float noiseScale;

    public int octaves;
    [RangeAttribute (0, 1)]
    public float persistance;
    public float lacunarity;

    public int seed;
    public Vector2 offset;

    public bool autoUpdate;

    public TerrainType [] regions;

    public void GenerateMap ()
    {
        float [,] noiseMap = Noise.GenerateNoiseMap (mapWidth, mapHeight, noiseScale, seed, octaves, persistance, lacunarity, offset);

        Color [] colorMap = new Color [mapWidth * mapHeight];

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                float currentHeight = noiseMap [x, y];
                
                for (int i = 0; i < regions.Length; i++)
                {
                    if (currentHeight <= regions [i].height)
                    {
                        colorMap [x * mapHeight + y] = regions [i].color;
                        break;
                    }
                }
            }
        }

        MapDisplay display = FindObjectOfType<MapDisplay> ();
        if (drawMode == DrawMode.Noise)
        {
            display.DrawTexture (TextureGenerator.TextureFromHeightMap (noiseMap));
        }
        else if (drawMode == DrawMode.ColorMap)
        {
            display.DrawTexture (TextureGenerator.TextureFromColorMap (colorMap, mapWidth, mapHeight));
        }
    }

    private void OnValidate ()
    {
        if (mapWidth < 1)
        {
            mapWidth = 1;
        }
        if (mapHeight < 1)
        {
            mapHeight = 1;
        }
        if (octaves < 0)
        {
            octaves = 0;
        }
        if (lacunarity < 1)
        {
            lacunarity = 1;
        }
    }

}

[System.SerializableAttribute]
public struct TerrainType {
    public string name;
    public float height;
    public Color color;
}