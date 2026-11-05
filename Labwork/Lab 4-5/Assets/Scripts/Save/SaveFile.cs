// CMP5402 Mobile Development - Week 7: Saving, Cloud Data & Online
// SaveFile: JSON in persistentDataPath, written so a crash cannot corrupt it ("Write it so a crash can't corrupt it" slide).
//
// Write: write to a unique .tmp file, then swap it into place with File.Replace (keeps the previous file as .bak).
//        Every write carries a revision number; a write whose revision is older than one already on disk is discarded,
//        so a slow background write can never land on top of a newer synchronous one.
// Read:  try <slot>.json, then <slot>.bak, then return a fresh SaveData. Loading must never stop the game launching.
//        A file written by a NEWER build is not loaded and not overwritten: BlockedByNewerSave is set and Write refuses
//        until DiscardNewerSave is called. Silently replacing it would destroy the player's newer progress.
// Normalise: one validation + migration path used for local AND cloud copies (null lists, out-of-range values, old versions).
//
// Sources: Application.persistentDataPath (Unity 6.3 Scripting API): Android /storage/emulated/<user>/Android/data/<pkg>/files,
// iOS <app>/Documents, erased only by the user (which includes uninstalling). File.Replace and File.Move are System.IO.
// Application.persistentDataPath is a Unity API, so it is read once on the main thread (Init) and cached for background use.
// Encryption is deliberately absent: a key shipped in the build only stops casual editing. Treat local data as the player's.

using System;
using System.IO;
using UnityEngine;

namespace CMP5402.Week7
{
    public static class SaveFile
    {
        public const string DefaultSlot = "save";

        static string dir;
        static readonly object gate = new object();
        static long highestWrittenRevision = -1;

        /// <summary>Call once from the main thread (SaveManager.Awake does). Caches persistentDataPath for background writes.</summary>
        public static void Init() { if (dir == null) dir = Application.persistentDataPath; }

        public static string Dir { get { Init(); return dir; } }
        public static string PathFor(string file) => Path.Combine(Dir, file);
        public static string MainPath(string slot = DefaultSlot) => PathFor(slot + ".json");
        public static string BackupPath(string slot = DefaultSlot) => PathFor(slot + ".bak");
        public static string UnsupportedPath(string slot = DefaultSlot) => PathFor(slot + ".newer.json");

        /// <summary>True when the main file was written by a newer build than this one. Writes are refused while set.</summary>
        public static bool BlockedByNewerSave { get; private set; }

        public static bool Exists(string slot = DefaultSlot) => File.Exists(MainPath(slot)) || File.Exists(BackupPath(slot));

        /// <summary>Serialise on the calling thread and write. Convenience for callers that don't track revisions.</summary>
        public static bool Write(SaveData data, string slot = DefaultSlot) =>
            WriteJson(JsonUtility.ToJson(data), slot, long.MaxValue);

        /// <summary>Atomic write of already-serialised JSON. Safe from any thread (no Unity calls). Returns false if the
        /// write failed, if a newer revision is already on disk, or if a newer build's save is being protected.</summary>
        public static bool WriteJson(string json, string slot, long revision)
        {
            if (BlockedByNewerSave)
            {
                Debug.LogWarning("[SaveFile] write refused: the save on disk is from a newer build (see DiscardNewerSave)");
                return false;
            }
            string final = MainPath(slot), backup = BackupPath(slot);
            string temp = PathFor(slot + "." + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                Directory.CreateDirectory(Dir);
                File.WriteAllText(temp, json);                    // 1. write beside the real file
                lock (gate)                                       // 2. one swap at a time, newest revision wins
                {
                    if (revision <= highestWrittenRevision && revision != long.MaxValue)
                    {
                        File.Delete(temp);
                        return false;                             // an older snapshot finishing late: dropped
                    }
                    if (File.Exists(final))
                        File.Replace(temp, final, backup);        // swap in; the old file becomes .bak
                    else
                        File.Move(temp, final);
                    if (revision != long.MaxValue) highestWrittenRevision = revision;
                }
                return true;
            }
            catch (Exception e)                                   // IOException, UnauthorizedAccessException, disk full
            {
                Debug.LogError($"[SaveFile] write failed for {slot}: {e.Message}");
                try { if (File.Exists(temp)) File.Delete(temp); } catch (Exception) { }
                return false;
            }
        }

