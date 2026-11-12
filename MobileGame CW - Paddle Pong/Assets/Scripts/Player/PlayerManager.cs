using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    // Player Variables
    [Header("Player Variables")]
    [SerializeField] int moveSpeed;
    [SerializeField] float topBound;
    [SerializeField] float bottomBound;

    [SerializeField] public int playerScore;
    public float TopBound => topBound;
    public float BottomBound => bottomBound;


    Vector2 lastTouchPosition;
    bool isDragging;

    private void Awake()
    {
        playerScore = 0;
    }

    void Update()
    {
        HandleTouchMovement();
        ClampPosition();
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

        //Set dragging to true when finger touches the screen
        if (touch.phase == TouchPhase.Began)
        {
            lastTouchPosition = touch.position;
            isDragging = true;
        }
        //Update player position based on touch movement
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
        else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
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

    //When bullet hits player, add to score
    public void AddScore(int scoreToAdd)
    {
        playerScore += scoreToAdd;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Bullet"))
        {
            AddScore(100);
            //Vibrate on collision
            Handheld.Vibrate();
        }
    }
}