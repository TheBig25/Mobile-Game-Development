using UnityEngine;

public class CollectibleManager : MonoBehaviour
{
    private PlayerMovement playerMovement;

    public Rigidbody2D rb;
    public GameObject player;
    public GameObject collectible;

    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        GameObject player = GameObject.Find("Player");
        playerMovement = player.GetComponent<PlayerMovement>();
    }

    // Update is called once per frame
    void Update()
    {
        playerMovement = FindFirstObjectByType<PlayerMovement>();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerMovement playerMove = collision.gameObject.GetComponent<PlayerMovement>();

            // Add points to the player's score
            playerMove.pScore++;
            // Create a new collectible at a random position within the camera's view
            Instantiate(collectible, new Vector3(Random.Range(-8f, 8f), Random.Range(-4f, 4f), 0), Quaternion.identity);
            //Destroy the collectible that was collected
            Destroy(gameObject);
        }
    }
}