        /// <summary>Main file, then backup, then a fresh SaveData. Never throws. 'source' is "main", "backup", "new" or "blocked".</summary>
        public static SaveData Read(out string source, string slot = DefaultSlot)
        {
            BlockedByNewerSave = false;
            foreach (var (path, label) in new[] { (MainPath(slot), "main"), (BackupPath(slot), "backup") })
            {
                if (!File.Exists(path)) continue;
                try
                {
                    var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                    if (data == null) throw new InvalidDataException("empty JSON");
                    if (data.version > SaveData.CurrentVersion)
                    {
                        // Keep it. The player updated the game on another phone, or downgraded this one.
                        BlockedByNewerSave = true;
                        Debug.LogWarning($"[SaveFile] {label} file is version {data.version}; this build understands {SaveData.CurrentVersion}. Saving is disabled until the game is updated or the file is discarded.");
                        source = "blocked";
                        return new SaveData();
                    }
                    source = label;
                    return Normalise(data);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[SaveFile] {label} file unusable ({e.Message}); trying the next");
                }
            }
            source = "new";
            return new SaveData();
        }

        public static SaveData Read(string slot = DefaultSlot) => Read(out _, slot);

        /// <summary>The one validation and migration path. Used for the local file and for cloud copies.
        /// Throws for a save from a newer build; repairs anything else it can.</summary>
        public static SaveData Normalise(SaveData data)
        {
            if (data == null) throw new InvalidDataException("no data");
            if (data.version > SaveData.CurrentVersion)
                throw new InvalidDataException($"save version {data.version} is newer than this build ({SaveData.CurrentVersion})");
            Migrate(data);
            // Valid JSON can still carry impossible values or missing collections. Repair rather than crash later.
            data.saveId ??= "";
            data.highScore = Mathf.Max(0, data.highScore);
            data.saveCounter = Math.Max(0, data.saveCounter);
            data.playTimeSeconds = Mathf.Max(0f, data.playTimeSeconds);
            return data;
        }

        /// <summary>Bring an older save up to CurrentVersion. Add one 'if' per version, in order, and never delete one.</summary>
        static void Migrate(SaveData data)
        {
            if (data.version < 1) data.version = 1;
            if (data.version < 2)
            {
                data.version = 2;
            }
            // if (data.version < 3) { ... data.version = 3; }
        }

        /// <summary>The player (or the test menu) chose to abandon a save from a newer build. It is kept as <slot>.newer.json.</summary>
        public static void DiscardNewerSave(string slot = DefaultSlot)
        {
            try
            {
                if (File.Exists(MainPath(slot))) File.Copy(MainPath(slot), UnsupportedPath(slot), overwrite: true);
                Delete(slot);
            }
            catch (Exception e) { Debug.LogWarning("[SaveFile] could not set the newer save aside: " + e.Message); }
            BlockedByNewerSave = false;
        }

        public static void Delete(string slot = DefaultSlot)
        {
            foreach (var p in new[] { MainPath(slot), BackupPath(slot) })
                try { if (File.Exists(p)) File.Delete(p); } catch (Exception e) { Debug.LogWarning($"[SaveFile] could not delete {p}: {e.Message}"); }
            lock (gate) highestWrittenRevision = -1;
        }

        // ---- test helpers (used by SaveTestMenu and the lab's test matrix) ----

        /// <summary>Truncate the main file to half its length, as if the write had been interrupted.</summary>
        public static void CorruptForTesting(string slot = DefaultSlot)
        {
            string p = MainPath(slot);
            if (!File.Exists(p)) return;
            string text = File.ReadAllText(p);
            File.WriteAllText(p, text.Substring(0, text.Length / 2));
        }

        /// <summary>Rewrite the main file claiming an impossible version, to test the 'newer than this build' path.</summary>
        public static void SetFutureVersionForTesting(string slot = DefaultSlot)
        {
            var data = Read(slot);
            data.version = 99;
            File.WriteAllText(MainPath(slot), JsonUtility.ToJson(data));
        }
    }
}
