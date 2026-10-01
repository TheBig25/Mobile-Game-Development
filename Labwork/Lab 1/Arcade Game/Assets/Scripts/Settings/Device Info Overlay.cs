using UnityEngine;

public class DeviceInfoOverlay : MonoBehaviour
{
    float smoothedDelta = 1f / 30f;
    GUIStyle style;

    void Awake()
    {
        Application.targetFrameRate = 60;
    }

    void Update()
    {
        smoothedDelta += (Time.unscaledDeltaTime - smoothedDelta) * 0.1f;
    }

    void OnGUI()
    {
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.box);
            style.alignment = TextAnchor.UpperLeft;
            style.normal.textColor = Color.white;
        }

        style.fontSize = Mathf.Max(14, Screen.height / 40);

        float fps = 1f / smoothedDelta;
        Resolution res = Screen.currentResolution;

        string info =
            $"FPS: {fps:0}\n" +
            $"Device: {SystemInfo.deviceModel}\n" +
            $"OS: {SystemInfo.operatingSystem}\n" +
            $"Screen: {Screen.width} x {Screen.height} @ {res.refreshRateRatio.value:0} Hz\n" +
            $"DPI: {Screen.dpi:0}\n" +
            $"GPU: {SystemInfo.graphicsDeviceName} ({SystemInfo.graphicsMemorySize} MB)\n" +
            $"RAM: {SystemInfo.systemMemorySize} MB";

        Vector2 size = style.CalcSize(new GUIContent(info));
        GUI.Label(new Rect(40, 40, size.x + 20, size.y + 20), info, style);
    }
}