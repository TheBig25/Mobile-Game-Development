// CMP5402 Mobile Development - Week 7: Saving, Cloud Data & Online
// CloudSync: a copy of the local save in Unity Cloud Save, with anonymous sign-in and a real conflict rule
// ("Cloud Save in twenty lines", "Anonymous accounts" and "When the phone and the cloud disagree" slides).
//
// Set-up: Window > Package Manager > Unity Registry > Cloud Save (Authentication is installed with it).
// Edit > Project Settings > Services: sign in and link the project to a Unity Gaming Services project ID.
// Then add UGS_CLOUDSAVE_INSTALLED to Project Settings > Player > Scripting Define Symbols. Without the define this
// live adapter is excluded. A development scene can still supply the in-memory test store.
//
// Design (the same whatever backend you use):
//   - The LOCAL file is the truth. The cloud is a copy. Nothing here replaces local data without a decision.
//   - ONE sync operation runs at a time. Local saves, reconnection, launch and the settings toggle all just ask for a
//     sync; if one is running, another runs after it. Uploads cannot overlap or finish out of order.
//   - Three-way comparison, not "higher counter wins". Every save has a random saveId. The device remembers the ID
//     of the cloud copy from the last successful sync (the common ancestor). Then:
//       neither side moved            -> in sync
//       only local moved              -> upload, protected by the cloud write lock from the last read
//       only cloud moved              -> adopt the cloud copy (reinstall, new phone), unless local changed meanwhile
//       both moved                    -> a genuine conflict: suspend uploads and ask the player (Conflict event)
//     A counter cannot tell "continued from 38 to 41" apart from "two phones each reached 41 differently", and a
//     reset game can reuse an old number; a random ID plus the remembered ancestor can. Unsaved edits (Dirty) count
//     as "local moved" too, so a restore never lands on progress that has not reached disk yet.
//   - Every decision works on an immutable snapshot of the local data taken after the download, so a save that
//     happens during an upload cannot change what gets recorded as uploaded.
//   - Write locks: every read and write returns a writeLock. Sending it back makes the service reject a save if another
//     device wrote in between (CloudSaveConflictException); the loop then re-reads and re-decides. null bypasses the
//     lock, which is only used when the player has chosen a side. Omit it when creating the item.
//   - A FAILED download is not a MISSING save. Only an answer from the service (Absent) permits an upload without a lock.
//   - A download that finishes late cannot overwrite progress earned meanwhile: SaveManager.Revision is captured before
//     the read and compared after it.
//   - Opt-in: SettingsPrefs.CloudSyncEnabled is checked on every sync, not just at start. Deleting the cloud copy
//     suspends uploads first, so an in-flight save cannot recreate the keys.
// Limits: 2000 keys and 5 MiB per access class per player. One key holding the whole JSON save is the simplest shape.
// Privacy: an anonymous player ID and the progress JSON are the only things sent. No names, contacts or location.
//
// Sources: Cloud Save: Get started; Cloud Save Unity SDK tutorial; Cloud Save Player Data; Cloud Save: Write locks;
// Use anonymous sign-in (all Unity Gaming Services documentation); SaveItem and Item class references
// (com.unity.services.cloudsave package API).

using System;
using System.Threading.Tasks;
using UnityEngine;


namespace CMP5402.Week7
{
    public class CloudSync : MonoBehaviour
    {
        public const string SaveKey = "save";
        const string PrefAncestor = "cmp5402.cloud.ancestorId";   // saveId of the cloud copy at the last successful sync

        public enum State { NotInstalled, Disabled, Offline, SigningIn, Idle, Syncing, Conflict, Error }
        public enum Outcome { Found, Absent, Failed }

        public static CloudSync Instance { get; private set; }
        public State Status { get; private set; } = State.NotInstalled;
        public string StatusDetail { get; private set; } = "Cloud Save package not installed";
        public string PlayerId { get; private set; } = "";
        public bool SyncRunning { get; private set; }
        public bool SyncRequested { get; private set; }

        /// <summary>Raised once when both sides changed since the last sync. The game shows both and calls Resolve.
        /// Until then automatic uploads are suspended and nothing is replaced. SaveTestMenu subscribes as a fallback UI.</summary>
        public event Action<SaveData, SaveData> Conflict;   // (local, cloud)
        public SaveData PendingCloudCopy { get; private set; }
        public bool HasConflict => PendingCloudCopy != null;

        [Tooltip("Seconds the launch waits for the first sync before the game carries on. The sync continues in the background.")]
        [SerializeField] float launchBudgetSeconds = 2f;
        [Tooltip("Seconds before the first retry of a failed upload (doubles each time, four attempts).")]
        [SerializeField] float retrySeconds = 1f;

