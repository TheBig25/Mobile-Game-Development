using System;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.Profiling;
using UnityEngine;

namespace CMP5402.Week4
{
    // Frame interval measures pacing, including waits. It is NOT CPU busy time or GPU time.
    public class Lab4PerformanceMonitor : MonoBehaviour
    {
        [SerializeField] bool showOverlay = true;
        [SerializeField, Range(1000, 100000)] int maximumSamples = 80000;
        struct Sample
        {
            public double elapsed;
            public float intervalMs;
            public long mainThreadNs, systemBytes, gcBytes;
        }
        Sample[] samples;
        int sampleCount;
        bool warming;
        double warmupEnds, captureStarted;
        float duration;
        string scenario, metadata, display = "Waiting for samples";
        double nextDisplay;
        ProfilerRecorder mainThread, systemMemory, gcAlloc;
        public bool IsRecording { get; private set; }
        public bool IsBusy => warming || IsRecording;
        public string LastCsvPath { get; private set; } = "";
        public string LastMessage { get; private set; } = "No capture yet";

        void OnEnable()
        {
            samples = new Sample[Mathf.Clamp(maximumSamples, 1000, 100000)];
            mainThread = StartRecorder(ProfilerCategory.Internal, "Main Thread");
            systemMemory = StartRecorder(ProfilerCategory.Memory, "System Used Memory");
            gcAlloc = StartRecorder(ProfilerCategory.Memory, "GC Allocated In Frame");
        }

        static ProfilerRecorder StartRecorder(ProfilerCategory category, string name)
        {
            try { return ProfilerRecorder.StartNew(category, name, 1); }
            catch (Exception ex)
            {
                Debug.LogWarning("Lab4 counter unavailable: " + name + ". " + ex.Message);
                return default;
            }
        }

        static long Read(ProfilerRecorder recorder)
        {
            return recorder.Valid && recorder.Count > 0 ? recorder.LastValue : -1;
        }

        public void BeginCapture(string label, float seconds = 30f)
        {
            if (IsBusy) return;
            scenario = label ?? "Unlabelled";
            duration = Mathf.Clamp(seconds, 5f, 600f);
            sampleCount = 0;
            metadata = "unity=" + Application.unityVersion + "; device=" + SystemInfo.deviceModel
                + "; os=" + SystemInfo.operatingSystem + "; graphics=" + SystemInfo.graphicsDeviceType
                + "; target_fps=" + Application.targetFrameRate
                + "; development=" + Debug.isDebugBuild + "; scenario=" + scenario;
            warmupEnds = Time.realtimeSinceStartupAsDouble + 2.0;
            warming = true;
            LastMessage = "Capture armed. Controls hide during warm-up and measurement.";
        }

        void Update()
        {
            double now = Time.realtimeSinceStartupAsDouble;
            if (warming)
            {
                if (now < warmupEnds) return;
                warming = false;
                IsRecording = true;
                captureStarted = now;
                return; // Discard the warm-up boundary frame.
            }
            if (IsRecording)
            {
                if (sampleCount < samples.Length)
                {
                    samples[sampleCount++] = new Sample
                    {
                        elapsed = now - captureStarted,
                        intervalMs = Time.unscaledDeltaTime * 1000f,
                        mainThreadNs = Read(mainThread),
                        systemBytes = Read(systemMemory),
                        gcBytes = Read(gcAlloc)
                    };
                }
                if (now - captureStarted >= duration || sampleCount >= samples.Length)
                    FinishCapture(sampleCount >= samples.Length ? "sample capacity reached" : "complete");
                return;
            }
            if (now < nextDisplay) return;
            nextDisplay = now + 0.5;
            long thread = Read(mainThread), memory = Read(systemMemory), gc = Read(gcAlloc);
            float dt = Time.unscaledDeltaTime;
            display = "Frame interval: " + (dt * 1000f).ToString("F1") + " ms / "
                + (dt > 0f ? (1f / dt).ToString("F1") : "N/A") + " FPS\n"
                + "Main Thread marker (includes waits): " + (thread < 0 ? "N/A" : (thread * 1e-6).ToString("F2") + " ms")
                + "\nSystem Used Memory: " + (memory < 0 ? "N/A" : (memory / 1048576.0).ToString("F1") + " MiB")
                + "\nGC allocated previous frame: " + (gc < 0 ? "N/A" : gc.ToString() + " bytes");
        }

