// CMP5402 Mobile Development - Week 6: Graphics & Device Scalability
// ThermalWatcher: react when the OS warns that throttling is coming ("Reacting: the Indexer, or your own tier drop" slide).
//
// Needs the Adaptive Performance package: Project Settings > Adaptive Performance > Enable, then tick a provider
// (Android or Apple). The code is wrapped in a define so the rest of the pack compiles without the package:
// after installing it, add ADAPTIVE_PERFORMANCE_INSTALLED to Project Settings > Player > Scripting Define Symbols.
//
// Two kinds of response, because a full quality-level swap is expensive and belongs on a loading screen:
//   NOW (cheap during play): cap the render scale through RenderScaleController. It only resizes the render target.
//   LATER (at the next scene load or menu close): QualityTiers.Request / RequestStepDown queue a tier change that
//   QualityTiers.ApplyPending carries out at a safe point. Neither is saved as the player's preference.
// When the warning clears it waits several minutes, lifts the cap, cancels any downgrade still queued, and queues one
// step back up, never above the player's chosen ceiling. Queued and applied changes are tracked separately so a
// downgrade queued while hot cannot land after the phone has cooled, and a queued recovery upgrade is cancelled if
// the warning returns before it is applied. The current thermal state is processed on subscribe, not just on the
// next event.
// Test it in the Device Simulator: Control Panel > Adaptive Performance > thermal warning level.

using UnityEngine;
#if ADAPTIVE_PERFORMANCE_INSTALLED
using UnityEngine.AdaptivePerformance;
#endif

namespace CMP5402.Week6
{
    public class ThermalWatcher : MonoBehaviour
    {
        [Tooltip("Render scale ceiling while throttling is imminent, and while throttling.")]
        [SerializeField] float imminentScaleCap = 0.8f, throttlingScaleCap = 0.6f;
        [Tooltip("Minutes without a warning before lifting the cap and stepping back up one tier.")]
        [SerializeField] float recoverAfterMinutes = 3f;
        [SerializeField] RenderScaleController renderScale;   // optional but recommended

        public string Status { get; private set; } = "Adaptive Performance not installed";

#if ADAPTIVE_PERFORMANCE_INSTALLED
        IAdaptivePerformance ap;
        float clearSince = -1f;
        bool requestedDown;      // a heat downgrade has been asked for (queued or applied) since the last full recovery
        bool downThisEpisode;    // one step-down per warning episode; reset when the warning clears
        int tierBeforeHeat = -1; // the tier that was running when heat first forced a change

        // A queued step UP from a previous recovery must not land while the phone is hot again.
        static void CancelQueuedHeatUpgrade()
        {
            if (QualityTiers.Pending > QualityTiers.Current && QualityTiers.PendingReason.StartsWith("heat"))
                QualityTiers.CancelPending();
        }

        void OnTierApplied(int tier, string reason)
        {
            // One of our requests has been carried out. We are "down" until we're back at the pre-heat tier.
            if (reason.StartsWith("heat")) requestedDown = tierBeforeHeat >= 0 && QualityTiers.Current < tierBeforeHeat;
        }

        void Start()
        {
            ap = Holder.Instance;
            if (ap == null || !ap.Active) { Status = "Adaptive Performance inactive"; enabled = false; return; }
            if (renderScale == null) renderScale = FindFirstObjectByType<RenderScaleController>();
            ap.ThermalStatus.ThermalEvent += OnThermal;
            QualityTiers.Changed += OnTierApplied;
            OnThermal(ap.ThermalStatus.ThermalMetrics);   // the phone may already be warm when we start
        }

        void OnDestroy()
        {
            if (ap != null) ap.ThermalStatus.ThermalEvent -= OnThermal;
            QualityTiers.Changed -= OnTierApplied;
            if (renderScale != null) renderScale.ThermalCap = 1f;
            if (requestedDown) QualityTiers.CancelPending();
        }

        void OnThermal(ThermalMetrics m)
        {
            Status = "thermal: " + m.WarningLevel + " level " + m.TemperatureLevel.ToString("F2");
            switch (m.WarningLevel)
            {
                case WarningLevel.ThrottlingImminent:
                    CancelQueuedHeatUpgrade();                                                   // hot again: no step up
                    if (renderScale != null) renderScale.ThermalCap = imminentScaleCap;          // now
                    if (!downThisEpisode)                                                        // later, one tier per episode
                    {
                        if (tierBeforeHeat < 0) tierBeforeHeat = QualityTiers.Current;
                        QualityTiers.RequestStepDown("heat: throttling imminent");
                        requestedDown = true;
                        downThisEpisode = true;
                    }
                    clearSince = -1f;
                    break;
                case WarningLevel.Throttling:
                    CancelQueuedHeatUpgrade();
                    if (renderScale != null) renderScale.ThermalCap = throttlingScaleCap;        // now
                    if (tierBeforeHeat < 0) tierBeforeHeat = QualityTiers.Current;
                    QualityTiers.Request(QualityTiers.Low, "heat: throttling");                  // later
                    requestedDown = true;
                    downThisEpisode = true;
                    clearSince = -1f;
                    break;
                default:
                    downThisEpisode = false;                   // the episode is over; a new warning may step down again
                    if (clearSince < 0f) clearSince = Time.unscaledTime;
                    break;
            }
        }

        void Update()
        {
            // Recover slowly: lift the cap, drop any downgrade still queued, then queue one tier up, minutes apart,
            // never above the player's ceiling or the tier we started from.
            if (clearSince >= 0f && Time.unscaledTime - clearSince > recoverAfterMinutes * 60f)
            {
                if (renderScale != null) renderScale.ThermalCap = 1f;
                if (QualityTiers.Pending >= 0 && QualityTiers.Pending < QualityTiers.Current)
                    QualityTiers.CancelPending();                       // cooled before the downgrade was applied

                int limit = Mathf.Min(QualityTiers.High, QualityTiers.Ceiling, tierBeforeHeat < 0 ? QualityTiers.High : tierBeforeHeat);
                if (requestedDown && QualityTiers.Current < limit)
                    QualityTiers.Request(QualityTiers.Current + 1, "heat cleared");
                else { requestedDown = false; tierBeforeHeat = -1; }  // fully recovered
                clearSince = Time.unscaledTime;
            }
        }
#endif
    }
}
