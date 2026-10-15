using UnityEngine;
using UnityEngine.Rendering;

namespace CMP5402.Week4
{
    [RequireComponent(typeof(Lab4Workload), typeof(Lab4RenderingLoad), typeof(Lab4PerformanceMonitor))]
    public class Lab4Sandbox : MonoBehaviour
    {
        [SerializeField] Material geometryMaterial;
        [SerializeField] Camera sandboxCamera;
        [SerializeField, Range(30, 60)] int targetFramesPerSecond = 60;
        Lab4Workload workload;
        Lab4RenderingLoad renderingLoad;
        Lab4PerformanceMonitor monitor;
        string scenario = "Idle";
        string retainedLabel = "Retained memory: 0 MiB";
        int previousTarget, previousVsync, previousInterval;

        void Awake()
        {
            workload = GetComponent<Lab4Workload>();
            renderingLoad = GetComponent<Lab4RenderingLoad>();
            monitor = GetComponent<Lab4PerformanceMonitor>();
            if (Application.unityVersion != "6000.3.20f1")
                Debug.LogWarning("Lab4 targets Unity 6000.3.20f1. Current Editor: " + Application.unityVersion);
        }

        void OnEnable()
        {
            previousTarget = Application.targetFrameRate;
            previousVsync = QualitySettings.vSyncCount;
            previousInterval = OnDemandRendering.renderFrameInterval;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = targetFramesPerSecond;
            OnDemandRendering.renderFrameInterval = 1;
        }

        void Start()
        {
            if (sandboxCamera == null) sandboxCamera = Camera.main;
            if (sandboxCamera == null)
                sandboxCamera = new GameObject("Lab4Camera", typeof(Camera)).GetComponent<Camera>();
            sandboxCamera.transform.SetPositionAndRotation(new Vector3(0, 0, -10), Quaternion.identity);
            sandboxCamera.orthographic = true;
            sandboxCamera.orthographicSize = 4f;
            sandboxCamera.clearFlags = CameraClearFlags.SolidColor;
            sandboxCamera.backgroundColor = new Color(0.03f, 0.04f, 0.07f);
            renderingLoad.Build(geometryMaterial);
            if (geometryMaterial == null)
                Debug.LogWarning("Lab4: assign a pipeline-compatible material for geometry. Transparent UI still works.");
        }

        void SelectScenario(int selected)
        {
            renderingLoad.SetVisible(selected == 3);
            workload.SetMode(selected == 1 ? Lab4Workload.WorkloadMode.Cpu
                : selected == 2 ? Lab4Workload.WorkloadMode.Allocations : Lab4Workload.WorkloadMode.Idle);
            scenario = selected == 1 ? "CPU" : selected == 2 ? "Allocations" : selected == 3 ? "Rendering" : "Idle";
        }

        void OnGUI()
        {
            if (monitor == null || monitor.IsBusy) return;
            float scale = Mathf.Min(Screen.width / 960f, Screen.height / 540f);
            if (scale <= 0f) return;
            Matrix4x4 old = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            GUI.Box(new Rect(20, 135, 920, 355), "Lab 4: " + scenario);
            if (GUI.Button(new Rect(40, 170, 200, 55), "Idle")) SelectScenario(0);
            if (GUI.Button(new Rect(260, 170, 200, 55), "CPU work")) SelectScenario(1);
            if (GUI.Button(new Rect(480, 170, 200, 55), "Allocations")) SelectScenario(2);
            if (GUI.Button(new Rect(700, 170, 200, 55), "Rendering")) SelectScenario(3);
            if (GUI.Button(new Rect(40, 240, 200, 55), "Capture 30 seconds")) monitor.BeginCapture(scenario);
            if (GUI.Button(new Rect(260, 240, 200, 55), "Retain +8 MiB"))
            {
                workload.RetainEightMiB();
                retainedLabel = "Retained memory: " + workload.RetainedMiB + " MiB";
            }
            if (GUI.Button(new Rect(480, 240, 200, 55), "Release memory"))
            {
                workload.ReleaseRetainedMemory(); retainedLabel = "Retained memory: 0 MiB";
            }
            if (GUI.Button(new Rect(700, 240, 90, 55), "30 FPS")) Application.targetFrameRate = 30;
            if (GUI.Button(new Rect(805, 240, 95, 55), "60 FPS")) Application.targetFrameRate = 60;
            GUI.Label(new Rect(40, 310, 850, 30), retainedLabel);
            GUI.Label(new Rect(40, 345, 850, 60), monitor.LastMessage);
            GUI.Label(new Rect(40, 415, 850, 55), "Captures hide diagnostic UI. Keep the app open for 32 seconds.\nCSV location is printed to the device log after capture.");
            GUI.matrix = old;
        }

        void OnDisable()
        {
            Application.targetFrameRate = previousTarget;
            QualitySettings.vSyncCount = previousVsync;
            OnDemandRendering.renderFrameInterval = previousInterval;
        }
    }
}
