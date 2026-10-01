using UnityEngine;

public class Multi_Cannon : MonoBehaviour
{
    public int enemySpeed;
    public float enemyFireCooldown;

    [SerializeField] private float randomInterval;

    [Header("Audio Clips")]
    [SerializeField] AudioClip enemyShootSFX;

    public Rigidbody2D rb;

    public GameObject[] targets;
    public GameObject enemyTargetedBullet;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        randomInterval = Random.Range(2, 4);
    }

    // Update is called once per frame
    void Update()
    {
        //Move from right to left
        rb.linearVelocityX = -enemySpeed;

        enemyFireCooldown += Time.deltaTime;

        //Shoot while player is alive
        if (enemyFireCooldown > randomInterval)
            if (GameObject.FindGameObjectWithTag("Player") != null)
            {
                SoundManager.instance.PlaySound(enemyShootSFX);

                enemyFireCooldown = 0;
                foreach (GameObject target in targets)
                {
                    GameObject bullet = Instantiate(enemyTargetedBullet, target.transform.position, Quaternion.identity);

                    Vector2 dir = (target.transform.position - transform.position).normalized;

                    bullet.GetComponent<Enemy_Targeted_Bullet>().SetDirection(dir);
                }
            }
    }

    private void OnBecameInvisible()
    {
        Destroy(this.gameObject);
    }
}
