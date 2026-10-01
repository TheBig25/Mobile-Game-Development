using System.Collections;
using UnityEngine;

public class HealthSpawner : MonoBehaviour
{
    public GameObject[] HealthObjects;

    public float SpawnInterval;

    public GameObject player;
    public Player_Health player_Health;

    private Coroutine spawnHealthCoroutine;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Find player parts
        player = GameObject.Find("Player");
        player_Health = player.GetComponent<Player_Health>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    private IEnumerator spawnHealth(float interval)
    {
        
        while (true)
        {

            if (player_Health.currentHealth < 3 && player_Health != null)
            {
                GameObject health = HealthObjects[Random.Range(0, HealthObjects.Length)];

                //Create health object in different locations
                Instantiate(health, new Vector3(8.75f, Random.Range(-2.5f, 2.5f), 0), Quaternion.identity);

                
            }
            yield return new WaitForSeconds(interval);
        }         
    }

    void OnEnable()
    {
        spawnHealthCoroutine = StartCoroutine(spawnHealth(SpawnInterval));
    }

    void OnDisable()
    {
        if (spawnHealthCoroutine != null)
        {
            StopCoroutine(spawnHealthCoroutine);
        }
        
    }
}