        static string Csv(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";

        public void FinishCapture(string reason = "manual stop")
        {
            if (!IsBusy) return;
            warming = false;
            IsRecording = false; // Export and sorting happen AFTER measurement ends.
            if (sampleCount == 0) { LastMessage = "No samples captured: " + reason; return; }
            try
            {
                var intervals = new float[sampleCount];
                double total = 0;
                var sb = new StringBuilder(sampleCount * 70);
                sb.AppendLine("metadata," + Csv(metadata));
                sb.AppendLine("result," + Csv(reason));
                sb.AppendLine("elapsed_s,frame_interval_ms,main_thread_marker_ms,system_used_bytes,gc_allocated_bytes");
                var culture = CultureInfo.InvariantCulture;
                for (int i = 0; i < sampleCount; i++)
                {
                    Sample s = samples[i];
                    intervals[i] = s.intervalMs;
                    total += s.intervalMs;
                    sb.Append(s.elapsed.ToString("F6", culture)).Append(',');
                    sb.Append(s.intervalMs.ToString("F4", culture)).Append(',');
                    if (s.mainThreadNs >= 0) sb.Append((s.mainThreadNs * 1e-6).ToString("F4", culture));
                    sb.Append(',');
                    if (s.systemBytes >= 0) sb.Append(s.systemBytes);
                    sb.Append(',');
                    if (s.gcBytes >= 0) sb.Append(s.gcBytes);
                    sb.AppendLine();
                }
                Array.Sort(intervals);
                double mean = total / sampleCount;
                int p95Index = Mathf.Clamp((int)Math.Ceiling(sampleCount * 0.95) - 1, 0, sampleCount - 1);
                string summary = "frames=" + sampleCount + "; mean_ms=" + mean.ToString("F3", culture)
                    + "; p95_ms=" + intervals[p95Index].ToString("F3", culture)
                    + "; worst_ms=" + intervals[sampleCount - 1].ToString("F3", culture)
                    + "; average_fps=" + (mean > 0 ? 1000.0 / mean : 0).ToString("F3", culture);
                sb.AppendLine("summary," + Csv(summary));
                string folder = Path.Combine(Application.persistentDataPath, "Lab4Captures");
                Directory.CreateDirectory(folder);
                LastCsvPath = Path.Combine(folder, "capture_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff") + ".csv");
                File.WriteAllText(LastCsvPath, sb.ToString(), new UTF8Encoding(false));
                LastMessage = summary + " (" + reason + ")";
                Debug.Log("Lab4 capture saved: " + LastCsvPath + "\n" + LastMessage);
            }
            catch (Exception ex)
            {
                LastMessage = "CSV export failed: " + ex.Message;
                Debug.LogError(LastMessage);
            }
        }

        void OnGUI()
        {
            if (!showOverlay || IsBusy) return;
            Matrix4x4 old = GUI.matrix;
            float scale = Mathf.Min(Screen.width / 960f, Screen.height / 540f);
            if (scale <= 0) return;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            GUI.Box(new Rect(20, 20, 700, 100), display);
            GUI.matrix = old;
        }

        void OnApplicationPause(bool paused)
        {
            if (paused && IsBusy) FinishCapture("interrupted by application pause");
        }

        void OnDisable()
        {
            if (IsBusy) FinishCapture("monitor disabled");
            mainThread.Dispose(); systemMemory.Dispose(); gcAlloc.Dispose();
        }
    }
}
