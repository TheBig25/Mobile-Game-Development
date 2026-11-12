using UnityEngine;

public class Bullet_Yellow : MonoBehaviour
{
    public float bulletSpeed = 10;
    private Rigidbody2D rb;
    private int direction = -1;
    public int vibrateDurationMS = 100; //Milliseconds

    //Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    //Update is called once per frame
    void Update()
    {
        rb.linearVelocityX = direction * bulletSpeed;
    }

    //Change direction when colliding with player
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            direction = 1;

            //Vibrate on collision
            ////Handheld.Vibrate();
        }
    }

    //Ignore bullets colliding with each other
    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Bullet"))
        {
            Physics2D.IgnoreCollision(collision.collider, GetComponent<Collider2D>());
        }
    }

    //Destroy off screen
    void OnBecameInvisible()
    {
        Destroy(gameObject);
    }
}