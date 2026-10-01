using UnityEngine;

public class Enemy_Targeted_Bullet : MonoBehaviour
{
    public float bulletSpeed;
    public float bulletLifetime;

    private Vector3 direction;
    public Rigidbody2D rb;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb.linearVelocity = direction * bulletSpeed;
        rb = GetComponent<Rigidbody2D>();
       
    }
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        //Ignore player layer
        Physics2D.IgnoreLayerCollision(6, 6);

        //Bullet lifetime
        Destroy(this.gameObject, bulletLifetime);
    }

    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Destroy(this.gameObject);
        }
    }
}
