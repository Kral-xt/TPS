using UnityEngine;

namespace TPS.Player.Infrastructure
{
    [CreateAssetMenu(fileName = "PlayerConfig", menuName = "TPS/Player/Player Config")]
    public sealed class PlayerConfig : ScriptableObject
    {


        [Header("基础属性")]
        [Min(1f)] public float MaxHp = 100f;
        [Min(1)] public int InitialLevel = 1;
        [Min(1)] public int InitialRequiredExp = 100;
        [Min(1f)] public float ExpGrowthRate = 1.1f;
        [Range(0f, 1f)] public float CriticalRate;
        [Min(0f)] public float CriticalDamage;

        [Header("地面移动")]
        [Min(0f)] public float MoveSpeed = 7f;
        [Min(0f)] public float SprintSpeed = 12f;
        [Min(0f)] public float Acceleration = 18f;
        [Min(0f)] public float Deceleration = 22f;
        [Min(0f)] public float RotationSpeed = 18f;
        [Range(0f, 1f)] public float SprintTurnControl = 0.65f;

        [Header("跳跃与重力")]
        [Min(0f)] public float JumpHeight = 2f;
        public bool EnableDoubleJump = true;
        [Min(0)] public int MaxAirJumpCount = 1;
        [Min(0f)] public float DoubleJumpHeight = 1.8f;
        public float Gravity = -16f;
        public float GroundedStickForce = -2f;
        [Min(0.1f)] public float AirborneDistanceMultiplier = 1f;
        [Min(0f)] public float AirborneEnterDelay = 0.08f;
        [Range(0.1f, 1f)] public float AirborneExitDistanceMultiplier = 0.8f;
        [Min(0f)] public float GroundProbeRadius;
        public LayerMask GroundLayerMask = ~0;
        [Min(0f)] public float CoyoteTime = 0.12f;

        [Header("动画")]
        [Min(0)] public int WalkLayer = 1;
        [Min(0)] public int SprintLayer = 2;
        [Min(0f)] public float AnimationDampTime = 0.08f;
        [Min(0f)] public float LayerBlendSpeed = 8f;
        [Min(0f)] public float JumpParameterHoldTime = 0.08f;

        [Header("蹲伏与滑铲")]
        public KeyCode CrouchKey = KeyCode.C;
        [Min(0.1f)] public float CrouchHeight = 1f;
        public float CrouchCenterY = 0.5f;
        [Range(0f, 1f)] public float CrouchMoveSpeedMultiplier = 0.5f;
        [Min(0f)] public float CrouchTransitionSpeed = 15f;
        [Min(0f)] public float StandUpCheckRadius = 0.5f;
        [Min(0)] public int CrouchLayer = 3;
        [Min(0f)] public float SlideSpeedThreshold = 7f;
        [Min(0f)] public float SlideDeceleration = 8f;
        [Min(0f)] public float SlideEndSpeed = 1f;
        [Min(0f)] public float SlideMinimumSpeed = 9.5f;
        [Min(0)] public int SlideLayer = 4;
        [Min(0f)] public float SlideInitialBoostMultiplier = 1.2f;
        public LayerMask StandUpBlockMask = ~0;
        public bool DebugStandUpChecks;

        [Header("攀爬")]
        public LayerMask ClimbObstacleMask = ~0;
        [Min(0f)] public float ClimbCheckDistance = 1f;
        [Min(0f)] public float ClimbMaxHeightMultiplier = 1.5f;
        [Min(0f)] public float ClimbProbeRadius = 0.3f;
        [Min(0f)] public float ClimbForwardOffset = 0.45f;
        [Min(0.01f)] public float ClimbDuration = 1.1f;
        public string ClimbTag = "Climbable";
        public bool DebugClimbChecks;

        [Header("普通跨越")]
        public bool EnableVault = true;
        [Min(0f)] public float VaultCheckDistance = 1.2f;
        [Min(0f)] public float VaultMaxHeightRatio = 0.5f;
        [Min(0f)] public float VaultMaxWidthMultiplier = 3.5f;
        [Min(0.01f)] public float VaultDuration = 0.25f;
        [Min(0f)] public float VaultForwardOffset = 0.75f;
        [Min(0f)] public float VaultHeightOffset = 0.05f;
        public LayerMask VaultLayerMask = ~0;
        public bool VaultRequireGrounded = true;

        [Header("前空翻跨越")]
        public bool EnableFrontFlipVault = true;
        [Min(0f)] public float FrontFlipMaxObstacleHeight = 1.8f;
        [Min(0f)] public float FrontFlipMaxObstacleWidth = 3f;
        [Min(0f)] public float FrontFlipCheckDistance = 1.5f;
        [Min(0f)] public float FrontFlipTriggerRadius = 0.8f;
        [Min(0.01f)] public float FrontFlipDuration = 0.4f;
        [Min(0f)] public float FrontFlipForwardOffset = 0.6f;
        [Min(0f)] public float FrontFlipHeightOffset = 0.8f;
        public LayerMask FrontFlipLayerMask = ~0;

