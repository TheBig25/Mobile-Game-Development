using System.Collections;
using UnityEngine;

public class Enemy_Shooter : MonoBehaviour
{
    public int enemySpeed;
    public float enemyFireCooldown;

    [SerializeField] private float randomInterval;

    [Header("Audio Clips")]
    [SerializeField] AudioClip enemyShootSFX;

    public GameObject semiHomingBullet;
    public Rigidbody2D rb;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponentInChildren<Rigidbody2D>();

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
                Instantiate(semiHomingBullet, transform.position, Quaternion.identity);
            }
    }

    private void OnBecameInvisible()
    {
        Destroy(this.gameObject);
    }
}
