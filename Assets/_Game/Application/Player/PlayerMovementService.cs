using TPS.Player.Domain;

namespace TPS.Player.Application
{
    /// <summary>玩家移动服务，封装移动相关规则的计算逻辑</summary>
    public sealed class PlayerMovementService
    {
        /// <summary>获取目标移动速度（考虑冲刺/蹲伏）</summary>
        public float GetTargetSpeed(float walkSpeed, float sprintSpeed, float crouchMultiplier, bool sprinting, bool crouching)
        {
            return PlayerMovementRules.GetTargetSpeed(walkSpeed, sprintSpeed, crouchMultiplier, sprinting, crouching);
        }

        /// <summary>判断障碍物高度是否可攀爬</summary>
        public bool IsClimbHeightValid(float obstacleHeight, float characterHeight, float maxHeightMultiplier)
        {
            return PlayerMovementRules.IsClimbHeightValid(obstacleHeight, characterHeight, maxHeightMultiplier);
        }

        /// <summary>根据入墙速度计算墙跑持续时间</summary>
        public float GetWallRunDuration(float entrySpeed, float minSpeed, float maxSpeed, float minDuration, float maxDuration)
        {
            return PlayerMovementRules.CalculateWallRunDuration(entrySpeed, minSpeed, maxSpeed, minDuration, maxDuration);
        }

        /// <summary>根据入墙速度计算墙跑下滑速度</summary>
        public float GetWallSlideSpeed(float entrySpeed, float minSpeed, float maxSpeed, float maxSlideSpeed, float minSlideSpeed)
        {
            return PlayerMovementRules.CalculateWallSlideSpeed(entrySpeed, minSpeed, maxSpeed, maxSlideSpeed, minSlideSpeed);
        }

        public bool ShouldEnterAirborne(
            bool isGrounded,
            bool hasGroundBelow,
            float groundDistance,
            float characterHeight,
            float distanceMultiplier)
        {
            return PlayerMovementRules.ShouldEnterAirborne(
                isGrounded,
                hasGroundBelow,
                groundDistance,
                characterHeight,
                distanceMultiplier);
        }

        public bool ShouldExitAirborne(
            bool isGrounded,
            bool hasGroundBelow,
            float groundDistance,
            float characterHeight,
            float exitDistanceMultiplier)
        {
            return PlayerMovementRules.ShouldExitAirborne(
                isGrounded,
                hasGroundBelow,
                groundDistance,
                characterHeight,
                exitDistanceMultiplier);
        }
    }
}
