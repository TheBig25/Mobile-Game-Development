using System.Collections;
using UnityEngine;

public class Enemy_Health : MonoBehaviour
{
    [Header("Health Properties")]
    [SerializeField] private float startingHealth;
    [SerializeField] private float currentHealth; 

    [Header("Enemy Status")]
    public bool enemyIsDead;

    [Header("Audio Clips")]
    [SerializeField] AudioClip enemyHitSFX;
    [SerializeField] AudioClip enemyExplodeSFX;

    private SpriteRenderer spriteRend;

    private PlayerManager playerManager;
    
    private Animator animator;

    public int enemyValue;
    [SerializeField] private int playerScore;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRend = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        currentHealth = startingHealth;

        //Player Objects
        GameObject player = GameObject.Find("Player");
        playerManager = player.GetComponent<PlayerManager>();
        
    }

    // Update is called once per frame
    void Update()
    {
        if (GameObject.Find("Player") != null)
        {
            playerScore = playerManager.playerScore;

        }
    }

    public void EnemyTakeDamage(float _damage)
    {
        currentHealth = Mathf.Clamp(currentHealth - _damage, 0, startingHealth);

        if (currentHealth > 0)
        {
            SoundManager.instance.PlaySound(enemyHitSFX);
            //Invulnerability
            StartCoroutine(EnemyInvulnerability());
        }
        else
        {
            // If the enemy is dead
            if (!enemyIsDead)
            {
                SoundManager.instance.PlaySound(enemyExplodeSFX);

                //Add to player score
                playerManager.playerScore += enemyValue;

                animator.SetTrigger("isDead");

                //Destroy the enemy
                Destroy(this.gameObject, 0.25f);
            }
        }
    }


    private IEnumerator EnemyInvulnerability()
    {
        Color originalColour = spriteRend.color;

        yield return new WaitForSecondsRealtime(0.1f);
        //loop and disable sprite rendering to flash 
        for (int i = 0; i < 3; i++)
        {
            //Flash on
            spriteRend.color = Color.red;
            yield return new WaitForSecondsRealtime(0.05f);

            //Flash off
            spriteRend.color = originalColour;
            yield return new WaitForSecondsRealtime(0.05f);
            
        }

        spriteRend.color = originalColour;
    }



    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player Bullet"))
        {
            float damage = collision.gameObject.GetComponent<Player_Standard_Bullet>().bulletDamage;
            EnemyTakeDamage(damage);
           
        }
    }
}
