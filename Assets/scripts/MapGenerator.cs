using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading;

public class MapGenerator : MonoBehaviour {

    public const int MAP_CHUNK_SIZE = 241;

    public enum DrawMode {
        Noise,
        ColorMap,
        Mesh
    };

    public DrawMode drawMode;

    public float noiseScale;

    public int octaves;
    [RangeAttribute (0, 1)]
    public float persistance;
    public float lacunarity;

    public int seed;
    public Vector2 offset;

    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;

    [RangeAttribute (0, 6)]
    public int levelOfDetail;

    public bool autoUpdate;

    public TerrainType [] regions;

    private Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>> ();

    private void Update ()
    {
        if (mapDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < mapDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue ();
                threadInfo.callback (threadInfo.parameter);
            }
        }
    }

    public void DrawMapInEditor ()
    {
        MapData mapData = GenerateMapData ();
        MapDisplay display = FindObjectOfType<MapDisplay> ();
        if (drawMode == DrawMode.Noise)
        {
            display.DrawTexture (TextureGenerator.TextureFromHeightMap (mapData.heightMap));
        }
        else if (drawMode == DrawMode.ColorMap)
        {
            display.DrawTexture (TextureGenerator.TextureFromColorMap (mapData.colorMap, MAP_CHUNK_SIZE, MAP_CHUNK_SIZE));
        }
        else if (drawMode == DrawMode.Mesh)
        {
            display.DrawMesh (MeshGenerator.GenerateTerrainMesh (mapData.heightMap, meshHeightMultiplier, meshHeightCurve, levelOfDetail), TextureGenerator.TextureFromColorMap (mapData.colorMap, MAP_CHUNK_SIZE, MAP_CHUNK_SIZE));
        }
    }

    public void RequestMapData (Action<MapData> callback)
    {
        ThreadStart threadStart = delegate {
            MapDataThread (callback);
        };

        new Thread (threadStart).Start ();
    }

    private void MapDataThread (Action<MapData> callback)
    {
        MapData mapData = GenerateMapData ();
        lock (mapDataThreadInfoQueue)
        {
            mapDataThreadInfoQueue.Enqueue (new MapThreadInfo<MapData> (callback, mapData));
        }
    }

    private MapData GenerateMapData ()
    {
        float [,] noiseMap = Noise.GenerateNoiseMap (MAP_CHUNK_SIZE, MAP_CHUNK_SIZE, seed, noiseScale, octaves, persistance, lacunarity, offset);

        Color [] colorMap = new Color [MAP_CHUNK_SIZE * MAP_CHUNK_SIZE];

        for (int x = 0; x < MAP_CHUNK_SIZE; x++)
        {
            for (int y = 0; y < MAP_CHUNK_SIZE; y++)
            {
                float currentHeight = noiseMap [x, y];
                
                for (int i = 0; i < regions.Length; i++)
                {
                    if (currentHeight <= regions [i].height)
                    {
                        colorMap [y * MAP_CHUNK_SIZE + x] = regions [i].color;
                        break;
                    }
                }
            }
        }

        return new MapData (noiseMap, colorMap);

    }

    private void OnValidate ()
    {
        if (octaves < 0)
        {
            octaves = 0;
        }
        if (lacunarity < 1)
        {
            lacunarity = 1;
        }
    }

    struct MapThreadInfo<T> {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo (Action<T> _callback, T _paramter)
        {
            callback = _callback;
            parameter = _paramter;
        }
    }

}

[System.SerializableAttribute]
public struct TerrainType {
    public string name;
    public float height;
    public Color color;
}

public struct MapData {
    public readonly float [,] heightMap;
    public readonly Color [] colorMap;

    public MapData (float [,] _heightMap, Color [] _colorMap)
    {
        heightMap = _heightMap;
        colorMap = _colorMap;
    }
}