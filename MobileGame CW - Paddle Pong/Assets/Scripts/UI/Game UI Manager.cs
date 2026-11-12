using TMPro;
using UnityEngine;

public class GameUIManager : MonoBehaviour
{
    [Header("Script References")]
    PlayerManager playerManager;


    [Header("UI Elements")]
    public TextMeshProUGUI scoreText;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Player finding
        GameObject player = GameObject.Find("Player");
        playerManager = player.GetComponent<PlayerManager>();
    }

    // Update is called once per frame
    void Update()
    {
        //Score Text
        if (playerManager != null)
        {
            scoreText.text = "Score: " + playerManager.playerScore.ToString();
        }
    }
}
