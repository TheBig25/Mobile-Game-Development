using System.Collections;
using UnityEngine;

public class LevelPartSpawners : MonoBehaviour
{
    [Header("Level Part Spawner Properties")]
    public GameObject[] LevelParts;
    public int allowedPartCount;

    public float spawnInterval;
    public float spawnDelay;
    public float spawnEndDelay;

    public bool isSpawningLevelParts = false;

    [Header("Spawner GameObjects")]
    public GameObject healthSpawner;
    public GameObject enemySpawner;
    public GameObject weaponSpawner;

    [Header("Other")]
    private PlayerManager playerManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        TryToFindPlayer();
        StartCoroutine(spawnLevelParts(spawnInterval));
    }

    // Update is called once per frame
    void Update()
    {
        if (playerManager == null)
        {
            TryToFindPlayer();
        }
    }

    // Prevent unity from crashing if player isnt there
    void TryToFindPlayer()
    {
        GameObject player = GameObject.FindWithTag("Player");

        if (player != null)
        {
            playerManager = player.GetComponent<PlayerManager>();
        }       
    }


    void UpdateMaxParts()
    {
        //Don't run if player dead
        if (playerManager == null)
        {
            return;
        }
            

        //Update max level parts spawned based on player level
        switch (playerManager.playerLevel)
        {
            case 1:
                allowedPartCount = 3;
                break;
            case 2:
                allowedPartCount = 6;
                break;
            case 3:
                allowedPartCount = LevelParts.Length;
                break;
        }

        //Limit to available parts
        allowedPartCount = Mathf.Min(allowedPartCount, LevelParts.Length);
    }

    void ToggleOtherSpawners(bool toggle)
    {
        healthSpawner.SetActive(toggle);
        enemySpawner.SetActive(toggle);
        weaponSpawner.SetActive(toggle);
    }

    private IEnumerator spawnLevelParts(float interval)
    {
        while (true)
        {
            UpdateMaxParts();

            interval = spawnInterval;

            //Select random level part within limit
            GameObject levelPart = LevelParts[Random.Range(0, allowedPartCount)];

            yield return new WaitForSeconds(interval);

            //Start spawning level parts
            isSpawningLevelParts = true;

            //Disable other spawners
            ToggleOtherSpawners(false);

            yield return new WaitForSeconds(spawnDelay);

            //Create part
            Instantiate(levelPart, new Vector3(13f, 3.45f, 0), Quaternion.identity);

            //Stop spawning level parts
            isSpawningLevelParts = false;

            yield return new WaitForSeconds(spawnEndDelay);

            //Re-enable other spawners
            ToggleOtherSpawners(true);
        }
    }
}