        string AncestorId
        {
            get => PlayerPrefs.GetString(ancestorPreference, "");
            set { PlayerPrefs.SetString(ancestorPreference, value ?? ""); PlayerPrefs.Save(); }
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
#if UGS_CLOUDSAVE_INSTALLED
            if (store == null) store = new UgsCloudStore();
#endif
            DontDestroyOnLoad(gameObject);
        }

        ICloudStore store;
        string ancestorPreference = PrefAncestor;

#if UNITY_EDITOR || DEVELOPMENT_BUILD || WEEK7_DESKTOP_HARNESS
        // Install before starting sync. The test ancestor is isolated from the live player's history.
        public void UseTestStore(TestCloudStore testStore)
        {
            if (testStore == null) throw new ArgumentNullException(nameof(testStore));
            if (SyncRunning || deleting || PlayerId != "")
                throw new InvalidOperationException("Install the test store before starting sync.");
            store = testStore;
            ancestorPreference = PrefAncestor + ".test";
            AncestorId = "";
        }

        public Task CurrentSync => runningSync ?? Task.CompletedTask;
        public string LastSyncedSaveId => AncestorId;
#endif

        string cloudWriteLock;          // from the last read or write; sent back with the next upload
        int generation;                 // bumped by SetEnabled: a sync that started under an older generation applies nothing
        string pendingCloudLock;
        bool deleting;
        Task runningSync;

        void OnEnable()
        {
            SaveManager.Saved += OnLocalSaved;
            NetProbe.ReachabilityChanged += OnReachabilityChanged;
        }

        void OnDisable()
        {
            SaveManager.Saved -= OnLocalSaved;
            NetProbe.ReachabilityChanged -= OnReachabilityChanged;
        }

        async void Start()
        {
            await SyncOnLaunchAsync();
        }

        void Set(State s, string detail) { Status = s; StatusDetail = detail; }

        bool Enabled => store != null && SettingsPrefs.CloudSyncEnabled && !deleting;

        /// <summary>The settings toggle. Turning it on starts a sync; turning it off stops all uploads from now on.</summary>
        public void SetEnabled(bool on)
        {
            generation++;               // any sync already in flight must not apply its result
            SettingsPrefs.CloudSyncEnabled = on;
            if (store == null) return;
            if (on) RequestSync("enabled in settings");
            else Set(State.Disabled, "cloud sync is off in Settings");
        }

        /// <summary>Initialise UGS and sign in anonymously. The session token is cached on the device, so the same player
        /// comes back on every launch, until the app is uninstalled. Linking a provider (Section 7 extension) survives that.</summary>
        public async Task<bool> SignInAsync()
        {
            if (store == null) return false;
            try
            {
                Set(State.SigningIn, "initialising services");
                PlayerId = await store.SignInAsync();
                Set(State.Idle, "signed in");
                return true;
            }
            catch (Exception e)       // RequestFailedException, AuthenticationException, or no network at all
            {
                Set(State.Offline, "sign-in failed: " + e.Message);
                Debug.LogWarning("[CloudSync] " + StatusDetail);
                return false;
            }
        }

