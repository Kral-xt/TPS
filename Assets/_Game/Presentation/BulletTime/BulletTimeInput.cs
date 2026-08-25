using TPS.BulletTime.Application;
using TPS.Player.Application;
using UnityEngine;

namespace TPS.BulletTime.Presentation
{
    [DisallowMultipleComponent]
    public sealed class BulletTimeInput : MonoBehaviour
    {
        private BulletTimeController controller;
        private PlayerHealthController playerHealth;

        private void Awake()
        {
            controller = GetComponent<BulletTimeController>();
        }

        private void Start()
        {
            FindPlayerHealth();
        }

        private void FindPlayerHealth()
        {
            if (playerHealth != null) return;
            var player = Object.FindFirstObjectByType<TPS.Player.TpsPrototypePlayerController>();
            if (player != null)
            {
                playerHealth = player.GetComponent<PlayerHealthController>();
            }
        }

        private void Update()
        {
            if (!TPS.Player.Presentation.PlayerInputGate.IsGameplay)
            {
                return;
            }

            FindPlayerHealth();
            if (playerHealth != null && playerHealth.IsDead)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                controller?.Toggle();
            }
        }
    }
}
