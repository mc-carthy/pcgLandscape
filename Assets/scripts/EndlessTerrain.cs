using UnityEngine;
using System.Collections.Generic;

public class EndlessTerrain : MonoBehaviour {

    private const float VIEWER_MOVE_THRESHOLD_FOR_CHUNK_UPDATE = 25f;
    private const float SQR_VIEWER_MOVE_THRESHOLD_FOR_CHUNK_UPDATE = VIEWER_MOVE_THRESHOLD_FOR_CHUNK_UPDATE * VIEWER_MOVE_THRESHOLD_FOR_CHUNK_UPDATE;

    private static MapGenerator mapGenerator;
    
    public LodInfo [] detailLevels;
	public static float maxViewDistance;

    public Transform viewer;
    public Material mapMaterial;

    public static Vector2 viewerPosition;
    private Vector2 viewerPositionOld;

    private int chunkSize;
    private int chunksVisibleInViewDistance;

    private Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk> ();
    private List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk> ();

    private void Start ()
    {
        mapGenerator = FindObjectOfType<MapGenerator> ();
        maxViewDistance = detailLevels [detailLevels.Length - 1].visibleDistThreshold;
        chunkSize = MapGenerator.MAP_CHUNK_SIZE - 1;
        chunksVisibleInViewDistance = Mathf.RoundToInt (maxViewDistance / chunkSize);
        
        UpdateVisibleChunks ();
    }

    private void Update ()
    {
        viewerPosition = new Vector2 (viewer.position.x, viewer.position.z);

        if ((viewerPositionOld - viewerPosition).sqrMagnitude > SQR_VIEWER_MOVE_THRESHOLD_FOR_CHUNK_UPDATE)
        {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks ();
        }
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
                    terrainChunkDictionary.Add (viewedChunkCoord, new TerrainChunk (viewedChunkCoord, chunkSize, detailLevels, transform, mapMaterial));
                }
            }
        }
    }

    public class TerrainChunk {

        private Vector2 position;
        private GameObject meshObject;
        private Bounds bounds;

        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;

        private LodInfo [] detailLevels;
        private LODMesh [] lodMeshes;

        private MapData mapData;
        private bool mapDataReceived;
        private int previousLodIndex = -1;

        public TerrainChunk (Vector2 coord, int size, LodInfo [] _detailLevels, Transform parent, Material material)
        {
            detailLevels = _detailLevels;
            position = coord * size;
            bounds = new Bounds (position, Vector2.one * size);
            Vector3 positionV3 = new Vector3 (position.x, 0, position.y);

            meshObject = new GameObject ("TerrainChunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer> ();
            meshFilter = meshObject.AddComponent<MeshFilter> ();

            meshRenderer.material = material;

            meshObject.transform.position = positionV3;
            meshObject.transform.parent = parent;
            SetVisible (false);

            lodMeshes = new LODMesh [detailLevels.Length];

            for (int i = 0; i < detailLevels.Length; i++)
            {
                lodMeshes [i] = new LODMesh (detailLevels [i].lod, UpdateTerrainChunk);
            }

            mapGenerator.RequestMapData (OnMapDataReceived);
        }

        public void UpdateTerrainChunk ()
        {
            if (mapDataReceived)
            {
                float viewerDistanceFromNearestEdge = Mathf.Sqrt (bounds.SqrDistance (viewerPosition));
                bool isVisible = viewerDistanceFromNearestEdge <= maxViewDistance;

                if (isVisible)
                {
                    int lodIndex = 0;

                    for (int i = 0; i < detailLevels.Length - 1; i++)
                    {
                        if (viewerDistanceFromNearestEdge > detailLevels [i].visibleDistThreshold)
                        {
                            lodIndex = i + 1;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (lodIndex != previousLodIndex)
                    {
                        LODMesh lodMesh = lodMeshes [lodIndex];
                        if (lodMesh.hasMesh)
                        {
                            previousLodIndex = lodIndex;
                            meshFilter.mesh = lodMesh.mesh;
                        }
                        else if (!lodMesh.hasRequestedMesh)
                        {
                            lodMesh.RequestMesh (mapData);
                        }
                    }
                }
                SetVisible (isVisible);
            }
        }

        public void SetVisible (bool isVisible)
        {
            meshObject.SetActive (isVisible);
        }

        public bool IsVisible ()
        {
            return meshObject.activeSelf;
        }

        private void OnMapDataReceived (MapData _mapData)
        {
            mapData = _mapData;
            mapDataReceived = true;

            Texture2D texture = TextureGenerator.TextureFromColorMap (mapData.colorMap, MapGenerator.MAP_CHUNK_SIZE, MapGenerator.MAP_CHUNK_SIZE);
            meshRenderer.material.mainTexture = texture;

            UpdateTerrainChunk ();
        }


    }

    class LODMesh {

        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        private int lod;
        System.Action updateCallback;

        public LODMesh (int _lod, System.Action _updateCallback)
        {
            lod = _lod;
            updateCallback = _updateCallback;
        }

        public void RequestMesh (MapData mapData)
        {
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData (mapData, lod, OnMeshDataReceived);

            updateCallback ();
        }

        private void OnMeshDataReceived (MeshData meshData)
        {
            mesh = meshData.CreateMesh ();
            hasMesh = true;
        }
    }

    [System.SerializableAttribute]
    public struct LodInfo {
        public int lod;
        public float visibleDistThreshold;
    }
}
