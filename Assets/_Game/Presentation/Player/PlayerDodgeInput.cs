using TPS.Player.Application;
using UnityEngine;

namespace TPS.Player.Presentation
{
    [DefaultExecutionOrder(-300)]
    [DisallowMultipleComponent]
    public sealed class PlayerDodgeInput : MonoBehaviour
    {
        private PlayerDodgeController dodgeController;
        private PlayerHealthController health;
        private Camera gameCamera;

        private void Awake()
        {
            dodgeController = GetComponent<PlayerDodgeController>();
            health = GetComponent<PlayerHealthController>();
        }

        private void Update()
        {
            if (health != null && health.IsDead) return;
            if (!PlayerInputGate.IsGameplay
                || !Input.GetKeyDown(KeyCode.LeftAlt)
                || dodgeController == null)
            {
                return;
            }

            Vector2 input = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical"));
            Vector3 direction;
            if (input.sqrMagnitude >= 0.01f)
            {
                gameCamera ??= Camera.main;
                Transform cameraTransform = gameCamera != null ? gameCamera.transform : transform;
                Vector3 cameraForward =
                    Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
                Vector3 cameraRight =
                    Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
                direction = cameraForward * input.y + cameraRight * input.x;
            }
            else
            {
                direction = transform.forward;
            }

            dodgeController.TryStartDodge(direction);
        }
    }
}
