using UnityEngine;

namespace TPS.CameraSystem
{
    [CreateAssetMenu(
        fileName = "CameraFeedbackProfile",
        menuName = "TPS/镜头反馈配置")]
    public sealed class CameraFeedbackProfile : ScriptableObject
    {
        [Header("射击")]
        public CameraFeedbackPreset shoot;

        [Header("受击")]
        public CameraFeedbackPreset hit;

        [Header("暴击")]
        public CameraFeedbackPreset critical;

        [Header("冲刺")]
        public CameraFeedbackPreset dash;

        public CameraFeedbackPreset GetPreset(CameraFeedbackType type)
        {
            switch (type)
            {
                case CameraFeedbackType.Shoot:
                    return shoot;
                case CameraFeedbackType.Hit:
                    return hit;
                case CameraFeedbackType.Critical:
                    return critical;
                case CameraFeedbackType.Dash:
                    return dash;
                default:
                    return default;
            }
        }
    }
}
