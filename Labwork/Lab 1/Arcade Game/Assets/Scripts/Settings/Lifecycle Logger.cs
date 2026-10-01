using UnityEngine;

public class LifecycleLogger : MonoBehaviour
{
    void OnApplicationFocus(bool hasFocus)
    {
        Debug.Log($"[Lifecycle] Focus = {hasFocus} at {Time.realtimeSinceStartup:0.0}s");
    }

    void OnApplicationPause(bool isPaused)
    {
        Debug.Log($"[Lifecycle] Paused = {isPaused} at {Time.realtimeSinceStartup:0.0}s");
    }

    void OnApplicationQuit()
    {
        Debug.Log("[Lifecycle] Quit");
    }
}