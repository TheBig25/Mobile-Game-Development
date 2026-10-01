using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject[] EnemyObjects;
    public float SpawnInterval;
    private Coroutine spawnEnemyCoroutine;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
      
    }

    private IEnumerator spawnEnemy(float interval)
    {
        while (true)
        {
            interval = SpawnInterval;

            yield return new WaitForSeconds(interval);

            GameObject enemy = EnemyObjects[Random.Range(0, EnemyObjects.Length)];
            Vector3 spawnPos = Vector3.zero;

            //Create enemies in different locations
            #region
            switch (enemy.name)
            {
                case "Shooter Enemy":
                    spawnPos = new Vector2(8.75f, Random.Range(-2.5f, 2.5f));
                    break;
                case "Sine Shooter Enemy":
                    spawnPos = new Vector2(7f, Random.Range(-6.75f, -0.25f));
                    break;
                case "Multi Cannon":
                    spawnPos = new Vector2(8.75f, Random.Range(-2.5f, 2.5f));
                    break;
                case "Exploder":
                    spawnPos = new Vector2(8.75f, Random.Range(-2.5f, 2.5f));
                    break;
                case "Dasher":
                    spawnPos = new Vector2(8.75f, Random.Range(-2.5f, 2.5f));
                    break;
            }
            #endregion

            Instantiate(enemy, spawnPos, Quaternion.identity);
        }      
    }

    //Spawn enemies when enabled
    void OnEnable()
    {
        spawnEnemyCoroutine = StartCoroutine(spawnEnemy(SpawnInterval));
    }

    //Stop spawning enemies when disabled
    void OnDisable()
    {
        if (spawnEnemyCoroutine != null)
        {
            StopCoroutine(spawnEnemyCoroutine);
        }         
    }
}
