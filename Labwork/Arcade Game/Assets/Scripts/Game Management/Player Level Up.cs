using UnityEngine;

public class LevelUp : MonoBehaviour
{
    [Header("Level Up Conditions")]
    public int lv2Cond;
    public int lv3Cond;

    private bool reachedLv2 = false;
    private bool reachedLv3 = false;

    private PlayerManager playerManager;
    private EnemySpawner enemySpawner;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        enemySpawner = FindFirstObjectByType<EnemySpawner>();
    }

    // Update is called once per frame
    void Update()
    {
        if (playerManager == null)
        {
            //Wait for player
            playerManager = FindFirstObjectByType<PlayerManager>();
            return;
        }

        CheckScoreMilestones();
    }

    
    // Check to see if player has reached conditons
    void CheckScoreMilestones()
    {
        //Level 2 Check
        if (playerManager.playerScore >= lv2Cond && !reachedLv2)
        {
            playerManager.playerLevel += 1;
            enemySpawner.SpawnInterval -= 0.55f;
            reachedLv2 = true;
        }

        //Level 3 Check
        if (playerManager.playerScore >= lv3Cond && !reachedLv3)
        {
            playerManager.playerLevel += 1;
            enemySpawner.SpawnInterval -= 0.55f;
            reachedLv3 = true;
        }
    }
}
