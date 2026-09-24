using UnityEngine;

public class LevelPartManager : MonoBehaviour
{
    public int levelPartMoveSpeed = 3;
    public Rigidbody2D rb;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        rb.linearVelocityX = -levelPartMoveSpeed;

        if (transform.position.x < -20f)
        {
            Destroy(gameObject);
        }
    }

    void OnBecameInvisible()
    {
        Destroy(gameObject);
    }
}
