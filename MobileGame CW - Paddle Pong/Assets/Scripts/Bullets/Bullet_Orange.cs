using Unity.VisualScripting;
using UnityEngine;


public class Bullet_Orange : MonoBehaviour
{
    public float bulletSpeed = 10;
    private Rigidbody2D rb;
    private int direction = -1;

    //Bounce Curve
    [SerializeField] public float bounceStartPoint;
    [SerializeField] public float bounceEndPoint;
    [SerializeField] public float bounceTargetDistance;
    [SerializeField] public float bounceSpeed;

    //Bounce limits
    [SerializeField] public int bounceNumber;
    [SerializeField] public int bounceMax;


    //Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        //Furthest distance to travel
        bounceTargetDistance = Random.Range(-5f, 5f);

        rb.linearVelocityX = direction * bulletSpeed;
    }

    private void Awake()
    {
        //Number of allowed bounces
        bounceNumber = Random.Range(1, 4);

        bounceEndPoint = Random.Range(-3.25f, 3.25f);
    }

    //Update is called once per frame
    void Update()
    {
        
    }

    //Change direction when colliding with player
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            
            //Reflect once 
           // direction = 1;         
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