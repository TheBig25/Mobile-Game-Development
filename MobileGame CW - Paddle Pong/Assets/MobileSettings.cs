using UnityEngine;

public class MobileSettings : MonoBehaviour
{
    public static int targetFrameRate = 60;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Application.targetFrameRate = targetFrameRate;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
