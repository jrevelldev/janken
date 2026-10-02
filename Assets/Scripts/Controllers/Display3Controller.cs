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
        // Display3 inherits all functionality of Display2Controller automatically.
        // It binds to Display3Manager.uxml and Display3Manager.uss via UIDocument on its GameObject.
    }
}
