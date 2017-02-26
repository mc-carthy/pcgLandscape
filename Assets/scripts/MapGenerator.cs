using UnityEngine;

public class MapGenerator : MonoBehaviour {

    private const int MAP_CHUNK_SIZE = 241;

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

    public void GenerateMap ()
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

        MapDisplay display = FindObjectOfType<MapDisplay> ();
        if (drawMode == DrawMode.Noise)
        {
            display.DrawTexture (TextureGenerator.TextureFromHeightMap (noiseMap));
        }
        else if (drawMode == DrawMode.ColorMap)
        {
            display.DrawTexture (TextureGenerator.TextureFromColorMap (colorMap, MAP_CHUNK_SIZE, MAP_CHUNK_SIZE));
        }
        else if (drawMode == DrawMode.Mesh)
        {
            display.DrawMesh (MeshGenerator.GenerateTerrainMesh (noiseMap, meshHeightMultiplier, meshHeightCurve, levelOfDetail), TextureGenerator.TextureFromColorMap (colorMap, MAP_CHUNK_SIZE, MAP_CHUNK_SIZE));
        }
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

}

[System.SerializableAttribute]
public struct TerrainType {
    public string name;
    public float height;
    public Color color;
}