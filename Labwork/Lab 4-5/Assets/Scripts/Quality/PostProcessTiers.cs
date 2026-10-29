// CMP5402 Mobile Development - Week 6: Graphics & Device Scalability
// PostProcessTiers: the post-processing row of the tier table ("Three tiers for a mobile game" slide).
//
// Post-processing is NOT part of the URP asset, so swapping quality levels doesn't change it. In URP it comes from
// the Volume framework: a Volume component in the scene with a Volume Profile asset, plus the Post Processing tick
// on the camera. This script keeps one profile per tier and assigns the right one when the tier changes.
//
// Set-up:
//   1. Create two Volume Profiles (Create > Volume Profile): PP_Mid (Bloom, High Quality Filtering off) and
//      PP_High (Bloom, Vignette, Color Adjustments). Low needs no profile: post-processing is switched off on the camera.
//   2. Add a global Volume to the scene and drag this script next to it. Assign the Volume and the profiles
//      (leave the Low slot empty).
//   3. On the camera, tick Post Processing. On the Low tier the script turns it off entirely.
// Unity notes post-processing effects can take up a lot of frame time, and lists Bloom (without High Quality
// Filtering), Vignette and Color Grading among the more mobile-friendly effects.

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CMP5402.Week6
{
    public class PostProcessTiers : MonoBehaviour
    {
        [SerializeField] Volume volume;
        [SerializeField] Camera targetCamera;
        [Tooltip("Index 0 = Low, 1 = Medium, 2 = High. Leave Low empty to disable post-processing entirely.")]
        [SerializeField] VolumeProfile[] profiles = new VolumeProfile[3];

        void OnEnable()
        {
            QualityTiers.Changed += OnTier;
            if (QualityTiers.Current >= 0) OnTier(QualityTiers.Current, "enable");
        }

        void OnDisable() => QualityTiers.Changed -= OnTier;

        void OnTier(int tier, string reason)
        {
            bool postProcessing = tier != QualityTiers.Low;   // explicit: Low never renders post-processing
            VolumeProfile profile = postProcessing && tier < profiles.Length ? profiles[tier] : null;
            if (volume != null) volume.profile = profile;

            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera != null)
            {
                var data = targetCamera.GetUniversalAdditionalCameraData();
                if (data != null) data.renderPostProcessing = postProcessing && profile != null;
            }
        }
    }
}
