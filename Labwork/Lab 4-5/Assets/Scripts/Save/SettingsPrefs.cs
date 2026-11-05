// CMP5402 Mobile Development - Week 7: Saving, Cloud Data & Online
// SettingsPrefs: the settings that belong in PlayerPrefs ("PlayerPrefs for settings, files for progress" slide).
//
// PlayerPrefs is a key-value store, kept unencrypted on the device (Android: shared_prefs XML, iOS: NSUserDefaults).
// Unity's docs say not to store sensitive data in it. It is the right home for small, flat, per-device preferences
// that do not need versions, lists or migration. Progress goes through SaveManager.
// The Week 6 pack already uses it for the quality tier (QualityTiers.PrefKey) and battery saver; those keys are left alone.
//
// Source: PlayerPrefs (Unity 6.3 Scripting API).

using UnityEngine;

namespace CMP5402.Week7
{
    public static class SettingsPrefs
    {
        const string KMusic = "cmp5402.settings.music";
        const string KSfx = "cmp5402.settings.sfx";
        const string KHaptics = "cmp5402.settings.haptics";
        const string KLanguage = "cmp5402.settings.language";
        const string KCloud = "cmp5402.settings.cloudSync";
        const string KFirstRun = "cmp5402.settings.firstRunUnix";

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(KMusic, 0.8f);
            set => Set(KMusic, Mathf.Clamp01(value));
        }

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(KSfx, 1f);
            set => Set(KSfx, Mathf.Clamp01(value));
        }

        public static bool Haptics
        {
            get => PlayerPrefs.GetInt(KHaptics, 1) == 1;
            set => Set(KHaptics, value ? 1 : 0);
        }

        public static string Language
        {
            get => PlayerPrefs.GetString(KLanguage, "en");
            set => Set(KLanguage, value);
        }

        /// <summary>Has the player opted in to cloud sync? Off by default: it is their data and their choice (privacy slide).</summary>
        public static bool CloudSyncEnabled
        {
            get => PlayerPrefs.GetInt(KCloud, 0) == 1;
            set => Set(KCloud, value ? 1 : 0);
        }

        /// <summary>Records the first launch. Survives app updates but not an uninstall, like everything in PlayerPrefs.</summary>
        public static bool IsFirstRun
        {
            get
            {
                if (PlayerPrefs.HasKey(KFirstRun)) return false;
                Set(KFirstRun, (int)(System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() % int.MaxValue));
                return true;
            }
        }

        // PlayerPrefs.Save() writes to disk. Unity also writes on OnApplicationQuit, which phones may never reach, so save on every change.
        static void Set(string k, float v) { PlayerPrefs.SetFloat(k, v); PlayerPrefs.Save(); }
        static void Set(string k, int v) { PlayerPrefs.SetInt(k, v); PlayerPrefs.Save(); }
        static void Set(string k, string v) { PlayerPrefs.SetString(k, v); PlayerPrefs.Save(); }

        public static void ResetAll()
        {
            foreach (var k in new[] { KMusic, KSfx, KHaptics, KLanguage, KCloud })
                PlayerPrefs.DeleteKey(k);
            PlayerPrefs.Save();
        }
    }
}
