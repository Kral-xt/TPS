namespace TPS.Player.Domain
{
    /// <summary>玩家移动规则静态类，提供纯函数计算移动参数</summary>
    public static class PlayerMovementRules
    {
        /// <summary>计算目标速度（冲刺/蹲伏修正）</summary>
        public static float GetTargetSpeed(float walkSpeed, float sprintSpeed, float crouchMultiplier, bool sprinting, bool crouching)
        {
            float speed = sprinting ? sprintSpeed : walkSpeed;
            return crouching ? speed * crouchMultiplier : speed;
        }

        /// <summary>判断障碍物高度是否在可攀爬范围内</summary>
        public static bool IsClimbHeightValid(float obstacleHeight, float characterHeight, float maxHeightMultiplier)
        {
            return obstacleHeight > 0.1f && obstacleHeight <= characterHeight * maxHeightMultiplier;
        }

        /// <summary>根据速度插值计算墙跑持续时间</summary>
        public static float CalculateWallRunDuration(float entrySpeed, float minSpeed, float maxSpeed, float minDuration, float maxDuration)
        {
            return Lerp(minDuration, maxDuration, InverseLerp(minSpeed, maxSpeed, entrySpeed));
        }

        /// <summary>根据速度插值计算墙跑下滑速度</summary>
        public static float CalculateWallSlideSpeed(float entrySpeed, float minSpeed, float maxSpeed, float maxSlideSpeed, float minSlideSpeed)
        {
            return Lerp(maxSlideSpeed, minSlideSpeed, InverseLerp(minSpeed, maxSpeed, entrySpeed));
        }

        /// <summary>判断非主动跳跃状态是否满足进入稳定滞空的距离条件。</summary>
        public static bool ShouldEnterAirborne(
            bool isGrounded,
            bool hasGroundBelow,
            float groundDistance,
            float characterHeight,
            float distanceMultiplier)
        {
            if (isGrounded)
            {
                return false;
            }

            return !hasGroundBelow
                || groundDistance > characterHeight * distanceMultiplier;
        }

        /// <summary>使用更低的退出阈值形成滞回，避免临界高度反复切换。</summary>
        public static bool ShouldExitAirborne(
            bool isGrounded,
            bool hasGroundBelow,
            float groundDistance,
            float characterHeight,
            float exitDistanceMultiplier)
        {
            return isGrounded
                || (hasGroundBelow
                    && groundDistance < characterHeight * exitDistanceMultiplier);
        }

        private static float InverseLerp(float from, float to, float value)
        {
            if (from == to) return 0f;
            float factor = (value - from) / (to - from);
            return factor < 0f ? 0f : factor > 1f ? 1f : factor;
        }

        private static float Lerp(float from, float to, float factor)
        {
            return from + (to - from) * factor;
        }
    }
}
