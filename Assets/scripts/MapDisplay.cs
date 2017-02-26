using UnityEngine;

public class MapDisplay : MonoBehaviour {

    public Renderer textRen;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    public void DrawTexture (Texture2D texture)
    {
        textRen.sharedMaterial.mainTexture = texture;
        textRen.transform.localScale = new Vector3 (texture.width, 1, texture.height);
    }

    public void DrawMesh (MeshData meshData, Texture2D texture)
    {
        meshFilter.sharedMesh = meshData.CreateMesh ();
        meshRenderer.sharedMaterial.mainTexture = texture;
    }

}
