// CMP5402 Mobile Development - Week 7: Saving, Cloud Data & Online
// SaveData: the one class that holds everything worth keeping ("One class holds everything worth keeping" slide).
//
// Rules (Unity 6.3 Manual, JSON serialization):
//   - JsonUtility serialises public fields and [SerializeField] private fields. Properties are ignored.
//   - Dictionary<K,V> is not supported: use two lists, or a List of a small [Serializable] pair class (see SkinStat).
//   - A top-level array or primitive cannot be the root: this class is the root.
// Settings (volume, quality tier, language) do NOT live here. They are PlayerPrefs (see SettingsPrefs).

using System;
using System.Collections.Generic;

namespace CMP5402.Week7
{
    [Serializable]
    public class SaveData
    {
        /// <summary>Bump CurrentVersion whenever the shape of this class changes, and add a step to SaveFile.Migrate.</summary>
        public const int CurrentVersion = 2;

        public int version = CurrentVersion;

        /// <summary>Goes up by one on every save and is never reset (a new game carries it on). Used to describe and
        /// order two copies in a conflict dialog, without trusting the phone's clock.</summary>
        public long saveCounter;

        /// <summary>A fresh random ID on every save. This, not the counter, is the identity of a save: two phones can
        /// both reach counter 41 with different content, but they cannot produce the same ID. CloudSync remembers the
        /// ID of the last synced cloud copy as the common ancestor.</summary>
        public string saveId = "";

        /// <summary>For display only ("last saved 3 minutes ago"). Never use it to decide which copy wins.</summary>
        public long savedAtUnixSeconds;

        // ---- progress ----
        public int highScore;
        public float playTimeSeconds;

        public SaveData Clone() => UnityEngine.JsonUtility.FromJson<SaveData>(UnityEngine.JsonUtility.ToJson(this));

        public override string ToString() =>
            $"v{version} #{saveCounter} ({(string.IsNullOrEmpty(saveId) ? "no id" : saveId.Substring(0, Math.Min(6, saveId.Length)))}) best {highScore}";
    }
}
