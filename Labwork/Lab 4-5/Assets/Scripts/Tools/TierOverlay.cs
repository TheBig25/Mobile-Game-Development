// CMP5402 Mobile Development - Week 6: Graphics & Device Scalability
// TierOverlay: shows which tier is active, why, the current render scale and the device signals
// ("The life of a quality setting" slide: every automatic change is visible to testers).
// Add to any GameObject. Pair it with PerfOverlay (Week 4) for the frame numbers, or use both.
// Three-finger tap (or F2 in the Editor) hides and shows it. Remove before release.

using System.Text;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CMP5402.Week6
{
    public class TierOverlay : MonoBehaviour
    {
        [SerializeField] bool show = true;
        [SerializeField] RenderScaleController renderScale;   // optional
        [SerializeField] ThermalWatcher thermal;               // optional

        GUIStyle style;
        string text = "";
        float nextRefresh;
        string lastChange = "";
        bool wasMultiTouch;
        readonly StringBuilder sb = new StringBuilder();

        void OnEnable() => QualityTiers.Changed += OnChanged;
        void OnDisable() => QualityTiers.Changed -= OnChanged;

        void OnChanged(int tier, string reason) => lastChange = QualityTiers.Name(tier) + " (" + reason + ") at " + Time.unscaledTime.ToString("0") + " s";

        void Update()
        {
            if (TogglePressed()) show = !show;
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.5f;

            sb.Clear();
            sb.Append("Tier: ").Append(QualityTiers.Current < 0 ? "not applied" : QualityTiers.Name(QualityTiers.Current))
              .Append("   Quality level: ").Append(QualitySettings.names[QualitySettings.GetQualityLevel()])
              .Append("   Target FPS: ").Append(Application.targetFrameRate);
            sb.Append("\nLast change: ").Append(lastChange.Length > 0 ? lastChange : QualityBootstrap.LastExplanation);
            if (renderScale != null) sb.Append("\nRender scale: ").Append(renderScale.CurrentScale.ToString("0.00")).Append("   ").Append(renderScale.LastReason);
            if (thermal != null) sb.Append("\nThermal: ").Append(thermal.Status);
            sb.Append("\nDevice: ").Append(SystemInfo.deviceModel)
              .Append("  RAM ").Append(SystemInfo.systemMemorySize).Append(" MB")
              .Append("  GPU mem ").Append(SystemInfo.graphicsMemorySize).Append(" MB")
              .Append("  cores ").Append(SystemInfo.processorCount)
              .Append("  ").Append(SystemInfo.graphicsDeviceType)
              .Append(SystemInfo.supportsComputeShaders ? "  compute" : "  no compute");
            text = sb.ToString();
        }

        bool TogglePressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame) return true;
            int touches = 0;
            if (Touchscreen.current != null)
                foreach (var t in Touchscreen.current.touches)
                    if (t.isInProgress) touches++;
            bool multi = touches >= 3;
            bool pressed = multi && !wasMultiTouch;
            wasMultiTouch = multi;
            return pressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.F2)) return true;
            bool multi = Input.touchCount >= 3;
            bool pressed = multi && !wasMultiTouch;
            wasMultiTouch = multi;
            return pressed;
#else
            return false;
#endif
        }

        void OnGUI()
        {
            if (!show) return;
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, richText = false, wordWrap = true };
                style.normal.textColor = Color.white;
            }
            style.fontSize = Mathf.Max(13, Mathf.Min(Screen.width, Screen.height) / 42);
            Rect safe = Screen.safeArea;
            float x = safe.xMin + 20f, y = Screen.height - safe.yMin - 20f;   // bottom-left of the safe area
            var content = new GUIContent(text);
            float w = Mathf.Min(Screen.width - 40f, safe.width - 40f);
            float h = style.CalcHeight(content, w) + 10f;
            GUI.Box(new Rect(x, y - h, w, h), content, style);
        }
    }
}
