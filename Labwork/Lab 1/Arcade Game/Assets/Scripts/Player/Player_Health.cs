using System.Collections;
using UnityEngine;

public class Player_Health : MonoBehaviour
{
    [Header("Health Properties")]
    [SerializeField] private float startingHealth;
    [SerializeField] public float currentHealth;

    [Header("Player Status")]
    public bool playerIsDead;

    [Header("Audio Clips")]
    [SerializeField] AudioClip playerHitSFX;
    [SerializeField] AudioClip playerExplodeSFX;

    private SpriteRenderer spriteRend;
    private Animator animator;
    private PlayerManager playerManager;
    private void Awake()
    {
        spriteRend = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        currentHealth = startingHealth;
    }

    // Update is called once per frame
    void Update()
    {
        //Limit health
        currentHealth = Mathf.Clamp(currentHealth, 0, startingHealth);

        //if (Input.GetKeyDown(KeyCode.O))
        //{
        //    PlayerTakeDamage(1);
        //}
    }

    public void PlayerTakeDamage(float _damage)
    {
        currentHealth = Mathf.Clamp(currentHealth - _damage, 0, startingHealth);

        if (currentHealth > 0)
        {
            SoundManager.instance.PlaySound(playerHitSFX);
            //Invulnerability
            StartCoroutine(PlayerInvulnerability());
        }
        else
        {
            // If the player is dead
            if (!playerIsDead)
            {
                GetComponent<PlayerManager>().moveSpd = 0;

                //Play animation
                SoundManager.instance.PlaySound(playerExplodeSFX);
                animator.SetTrigger("isDead");

                //Destroy the player
                Destroy(this.gameObject, 1f);
            }
        }
    }


    private IEnumerator PlayerInvulnerability()
    {
        Color originalColour = spriteRend.color;

        yield return new WaitForSecondsRealtime(0.05f);

        GetComponent<BoxCollider2D>().enabled = false;

        //loop and disable sprite rendering to flash 
        for (int i = 0; i < 3; i++)
        {
            //Flash on
            spriteRend.color = Color.clear;
            yield return new WaitForSecondsRealtime(0.1f);

            //Flash off
            spriteRend.color = originalColour;
            yield return new WaitForSecondsRealtime(0.1f);
            
        }

        GetComponent<BoxCollider2D>().enabled = true;
        spriteRend.color = originalColour;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {          
            PlayerTakeDamage(1);
        }

        if (collision.gameObject.CompareTag("Enemy Bullet"))
        {
            PlayerTakeDamage(0.5f);
           
        }
        if (collision.gameObject.CompareTag("Terrain"))
        {
            PlayerTakeDamage(2);
        }
    }
}
