using UnityEngine;

public class Enemy_Exploder : MonoBehaviour
{

    public float enemySpeed;

    public GameObject player;
    private Vector2 target;

    public float enemyLifeTime;

    public Rigidbody2D rb2d;
    private Animator animator;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player");     
        rb2d = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        //Move enemy to player
        if (player != null)
        {
            target = new Vector2(player.transform.position.x, player.transform.position.y);
            transform.position = Vector2.MoveTowards(transform.position, target, enemySpeed * Time.deltaTime);
        }
        else
        {
            rb2d.linearVelocityX = -enemySpeed;
        }

      
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            animator.SetTrigger("isDead");
            Destroy(this.gameObject, 0.25f);
        }
    }

    private void OnBecameInvisible()
    {
        Destroy(this.gameObject);
    }
}
