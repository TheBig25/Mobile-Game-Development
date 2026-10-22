using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] public int pScore = 0;

    private Rigidbody2D rb2d;

    public InputAction moveAction;
    public TextMeshProUGUI scoreText;

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
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Collectible"))
        {
            pScore++;
        }
    }
}