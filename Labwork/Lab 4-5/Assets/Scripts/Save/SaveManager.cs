// CMP5402 Mobile Development - Week 7: Saving, Cloud Data & Online
// SaveManager: one owner of the save file, saving on events and on pause ("Save on pause. Never trust quit." slide).
//
// Put ONE SaveManager in your first scene (it survives scene loads). Game code reads SaveManager.Data, changes it,
// and calls MarkDirty(). SaveNow(reason) writes if anything changed. The manager saves itself:
//   - OnApplicationPause(true): the game was hidden by another app, the Home button, a call or the lock screen.
//     This is the last callback you can rely on before the OS may kill the app. Always a synchronous write.
//   - OnApplicationFocus(false): Android raises this for the keyboard and dialogs, as well as some pause cases.
//   - NOT OnApplicationQuit. Unity's docs: iOS applications suspend rather than quit, so it is not called, and if the
//     OS kills a suspended app Unity may be unable to call it on Android either.
//
// Revisions: every change bumps Revision; every write records the revision it serialised. Dirty means "Revision is
// newer than the last revision written", so edits made while a background write is in flight stay dirty, and a
// background write that finishes after a newer synchronous one is discarded by SaveFile. The Saved event carries the
// exact snapshot that reached disk, not the live object.
//
// Threads: JsonUtility.ToJson can be called from background threads, but the object must not be altered while it runs
// (Unity 6.3 Scripting API, JsonUtility.ToJson). Gameplay alters Data every frame, so this class serialises on the main
// thread and moves only the finished string to a background thread (Awaitable.BackgroundThreadAsync).
//
// Sources: MonoBehaviour.OnApplicationQuit, MonoBehaviour.OnApplicationPause, JsonUtility.ToJson (Unity 6.3 Scripting
// API); Introduction to asynchronous programming with Awaitable (Unity 6.3 Manual).

using System;
using UnityEngine;
using Unity.Profiling;