        /// <summary>Launch: start a sync and wait at most launchBudgetSeconds for it. The game then carries on from the
        /// local file; the sync keeps running and its result is observed and logged.</summary>
        public async Task SyncOnLaunchAsync()
        {
            if (store == null) return;
            if (!Enabled) { Set(State.Disabled, "cloud sync is off in Settings"); return; }
            RequestSync("launch");
            var work = runningSync ?? Task.CompletedTask;
            var timeout = Task.Delay(TimeSpan.FromSeconds(launchBudgetSeconds));
            if (await Task.WhenAny(work, timeout) == timeout && !work.IsCompleted)
            {
                Debug.Log("[CloudSync] launch sync still running after the budget; the game carries on, the sync finishes in the background");
                _ = work.ContinueWith(t =>
                {
                    if (t.IsFaulted) Debug.LogWarning("[CloudSync] background launch sync failed: " + t.Exception?.GetBaseException().Message);
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
        }

        /// <summary>Ask for a sync. Coalesces: one runs at a time, and at most one more is queued behind it.</summary>
        public void RequestSync(string why)
        {
            if (!Enabled) return;
            SyncRequested = true;
            if (!SyncRunning) runningSync = RunSyncLoop(why);
        }

        async Task RunSyncLoop(string why)
        {
            SyncRunning = true;
            try
            {
                int guard = 0;
                while (SyncRequested && Enabled && guard++ < 5)   // a write-lock conflict re-runs the loop; cap it
                {
                    SyncRequested = false;
                    await SyncOnce(why);
                }
            }
            catch (Exception e)
            {
                Set(State.Error, "sync failed: " + e.Message);
                Debug.LogWarning("[CloudSync] " + StatusDetail);
            }
            finally { SyncRunning = false; }
        }

        async Task SyncOnce(string why)
        {
            if (HasConflict) { Set(State.Conflict, "waiting for the player to resolve a conflict"); return; }
            int gen = generation;
            if (!await SignInAsync()) return;
            if (!StillWanted(gen)) return;

            Set(State.Syncing, "downloading (" + why + ")");
            long revisionBefore = SaveManager.Revision;
            var (outcome, cloud, cloudLock) = await DownloadAsync();
            if (!StillWanted(gen)) return;    // the player turned sync off (or off and on) while we waited: apply nothing

            // Everything below works on this snapshot. Gameplay may change SaveManager.Data at any time after this line.
            SaveData local = SaveManager.Data.Clone();
            bool localDirty = SaveManager.Dirty || SaveManager.Revision != revisionBefore;   // unsaved edits, before or during the read
            string ancestor = AncestorId;

            switch (outcome)
            {
                case Outcome.Failed:
                    Set(State.Error, "download failed; nothing uploaded");   // a failed read never authorises an overwrite
                    return;

                case Outcome.Absent:
                    cloudWriteLock = null;                                   // creating: omit the lock
                    if (localDirty) SaveManager.SaveNow("pre-sync");          // never upload unsaved content under a saved ID
                    else if (local.saveCounter > 0) await UploadAsync(local, createIfMissing: true);
                    else Set(State.Idle, "nothing to upload yet");
                    return;

                case Outcome.Found:
                    cloudWriteLock = cloudLock;
                    bool localMoved = localDirty || (ancestor == "" ? local.saveCounter > 0 : local.saveId != ancestor);
                    bool cloudMoved = ancestor == "" || cloud.saveId != ancestor;

                    if (!localMoved && !cloudMoved) { Set(State.Idle, "in sync (#" + cloud.saveCounter + ")"); return; }

                    if (localMoved && !cloudMoved)
                    {
                        if (localDirty) SaveManager.SaveNow("pre-sync");   // raises Saved -> another sync uploads the saved snapshot
                        else await UploadAsync(local, createIfMissing: false);
                        return;
                    }

                    if (!localMoved && cloudMoved)
                    {
                        // Fresh install, new phone, or this device is simply behind: take the cloud copy.
                        Restore(cloud, cloudLock, "cloud restore");
                        Set(State.Idle, "restored from cloud (#" + cloud.saveCounter + ")");
                        return;
                    }

                    // Both sides moved (or there are unsaved local edits). Never choose silently.
                    PendingCloudCopy = cloud;
                    pendingCloudLock = cloudLock;
                    Set(State.Conflict, "conflict: local #" + local.saveCounter + " vs cloud #" + cloud.saveCounter);
                    Debug.LogWarning("[CloudSync] " + StatusDetail + "; uploads suspended until Resolve is called");
                    Conflict?.Invoke(local, cloud);
                    return;
            }
        }

        bool StillWanted(int gen)
        {
            if (gen == generation && Enabled) return true;
            Set(Enabled ? State.Idle : State.Disabled, "sync abandoned: settings changed while it was running");
            return false;
        }

        /// <summary>Adopt a cloud copy. The ancestor is the downloaded copy's own ID, recorded BEFORE the local save
        /// stamps a new one, and Replace works on a clone so the downloaded object is never mutated.</summary>
        void Restore(SaveData cloud, string cloudLock, string reason)
        {
            AncestorId = cloud.saveId;
            cloudWriteLock = cloudLock;
            SaveManager.Replace(cloud, reason, "cloud");   // saves a clone: new counter and ID locally -> "only local moved" -> upload
        }

        /// <summary>Called by the game's conflict dialog (or SaveTestMenu).</summary>
        public void Resolve(bool keepCloud)
        {
            if (!HasConflict) return;
            var cloud = PendingCloudCopy;
            PendingCloudCopy = null;
            if (keepCloud)
            {
                Restore(cloud, pendingCloudLock, "player chose cloud copy");
                Set(State.Idle, "conflict resolved: cloud copy kept");
            }
            else
            {
                // Keep local. The cloud copy becomes the acknowledged ancestor and the local save gets a new ID past it,
                // so the next sync sees "only local moved" and uploads with the lock we read. A second device writing in
                // between still produces a lock rejection and a fresh conflict, which is the correct outcome.
                SaveManager.Change(d => d.saveCounter = Math.Max(d.saveCounter, cloud.saveCounter));
                AncestorId = cloud.saveId;
                cloudWriteLock = pendingCloudLock;
                Set(State.Idle, "conflict resolved: local copy kept");
                SaveManager.SaveNow("player kept local copy", force: true);   // raises Saved -> RequestSync -> upload
            }
            pendingCloudLock = null;
        }

        void OnLocalSaved(SaveData snapshot, string reason)
        {
            if (Enabled) RequestSync("local save: " + reason);
        }

        void OnReachabilityChanged(NetProbe.Reach now)
        {
            if (now == NetProbe.Reach.Online && Enabled) RequestSync("back online");   // reconcile before any upload
        }

        /// <summary>Upload one snapshot with the stored write lock. Four attempts with doubling delays for transport
        /// errors; a write-lock conflict stops immediately and asks the loop to re-read. 'data' must be a snapshot the
        /// caller owns (SyncOnce passes a clone): its JSON, counter and ID are captured before the first await.</summary>
        async Task<bool> UploadAsync(SaveData data, bool createIfMissing)
        {
            string json = JsonUtility.ToJson(data);
            long counter = data.saveCounter;
            string id = data.saveId;
            float delay = retrySeconds;
            for (int attempt = 1; attempt <= 4; attempt++)
            {
                if (!Enabled) { Set(State.Disabled, "upload cancelled: cloud sync turned off"); return false; }
                try
                {
                    Set(State.Syncing, "uploading #" + counter + (cloudWriteLock == null ? " (no lock)" : ""));
                    cloudWriteLock = await store.WriteAsync(SaveKey, json, createIfMissing ? null : cloudWriteLock);
                    AncestorId = id;                                       // exactly the copy that was uploaded (it is in the cloud now, whatever the setting)
                    Set(State.Idle, "uploaded #" + counter);
                    return true;
                }
                catch (CloudStoreConflictException)
                {
                    // Another device wrote since we last read. Re-read and re-decide; the loop handles it.
                    Debug.LogWarning("[CloudSync] write lock rejected: the cloud changed since the last read; re-syncing");
                    cloudWriteLock = null;
                    SyncRequested = true;
                    return false;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[CloudSync] upload attempt {attempt} failed: {e.Message}");
                    if (attempt == 4) break;
                    await Task.Delay(TimeSpan.FromSeconds(delay));
                    delay *= 2f;
                }
            }
            Set(State.Error, "upload failed; will retry at the next save or reconnection");
            return false;
        }

        /// <summary>Found (with data and write lock), Absent (the service answered: no such key), or Failed (no answer).</summary>
        public async Task<(Outcome outcome, SaveData data, string writeLock)> DownloadAsync()
        {
            if (store == null) return (Outcome.Failed, null, null);
            try
            {
                var item = await store.ReadAsync(SaveKey);
                if (item.json == null) return (Outcome.Absent, null, null);
                var data = JsonUtility.FromJson<SaveData>(item.json);
                return (Outcome.Found, SaveFile.Normalise(data), item.writeLock);   // same validation as the local file
            }
            catch (Exception e)
            {
                // Includes a cloud save from a newer build: Normalise throws, and we must not upload over it.
                Set(State.Error, "download failed: " + e.Message);
                Debug.LogWarning("[CloudSync] " + StatusDetail);
                return (Outcome.Failed, null, null);
            }
        }

        /// <summary>'Delete my cloud data': suspends uploads, waits for any running sync, deletes the key, forgets the
        /// ancestor and turns cloud sync off. Local files and the account are untouched (see DeleteAccountAndDataAsync).</summary>
        public async Task<bool> DeleteCloudCopyAsync()
        {
            deleting = true;
            try
            {
                if (runningSync != null) { try { await runningSync; } catch (Exception) { } }
                if (!await SignInAsync()) return false;
                await store.DeleteAsync(SaveKey);
                AncestorId = "";
                cloudWriteLock = null;
                PendingCloudCopy = null;
                SettingsPrefs.CloudSyncEnabled = false;
                Set(State.Disabled, "cloud copy deleted; cloud sync turned off");
                return true;
            }
            catch (Exception e)
            {
                Set(State.Error, "delete failed: " + e.Message);
                return false;
            }
            finally { deleting = false; }
        }

        /// <summary>'Delete my account and all data': cloud copy, then the Unity player account, then the local files.
        /// Google Play also requires a web-based deletion route for apps with accounts; that is outside the game.</summary>
        public async Task<bool> DeleteAccountAndDataAsync()
        {
            if (!await DeleteCloudCopyAsync()) return false;
            try
            {
                await store.DeleteAccountAsync();
                PlayerId = "";
                SaveManager.NewGame();
                SettingsPrefs.ResetAll();
                Set(State.Disabled, "account and data deleted");
                return true;
            }
            catch (Exception e)
            {
                Set(State.Error, "account deletion failed: " + e.Message);
                return false;
            }
        }
    }
}
