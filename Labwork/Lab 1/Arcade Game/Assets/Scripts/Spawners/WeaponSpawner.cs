using System.Collections;
using UnityEngine;

public class WeaponSpawner : MonoBehaviour
{
    public GameObject[] WeaponObjects;

    public float SpawnInterval;

    private Coroutine spawnWeaponCoroutine;


    private IEnumerator spawnWeapon(float interval)
    {
        while (true)
        {
            //Wait before spawning next weapon object
            interval = SpawnInterval;
            yield return new WaitForSeconds(interval);

            GameObject weapon = WeaponObjects[Random.Range(0, WeaponObjects.Length)];

            //Create weapon in different locations
            Instantiate(weapon, new Vector3(8.75f, Random.Range(-2.5f, 2.5f), 0), Quaternion.identity);
        }
    }


    //Spawn weapons when enabled
    void OnEnable()
    {
        spawnWeaponCoroutine = StartCoroutine(spawnWeapon(SpawnInterval));
    }

    //Stop spawning weapons when disabled
    void OnDisable()
    {
        if (spawnWeaponCoroutine != null)
        {
            StopCoroutine(spawnWeaponCoroutine);
        }
    }
}
