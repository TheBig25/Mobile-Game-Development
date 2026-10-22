using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] public int pScore = 0;
    [SerializeField] public float timeLimit;

    private Rigidbody2D rb2d;

    public InputAction moveAction;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timeText;
    void Start()
    {
        rb2d = GetComponent<Rigidbody2D>();

        moveAction = InputSystem.actions.FindAction("Move");
    }

    void Update()
    {
        scoreText.text = "Score: " + pScore.ToString();

        Vector2 moveInput = moveAction.ReadValue<Vector2>();

        //Apply movement
        rb2d.linearVelocity = moveInput * moveSpeed;

        //Score Clamp
        pScore = Mathf.Clamp(pScore, 0, 99999999);

        Vector3 cameraBounds = Camera.main.WorldToViewportPoint(transform.position);
        cameraBounds.x = Mathf.Clamp01(cameraBounds.x);
        cameraBounds.y = Mathf.Clamp01(cameraBounds.y);
        transform.position = Camera.main.ViewportToWorldPoint(cameraBounds);

        //Decrease time
        if (timeLimit > 0)
        {
            timeLimit -= Time.deltaTime;
            timeText.text = "Time Remaining: " + timeLimit.ToString();
            float minutes = Mathf.FloorToInt(timeLimit / 60);
            float seconds = Mathf.FloorToInt(timeLimit % 60);

            timeText.text = string.Format("Time: {0:00}:{1:00}", minutes, seconds);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Collectible"))
        {
            pScore++;
        }
    }
}