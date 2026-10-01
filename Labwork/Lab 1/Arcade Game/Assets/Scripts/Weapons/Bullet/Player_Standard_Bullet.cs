using UnityEngine;

public class Player_Standard_Bullet : MonoBehaviour
{
    [Header("Bullet Attributes")]
    public float bulletSpeed;
    public float bulletDamage;

    [Header("Lifetime Attributes")]
    public float lifetime;
    public float lifespan;

    Rigidbody2D rb;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();


        //Read multiplier values on player
        PlayerManager player = FindFirstObjectByType<PlayerManager>();
        if (player != null)
        {
            bulletDamage *= player.bulletDamageMultiplier;
            bulletSpeed *= player.bulletSpeedMultiplier;
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        //Move from left to right
        rb.linearVelocityX = bulletSpeed;

        //Destroy the bullet if it's on screen for more than two seconds.
        lifetime += Time.deltaTime;
        if (lifetime > lifespan) Destroy(this.gameObject);
    }

  
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            //Destroy the bullet
            Destroy(this.gameObject);
        }
    }
    private void OnBecameInvisible()
    {
        Destroy(this.gameObject);
    }

}
