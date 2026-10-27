using UnityEngine;

namespace SafeVR.Player
{
    /// <summary>
    /// Debug helper to force mobile HUD in Editor for testing.
    /// </summary>
    public static class MobileHUDDebug
    {
        public static bool ForceMobileHUDInEditor = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            ForceMobileHUDInEditor = false;
        }
    }
}