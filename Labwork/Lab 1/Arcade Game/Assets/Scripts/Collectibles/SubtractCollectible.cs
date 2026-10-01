using System.Collections;
using UnityEngine;

public class SubtractCollectible : MonoBehaviour
{
    public int moveSpd;
    public float powerupDuration;
    public Rigidbody2D rb;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip weaponGetSFX;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        //Move left
        rb.linearVelocityX = -moveSpd;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            PlayerManager player = collision.gameObject.GetComponent<PlayerManager>();

            SoundManager.instance.PlaySound(weaponGetSFX);

            //Run the function if player alive
            if (player != null)
            {
                player.SubtractStartup(powerupDuration);
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