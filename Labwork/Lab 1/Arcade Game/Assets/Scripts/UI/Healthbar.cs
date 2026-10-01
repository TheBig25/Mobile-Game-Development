using UnityEngine;
using UnityEngine.UI;
public class Healthbar : MonoBehaviour
{
    public Player_Health health;
    public Image totalHealthbar;
    public Image currentHealthbar;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Make healthbar full at the start
        totalHealthbar.fillAmount = health.currentHealth / 10;
    }

    // Update is called once per frame
    void Update()
    {
        //Update healthbar fill amount based on player's current health
        currentHealthbar.fillAmount = health.currentHealth / 10;      
    }
}