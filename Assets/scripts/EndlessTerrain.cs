using UnityEngine;
using System.Collections.Generic;

public class EndlessTerrain : MonoBehaviour {

    private static MapGenerator mapGenerator;

	public const float MAX_VIEW_DISTANCE = 450f;
    public Transform viewer;

    public static Vector2 viewerPosition;

    private int chunkSize;
    private int chunksVisibleInViewDistance;

    private Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk> ();
    private List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk> ();

    private void Start ()
    {
        mapGenerator = FindObjectOfType<MapGenerator> ();
        chunkSize = MapGenerator.MAP_CHUNK_SIZE - 1;
        chunksVisibleInViewDistance = Mathf.RoundToInt (MAX_VIEW_DISTANCE / chunkSize);
    }

    private void Update ()
    {
        viewerPosition = new Vector2 (viewer.position.x, viewer.position.z);
        UpdateVisibleChunks ();
    }

    private void UpdateVisibleChunks ()
    {
        for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++)
        {
            terrainChunksVisibleLastUpdate [i].SetVisible (false);
        }

        terrainChunksVisibleLastUpdate.Clear ();

        int currentChunkCoordX = Mathf.RoundToInt (viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt (viewerPosition.y / chunkSize);

        for (int xOffset = -chunksVisibleInViewDistance; xOffset <= chunksVisibleInViewDistance; xOffset++)
        {
            for (int yOffset = -chunksVisibleInViewDistance; yOffset <= chunksVisibleInViewDistance; yOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2 (currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (terrainChunkDictionary.ContainsKey (viewedChunkCoord))
                {
                    terrainChunkDictionary [viewedChunkCoord].UpdateTerrainChunk ();
                    if (terrainChunkDictionary [viewedChunkCoord].IsVisible ())
                    {
                        terrainChunksVisibleLastUpdate.Add (terrainChunkDictionary [viewedChunkCoord]);
                    }
                }
                else
                {
                    terrainChunkDictionary.Add (viewedChunkCoord, new TerrainChunk (viewedChunkCoord, chunkSize, transform));
                }
            }
        }
    }

    public class TerrainChunk {

        private Vector2 position;
        private GameObject meshObject;
        private Bounds bounds;

        public TerrainChunk (Vector2 coord, int size, Transform parent)
        {
            position = coord * size;
            bounds = new Bounds (position, Vector2.one * size);
            Vector3 positionV3 = new Vector3 (position.x, 0, position.y);

            meshObject = GameObject.CreatePrimitive (PrimitiveType.Plane);
            meshObject.transform.position = positionV3;
            meshObject.transform.localScale = Vector3.one * size / 10f;
            meshObject.transform.parent = parent;
            SetVisible (false);

            mapGenerator.RequestMapData (OnMapDataReceived);
        }

        public void UpdateTerrainChunk ()
        {
            float viewerDistanceFromNearestEdge = Mathf.Sqrt (bounds.SqrDistance (viewerPosition));
            bool isVisible = viewerDistanceFromNearestEdge <= MAX_VIEW_DISTANCE;
            SetVisible (isVisible);
        }

        public void SetVisible (bool isVisible)
        {
            meshObject.SetActive (isVisible);
        }

        public bool IsVisible ()
        {
            return meshObject.activeSelf;
        }

        private void OnMapDataReceived (MapData mapData)
        {
            print ("Map data received");
        }

    }
}
