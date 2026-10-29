// CMP5402 Mobile Development - Week 5: Optimisation Techniques
// BeforeAfterLog: one-button evidence for the "Before and after" table ("Prove it" slide).
//
// Press the on-screen button (or Space in the Editor) to record a 30-second sample: average frame interval,
// worst frame, and system memory. Each sample is appended to a CSV with a label you type in the Inspector
// ("baseline", "pooled bullets", ...). The path is shown on screen so you can copy the file off the phone.
//
// Works in Development and Release builds. Uses ProfilerRecorder for memory (see Week 4's PerfOverlay).
// Frame interval here is the wall-clock time between frames, which is what the player feels.

using System.Globalization;
using System.IO;
using Unity.Profiling;
using UnityEngine;

namespace CMP5402.Week5
{
    public class BeforeAfterLog : MonoBehaviour
    {
        [Tooltip("What changed since the last sample. Edit before each run.")]
        public string label = "baseline";

        [SerializeField] float sampleSeconds = 30f;
        [SerializeField] float warmUpSeconds = 5f;

        ProfilerRecorder memory;
        bool sampling;
        float sampleEnd, warmEnd;
        int frames;
        double total;
        float worst;
        string status = "Tap RECORD to take a 30 s sample";
        string path;

        void OnEnable()
        {
            memory = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory");
            path = Path.Combine(Application.persistentDataPath, "before_after.csv");
        }

        void OnDisable() => memory.Dispose();

        public void StartSample()
        {
            sampling = true;
            warmEnd = Time.unscaledTime + warmUpSeconds;
            sampleEnd = warmEnd + sampleSeconds;
            frames = 0; total = 0; worst = 0f;
            status = "Warming up...";
        }

        void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Space) && !sampling) StartSample();
#endif
            if (!sampling) return;
            float now = Time.unscaledTime;
            if (now < warmEnd) return;

            float dt = Time.unscaledDeltaTime;
            frames++; total += dt; if (dt > worst) worst = dt;
            status = "Recording " + label + "... " + (sampleEnd - now).ToString("0") + " s";

            if (now >= sampleEnd) Finish();
        }

        void Finish()
        {
            sampling = false;
            float avgMs = frames > 0 ? (float)(total / frames) * 1000f : 0f;
            float worstMs = worst * 1000f;
            string mem = memory.Valid && memory.Count > 0 ? (memory.LastValue / (1024 * 1024)).ToString() : "N/A";

            bool newFile = !File.Exists(path);
            using (var w = new StreamWriter(path, true))
            {
                if (newFile) w.WriteLine("label,device,avg_ms,worst_ms,memory_mb,frames");
                w.WriteLine(string.Join(",", Csv(label), Csv(SystemInfo.deviceModel),
                    avgMs.ToString("F2", CultureInfo.InvariantCulture), worstMs.ToString("F1", CultureInfo.InvariantCulture), mem, frames.ToString()));
            }
            status = label + ": avg " + avgMs.ToString("F1") + " ms, worst " + worstMs.ToString("F0") + " ms, memory " + mem + " MB\nSaved to " + path;
        }

        static string Csv(string s) => "\"" + s.Replace("\"", "\"\"") + "\"";

        void OnGUI()
        {
            int fs = Mathf.Max(14, Screen.height / 45);
            GUI.skin.button.fontSize = fs;
            GUI.skin.label.fontSize = fs;
            Rect safe = Screen.safeArea;
            float x = safe.xMin + 20f, y = Screen.height - safe.yMax + 20f;
            if (!sampling && GUI.Button(new Rect(x, y, fs * 9f, fs * 2.5f), "RECORD")) StartSample();
            GUI.Label(new Rect(x, y + fs * 3f, Screen.width - 40f, fs * 5f), status);
        }
    }
}
