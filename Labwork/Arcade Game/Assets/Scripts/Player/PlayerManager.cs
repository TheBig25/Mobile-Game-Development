using System.Collections;
using System.Diagnostics.Contracts;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms.Impl;

public class PlayerManager : MonoBehaviour
{
    [SerializeField] public GameObject player;
    Rigidbody2D rb;

    [Header("Player Attributes")]
    //Attributes
    public int moveSpd;
    public int playerLevel = 1;

    [Header("Game Statistics")]
    //Score and other things
    public int playerScore;

    [Header("Boost Attributes")]
    //Boost Attributes
    public bool canBoost = true;
    public int boostDuration;
    public int boostCooldown;

    [Header("Weapon Attributes")]
    //Weapon Attributes
    public GameObject StandardBullet;
    public GameObject firePoint;
    public float fireCooldown;
    public bool canShoot = true;

    [Header("Powerup States")]
    //Subtract
    public bool isSubtractMode = false;

    //Multiply
    public float bulletDamageMultiplier = 1f;
    public float bulletSpeedMultiplier = 1f;

    public bool isMultiplyMode = false;

    [Header("Script Objects")]
    public GameUIManager gameUIManager;


    [Header("Player States")]
    //States
    public bool isDead = false;

    [Header("Animation")]
    public Animator animator;


    [Header("Audio Clips")]
    [SerializeField] AudioClip playerShootSFX;
    [SerializeField] AudioClip playerBoostSFX;

    [Header("Controller Inputs")]
    //Inputs from the input actions
    public InputAction moveAction;
    public InputAction shootAction;
    public InputAction boostAction;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Player Components
        rb = GetComponentInChildren<Rigidbody2D>();
        animator = rb.GetComponent<Animator>();
        playerLevel = Mathf.Clamp(playerLevel, 1, 3);

        //Input Actions
        moveAction = InputSystem.actions.FindAction("Move");
        shootAction = InputSystem.actions.FindAction("Shoot");
        boostAction = InputSystem.actions.FindAction("Boost");  
    }

    // Update is called once per frame
    void Update()
    {
        //Initialise controls
        Vector2 moveInput = moveAction.ReadValue<Vector2>();

        if (moveInput != Vector2.zero)
        {
            animator.SetBool("isMoving", true);
        }
        else
        {
            animator.SetBool("isMoving", false);
        }

        //Apply movement
        rb.linearVelocity = moveInput * moveSpd;

        //Score Clamp
        playerScore = Mathf.Clamp(playerScore, 0, 99999999);


        //Restrict player to screen boundaries
        Vector3 cameraBounds = Camera.main.WorldToViewportPoint(transform.position);
        cameraBounds.x = Mathf.Clamp01(cameraBounds.x);
        cameraBounds.y = Mathf.Clamp01(cameraBounds.y);
        transform.position = Camera.main.ViewportToWorldPoint(cameraBounds);


        //Action Instructions
        if (!gameUIManager.gamePaused)
        {
            if (boostAction.triggered)
            {
                if (canBoost)
                {
                    SoundManager.instance.PlaySound(playerBoostSFX);
                    BoostPlayer();
                }
            }

            if (shootAction.triggered)
            {
                if (canShoot)
                {
                    SoundManager.instance.PlaySound(playerShootSFX);
                    ShootWeapon();
                }
            }
        }
        

        ////Debug features
        //#region
        //if (Input.GetKeyDown(KeyCode.P))
        //{
        //    //Increase Score
        //    playerScore++;
        //}

        //if (Input.GetKey(KeyCode.R))
        //{
        //    //Restart the current scene
        //    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        //}
        //#endregion
    }

    //Boosting Mechanics
    #region
    public void BoostPlayer()
    {
        StartCoroutine(BoostCooldown());
        StartCoroutine(BoostDuration());
    }
    IEnumerator BoostDuration()
    {
        canBoost = false;
        moveSpd *= 2;
        yield return new WaitForSeconds(boostDuration);
        moveSpd /= 2;
    }
    IEnumerator BoostCooldown()
    {
        yield return new WaitForSeconds(boostCooldown);
        canBoost = true;
    }
    #endregion

    //Shooting Mechanics
    #region
    public void ShootWeapon()
   {
       canShoot = false;
       Instantiate(StandardBullet, firePoint.transform.position, Quaternion.identity);
       StartCoroutine(ShootCooldown());
   }

   IEnumerator ShootCooldown()
   {
       yield return new WaitForSeconds(fireCooldown);
       canShoot = true;
   }
    #endregion

    //Weapon Powerups
    #region
    //Subtract
    public void SubtractStartup(float duration)
    {
        StartCoroutine(SubtractGameMode(duration));
    }

    IEnumerator SubtractGameMode(float duration)
    {
        //Prevent multiple pickups of item
        if (isSubtractMode)
        {
            yield break;
        }

        isSubtractMode = true;

        //Half fire cooldown
        float originalCooldown = fireCooldown;
        fireCooldown *= 0.5f;   

        yield return new WaitForSeconds(duration);

        //Return to normal
        fireCooldown = originalCooldown;

        isSubtractMode = false;
    }
   

    //Multiply 
    public void MultiplyBullets(float duration)
    {
        StartCoroutine(MultiplyBulletsCoroutine(duration));
    }

    IEnumerator MultiplyBulletsCoroutine(float duration)
    {

        //Prevent multiple pickups of item
        if (isMultiplyMode)
        {
            yield break;
        }
        

        isMultiplyMode = true;

        //Multiply 
        bulletDamageMultiplier = 2f;
        bulletSpeedMultiplier = 2f;

        yield return new WaitForSeconds(duration);

        //Revert to normal
        bulletDamageMultiplier = 1f;
        bulletSpeedMultiplier = 1f;

        isMultiplyMode = false;
    }
    #endregion


}
