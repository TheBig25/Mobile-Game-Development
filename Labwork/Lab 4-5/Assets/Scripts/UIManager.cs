using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    private PlayerMovement playerMovement;
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Player finding
        playerMovement = FindFirstObjectByType<PlayerMovement>();

    }

    // Update is called once per frame
    void Update()
    {
        //Score Text
        if (playerMovement != null)
        {
            
        }
    }
}
