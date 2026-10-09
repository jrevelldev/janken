using UnityEngine;

namespace Janken.Controllers
{
    /// <summary>
    /// Controller for Display 3 (Vertical 1080x1920 Audience Stage Display).
    /// Inherits all tournament view rendering, video playback, referee cutout animations,
    /// and stinger transition capabilities from Display2Controller.
    /// </summary>
    public class Display3Controller : Display2Controller
    {
        private void Awake()
        {
            // Display 3 is the vertical secondary display.
            // Mute video and stinger audio only if Display 2 is active in hierarchy.
            Display2Controller d2 = UnityEngine.Object.FindFirstObjectByType<Display2Controller>();
            if (d2 != null && d2 != this && d2.gameObject.activeInHierarchy)
            {
                muteVideoAudio = true;
            }
            else
            {
                muteVideoAudio = false;
            }
        }
    }
}
