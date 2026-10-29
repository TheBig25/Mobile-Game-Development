// CMP5402 Mobile Development - Week 6: Graphics & Device Scalability
// DeviceTier: pick a starting quality tier from what the phone reports ("What the phone tells you about itself" slide).
//
// Unity's own example picks a level from SystemInfo.graphicsMemorySize. Phones share one pool of memory between CPU
// and GPU, so that number is approximate. This script scores several signals and rounds down, then lets a small list
// of known device models override the result for phones you have actually tested.
//
// The thresholds are deliberately conservative starting values. Tune them from your Lab 6 measurements.

using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace CMP5402.Week6
{
    public static class DeviceTier
    {
        [Serializable]
        public struct KnownDevice
        {
            public string modelContains;   // matched against SystemInfo.deviceModel, case-insensitive
            public int tier;
        }

        /// <summary>Optional overrides for phones you've tested. Fill from a ScriptableObject or leave empty.</summary>
        public static KnownDevice[] Known = Array.Empty<KnownDevice>();

        public static int Detect(out string explanation)
        {
            string model = SystemInfo.deviceModel ?? "";
            foreach (var k in Known)
                if (!string.IsNullOrEmpty(k.modelContains) && model.IndexOf(k.modelContains, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    explanation = "known device: " + model;
                    return Mathf.Clamp(k.tier, 0, QualityTiers.Count - 1);
                }

            int score = 0;
            var notes = new System.Text.StringBuilder();

            int ram = SystemInfo.systemMemorySize;          // MB
            if (ram >= 6000) { score++; notes.Append("RAM ").Append(ram).Append(" MB (+1) "); }
            if (ram >= 10000) { score++; notes.Append("(+1) "); }

            int cores = SystemInfo.processorCount;
            if (cores >= 8) { score++; notes.Append("cores ").Append(cores).Append(" (+1) "); }

            if (SystemInfo.supportsComputeShaders) { score++; notes.Append("compute (+1) "); }

            var api = SystemInfo.graphicsDeviceType;
            bool modernApi = api == GraphicsDeviceType.Vulkan || api == GraphicsDeviceType.Metal;
            if (modernApi) { score++; notes.Append(api).Append(" (+1) "); }

            // Five possible points. 4 or more = High, 2 or 3 = Mid, else Low.
            // Round down: a wrong Low is slow, a wrong High is unplayable. This is a heuristic, not a GPU benchmark.
            int tier = score >= 4 ? QualityTiers.High : score >= 2 ? QualityTiers.Mid : QualityTiers.Low;
            explanation = "score " + score + "/5: " + notes.ToString().Trim() + " -> " + QualityTiers.Name(tier);
            return tier;
        }
    }
}
