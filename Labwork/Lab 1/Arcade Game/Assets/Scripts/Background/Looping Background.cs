using UnityEngine;

public class LoopingBackground : MonoBehaviour
{
    public float scrollSpeed = 0.1f;
    private Material mat;
    private Vector2 offset;

    void Start()
    {
        mat = GetComponent<SpriteRenderer>().material;
    }

    void Update()
    {
        offset = mat.mainTextureOffset;
        offset.x += scrollSpeed * Time.deltaTime;   
        mat.mainTextureOffset = offset;
    }
}
