using UnityEngine;

public class MapDisplay : MonoBehaviour {

    public Renderer textRen;

    public void DrawTexture (Texture2D texture)
    {
        textRen.sharedMaterial.mainTexture = texture;
        textRen.transform.localScale = new Vector3 (texture.width, 1, texture.height);
    }

}
