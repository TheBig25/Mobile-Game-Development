// CMP5402 Mobile Development - Week 6: Graphics & Device Scalability
// QualityMenu: the Graphics dropdown and Battery saver toggle for your settings screen
// ("Let the player choose, and remember it" slide).
//
// Set-up: in your Week 3 settings menu add a TMP_Dropdown (options: Low, Medium, High) and a Toggle. Assign them here.
// Wire the dropdown's On Value Changed to OnDropdownChanged and the toggle's to OnBatterySaverChanged.
// Call ApplyPending from the menu's Close / Back button so the change happens on a screen transition, not mid-play.
//
// Two separate settings are remembered: the player's graphics preference (the ceiling automatic systems may not
// exceed) and the battery saver switch (Low tier + 30 FPS on top of that preference). Turning battery saver off
// goes back to the graphics preference. The dropdown shows the preference, not whatever tier heat has forced.

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CMP5402.Week6
{
    public class QualityMenu : MonoBehaviour
    {
        [SerializeField] TMP_Dropdown graphicsDropdown;
        [SerializeField] Toggle batterySaverToggle;
        [SerializeField] TMP_Text detectedLabel;   // optional: shows "Auto picked: Medium"

        int pendingTier;          // the dropdown value when the menu closes
        bool pendingBatterySaver;
        bool dirty;

        void OnEnable()
        {
            pendingTier = QualityTiers.Saved;              // the player's preference, not the forced tier
            pendingBatterySaver = QualityTiers.BatterySaver;
            dirty = false;
            if (graphicsDropdown != null) graphicsDropdown.SetValueWithoutNotify(pendingTier);
            if (batterySaverToggle != null) batterySaverToggle.SetIsOnWithoutNotify(pendingBatterySaver);
            if (detectedLabel != null)
                detectedLabel.text = QualityBootstrap.LastExplanation
                    + (QualityTiers.Current != QualityTiers.Saved ? "  (running " + QualityTiers.Name(QualityTiers.Current) + " right now)" : "");
        }

        public void OnDropdownChanged(int index) { pendingTier = index; dirty = true; }

        public void OnBatterySaverChanged(bool on) { pendingBatterySaver = on; dirty = true; }

        /// <summary>Call from the menu's Close button. A menu is a safe point for an expensive quality change.</summary>
        public void ApplyPending()
        {
            if (dirty)
            {
                QualityTiers.BatterySaver = pendingBatterySaver;
                QualityTiers.ApplyPlayerChoice(pendingTier);   // saves preference + ceiling, then applies (Low if battery saver)
                dirty = false;
            }
            else
            {
                QualityTiers.ApplyPending("menu close");      // a change heat asked for while we were playing
            }
        }

        /// <summary>Forget the player's choice and detect again (a "Reset to Auto" button).</summary>
        public void ResetToAuto()
        {
            PlayerPrefs.DeleteKey(QualityTiers.PrefKey);
            PlayerPrefs.DeleteKey(QualityTiers.PrefCeiling);
            QualityTiers.BatterySaver = false;
            int tier = DeviceTier.Detect(out string why);
            QualityTiers.Apply(tier, "auto: " + why, persist: true);
            OnEnable();
        }
    }
}
