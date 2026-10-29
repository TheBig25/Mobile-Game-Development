using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SocialPlatforms.Impl;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private TouchJoystick touchJoystick;
    [SerializeField] public int pScore = 0;
    [SerializeField] public float timeLimit;

    public bool playerCanMove = true;
    public bool gameEnd = false;

    private Rigidbody2D rb2d;

    public InputAction moveAction;

    [Header("UI")]
    public GameObject endScreen;
    public GameObject mainUI;

    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI endScoreText;

    public TextMeshProUGUI timeText;

    void Start()
    {
        rb2d = GetComponent<Rigidbody2D>();

        moveAction = InputSystem.actions.FindAction("Move");

        pScore = 0;
        
        Time.timeScale = 1;
    }

    void Update()
    {
        scoreText.text = "Score: " + pScore.ToString();

        Vector2 moveInput;

        if (touchJoystick != null)
        {
            moveInput = touchJoystick.InputDirection;
        }
        else
        {
            moveInput = moveAction.ReadValue<Vector2>();
        }

        // Apply movement
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

        //End game
        if (timeLimit <= 0)
        {
            Time.timeScale = 0;
            mainUI.SetActive(false);
            endScreen.SetActive(true);

            endScoreText.text = "Final Score: " + pScore.ToString();

            loseGame();
        }
    }

    public void loseGame()
    {

        {
            //Freeze game and set time display to 0:00
            timeLimit = 0;
            timeText.text = "00:00";
            Time.timeScale = 0;
            gameEnd = true;
            endScoreText.text = "Score: " + pScore.ToString();

            //Show/hide menus
            mainUI.SetActive(false);
            endScreen.SetActive(true);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Collectible"))
        {
            pScore++;
        }
    }

    //End game
    void freezeGame()
    {
        if (gameEnd == true)
        {
            playerCanMove = false;

        }
    }
}