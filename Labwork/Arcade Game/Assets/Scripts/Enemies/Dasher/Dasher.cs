using System.Collections;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Dasher : MonoBehaviour
{

    [Header("Dash Move Speed")]
    public int dasherMoveSpd;
    public int dashSpeedMultiplier;

    [Header("Dash Timers")]
    public float dashWaitTime;
    public int dashMoveAlarm;

    [Header("Dashing Time Limit")]
    public float dashingTime;
    public int dashingTimeStopLimit;

    public float dashCap;

    public GameObject player;

    private Vector2 target;

    public Rigidbody2D rb2d;

    public PlayerManager playerManager;

    public bool canDash = false;

    private Animator animator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GameObject.Find("Player");
        rb2d = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        //Player finding
        playerManager = player.GetComponent<PlayerManager>();
    }

    // Update is called once per frame
    void Update()
    {
          

        //Ignore player layer
        Physics2D.IgnoreLayerCollision(6, 6);

        //Move from right to left
        rb2d.linearVelocityX = -dasherMoveSpd;
        dashWaitTime += Time.deltaTime;

        
        if (dashWaitTime > dashMoveAlarm)
        {
            if (player != null)
            {
                target = new Vector2(player.transform.position.x, player.transform.position.y);

                //Cap Dash 
                canDash = true;
                dashWaitTime = dashCap;
                StartCoroutine(DashToPlayer());
            }
            else
            {
                rb2d.linearVelocityX = -dasherMoveSpd;
            }
        }
    }

    public IEnumerator DashToPlayer()
    {
        if (canDash == true)
        {
            animator.SetBool("isChasing", true);

            //Increase dashing time
            dashingTime += Time.deltaTime;
            transform.position = Vector2.MoveTowards(transform.position, target, dasherMoveSpd * dashSpeedMultiplier * Time.deltaTime);

            //Stop dashing after time limit
            if (dashingTime > dashingTimeStopLimit && canDash == true)
            {
                animator.SetBool("isChasing", false);
                canDash = false; 
                yield return new WaitForSeconds(dashCap);
                animator.SetTrigger("isDead");  
                Destroy(this.gameObject, 0.25f);
            }
        }
    }

   

    private void OnBecameInvisible()
    {
        Destroy(this.gameObject);
    }
}
