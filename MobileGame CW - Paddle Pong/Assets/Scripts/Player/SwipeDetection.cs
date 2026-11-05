using UnityEngine;

public class SwipeDetection : MonoBehaviour
{

    public GameObject player;
    private Vector2 startPos;
    public int pixelDistToDetect = 20;
    private bool fingerDown;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //If finger isn't down and there's more than one touch on screen, move.
        if (fingerDown = false && Input.touchCount > 0 && Input.touches[0].phase == TouchPhase.Began)
        {
            startPos = Input.touches[0].position;
            fingerDown = true;
        }

        //PC Check (debug)
        if (fingerDown && Input.GetMouseButtonDown(0))
        {
            startPos = Input.mousePosition;
            fingerDown = true;
        }

        if (fingerDown) 
        {
            if (Input.touches[0].position.y >= startPos.y + pixelDistToDetect)
            { 
                fingerDown = false;
                Debug.Log("Player swipe UP");
            }
        }
    }
}
