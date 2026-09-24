using System.Collections;
using UnityEngine;

public class Enemy_Sine_Shooter : MonoBehaviour
{
    [Header("Enemy Properties")]
    public int enemySpeed;
    public float enemyFireCooldown;

    [Header("Sinewave Properties")]
    public float amplitude = 0.5f;
    public float frequency = 2f;

    float sineCentreY;

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

        sineCentreY = transform.position.y;
    }

    // Update is called once per frame
    void Update()
    {
        //Move from right to left
        rb.linearVelocityX = -enemySpeed;

        //Sinewave movement
        float sinMovement = amplitude * Mathf.Sin(Time.time * frequency);
        transform.position = new Vector3(transform.position.x, sineCentreY + sinMovement, transform.position.z);

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
