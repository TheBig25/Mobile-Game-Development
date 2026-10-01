using UnityEngine;

public class LoopingForeground : MonoBehaviour
{
    public float scrollSpeed = 2f;
    private Vector2 startPosition;
    public float bgSizeHorizontal; 

    void Start()
    {
        //Set Starting position
        startPosition = transform.position;
    }

    void Update()
    {
        //Limit how far the background should move
        float newPosition = Mathf.Repeat(Time.time * scrollSpeed, bgSizeHorizontal);
        //Move the object towards the new position
        transform.position = startPosition + Vector2.left * newPosition;
    }
}