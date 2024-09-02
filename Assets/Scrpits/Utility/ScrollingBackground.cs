using UnityEngine;

public class ScrollingBackground : MonoBehaviour
{
    public float scrollSpeed = 0.5f;
    private Material backgroundMaterial;
    private Vector2 offset;

    void Start()
    {
        backgroundMaterial = GetComponent<Renderer>().material;
    }

    void Update()
    {
        offset = new Vector2(Time.time * -scrollSpeed, Time.time * -scrollSpeed);
        backgroundMaterial.mainTextureOffset = offset;
    }
}
