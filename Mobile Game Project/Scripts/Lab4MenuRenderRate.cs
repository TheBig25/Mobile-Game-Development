using UnityEngine;
using UnityEngine.Rendering;

namespace CMP5402.Week4
{
    // Optional example. Wire public methods to your own menu's buttons/events.
    // Do not attach this alongside another controller which owns the same global settings.
    public class Lab4MenuRenderRate : MonoBehaviour
    {
        int previousTarget, previousInterval;
        bool menuActive;

        public void EnterMenu()
        {
            if (menuActive) return;
            previousTarget = Application.targetFrameRate;
            previousInterval = OnDemandRendering.renderFrameInterval;
            Application.targetFrameRate = 60;
            OnDemandRendering.renderFrameInterval = 3;
            menuActive = true;
            // About 20 rendered FPS IF the player loop sustains 60 updates/s.
            // Update/input are not slowed by renderFrameInterval alone.
        }

        public void LeaveMenu()
        {
            if (!menuActive) return;
            Application.targetFrameRate = previousTarget;
            OnDemandRendering.renderFrameInterval = previousInterval;
            menuActive = false;
        }

        void OnDisable() => LeaveMenu();
    }
}
