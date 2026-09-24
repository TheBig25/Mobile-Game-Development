using UnityEngine;

public class HealthCollectible : MonoBehaviour
{
    public int moveSpd;
    public float collectibleValue;
    public Rigidbody2D rb;

    public Player_Health player_Health;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip healthGetSFX;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        //Move left
        rb.linearVelocityX = -moveSpd;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {

            SoundManager.instance.PlaySound(healthGetSFX);

            Player_Health playerHealth = collision.gameObject.GetComponent<Player_Health>();

            //Run the function if player alive
            if (playerHealth != null)
            {
                playerHealth.currentHealth += collectibleValue;
            }

            Destroy(gameObject);
        }
    }

    //Disappear offscreen
    private void OnBecameInvisible()
    {
        Destroy(this.gameObject);
    }
}
