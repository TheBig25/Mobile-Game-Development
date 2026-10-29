// CMP5402 Mobile Development - Week 6: Graphics & Device Scalability
// QualityBootstrap: runs once at launch, before the first scene, and applies the saved or detected tier
// ("The life of a quality setting" slide, steps 1 to 3).
//
// Nothing to add to a scene: RuntimeInitializeOnLoadMethod runs it automatically. It also appears in the Console so
// you can see which tier was picked and why (useful evidence for the lab report).

using UnityEngine;

namespace CMP5402.Week6
{
    public static class QualityBootstrap
    {
        public static string LastExplanation { get; private set; } = "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Run()
        {
            int tier;
            bool firstRun = !QualityTiers.HasSaved;
            if (!firstRun)
            {
                tier = QualityTiers.Saved;
                LastExplanation = "saved tier " + QualityTiers.Name(tier);
            }
            else
            {
                tier = DeviceTier.Detect(out string why);   // first run: detect once, then it's saved
                LastExplanation = "detected: " + why;
            }
            QualityTiers.Apply(tier, "bootstrap", persist: firstRun);
            Debug.Log("[Quality] " + LastExplanation + " on " + SystemInfo.deviceModel);
        }
    }
}
