using UnityEngine;

namespace PunchcardOperator
{
    /// <summary>App-wide runtime setup. No scene object required.</summary>
    internal static class Boot
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            // Touch the save so first launch writes a file with safe defaults.
            _ = SaveSystem.Data;
        }
    }
}
