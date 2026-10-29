// CMP5402 Mobile Development - Week 6: Graphics & Device Scalability
// RenderScaleController: trims URP Render Scale while frames run long and raises it again when there's headroom
// ("Dynamic scaling" slide). Add to one GameObject that lives for the whole game.
//
// How it decides: a smoothed frame interval is compared with the target interval (from Application.targetFrameRate,
// so 33.3 ms on the LOW tier and 16.7 ms on MID and HIGH). More than 'tolerance' over it: step the scale down.
// On or under the target (a frame-rate-capped game sits right on it): step back up. Then wait 'decisionInterval'
// before deciding again, so it doesn't flap.
//
// What it cannot tell: frame interval includes CPU time. If the game is CPU-bound, lowering resolution does nothing
// (Week 4: check Highlights). The overlay shows the current scale so testers can see when it moves.
//
// Editor warning: UniversalRenderPipeline.asset is the asset on disk, so changes in the Editor are saved into it.
// Every asset this script touches (one per tier) is remembered and restored in OnDisable. In a player the change
// lasts for the session only.
//
// Thermal cap: ThermalWatcher can lower the ceiling while the phone is hot. Changing render scale only resizes the
// render target, so it is far cheaper than a full quality-level swap and is the immediate response to heat.
// The cap is enforced against the asset's real value every frame, separately from the frame-time decision.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CMP5402.Week6
{
    public class RenderScaleController : MonoBehaviour
    {
        [SerializeField, Range(0.5f, 1f)] float minScale = 0.6f;
        [SerializeField, Range(0.5f, 1f)] float maxScale = 1f;
        [SerializeField] float step = 0.05f;
        [Tooltip("Seconds between decisions. Longer = calmer.")]
        [SerializeField] float decisionInterval = 2f;
        [Tooltip("Frame interval may exceed the target by this fraction before we step down.")]
        [SerializeField] float tolerance = 0.1f;
        [SerializeField] bool enabledOnLowTierOnly = false;

        public float CurrentScale { get; private set; } = 1f;
        public float SmoothedMs => smoothed * 1000f;
        public string LastReason { get; private set; } = "";

        /// <summary>An extra ceiling, lowered by ThermalWatcher while the phone is hot. 1 = no cap.</summary>
        public float ThermalCap { get; set; } = 1f;

        float smoothed;
        float nextDecision;
        readonly Dictionary<UniversalRenderPipelineAsset, float> originals = new Dictionary<UniversalRenderPipelineAsset, float>();

        float EffectiveMax => Mathf.Min(maxScale, ThermalCap);

        void OnEnable()
        {
            if (!Remember(UniversalRenderPipeline.asset)) { enabled = false; return; }
            EnforceCap();
            smoothed = 1f / Mathf.Max(30, Application.targetFrameRate);
            nextDecision = Time.unscaledTime + decisionInterval;
            QualityTiers.Changed += OnTierChanged;
        }

        void OnDisable()
        {
            QualityTiers.Changed -= OnTierChanged;
            foreach (var pair in originals)              // leave every asset as we found it
                if (pair.Key != null) pair.Key.renderScale = pair.Value;
            originals.Clear();
        }

        // Record an asset's original scale the first time we see it. Does not touch CurrentScale.
        bool Remember(UniversalRenderPipelineAsset urp)
        {
            if (urp == null) return false;
            if (!originals.ContainsKey(urp)) originals[urp] = urp.renderScale;
            return true;
        }

        // Write a scale to the active asset and report it. The asset's value is the truth; CurrentScale mirrors it.
        void SetScale(float scale)
        {
            var urp = UniversalRenderPipeline.asset;
            if (!Remember(urp)) return;
            scale = Mathf.Clamp(scale, minScale, EffectiveMax);
            if (!Mathf.Approximately(urp.renderScale, scale)) urp.renderScale = scale;
            CurrentScale = urp.renderScale;
        }

        // Every frame, independently of the frame-time decision: the real asset value must respect the cap,
        // and CurrentScale must report the real value (a tier change swaps the asset under us).
        void EnforceCap()
        {
            var urp = UniversalRenderPipeline.asset;
            if (!Remember(urp)) return;
            float clamped = Mathf.Clamp(urp.renderScale, minScale, EffectiveMax);
            if (!Mathf.Approximately(urp.renderScale, clamped)) { urp.renderScale = clamped; LastReason = "cap " + EffectiveMax.ToString("0.00"); }
            CurrentScale = urp.renderScale;
        }

        // A new tier brings its own URP asset: record it, enforce the cap on it, and pause decisions for a moment.
        void OnTierChanged(int tier, string reason)
        {
            EnforceCap();
            nextDecision = Time.unscaledTime + decisionInterval;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            smoothed += (dt - smoothed) * 0.1f;

            EnforceCap();   // before any early return

            if (Time.unscaledTime < nextDecision) return;
            if (enabledOnLowTierOnly && QualityTiers.Current != QualityTiers.Low) return;

            int target = Application.targetFrameRate > 0 ? Application.targetFrameRate : 60;
            float targetInterval = 1f / target;
            float scale = CurrentScale;

            if (smoothed > targetInterval * (1f + tolerance)) { scale -= step; LastReason = "slow: " + SmoothedMs.ToString("F1") + " ms"; }
            else if (smoothed <= targetInterval * 1.03f) { scale += step; LastReason = "on target: " + SmoothedMs.ToString("F1") + " ms"; }
            else { nextDecision = Time.unscaledTime + decisionInterval; return; }

            SetScale(scale);
            nextDecision = Time.unscaledTime + decisionInterval;
        }
    }
}
