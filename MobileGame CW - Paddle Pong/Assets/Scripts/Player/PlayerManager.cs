using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    // Player Variables
    [Header("Player Variables")]
    [SerializeField] int moveSpeed;
    [SerializeField] float topBound;
    [SerializeField] float bottomBound;

    public float TopBound => topBound;
    public float BottomBound => bottomBound;

    Vector2 lastTouchPosition;
    bool isDragging;

    void Update()
    {
        HandleKeyboardMovement();
        HandleTouchMovement();
        ClampPosition();
    }



    void HandleKeyboardMovement()
    {
        float movement = 0f;

        //Keyboard inputs
        if (Input.GetKey(KeyCode.W))
        {
            movement = moveSpeed * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            movement = -moveSpeed * Time.deltaTime;
        }

        transform.position += new Vector3(0f, movement, 0f);
    }

    void HandleTouchMovement()
    {
        //Touch inputs
        if (Input.touchCount == 0)
        {
            isDragging = false;
            return;
        }

        Touch touch = Input.GetTouch(0);

        //If the touch has just begun, set dragging to true
        if (touch.phase == TouchPhase.Began)
        {
            lastTouchPosition = touch.position;
            isDragging = true;
        }
        //Else if the touch is moving and dragging is true, update the player's position
        else if (touch.phase == TouchPhase.Moved && isDragging)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }

            float cameraDistance = Mathf.Abs(transform.position.z - mainCamera.transform.position.z);

            //Convert the touch position from screen to world space
            Vector3 previousWorldPosition = mainCamera.ScreenToWorldPoint(new Vector3(lastTouchPosition.x, lastTouchPosition.y, cameraDistance));

            Vector3 currentWorldPosition = mainCamera.ScreenToWorldPoint(new Vector3(touch.position.x, touch.position.y, cameraDistance));

            //Move player
            float verticalMovement = currentWorldPosition.y - previousWorldPosition.y;

            transform.position += new Vector3(0f, verticalMovement, 0f);
            lastTouchPosition = touch.position;
        }

        //Stop moving when finger lifted
        else if (touch.phase == TouchPhase.Ended ||touch.phase == TouchPhase.Canceled)
        {
            isDragging = false;
        }
    }

    void ClampPosition()
    {
        //Stop player from going out of bounds
        Vector3 position = transform.position;
        position.y = Mathf.Clamp(position.y, bottomBound, topBound);
        transform.position = position;
    }
}