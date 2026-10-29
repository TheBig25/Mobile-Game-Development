// CMP5402 Mobile Development - Week 6: Graphics & Device Scalability
// QualityTiers: apply and remember a quality tier ("One URP asset per tier, one call to switch" slide).
//
// Set-up (Project Settings > Quality): three levels in this order, Low, Medium, High, each with its own URP asset,
// all ticked for Android and iOS, Default = Low. The tier index below is the index among the levels ENABLED for the
// current platform, in Quality order. If you have other levels enabled, map them in TierToLevel.
//
// Changing quality level has a temporary but significant cost, especially when MSAA changes, so call Apply from
// a menu or loading screen, not during play. RenderScaleController handles the live adjustments instead.
//
// Two different things are stored: the tier currently APPLIED (Current) and the tier the PLAYER chose (saved in
// PlayerPrefs). Automatic changes (heat, bootstrap detection) never overwrite the player's choice: only
// ApplyPlayerChoice and the first-run detection persist. The player's choice is also the ceiling for StepUp.

using UnityEngine;
using UnityEngine.SceneManagement;

namespace CMP5402.Week6
{
    public static class QualityTiers
    {
        public const int Low = 0, Mid = 1, High = 2;
        public const int Count = 3;
        public const string PrefKey = "cmp5402.quality.tier";
        public const string PrefCeiling = "cmp5402.quality.ceiling";   // the tier the player chose by hand
        public const string PrefBattery = "cmp5402.quality.batterySaver";

        /// <summary>Frame rate target per tier. Change to suit your game.</summary>
        public static readonly int[] TargetFrameRate = { 30, 60, 60 };

        /// <summary>A multiplier your particle emitters and effects can read (0..1).</summary>
        public static readonly float[] EffectsScale = { 0.25f, 0.6f, 1f };

        public static int Current { get; private set; } = -1;
        public static float CurrentEffectsScale => Current < 0 ? 1f : EffectsScale[Current];

        /// <summary>Raised after a tier is applied. The TierOverlay listens to this.</summary>
        public static event System.Action<int, string> Changed;

        /// <summary>Map a tier (0..2) to a QualitySettings level index for this platform.</summary>
        static int TierToLevel(int tier) => tier;   // 1:1 when Low/Medium/High are the only enabled levels

        public static bool HasSaved => PlayerPrefs.HasKey(PrefKey);
        public static int Saved => PlayerPrefs.GetInt(PrefKey, Mid);

        /// <summary>The highest tier the automatic systems may use. Defaults to High until the player picks one.</summary>
        public static int Ceiling => PlayerPrefs.GetInt(PrefCeiling, High);

        /// <summary>Battery saver is its own switch: Low tier plus 30 FPS, on top of whatever graphics tier the player prefers.</summary>
        public static bool BatterySaver
        {
            get => PlayerPrefs.GetInt(PrefBattery, 0) == 1;
            set { PlayerPrefs.SetInt(PrefBattery, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        /// <summary>A tier change requested during play (by heat, for example). It is applied at the next safe point:
        /// a scene load, or when the settings menu closes. See ApplyPending.</summary>
        public static int Pending { get; private set; } = -1;
        public static string PendingReason { get; private set; } = "";

        static bool hooked;
        static void HookSceneLoads()
        {
            if (hooked) return;
            hooked = true;
            SceneManager.sceneLoaded += (scene, mode) => ApplyPending("scene load");
        }

        /// <summary>Apply a tier now. Pass persist = true only for a choice that should survive a restart
        /// (the player's, or the first-run detection). Automatic thermal or frame-time changes pass false.</summary>
        public static void Apply(int tier, string reason = "manual", bool persist = false)
        {
            HookSceneLoads();
            tier = Mathf.Clamp(tier, 0, Count - 1);
            if (persist)
            {
                PlayerPrefs.SetInt(PrefKey, tier);
                PlayerPrefs.Save();
            }
            if (BatterySaver) tier = Low;   // battery saver always runs the Low tier, whatever was asked for
            int level = Mathf.Clamp(TierToLevel(tier), 0, QualitySettings.names.Length - 1);
            if (QualitySettings.GetQualityLevel() != level)
                QualitySettings.SetQualityLevel(level, applyExpensiveChanges: true);   // we're on a menu or loading screen
            Application.targetFrameRate = BatterySaver ? 30 : TargetFrameRate[tier];
            Current = tier;
            Pending = -1;
            Changed?.Invoke(tier, reason);
        }

        /// <summary>Ask for a tier change without doing it now. Safe to call mid-play: nothing changes until
        /// ApplyPending runs at a scene load or when the settings menu closes.</summary>
        public static void Request(int tier, string reason)
        {
            HookSceneLoads();
            tier = Mathf.Clamp(tier, 0, Count - 1);
            if (tier == Current) { Pending = -1; return; }
            Pending = tier;
            PendingReason = reason;
        }

        public static void RequestStepDown(string reason) { if (Current > Low) Request(Current - 1, reason); }

        /// <summary>Drop a queued change that is no longer wanted (for example, the phone cooled before it was applied).</summary>
        public static void CancelPending() { Pending = -1; PendingReason = ""; }

        /// <summary>Apply a requested change, if there is one. Call from loading screens and menu close.</summary>
        public static bool ApplyPending(string where)
        {
            if (Pending < 0) return false;
            int tier = Pending;
            Apply(tier, PendingReason + " (applied at " + where + ")");
            return true;
        }

        /// <summary>The player chose this tier: save it, and make it the ceiling automatic systems may not exceed.</summary>
        public static void ApplyPlayerChoice(int tier)
        {
            PlayerPrefs.SetInt(PrefCeiling, Mathf.Clamp(tier, 0, Count - 1));
            Apply(tier, "player", persist: true);
        }

        /// <summary>Immediate step down. Only safe on a loading screen or menu; during play use RequestStepDown.</summary>
        public static void StepDown(string reason = "step down")
        {
            if (Current > Low) Apply(Current - 1, reason);
        }

        public static void StepUp(string reason = "step up")
        {
            if (Current < Mathf.Min(High, Ceiling)) Apply(Current + 1, reason);
        }

        public static string Name(int tier) => tier switch { Low => "Low", Mid => "Medium", _ => "High" };
    }
}
