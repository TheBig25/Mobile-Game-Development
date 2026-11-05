// CMP5402 Mobile Development - Week 7: Saving, Cloud Data & Online
// SaveTestMenu: on-screen buttons for the save-system test matrix ("The save-system test matrix" slide).
//
// Drop it on any GameObject in a test scene. It draws with the immediate-mode GUI so it needs no Canvas, and it uses
// touches or the mouse through the GUI system, so it works with either input backend. Hide it in release builds
// (it is wrapped in DEVELOPMENT_BUILD || UNITY_EDITOR). Three-finger tap or F3 toggles it.
//
// Buttons: add coins and save (dirty flag path), save now, corrupt the file, delete the file, set a future version,
// reload from disk, cloud on/off, sync now and delete (when CloudSync is present), and a network probe. It also acts as
// the fallback conflict dialog (Keep this phone's / Keep the cloud's) and the 'discard newer save' prompt.
// The readout shows where the current data was loaded from (main / backup / new), the last save reason and time,
// the cloud state and the probe result: everything the lab's evidence table asks for.

using UnityEngine;

namespace CMP5402.Week7
{
    public class SaveTestMenu : MonoBehaviour
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        [SerializeField] bool visible = true;
        [SerializeField] int fontSize = 28;
        GUIStyle label, button;
        string lastAction = "";

        // Fallback conflict UI: the game should show its own dialog, but something must answer a pending conflict.
        // It reads CloudSync.PendingCloudCopy every frame rather than subscribing to the event, so it works whatever
        // order the Systems objects initialise in, and shows a conflict that was raised before this menu existed.

        void Update()
        {
            bool toggle = false;
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.f3Key.wasPressedThisFrame) toggle = true;
            var ts = UnityEngine.InputSystem.Touchscreen.current;
            if (ts != null && ts.touches.Count >= 3 && ts.touches[2].press.wasPressedThisFrame) toggle = true;
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.F3)) toggle = true;
            if (Input.touchCount >= 3 && Input.GetTouch(2).phase == TouchPhase.Began) toggle = true;
#endif
            if (toggle) visible = !visible;
        }

        void OnGUI()
        {
            if (!visible) return;
            if (label == null)
            {
                label = new GUIStyle(GUI.skin.label) { fontSize = fontSize, richText = true };
                button = new GUIStyle(GUI.skin.button) { fontSize = fontSize };
            }
            float w = Screen.width * 0.9f, bh = fontSize * 2.2f;
            GUILayout.BeginArea(new Rect(Screen.width * 0.05f, Screen.height * 0.05f, w, Screen.height * 0.9f));

            var d = SaveManager.Data;
            GUILayout.Label($"<b>{d}</b>", label);
            GUILayout.Label($"loaded from: <b>{SaveManager.LoadedFrom}</b>   dirty: {SaveManager.Dirty}   revision {SaveManager.Revision}", label);
            if (SaveManager.SavingBlocked)
            {
                GUILayout.Label("<color=red>Saving disabled: the save on disk is from a newer build.</color>", label);
                if (GUILayout.Button("Discard newer save (kept as .newer.json)", button, GUILayout.Height(bh))) { SaveManager.DiscardNewerSave(); lastAction = "newer save set aside; reloaded"; }
            }
            GUILayout.Label($"last save: {SaveManager.LastSaveReason} ({SaveManager.LastSaveMs:F2} ms)", label);
            GUILayout.Label("file: " + SaveFile.MainPath(), label);
            var cs = CloudSync.Instance;
            if (cs != null) GUILayout.Label($"cloud: {cs.Status} ({cs.StatusDetail}) player {cs.PlayerId} enabled {SettingsPrefs.CloudSyncEnabled}", label);
            if (cs != null && cs.HasConflict)
            {
                GUILayout.Label($"<color=orange>Conflict.</color> This phone: {SaveManager.Data}   Cloud: {cs.PendingCloudCopy}", label);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Keep this phone's", button, GUILayout.Height(bh))) { cs.Resolve(false); lastAction = "kept local"; }
                if (GUILayout.Button("Keep the cloud's", button, GUILayout.Height(bh))) { cs.Resolve(true); lastAction = "kept cloud"; }
                GUILayout.EndHorizontal();
            }
            var np = NetProbe.Instance;
            if (np != null) GUILayout.Label($"net: {np.Last} via {NetProbe.ConnectionKind} ({np.LastDetail}) server offset {(np.ClockOffsetSeconds.HasValue ? np.ClockOffsetSeconds.Value.ToString("F1") + " s" : "unknown")}", label);
            if (lastAction != "") GUILayout.Label("<color=yellow>" + lastAction + "</color>", label);

            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+10 coins, next level", button, GUILayout.Height(bh)))
            {
                SaveManager.Change(s => { s.coins += 10; s.level++; });
                lastAction = "changed; dirty = true (save happens on pause or Save now)";
            }
            if (GUILayout.Button("Save now", button, GUILayout.Height(bh)))
                lastAction = SaveManager.SaveNow("test menu", force: true) ? "saved" : "save failed";
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Corrupt file", button, GUILayout.Height(bh))) { SaveFile.CorruptForTesting(); lastAction = "main file truncated; now Reload"; }
            if (GUILayout.Button("Delete files", button, GUILayout.Height(bh))) { SaveFile.Delete(); lastAction = "main, backup and temp deleted; now Reload"; }
            if (GUILayout.Button("Future version", button, GUILayout.Height(bh))) { SaveFile.SetFutureVersionForTesting(); lastAction = "main file claims version 99; now Reload"; }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Reload from disk", button, GUILayout.Height(bh)))
            {
                SaveManager.Instance.Load();
                lastAction = "reloaded from " + SaveManager.LoadedFrom;
            }

            if (cs != null)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(SettingsPrefs.CloudSyncEnabled ? "Cloud: on" : "Cloud: off", button, GUILayout.Height(bh))) { cs.SetEnabled(!SettingsPrefs.CloudSyncEnabled); lastAction = "cloud sync toggled"; }
                if (GUILayout.Button("Sync now", button, GUILayout.Height(bh))) { cs.RequestSync("test menu"); lastAction = "sync requested"; }
                if (GUILayout.Button("Delete cloud copy", button, GUILayout.Height(bh))) { _ = cs.DeleteCloudCopyAsync(); lastAction = "cloud delete started"; }
                GUILayout.EndHorizontal();
            }
            if (np != null && GUILayout.Button("Probe network", button, GUILayout.Height(bh))) { _ = np.ProbeAsync(); lastAction = "probe started"; }

            GUILayout.EndArea();
        }
#endif
    }
}