        [Header("蹬墙跑")]
        public bool EnableWallRun = true;
        public string WallTag = "Wall";
        [Min(0f)] public float WallCheckDistance = 0.8f;
        [Min(0f)] public float WallCheckRadius = 0.35f;
        public LayerMask WallLayerMask = ~0;
        [Min(0f)] public float MinWallRunSpeed = 4f;
        [Min(0f)] public float MaxWallRunSpeed = 12f;
        [Min(0f)] public float MinWallRunDuration = 0.35f;
        [Min(0f)] public float MaxWallRunDuration = 1.25f;
        [Min(0f)] public float MinWallSlideSpeed = 0.15f;
        [Min(0f)] public float MaxWallSlideSpeed = 2.2f;
        [Min(0f)] public float WallRunSpeedMultiplier = 1f;
        [Min(0f)] public float WallRunStickForce = 3f;
        [Min(0f)] public float WallJumpHorizontalForce = 8f;
        [Min(0f)] public float WallJumpVerticalForce = 8f;
        [Min(0f)] public float WallRunExitCooldown = 0.25f;
        public bool RequireForwardInputForWallRun = true;
        public float WallRunInitialRiseSpeed = 2.2f;
        [Min(0f)] public float WallRunRiseDuration = 0.18f;

        [Header("闪避")]
        [Min(0f)] public float DodgeDistance = 2.5f;
        [Min(0.01f)] public float DodgeDuration = 0.15f;
        [Min(0f)] public float DodgeCooldown = 0.75f;
        [Min(0f)] public float DodgeInvincibleDuration = 0.18f;
        [Min(0f)] public float DodgeStaminaCost;
        [Min(0f)] public float DodgeCollisionSkin = 0.05f;
        public LayerMask DodgeCollisionMask = ~0;

        [Header("精准闪避")]
        [Min(0f)] public float DodgeTriggerRange = 2.5f;
        [Min(0f)] public float PerfectDodgeWindow = 0.35f;
        [Min(1f)] public float PerfectDodgeRangeMultiplier = 1.5f;
        [Range(0f, 180f)] public float PerfectDodgeAngleTolerance = 30f;
        [Min(0f)] public float PerfectDodgeBulletTimeEnergy = 50f;

        [Header("残影")]
        [Min(0.01f)] public float AfterimageSpawnInterval = 0.03f;
        [Min(0.01f)] public float AfterimageLifetime = 0.2f;
        [Range(0f, 1f)] public float AfterimageStartAlpha = 0.6f;
        public Color AfterimageColor = new Color(0.1f, 0.55f, 1f, 1f);
        [Min(1)] public int AfterimageInitialCapacity = 12;
        [Min(1)] public int AfterimageMaxCapacity = 32;
        public bool AfterimageScaleDiagnostics;

        [Header("子弹时间残影")]
        [Min(0.01f)] public float BulletTimeAfterimageInterval = 0.08f;
        [Min(0.01f)] public float BulletTimeAfterimageLifetime = 0.35f;
        [Min(0f)] public float BulletTimeAfterimageMinMoveDistance = 0.03f;
        [Min(0f)] public float BulletTimeAfterimageMinRotationAngle = 2f;

        private void OnValidate()
        {
            MaxHp = Mathf.Max(1f, MaxHp);
            InitialLevel = Mathf.Max(1, InitialLevel);
            InitialRequiredExp = Mathf.Max(1, InitialRequiredExp);
            ExpGrowthRate = Mathf.Max(1f, ExpGrowthRate);
            CriticalRate = Mathf.Clamp01(CriticalRate);
            CriticalDamage = Mathf.Max(0f, CriticalDamage);
            Gravity = Mathf.Min(-0.01f, Gravity);
            GroundedStickForce = Mathf.Min(0f, GroundedStickForce);
            MaxAirJumpCount = Mathf.Max(0, MaxAirJumpCount);
            MaxWallRunSpeed = Mathf.Max(MinWallRunSpeed, MaxWallRunSpeed);
            MaxWallRunDuration = Mathf.Max(MinWallRunDuration, MaxWallRunDuration);
            MaxWallSlideSpeed = Mathf.Max(MinWallSlideSpeed, MaxWallSlideSpeed);
            AfterimageMaxCapacity = Mathf.Max(AfterimageInitialCapacity, AfterimageMaxCapacity);
        }
    }
}
