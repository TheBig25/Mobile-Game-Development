using UnityEngine;

public class Enemy_SH_Bullet : MonoBehaviour
{
    public float bulletSpeed;
    public float bulletLifetime;

    private GameObject player;
    private Vector2 target;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Find player object
        player = GameObject.FindGameObjectWithTag("Player");
        target = new Vector2(player.transform.position.x, player.transform.position.y);
    }

    // Update is called once per frame
    void Update()
    {
        //Ignore player layer
        Physics2D.IgnoreLayerCollision(6, 6);

        //Move bullet to target
        transform.position = Vector2.MoveTowards(transform.position, target, bulletSpeed * Time.deltaTime);

        //If bullet reaches player position, destroy bullet
        if (transform.position.x == target.x && transform.position.y == target.y)
        {
            Destroy(this.gameObject);
        }

        //Bullet lifetime
        Destroy(this.gameObject, bulletLifetime);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Destroy(this.gameObject);
        }
    }
}
