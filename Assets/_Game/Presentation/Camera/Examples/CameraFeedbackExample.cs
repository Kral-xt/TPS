using UnityEngine;

namespace TPS.CameraSystem
{
    public sealed class CameraFeedbackExample : MonoBehaviour
    {
        [SerializeField] private CameraFeedbackManager feedbackManager;

        private void Awake()
        {
            feedbackManager ??= CameraFeedbackManager.Resolve();
        }

        public void OnShoot()
        {
            feedbackManager.PlayShoot();
        }

        public void OnPlayerHit()
        {
            feedbackManager.PlayHit();
        }

        public void OnCriticalHit()
        {
            feedbackManager.PlayCritical();
        }

        public void OnDash()
        {
            feedbackManager.PlayDash();
        }

        public void OnLockTarget(Transform target, CameraLockSettings settings)
        {
            feedbackManager.PlayLockTarget(target, settings);
        }
    }
}
