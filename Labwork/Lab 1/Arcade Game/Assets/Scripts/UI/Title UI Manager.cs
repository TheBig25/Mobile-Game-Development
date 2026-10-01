using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class TitleUIManager : MonoBehaviour
{
    public GameObject menuScreen;
    public GameObject controlsScreen;

    [Header("Controller Inputs")]
    //Inputs from the input actions
    public InputAction backButton;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Controller Inputs
        backButton = InputSystem.actions.FindAction("Cancel");
    }

    // Update is called once per frame
    void Update()
    {
      
    }

    // Button Functions
    public void StartGame()
    {
        SceneManager.LoadScene("Level 1");
    }

    public void ViewControls()
    {
        SceneManager.LoadScene("ViewControls");
    }

    public void QuitGame()
    {
        Debug.Log("Game Quit!");
        Application.Quit();
    }

    
}