namespace CMP5402.Week7
{
    [DefaultExecutionOrder(-100)]
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }
        public static SaveData Data { get; private set; } = new SaveData();

        /// <summary>Where the current data came from: "main", "backup", "new", "blocked" or "cloud". Show it on the test overlay.</summary>
        public static string LoadedFrom { get; private set; } = "none";

        /// <summary>Goes up on every change. Compare two values to know whether Data changed in between.</summary>
        public static long Revision { get; private set; }
        static long writtenRevision;
        public static bool Dirty => Revision != writtenRevision;

        public static string LastSaveReason { get; private set; } = "";
        public static double LastSaveMs { get; private set; }
        public static bool SavingBlocked => SaveFile.BlockedByNewerSave;

        /// <summary>Raised after a successful write, with the snapshot that was written. CloudSync listens to upload it.</summary>
        public static event Action<SaveData, string> Saved;
        /// <summary>Raised after Load, and after a cloud copy replaces the local one.</summary>
        public static event Action<SaveData> Loaded;

        [SerializeField] string slot = SaveFile.DefaultSlot;
        [Tooltip("Hard route: write the JSON on a background thread for event saves. Pause and focus-loss saves are always synchronous. Measure the synchronous write (LastSaveMs) before turning this on.")]
        [SerializeField] bool writeInBackground;

        static readonly ProfilerMarker saveMarker = new ProfilerMarker("CMP5402.Save");
        bool backgroundWriteRunning;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SaveFile.Init();
            Load();
        }

        public void Load()
        {
            Data = SaveFile.Read(out string from, slot);
            LoadedFrom = from;
            Revision++;
            writtenRevision = Revision;        // what is in memory matches disk (or there is nothing on disk yet)
            Debug.Log($"[SaveManager] loaded from {from}: {Data}");
            Loaded?.Invoke(Data);
        }

        /// <summary>Call after any change to Data that should survive. Cheap; call it freely.</summary>
        public static void MarkDirty() => Revision++;

        /// <summary>Change and mark dirty in one go: SaveManager.Change(d => d.coins += 10);</summary>
        public static void Change(Action<SaveData> edit) { edit(Data); Revision++; }

        /// <summary>Write now if anything changed. Safe to call often: it does nothing when not dirty.
        /// Synchronous unless writeInBackground is on and the reason is not a pause or focus loss.</summary>
        public static bool SaveNow(string reason, bool force = false)
        {
            if (Instance == null) { Debug.LogWarning("[SaveManager] no instance in the scene"); return false; }
            if (!Dirty && !force) return false;
            if (SaveFile.BlockedByNewerSave) { Debug.LogWarning("[SaveManager] not saving: a newer build's save is on disk"); return false; }

            bool mustBeSync = reason.StartsWith("pause") || reason.StartsWith("focus") || reason.StartsWith("destroy");
            if (Instance.writeInBackground && !mustBeSync)
            {
                _ = Instance.SaveNowAsync(reason);
                return true;
            }
            return Instance.WriteSync(reason);
        }

        /// <summary>Take an immutable snapshot of Data for writing. Serialised on the main thread.</summary>
        (string json, long revision, SaveData snapshot) Snapshot()
        {
            Data.saveCounter++;
            Data.saveId = Guid.NewGuid().ToString("N");
            Data.savedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Revision++;                                   // the stamp itself is a change
            string json = JsonUtility.ToJson(Data);
            return (json, Revision, JsonUtility.FromJson<SaveData>(json));
        }

        bool WriteSync(string reason)
        {
            using (saveMarker.Auto())
            {
                var t = System.Diagnostics.Stopwatch.StartNew();
                var (json, rev, snapshot) = Snapshot();
                bool ok = SaveFile.WriteJson(json, slot, rev);
                t.Stop();
                LastSaveMs = t.Elapsed.TotalMilliseconds;
                Finish(ok, rev, snapshot, reason);
                Debug.Log($"[SaveManager] save ({reason}) {(ok ? "ok" : "FAILED")} in {LastSaveMs:F2} ms: {snapshot}");
                return ok;
            }
        }

        void Finish(bool ok, long rev, SaveData snapshot, string reason)
        {
            if (!ok) return;
            if (rev > writtenRevision) writtenRevision = rev;   // edits made after this snapshot stay dirty
            LastSaveReason = reason;
            Saved?.Invoke(snapshot, reason);
        }

        /// <summary>Hard route. Serialise on the main thread, write on a background thread. A second request while one
        /// is running is remembered and written afterwards. Never used for pause or focus loss.</summary>
        public async Awaitable<bool> SaveNowAsync(string reason)
        {
            if (backgroundWriteRunning) return false;         // still dirty: the follow-up write below will catch it
            backgroundWriteRunning = true;
            var (json, rev, snapshot) = Snapshot();
            string s = slot;
            bool ok = false;
            var t = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                await Awaitable.BackgroundThreadAsync();
                ok = SaveFile.WriteJson(json, s, rev);        // no Unity API calls on this thread
            }
            finally
            {
                await Awaitable.MainThreadAsync();
                backgroundWriteRunning = false;
            }
            LastSaveMs = t.Elapsed.TotalMilliseconds;
            Finish(ok, rev, snapshot, reason);
            // Only a SUCCESSFUL write earns a follow-up (edits arrived while it ran). A failed write (disk full,
            // permissions) leaves the data dirty for the next natural trigger instead of retrying in a loop.
            if (ok && Dirty && !SaveFile.BlockedByNewerSave) _ = SaveNowAsync(reason + " (follow-up)");
            else if (!ok) Debug.LogWarning("[SaveManager] background write failed; will try again at the next save trigger");
            return ok;
        }

        /// <summary>Replace the in-memory data (after a cloud download, or a 'new game'), write it, and tell listeners.
        /// The caller's object is cloned: saving stamps a new counter and ID, and the caller may still need the original
        /// (CloudSync keeps the downloaded copy's ID as the ancestor).</summary>
        public static void Replace(SaveData data, string reason, string source = "replace")
        {
            Data = data?.Clone() ?? new SaveData();
            LoadedFrom = source;
            Revision++;
            SaveNow(reason, force: true);
            Loaded?.Invoke(Data);
        }

        /// <summary>Fresh progress, but the save counter carries on so a reset game never looks like an old save.</summary>
        public static void NewGame()
        {
            long counter = Data != null ? Data.saveCounter : 0;
            SaveFile.Delete(Instance != null ? Instance.slot : SaveFile.DefaultSlot);
            Replace(new SaveData { saveCounter = counter }, "new game", "new");
        }

        /// <summary>The player chose to abandon a save written by a newer build (kept on disk as .newer.json).</summary>
        public static void DiscardNewerSave()
        {
            SaveFile.DiscardNewerSave(Instance != null ? Instance.slot : SaveFile.DefaultSlot);
            Instance?.Load();
        }

        // ---- the lifecycle callbacks that matter on a phone ----

        void OnApplicationPause(bool paused)
        {
            if (paused) SaveNow("pause");            // synchronous even if a background write is running: newest wins
        }

        void OnApplicationFocus(bool focus)
        {
            if (!focus) SaveNow("focus lost");
        }

        // In the Editor and on desktop builds this is reliable; on phones it is not. It costs nothing to keep for those.
        void OnApplicationQuit()
        {
            SaveNow("quit (desktop/editor only)");
        }

        // Runs if the SaveManager object is destroyed while dirty (e.g. you reload the first scene). Prefer saving on pause.
        void OnDestroy()
        {
            if (Instance == this && Dirty) SaveNow("destroy");
        }
    }
}
