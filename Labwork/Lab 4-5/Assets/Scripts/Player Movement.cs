using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    private bool dragging = false;
    private Vector3 offset;

    void Update()
    {
        if (dragging) 
        {
            //Move object with original offset
            transform.position = Camera.main.ScreenToWorldPoint(transform.position);
        }
    }

    private void OnMouseDown()
    {
        offset = transform.position - Camera.main.ScreenToWorldPoint(Input.mousePosition);
        dragging = true;
    }

    private void OnMouseUp() 
    { 
        dragging = false;
    }
}