using UnityEngine;
using UnityEngine.SceneManagement;

public class ControlsUIManager : MonoBehaviour
{
    public void ToMenu()
    {
        SceneManager.LoadScene("TitleScreen");
    }
}
