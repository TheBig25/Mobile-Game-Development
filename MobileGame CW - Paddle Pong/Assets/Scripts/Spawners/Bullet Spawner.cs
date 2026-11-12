using UnityEngine;

public class BulletSpawner : MonoBehaviour
{
    [SerializeField] GameObject bulletPrefab;
    [SerializeField] float spawnInterval = 3f;

    PlayerManager player;
    float spawnTimer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = FindFirstObjectByType<PlayerManager>();
        spawnTimer = spawnInterval;
    }

    // Update is called once per frame
    void Update()
    {
        //Spawn Bullets every few seconds
        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0f)
        {
            SpawnBullet();
            spawnTimer = spawnInterval;
        }
    }

    void SpawnBullet()
    {
        //Check if bullet and player are not null before spawning
        if (bulletPrefab == null || player == null)
        {
            return;
        }

        //Spawn bullet between player bounds
        float spawnY = Random.Range(player.BottomBound, player.TopBound);

        Vector3 spawnPosition = new Vector3(transform.position.x, spawnY,transform.position.z);
        Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);
    }
}