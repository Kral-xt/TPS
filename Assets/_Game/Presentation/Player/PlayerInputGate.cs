using System;
using UnityEngine;

namespace TPS.Player.Presentation
{
    public enum PlayerInputMode
    {
        Gameplay,
        UI
    }

    public static class PlayerInputGate
    {
        public static PlayerInputMode Mode { get; private set; } = PlayerInputMode.Gameplay;

        public static event Action<PlayerInputMode> ModeChanged;

        public static bool IsGameplay => Mode == PlayerInputMode.Gameplay;
        public static bool IsUI => Mode == PlayerInputMode.UI;

        public static void SetMode(PlayerInputMode mode)
        {
            if (Mode == mode)
            {
                return;
            }

            Mode = mode;
            ModeChanged?.Invoke(mode);
        }

        public static void SetGameplay()
        {
            SetMode(PlayerInputMode.Gameplay);
        }

        public static void SetUI()
        {
            SetMode(PlayerInputMode.UI);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            Mode = PlayerInputMode.Gameplay;
            ModeChanged = null;
        }
    }
}
