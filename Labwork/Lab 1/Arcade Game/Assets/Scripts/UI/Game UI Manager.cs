using JetBrains.Annotations;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEditor;

public class GameUIManager : MonoBehaviour
{
    [Header("Script References")]
    private PlayerManager playerManager;
    private LevelUp levelUp;

    [Header("Text Objects")]
    public TextMeshProUGUI boostText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI powerupText;
    public TextMeshProUGUI loseScoreText;
    public TextMeshProUGUI levelNoText;
    public TextMeshProUGUI levelIndicatorText;

    [Header("Controller Inputs")]
    //Inputs from the input actions
    public InputAction pauseButton;

    [Header("Game UI Objects")]
    public GameObject p1GameUI;
    public GameObject pauseMenu;
    public GameObject loseMenu;

    public GameObject firstPauseOption;
    public GameObject firstLoseOption;

    [Header("Game UI States")]
    public bool gamePaused;
    private bool loseMenuActive = false;


    //Coroutines Running
    private Coroutine boostFlashCoroutine;

    void Start()
    {
        //Player finding
        GameObject player = GameObject.Find("Player");
        playerManager = player.GetComponent<PlayerManager>();

        GameObject gameManager = GameObject.Find("Game Manager");
        levelUp = gameManager.GetComponent<LevelUp>();

        //Boost enabled
        boostText.enabled = true;

        //Controller Inputs
        pauseButton = InputSystem.actions.FindAction("Pause");

        //UI States
        gamePaused = false;    
    }

    void Update()
    {
        //Level Text
        if (playerManager.playerLevel == 1)
        {
            levelNoText.text = "Level " + playerManager.playerLevel.ToString() + ": Easy";
        }
        else if (playerManager.playerLevel == 2)
        {
            levelNoText.text = "Level " + playerManager.playerLevel.ToString() + ": Medium";
        }
        else if (playerManager.playerLevel == 3)
        {
            levelNoText.text = "Level " + playerManager.playerLevel.ToString() + ": Hard";
        }

        //Level Indicator Text
        if (playerManager.playerLevel == 1)
        {
            levelIndicatorText.text = "Next: " + levelUp.lv2Cond.ToString();
        }
        else if (playerManager.playerLevel == 2)
        {
            levelIndicatorText.text = "Next: " + levelUp.lv3Cond.ToString();
        }
        else if (playerManager.playerLevel >= 3)
        {
            levelIndicatorText.text = "Max Level Reached";
        }


        //Score Text
        if (playerManager != null)
        {
            scoreText.text = "Score: " + playerManager.playerScore.ToString();
        }

        //Game Over Score Text
        loseScoreText.text = "Final Score: " + playerManager.playerScore.ToString();

        //Powerup Text
        #region
        //Subtract
        if (playerManager.isSubtractMode)
        {
            powerupText.text = "Subtract";
            powerupText.color = Color.cyan;
        }
        else if (playerManager.isMultiplyMode)
        {
            powerupText.text = "Multiply";
            powerupText.color = Color.green;
        }
        else
        {
            powerupText.text = "Standard";
            powerupText.color= Color.white;
        }
        #endregion


        //Boost Text Animation
        #region
        if (playerManager.canBoost)
        {
            boostText.text = "BOOST READY!!";
            boostText.color = Color.cyan;
        }
        else
        {
            boostText.text = "RECHARGING";
            boostText.color = Color.blue;
        }
        #endregion


        //Pause Game
        if (pauseButton.triggered) 
        {
            if (playerManager != null)
            {
                if (!gamePaused)
                {
                    PauseGame();
                }

                else
                {
                    UnpauseGame();
                }
            }

            else
            {
                return;
            }
        }

        //Lose Screen
        if (!loseMenuActive && playerManager == null)
        {
            loseMenuActive = true;
            ActivateLoseMenu();
        }

    }

    public void ActivateLoseMenu()
    {
        Time.timeScale = 0f;
        loseMenu.SetActive(true);
        pauseMenu.SetActive(false);
        p1GameUI.SetActive(false);

        //Clear selected option
        EventSystem.current.SetSelectedGameObject(null);
        //Set new Selected option
        EventSystem.current.SetSelectedGameObject(firstLoseOption);
    }

    public void PauseGame()
    {
        //Pause game
        gamePaused = true;
        pauseMenu.SetActive(true);
        p1GameUI.SetActive(false);
        Time.timeScale = 0f;
        
        StartCoroutine(SetPauseSelection());
    }

    private IEnumerator SetPauseSelection()
    {
        // Wait a frame
        yield return null;
        //Clear selected option
        EventSystem.current.SetSelectedGameObject(null);
        //Set new Selected option
        EventSystem.current.SetSelectedGameObject(firstPauseOption);
    }

    public void UnpauseGame()
    {
        //Unpause game
        gamePaused = false;
        pauseMenu.SetActive(false);
        p1GameUI.SetActive(true);
        Time.timeScale = 1f;

        //Clear Selection
        EventSystem.current.SetSelectedGameObject(null);
    }

    //Pause Functions
    #region
    public void ContinueGame()
    {
        UnpauseGame();
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        Time.timeScale = 1f;
    }

    public void BacktoMenu()
    {
        SceneManager.LoadScene("TitleScreen");
        Time.timeScale = 1f;
    }
    #endregion
}
