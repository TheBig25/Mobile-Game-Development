using UnityEngine;

public class AudioLevelUp : MonoBehaviour
{
    public PlayerManager playerManager;

    private AudioSource audioSource;

    public AudioClip level2Music;
    public AudioClip level3Music;

    private int lastLevel = -1;

    void Start()
    {
        audioSource = Camera.main.GetComponent<AudioSource>();
    }

    void Update()
    {
        //Get player level
        int currentLevel = playerManager.playerLevel;

        //Check if level 2
        if (currentLevel == 2 && lastLevel != 2)
        {
            audioSource.resource = level2Music;
            audioSource.Play();
        }

        //Check if level 3
        if (currentLevel == 3 && lastLevel != 3)
        {
            audioSource.resource = level3Music;
            audioSource.Play();
        }

        //Keep Current Level
        lastLevel = currentLevel;
    }
}
