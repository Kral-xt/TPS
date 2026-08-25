using UnityEngine;
using TPS.Player.Application;
using TPS.Player.Presentation;
using TPS.Combat.Domain;
using TPS.Player.Infrastructure;

namespace TPS.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class TpsPrototypePlayerController : MonoBehaviour
    {
        // 角色引用：相机用于计算相对移动方向，Animator用于驱动动作层和参数。
        [Header("References")]
        [SerializeField] private Transform cameraRoot; [SerializeField] private Animator animator;
        // 地面移动参数：包含普通移动、冲刺、加减速和转向控制。

        [SerializeField] private PlayerConfig config;
        private float moveSpeed;
        private float sprintSpeed;
        private float acceleration;
        private float deceleration;
        private float rotationSpeed;
        private float sprintTurnControl;
        private float jumpHeight;
        private bool enableDoubleJump;
        private int maxAirJumpCount;
        private float doubleJumpHeight;
        private float gravity;
        private float groundedStickForce;
        private float airborneDistanceMultiplier;
        private float airborneEnterDelay;
        private float airborneExitDistanceMultiplier;
        private float groundProbeRadius;
        private LayerMask groundLayerMask;
        private float coyoteTime;
        private int walkLayer;
        private int sprintLayer;
        private float animationDampTime;
        private float layerBlendSpeed;
        private float jumpParameterHoldTime;
        private KeyCode crouchKey;
        private float crouchHeight;
        private float crouchCenterY;
        private float crouchMoveSpeedMultiplier;
        private float crouchTransitionSpeed;
        private float standUpCheckRadius;
        private int crouchLayer;
        private float slideSpeedThreshold;
        private float slideDeceleration;
        private float slideEndSpeed;
        private float slideMinimumSpeed;
        private int slideLayer;
        private float slideInitialBoostMultiplier;
        private LayerMask climbObstacleMask;
        private float climbCheckDistance;
        private float climbMaxHeightMultiplier;
        private float climbProbeRadius;
        private float climbForwardOffset;
        private float climbDuration;
        private string climbTag;
        private LayerMask standUpBlockMask;
        private bool debugClimbChecks;
        private bool debugStandUpChecks;
        private bool enableVault;
        private float vaultCheckDistance;
        private float vaultMaxHeightRatio;
        private float vaultMaxWidthMultiplier;
        private float vaultDuration;
        private float vaultForwardOffset;
        private float vaultHeightOffset;
        private LayerMask vaultLayerMask;
        private bool vaultRequireGrounded;
        private bool enableFrontFlipVault;
        private float frontFlipMaxObstacleHeight;
        private float frontFlipMaxObstacleWidth;
        private float frontFlipCheckDistance;
        private float frontFlipTriggerRadius;
        private float frontFlipDuration;
        private float frontFlipForwardOffset;
        private float frontFlipHeightOffset;
        private LayerMask frontFlipLayerMask;
        private bool enableWallRun;
        private string wallTag;
        private float wallCheckDistance;
        private float wallCheckRadius;
        private LayerMask wallLayerMask;
        private float minWallRunSpeed;
        private float maxWallRunSpeed;
        private float minWallRunDuration;
        private float maxWallRunDuration;
        private float minWallSlideSpeed;
        private float maxWallSlideSpeed;
        private float wallRunSpeedMultiplier;
        private float wallRunStickForce;
        private float wallJumpHorizontalForce;
        private float wallJumpVerticalForce;
        private float wallRunExitCooldown;
        private bool requireForwardInputForWallRun;
        private float wallRunInitialRiseSpeed;
        private float wallRunRiseDuration;

        private static readonly int GroundedHash = Animator.StringToHash("Grounded");
        private static readonly int MovingHash = Animator.StringToHash("Moving");
        private static readonly int InputXHash = Animator.StringToHash("InputX");
        private static readonly int InputYHash = Animator.StringToHash("InputY");
        private static readonly int TurnHash = Animator.StringToHash("Turn");
        private static readonly int JumpHash = Animator.StringToHash("Jump");
        private static readonly int RunSlideHash = Animator.StringToHash("RunSlide");
        private static readonly int RollHash = Animator.StringToHash("Roll");
        private static readonly int WallRunHash = Animator.StringToHash("WallRun");
        private static readonly int WallRunLeftHash = Animator.StringToHash("WallRunLeft");
        private static readonly int ClimbHash = Animator.StringToHash("Climb");
        private const string WallRunLayerName = "WallRun";

        // 运行时缓存：CharacterController、水平/垂直速度、落地和跳跃窗口状态。
        private CharacterController characterController;
        private PlayerMovementService movementService;
        private PlayerAnimatorPresenter animatorPresenter;
        private Vector3 horizontalVelocity; private float verticalVelocity;
        private float airborneCandidateTime, groundDistance;
        private bool airborne, hasGroundBelow, jumpForcesAirborne;
        private readonly RaycastHit[] groundProbeHits = new RaycastHit[8];
        private float lastGroundedTime, currentWalkLayerWeight, currentSprintLayerWeight, jumpParameterReleaseTime, previousRollTriggerInput;        private bool isJumpParameterActive, jumpLeftGround, grounded; private int airJumpCount;
        private PlayerHealthController health;
        private bool isDoubleJumpRolling; private float doubleJumpRollReleaseTime;
        private float previousTurnInput;
        // 站立姿态的原始碰撞体数据，用于蹲伏/滑铲后恢复。
        private float originalHeight; private Vector3 originalCenter;
        // 当前动作状态：蹲伏、滑铲、起身、攀爬、翻越、跑墙、前空翻翻越。
        private bool isCrouchActive, isSliding, isStandingUp, isClimbing, isVaulting, isWallRunning, isFrontFlipVaulting;
        // 动作计时和位移插值数据。
        private float targetHeight, currentCrouchLayerWeight, climbStartTime, vaultStartTime, frontFlipStartTime;
        private Vector3 targetCenter, climbStartPosition, climbTargetPosition, vaultStartPosition, vaultTargetPosition, frontFlipStartPosition, frontFlipTargetPosition;
        // 自身碰撞体缓存，用于起身检测时排除自己。
        private Collider[] selfColliders;
        // 跑墙状态：墙面法线、跑墙方向、起始速度、持续时间、滑落速度和冷却。
        private Vector3 wallNormal, wallRunDirection; private float wallEntrySpeed, wallRunStartTime, currentWallRunDuration, wallSlideSpeed, wallRunCooldownUntil; private Collider wallCollider, wallJumpBlockedCollider;
        private int wallSide;
        private const float Skin = 0.02f;
        private const float MinClimbAnimationDuration = 1.08f;

        public Vector3 HorizontalVelocity => horizontalVelocity;
        public bool IsSprinting { get; private set; }
        public bool IsAiming { get; private set; }
        
        public bool IsSliding => isSliding;
        public bool IsAirborne => airborne;
public bool IsCrouching => isCrouchActive || isSliding || isStandingUp;
        public bool IsTraversalBusy => isClimbing || isVaulting || isWallRunning || isFrontFlipVaulting;
        public float CurrentSpeed => horizontalVelocity.magnitude;
        public PlayerAnimatorPresenter AnimatorPresenter => animatorPresenter;
        private bool IsPlayerDead => health != null && health.IsDead;

        private void Awake()
        {
            config ??= PlayerConfigProvider.Load();
            ApplyConfig(config);
            characterController = GetComponent<CharacterController>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (cameraRoot == null && Camera.main != null) cameraRoot = Camera.main.transform;
            if (animator != null) animator.applyRootMotion = false;
            movementService = new PlayerMovementService();
            animatorPresenter = new PlayerAnimatorPresenter(animator);
            health = GetComponent<PlayerHealthController>();
            originalHeight = characterController.height; originalCenter = characterController.center; targetHeight = originalHeight; targetCenter = originalCenter; selfColliders = GetComponentsInChildren<Collider>(true); grounded = characterController.isGrounded;
        }


        private void ApplyConfig(PlayerConfig value)
        {
            moveSpeed = value.MoveSpeed;
            sprintSpeed = value.SprintSpeed;
            acceleration = value.Acceleration;
            deceleration = value.Deceleration;
            rotationSpeed = value.RotationSpeed;
            sprintTurnControl = value.SprintTurnControl;
            jumpHeight = value.JumpHeight;
            enableDoubleJump = value.EnableDoubleJump;
            maxAirJumpCount = value.MaxAirJumpCount;
            doubleJumpHeight = value.DoubleJumpHeight;
            gravity = value.Gravity;
            groundedStickForce = value.GroundedStickForce;
            airborneDistanceMultiplier = value.AirborneDistanceMultiplier;
            airborneEnterDelay = value.AirborneEnterDelay;
            airborneExitDistanceMultiplier = value.AirborneExitDistanceMultiplier;
            groundProbeRadius = value.GroundProbeRadius;
            groundLayerMask = value.GroundLayerMask;
            coyoteTime = value.CoyoteTime;
            walkLayer = value.WalkLayer;
            sprintLayer = value.SprintLayer;
            animationDampTime = value.AnimationDampTime;
            layerBlendSpeed = value.LayerBlendSpeed;
            jumpParameterHoldTime = value.JumpParameterHoldTime;
            crouchKey = value.CrouchKey;
            crouchHeight = value.CrouchHeight;
            crouchCenterY = value.CrouchCenterY;
            crouchMoveSpeedMultiplier = value.CrouchMoveSpeedMultiplier;
            crouchTransitionSpeed = value.CrouchTransitionSpeed;
            standUpCheckRadius = value.StandUpCheckRadius;
            crouchLayer = value.CrouchLayer;
            slideSpeedThreshold = value.SlideSpeedThreshold;
            slideDeceleration = value.SlideDeceleration;
            slideEndSpeed = value.SlideEndSpeed;
            slideMinimumSpeed = value.SlideMinimumSpeed;
            slideLayer = value.SlideLayer;
            slideInitialBoostMultiplier = value.SlideInitialBoostMultiplier;
            climbObstacleMask = value.ClimbObstacleMask;
            climbCheckDistance = value.ClimbCheckDistance;
            climbMaxHeightMultiplier = value.ClimbMaxHeightMultiplier;
            climbProbeRadius = value.ClimbProbeRadius;
            climbForwardOffset = value.ClimbForwardOffset;
            climbDuration = value.ClimbDuration;
            climbTag = value.ClimbTag;
            standUpBlockMask = value.StandUpBlockMask;
            debugClimbChecks = value.DebugClimbChecks;
            debugStandUpChecks = value.DebugStandUpChecks;
            enableVault = value.EnableVault;
            vaultCheckDistance = value.VaultCheckDistance;
            vaultMaxHeightRatio = value.VaultMaxHeightRatio;
            vaultMaxWidthMultiplier = value.VaultMaxWidthMultiplier;
            vaultDuration = value.VaultDuration;
            vaultForwardOffset = value.VaultForwardOffset;
            vaultHeightOffset = value.VaultHeightOffset;
            vaultLayerMask = value.VaultLayerMask;
            vaultRequireGrounded = value.VaultRequireGrounded;
            enableFrontFlipVault = value.EnableFrontFlipVault;
            frontFlipMaxObstacleHeight = value.FrontFlipMaxObstacleHeight;
            frontFlipMaxObstacleWidth = value.FrontFlipMaxObstacleWidth;
            frontFlipCheckDistance = value.FrontFlipCheckDistance;
            frontFlipTriggerRadius = value.FrontFlipTriggerRadius;
            frontFlipDuration = value.FrontFlipDuration;
            frontFlipForwardOffset = value.FrontFlipForwardOffset;
            frontFlipHeightOffset = value.FrontFlipHeightOffset;
            frontFlipLayerMask = value.FrontFlipLayerMask;
            enableWallRun = value.EnableWallRun;
            wallTag = value.WallTag;
            wallCheckDistance = value.WallCheckDistance;
            wallCheckRadius = value.WallCheckRadius;
            wallLayerMask = value.WallLayerMask;
            minWallRunSpeed = value.MinWallRunSpeed;
            maxWallRunSpeed = value.MaxWallRunSpeed;
            minWallRunDuration = value.MinWallRunDuration;
            maxWallRunDuration = value.MaxWallRunDuration;
            minWallSlideSpeed = value.MinWallSlideSpeed;
            maxWallSlideSpeed = value.MaxWallSlideSpeed;
            wallRunSpeedMultiplier = value.WallRunSpeedMultiplier;
            wallRunStickForce = value.WallRunStickForce;
            wallJumpHorizontalForce = value.WallJumpHorizontalForce;
            wallJumpVerticalForce = value.WallJumpVerticalForce;
            wallRunExitCooldown = value.WallRunExitCooldown;
            requireForwardInputForWallRun = value.RequireForwardInputForWallRun;
            wallRunInitialRiseSpeed = value.WallRunInitialRiseSpeed;
            wallRunRiseDuration = value.WallRunRiseDuration;
        }

        private void Update()
        {
            if (IsPlayerDead) return;
            UpdateAimInput();
            Vector2 moveInput = ReadMoveInput(); bool hasMoveInput = moveInput.sqrMagnitude > 0.01f; bool sprintHeld = PlayerInputGate.IsGameplay && Input.GetKey(KeyCode.LeftShift);
            // 优先处理会接管位移的特殊动作，避免同帧继续执行普通移动。
            UpdateVault(); if (isVaulting) { UpdateAnimator(moveInput); return; }
            UpdateFrontFlipVault(); if (isFrontFlipVaulting) { UpdateAnimator(moveInput); return; }
            UpdateWallRun(moveInput);
            if (isWallRunning) { if (PlayerInputGate.IsGameplay && Input.GetKeyDown(KeyCode.Space)) WallJump(); UpdateAnimator(moveInput); return; }
            if (airborne && !isClimbing && !isSliding && !isCrouchActive && !isWallRunning) TryStartWallRun(moveInput);
            if (isWallRunning) { UpdateAnimator(moveInput); return; }
            if (isCrouchActive && !isClimbing && sprintHeld && hasMoveInput) TryStartStandUp(false);
            IsSprinting = sprintHeld && hasMoveInput && !isCrouchActive && !isSliding && !isClimbing && !isWallRunning;
            HandleCrouch(); UpdateClimb(); if (isClimbing) { UpdateAnimator(moveInput); return; } CheckStandingTransition();
            UpdateMovement(moveInput); UpdateJump(); UpdateAnimator(moveInput);
        }

        private void OnDisable()
        {
            IsAiming = false;
        }

        private void UpdateAimInput()
        {
            IsAiming = PlayerInputGate.IsGameplay
                && Cursor.lockState == CursorLockMode.Locked
                && Input.GetMouseButton(1);
        }

        private void LateUpdate()
        {
            if (isClimbing && animatorPresenter != null)
            {
                animatorPresenter.StabilizeClimbVisual(transform);
            }
        }


        private Vector2 ReadMoveInput()
        {
            if (!PlayerInputGate.IsGameplay)
            {
                return Vector2.zero;
            }

            float horizontalInput = Input.GetAxisRaw("Horizontal");
            float verticalInput = Input.GetAxisRaw("Vertical");
            return Vector2.ClampMagnitude(new Vector2(horizontalInput, verticalInput), 1f);
        }

        // 计算地面/空中移动、重力和CharacterController碰撞结果。
        private void UpdateMovement(Vector2 moveInput)
        {
            Vector3 worldDirection = Vector3.zero;
            if (isClimbing) horizontalVelocity = Vector3.zero;
            else if (isSliding) horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, Vector3.zero, slideDeceleration * Time.deltaTime);
            else { worldDirection = GetCameraRelativeDirection(moveInput); float targetSpeed = movementService.GetTargetSpeed(moveSpeed, sprintSpeed, crouchMoveSpeedMultiplier, IsSprinting, isCrouchActive); Vector3 targetVelocity = worldDirection * targetSpeed; float speedChangeRate = targetVelocity.sqrMagnitude > horizontalVelocity.sqrMagnitude ? acceleration : deceleration; horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, speedChangeRate * Time.deltaTime); }
            if (!isClimbing) { if (grounded) { if (verticalVelocity < 0f) verticalVelocity = groundedStickForce; } else verticalVelocity += gravity * Time.deltaTime; }
            Vector3 motion = isClimbing ? Vector3.zero : horizontalVelocity; motion.y = isClimbing ? 0f : verticalVelocity;
            CollisionFlags flags = characterController.Move(motion * Time.deltaTime);
            grounded = (flags & CollisionFlags.Below) != 0;
            UpdateAirborneState(Time.deltaTime);
            if (!airborne) { lastGroundedTime = Time.time; airJumpCount = 0; wallJumpBlockedCollider = null; }
            if (grounded && verticalVelocity < 0f) verticalVelocity = groundedStickForce;
            if (isJumpParameterActive && !grounded) jumpLeftGround = true;
            if (!isSliding && !isClimbing) RotateTowardsMovement(worldDirection);
        }

        private void UpdateAirborneState(float deltaTime)
        {
            float characterHeight = GetWorldCharacterHeight();
            hasGroundBelow = TryGetGroundDistance(characterHeight, out groundDistance);

            if (grounded)
            {
                airborne = false;
                airborneCandidateTime = 0f;
                jumpForcesAirborne = false;
                return;
            }

            if (jumpForcesAirborne)
            {
                airborne = true;
                airborneCandidateTime = 0f;
                return;
            }

            if (airborne)
            {
                if (movementService.ShouldExitAirborne(
                    grounded,
                    hasGroundBelow,
                    groundDistance,
                    characterHeight,
                    airborneExitDistanceMultiplier))
                {
                    airborne = false;
                }
                return;
            }

            bool shouldEnter = movementService.ShouldEnterAirborne(
                grounded,
                hasGroundBelow,
                groundDistance,
                characterHeight,
                airborneDistanceMultiplier);
            if (!shouldEnter)
            {
                airborneCandidateTime = 0f;
                return;
            }

            airborneCandidateTime += Mathf.Max(0f, deltaTime);
            if (airborneCandidateTime >= airborneEnterDelay)
            {
                airborne = true;
                airborneCandidateTime = 0f;
            }
        }

        private bool TryGetGroundDistance(float characterHeight, out float distance)
        {
            float worldRadius = characterController.radius * Mathf.Max(
                Mathf.Abs(transform.lossyScale.x),
                Mathf.Abs(transform.lossyScale.z));
            float probeRadius = groundProbeRadius > 0f
                ? groundProbeRadius
                : worldRadius * 0.9f;
            probeRadius = Mathf.Clamp(probeRadius, 0.01f, Mathf.Max(0.01f, worldRadius - Skin));

            Vector3 worldCenter = transform.TransformPoint(characterController.center);
            float bottomToCenter = Mathf.Max(0f, characterHeight * 0.5f - worldRadius);
            Vector3 probeOrigin = worldCenter - Vector3.up * bottomToCenter + Vector3.up * Skin;
            float probeDistance = characterHeight * Mathf.Max(1f, airborneDistanceMultiplier) + Skin;
            int hitCount = Physics.SphereCastNonAlloc(
                probeOrigin,
                probeRadius,
                Vector3.down,
                groundProbeHits,
                probeDistance,
                groundLayerMask,
                QueryTriggerInteraction.Ignore);

            float nearestDistance = float.PositiveInfinity;
            float minimumGroundNormal = Mathf.Cos(characterController.slopeLimit * Mathf.Deg2Rad);
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = groundProbeHits[i];
                if (hit.collider == null
                    || hit.collider.transform == transform
                    || hit.collider.transform.IsChildOf(transform)
                    || Vector3.Dot(hit.normal, Vector3.up) < minimumGroundNormal)
                {
                    continue;
                }

                nearestDistance = Mathf.Min(nearestDistance, Mathf.Max(0f, hit.distance - Skin));
            }

            distance = nearestDistance;
            return !float.IsPositiveInfinity(nearestDistance);
        }

        private float GetWorldCharacterHeight()
        {
            float scaleY = Mathf.Abs(transform.lossyScale.y);
            float scaleXZ = Mathf.Max(
                Mathf.Abs(transform.lossyScale.x),
                Mathf.Abs(transform.lossyScale.z));
            return Mathf.Max(
                characterController.height * scaleY,
                characterController.radius * scaleXZ * 2f);
        }




        // 将输入方向转换为相机水平面的相对移动方向。
        private Vector3 GetCameraRelativeDirection(Vector2 moveInput) { if (moveInput.sqrMagnitude <= 0.0001f) return Vector3.zero; Transform referenceTransform = cameraRoot != null ? cameraRoot : transform; Vector3 cameraForward = referenceTransform.forward; cameraForward.y = 0f; Vector3 cameraRight = referenceTransform.right; cameraRight.y = 0f; cameraForward.Normalize(); cameraRight.Normalize(); return (cameraForward * moveInput.y + cameraRight * moveInput.x).normalized; }

        private void RotateTowardsMovement(Vector3 worldDirection) { if (worldDirection.sqrMagnitude <= 0.0001f) return; transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(worldDirection, Vector3.up), rotationSpeed * Time.deltaTime); }

        // 根据蹲伏键和当前速度，在蹲伏、滑铲和起身之间切换。
        private void HandleCrouch()
        {
            bool acceptsGameplayInput = PlayerInputGate.IsGameplay;
            bool canStartSlide = !isCrouchActive && !isSliding && !isStandingUp && !isClimbing && !isVaulting && !isWallRunning && CurrentSpeed > slideSpeedThreshold;
            if (acceptsGameplayInput && !isClimbing && !isVaulting && !isWallRunning && Input.GetKeyDown(crouchKey)) { if (isSliding) { } else if (canStartSlide) EnterSlide(); else if (isCrouchActive) TryStartStandUp(false); else if (!isStandingUp) EnterCrouch(); }
            if (!isClimbing && !isVaulting && !isWallRunning && isSliding) if (!acceptsGameplayInput || !Input.GetKey(crouchKey) || CurrentSpeed <= slideEndSpeed) EndSlide();
        }

        private bool TryStartStandUp(bool requestedByJump) { if (!CanStandUp()) return false; if (isSliding) { isSliding = false; if (animator != null) animator.SetBool(RunSlideHash, false); } isCrouchActive = false; isStandingUp = false; characterController.height = originalHeight; characterController.center = originalCenter; targetHeight = originalHeight; targetCenter = originalCenter; if (animator != null) { animator.SetBool(JumpHash, false); isJumpParameterActive = false; } return true; }

        private void EnterSlide() { isSliding = true; isStandingUp = false; float initSpeed = Mathf.Max(CurrentSpeed, slideMinimumSpeed) * slideInitialBoostMultiplier; if (horizontalVelocity.sqrMagnitude > 0.01f) horizontalVelocity = horizontalVelocity.normalized * initSpeed; else horizontalVelocity = transform.forward * initSpeed; characterController.height = crouchHeight; characterController.center = new Vector3(originalCenter.x, crouchCenterY, originalCenter.z); targetHeight = crouchHeight; targetCenter = new Vector3(originalCenter.x, crouchCenterY, originalCenter.z); if (animator != null) { animator.SetBool(JumpHash, false); animator.SetBool(RunSlideHash, true); isJumpParameterActive = false; } currentCrouchLayerWeight = 1f; }

        private void EndSlide() { isSliding = false; if (animator != null) animator.SetBool(RunSlideHash, false); if ((PlayerInputGate.IsGameplay && Input.GetKey(crouchKey)) || !CanStandUp()) { isCrouchActive = true; characterController.height = crouchHeight; characterController.center = new Vector3(originalCenter.x, crouchCenterY, originalCenter.z); targetHeight = crouchHeight; targetCenter = new Vector3(originalCenter.x, crouchCenterY, originalCenter.z); if (animator != null) { animator.SetBool(JumpHash, false); isJumpParameterActive = false; } } else TryStartStandUp(false); }

        private void EnterCrouch() { isCrouchActive = true; characterController.height = crouchHeight; characterController.center = new Vector3(originalCenter.x, crouchCenterY, originalCenter.z); targetHeight = crouchHeight; targetCenter = new Vector3(originalCenter.x, crouchCenterY, originalCenter.z); if (animator != null) { animator.SetBool(JumpHash, false); isJumpParameterActive = false; } currentCrouchLayerWeight = 1f; }

        private void CheckStandingTransition() { if (!isStandingUp) return; if (Mathf.Abs(characterController.height - originalHeight) < 0.01f && Vector3.Distance(characterController.center, originalCenter) < 0.01f) isStandingUp = false; }

        private bool CanStandUp() { float controllerRadius = characterController.radius; Vector3 capsuleCenter = transform.position + originalCenter; float halfHeight = originalHeight * 0.5f; Vector3 capsuleBottom = capsuleCenter + Vector3.down * (halfHeight - controllerRadius - Skin); Vector3 capsuleTop = capsuleCenter + Vector3.up * (halfHeight - controllerRadius - Skin); float checkRadius = Mathf.Max(0.01f, controllerRadius - Skin); Collider[] standUpHits = Physics.OverlapCapsule(capsuleBottom, capsuleTop, checkRadius, standUpBlockMask, QueryTriggerInteraction.Ignore); foreach (Collider standUpHit in standUpHits) { bool isSelfCollider = false; foreach (var selfCollider in selfColliders) { if (selfCollider == standUpHit) { isSelfCollider = true; break; } } if (!isSelfCollider && standUpHit.transform != null && !standUpHit.transform.IsChildOf(transform)) { if (debugStandUpChecks) Debug.Log("[CanStandUp] blocked: " + standUpHit.name, standUpHit.gameObject); return false; } } return true; }

        // 空格键入口：按优先级处理跑墙跳、翻越/攀爬、普通跳和二段跳。
        private void UpdateJump()
        {
            if (!PlayerInputGate.IsGameplay || !Input.GetKeyDown(KeyCode.Space)) return;
            if (isWallRunning) { WallJump(); return; }
            if (isVaulting || isFrontFlipVaulting || isClimbing) return;

            if (grounded && !isCrouchActive && !isSliding)
            {
                if (TryFrontFlipVault() || TryClimb() || TryVault()) return;
            }
            else if (!grounded && !isCrouchActive && !isSliding && !isStandingUp && TryClimb())
            {
                return;
            }

            if ((isCrouchActive || isSliding) && !TryStartStandUp(true)) return;
            if (Time.time - lastGroundedTime > coyoteTime) { TryDoubleJump(); return; }
            if (isClimbing || isStandingUp || isJumpParameterActive) return;

            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            lastGroundedTime = -999f;
            BeginJumpAnimation();
        }

        // 短暂保持Jump参数，保证Animator能稳定进入跳跃过渡。
        private void BeginJumpAnimation()
        {
            jumpForcesAirborne = true;
            airborne = true;
            airborneCandidateTime = 0f;
            if (animator == null) return;
            animator.SetBool(JumpHash, true);
            isJumpParameterActive = true;
            jumpParameterReleaseTime = Time.time + jumpParameterHoldTime;
            jumpLeftGround = false;
        }

        // 从墙面法线方向弹离，并设置跑墙退出冷却，避免立刻重新吸附同一面墙。
        private void WallJump()
        {
            Vector3 direction = (wallNormal * wallJumpHorizontalForce + Vector3.up * wallJumpVerticalForce).normalized;
            horizontalVelocity = Vector3.ProjectOnPlane(direction, Vector3.up).normalized * wallJumpHorizontalForce;
            verticalVelocity = wallJumpVerticalForce;
            airJumpCount = 0;
            wallJumpBlockedCollider = wallCollider;
            EndWallRun();
            wallRunCooldownUntil = Time.time + wallRunExitCooldown;
            BeginJumpAnimation();
        }

        private bool TryDoubleJump()
        {
            if (!enableDoubleJump || grounded || isVaulting || isWallRunning || isClimbing || isSliding || isCrouchActive || isStandingUp || airJumpCount >= maxAirJumpCount)
                return false;

            verticalVelocity = Mathf.Sqrt(doubleJumpHeight * -2f * gravity);
            jumpForcesAirborne = true;
            airborne = true;
            airborneCandidateTime = 0f;
            airJumpCount++;

            if (animator != null)
            {
                animator.SetBool(JumpHash, false);
                animator.SetBool(RollHash, true);
                isJumpParameterActive = false;
                jumpLeftGround = false;
                isDoubleJumpRolling = true;
                doubleJumpRollReleaseTime = Time.time + Mathf.Max(0.01f, frontFlipDuration);
            }

            return true;
        }

        // 检测低矮障碍并启动普通翻越。
        private bool TryVault()
        {
            if (!enableVault || (vaultRequireGrounded && !grounded) || isSliding || isCrouchActive || isClimbing || isVaulting || isWallRunning)
                return false;
            if (!TryFindForwardObstacle(vaultCheckDistance, climbProbeRadius, vaultLayerMask, out RaycastHit hit))
                return false;
            if (IsCross(hit.collider) || IsClimbable(hit.collider))
                return false;

            float height = hit.collider.bounds.max.y - transform.position.y;
            float width = GetProjectedBoundsSize(hit.collider.bounds, transform.right);
            if (height <= 0.05f || height > originalHeight * vaultMaxHeightRatio || width > characterController.radius * 2f * vaultMaxWidthMultiplier)
                return false;

            StartTraversal(BuildTraversalTarget(hit.collider, vaultForwardOffset, vaultHeightOffset), vaultDuration, false);
            return true;
        }

        // 翻越过程使用抛物线插值移动，并临时关闭碰撞避免卡在障碍上。
        private void UpdateVault()
        {
            if (!isVaulting) return;
            float traversalProgress = Mathf.Clamp01((Time.time - vaultStartTime) / Mathf.Max(0.01f, vaultDuration));
            MoveTraversal(vaultStartPosition, vaultTargetPosition, traversalProgress, Mathf.Max(0.25f, vaultHeightOffset + 0.25f));
            if (traversalProgress >= 1f) { isVaulting = false; characterController.detectCollisions = true; }
        }

        // 前空翻翻越流程，专门处理带Cross Tag的障碍。
private bool TryFrontFlipVault()
        {
            if (!enableFrontFlipVault || !grounded || isSliding || isCrouchActive || isClimbing || isVaulting || isWallRunning)
                return false;
            if (!TryFindCrossObstacle(frontFlipTriggerRadius * 2f, frontFlipLayerMask, out Collider crossObstacle))
                return false;

            Vector3 traversalDirection = Vector3.ProjectOnPlane(horizontalVelocity, Vector3.up).normalized;
            if (traversalDirection.sqrMagnitude <= 0.0001f)
                traversalDirection = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

            float height = crossObstacle.bounds.max.y - GetCharacterControllerBottom(characterController).y;
            if (height <= 0.05f || height > frontFlipMaxObstacleHeight)
                return false;

            frontFlipStartPosition = transform.position;

            Vector3 closestPoint = crossObstacle.ClosestPoint(transform.position);
            float approachDistance = Vector3.ProjectOnPlane(
                closestPoint - transform.position,
                Vector3.up).magnitude;
            float obstacleThickness = Mathf.Min(
                crossObstacle.bounds.size.x,
                crossObstacle.bounds.size.z);
            float traversalDistance = approachDistance
                + obstacleThickness
                + characterController.radius
                + frontFlipForwardOffset;

            frontFlipTargetPosition = transform.position + traversalDirection * traversalDistance;
            frontFlipTargetPosition.y = crossObstacle.bounds.max.y + frontFlipHeightOffset;
            frontFlipStartTime = Time.time;
            isFrontFlipVaulting = true;
            verticalVelocity = 0f;
            characterController.detectCollisions = false;
            if (animator != null)
            {
                animator.SetBool(JumpHash, false);
                animator.SetBool(RollHash, true);
            }
            isJumpParameterActive = false;
            return true;
        }

        // 更新前空翻翻越位移，并在结束时恢复碰撞和动画参数。
        private void UpdateFrontFlipVault()
        {
            if (!isFrontFlipVaulting) return;
            float traversalProgress = Mathf.Clamp01((Time.time - frontFlipStartTime) / Mathf.Max(0.01f, frontFlipDuration));
            MoveTraversal(frontFlipStartPosition, frontFlipTargetPosition, traversalProgress, Mathf.Max(0.45f, frontFlipHeightOffset));
            if (traversalProgress >= 1f)
            {
                isFrontFlipVaulting = false;
                characterController.detectCollisions = true;
                if (animator != null) animator.SetBool(RollHash, false);
            }
        }

        // 启动普通翻越的通用状态设置。
        private void StartTraversal(Vector3 target, float duration, bool roll)
        {
            vaultStartPosition = transform.position;
            vaultTargetPosition = target;
            vaultStartTime = Time.time;
            isVaulting = true;
            // 保留跨越前的水平动量，跨越结束后继续保持奔跑速度。
            verticalVelocity = 0f;
            characterController.detectCollisions = false;
            if (animator != null) { animator.SetBool(JumpHash, false); animator.SetBool(RollHash, roll); }
            isJumpParameterActive = false;
        }

        // 在起点和终点之间插值，同时添加一个简单的垂直弧线。
        private void MoveTraversal(Vector3 start, Vector3 target, float normalizedTime, float arcHeight)
        {
            Vector3 position = Vector3.Lerp(start, target, normalizedTime);
            position.y += 4f * normalizedTime * (1f - normalizedTime) * arcHeight;
            characterController.Move(position - transform.position);
        }

        // 攀爬位移不添加翻越弧线，避免角色在攀爬过程中被额外抬到障碍物上方。
        private void MoveClimbTraversal(Vector3 start, Vector3 target, float normalizedTime)
        {
            float smoothedTime = Mathf.SmoothStep(0f, 1f, normalizedTime);
            Vector3 position = Vector3.Lerp(start, target, smoothedTime);
            characterController.Move(position - transform.position);
        }

        // 检测可攀爬障碍并启动攀爬位移。
        private bool TryClimb()
        {
            if (isClimbing || isVaulting || isFrontFlipVaulting || isSliding || isCrouchActive || isWallRunning) return false;
            if (!TryFindClimbObstacle(out RaycastHit hit) || !IsClimbable(hit.collider))
                return false;

            Vector3 characterForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            Vector3 climbSurfaceForward = Vector3.ProjectOnPlane(-hit.normal, Vector3.up).normalized;
            if (climbSurfaceForward.sqrMagnitude <= 0.0001f
                || Vector3.Dot(characterForward, climbSurfaceForward) < 0.9f)
                return false;

            float height = hit.collider.bounds.max.y - transform.position.y;
            if (!movementService.IsClimbHeightValid(height, originalHeight, climbMaxHeightMultiplier)) return false;

            climbStartPosition = transform.position;
            climbTargetPosition = BuildClimbTarget(hit);
            climbStartTime = Time.time;
            isClimbing = true;
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
            characterController.detectCollisions = false;
            if (animatorPresenter != null && animatorPresenter.IsAvailable)
            {
                animatorPresenter.EnterClimb(
                    transform,
                    GetWallRunAnimatorLayer(),
                    ClimbHash,
                    JumpHash,
                    RollHash);
            }
            isJumpParameterActive = false;
            return true;
        }

        // 更新攀爬插值位移，并在结束时恢复碰撞和动画参数。
        private void UpdateClimb()
        {
            if (!isClimbing) return;

            float effectiveClimbDuration = Mathf.Max(MinClimbAnimationDuration, climbDuration);
            float traversalProgress = Mathf.Clamp01((Time.time - climbStartTime) / Mathf.Max(0.01f, effectiveClimbDuration));
            MoveClimbTraversal(climbStartPosition, climbTargetPosition, traversalProgress);

            if (animatorPresenter != null)
                animatorPresenter.ReleaseClimbRequestWhenEntered(GetWallRunAnimatorLayer(), ClimbHash);

            if (traversalProgress >= 1f)
            {
                isClimbing = false;
                characterController.detectCollisions = true;

                float snapDistance = Mathf.Max(
                    characterController.stepOffset,
                    characterController.skinWidth + Skin);
                CollisionFlags snapFlags = characterController.Move(Vector3.down * snapDistance);
                grounded = (snapFlags & CollisionFlags.Below) != 0 || characterController.isGrounded;
                verticalVelocity = grounded ? groundedStickForce : 0f;

                if (animatorPresenter != null)
                    animatorPresenter.ExitClimb(ClimbHash);

                int climbLayer = GetWallRunAnimatorLayer();
                if (animator != null && climbLayer >= 0 && climbLayer < animator.layerCount)
                    animator.SetLayerWeight(climbLayer, 0f);
            }
        }


        // 根据左右墙体检测结果和当前水平速度启动跑墙。
        private bool TryStartWallRun(Vector2 moveInput)
        {
            if (!enableWallRun || grounded || Time.time < wallRunCooldownUntil || horizontalVelocity.magnitude < minWallRunSpeed)
                return false;
            if (requireForwardInputForWallRun && moveInput.y <= 0.01f)
                return false;
            if (!TryFindWall(out RaycastHit hit, out int side) || hit.collider == wallJumpBlockedCollider)
                return false;

            wallCollider = hit.collider;
            wallNormal = hit.normal;
            wallSide = side;
            Vector3 candidate = Vector3.Cross(wallNormal, Vector3.up).normalized;
            Vector3 velocityDirection = horizontalVelocity.sqrMagnitude > 0.01f ? horizontalVelocity.normalized : transform.forward;
            wallRunDirection = Vector3.Dot(candidate, velocityDirection) >= 0f ? candidate : -candidate;
            wallEntrySpeed = horizontalVelocity.magnitude;
            currentWallRunDuration = movementService.GetWallRunDuration(wallEntrySpeed, minWallRunSpeed, maxWallRunSpeed, minWallRunDuration, maxWallRunDuration);
            wallSlideSpeed = movementService.GetWallSlideSpeed(wallEntrySpeed, minWallRunSpeed, maxWallRunSpeed, maxWallSlideSpeed, minWallSlideSpeed);
            wallRunStartTime = Time.time;
            isWallRunning = true;
            if (animator != null) { animator.SetBool(WallRunHash, true); animator.SetBool(WallRunLeftHash, wallSide < 0); }
            transform.rotation = Quaternion.LookRotation(wallRunDirection, Vector3.up);
            return true;
        }

        // 跑墙期间沿墙面方向移动，并施加贴墙力和受速度影响的下滑速度。
        private void UpdateWallRun(Vector2 moveInput)
        {
            if (!isWallRunning) return;
            if (grounded || Time.time - wallRunStartTime >= currentWallRunDuration
                || (requireForwardInputForWallRun && moveInput.y <= 0.01f)
                || !TryFindWall(out RaycastHit hit, out int side)
                || hit.collider != wallCollider)
            {
                EndWallRun();
                return;
            }

            wallNormal = hit.normal;
            wallSide = side;
            if (animator != null) animator.SetBool(WallRunLeftHash, wallSide < 0);
            float elapsed = Time.time - wallRunStartTime;
            horizontalVelocity = wallRunDirection * Mathf.Max(minWallRunSpeed, wallEntrySpeed * wallRunSpeedMultiplier);
            transform.rotation = Quaternion.LookRotation(wallRunDirection, Vector3.up);
            verticalVelocity = elapsed < wallRunRiseDuration ? wallRunInitialRiseSpeed : -wallSlideSpeed;
            CollisionFlags flags = characterController.Move((horizontalVelocity - wallNormal * wallRunStickForce + Vector3.up * verticalVelocity) * Time.deltaTime);
            grounded = (flags & CollisionFlags.Below) != 0;
            if (grounded) EndWallRun();
        }

        // 结束跑墙并清理Animator参数。
        private void EndWallRun()
        {
            if (!isWallRunning) return;
            isWallRunning = false;
            if (animator != null) { animator.SetBool(WallRunHash, false); animator.SetBool(WallRunLeftHash, false); }
        }

        private bool TryFindCrossObstacle(float radius, LayerMask layerMask, out Collider result)
        {
            Vector3 center = GetCharacterControllerBottom(characterController);
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            Collider[] hits = Physics.OverlapSphere(center, radius, layerMask, QueryTriggerInteraction.Ignore);
            float detectorHalfHeight = Mathf.Max(characterController.stepOffset, characterController.skinWidth);
            float nearestDistance = float.MaxValue;
            result = null;

            foreach (Collider hit in hits)
            {
                if (hit == null || hit.transform.IsChildOf(transform) || !IsCross(hit))
                    continue;

                Bounds bounds = hit.bounds;
                if (bounds.min.y > center.y + detectorHalfHeight
                    || bounds.max.y < center.y - detectorHalfHeight)
                    continue;

                Vector3 toBoundsCenter = Vector3.ProjectOnPlane(bounds.center - center, Vector3.up);
                if (Vector3.Dot(toBoundsCenter, forward) <= 0f)
                    continue;

                Vector3 closestPoint = hit.ClosestPoint(center);
                closestPoint.y = center.y;
                float planarDistance = (closestPoint - center).sqrMagnitude;
                if (planarDistance > radius * radius || planarDistance >= nearestDistance)
                    continue;

                nearestDistance = planarDistance;
                result = hit;
            }

            return result != null;
        }


        private bool TryFindForwardObstacle(float distance, float radius, LayerMask layerMask, out RaycastHit result)
        {
            Vector3 origin = transform.position + Vector3.up * Mathf.Max(characterController.radius, characterController.height * 0.45f);
            RaycastHit[] hits = Physics.SphereCastAll(origin, Mathf.Min(radius, Mathf.Max(0.05f, characterController.radius - Skin)),
                transform.forward, distance, layerMask, QueryTriggerInteraction.Ignore);
            float nearest = float.MaxValue;
            result = default;
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null || hit.collider.transform.IsChildOf(transform) || hit.distance >= nearest) continue;
                nearest = hit.distance;
                result = hit;
            }
            return result.collider != null;
        }

        // 分别检测角色左右两侧墙体，并返回墙在角色哪一侧。
        private bool TryFindWall(out RaycastHit result, out int side)
        {
            Vector3 origin = transform.position + Vector3.up * Mathf.Max(characterController.radius, characterController.height * 0.55f);
            float radius = Mathf.Min(wallCheckRadius, Mathf.Max(0.05f, characterController.radius - Skin));
            if (TryFindWall(origin, transform.right, radius, out result)) { side = 1; return true; }
            if (TryFindWall(origin, -transform.right, radius, out result)) { side = -1; return true; }
            side = 0;
            result = default;
            return false;
        }

        // 单方向墙体检测，要求命中对象带有配置的墙面Tag。
        private bool TryFindWall(Vector3 origin, Vector3 direction, float radius, out RaycastHit result)
        {
            if (Physics.SphereCast(origin, radius, direction, out result, wallCheckDistance, wallLayerMask, QueryTriggerInteraction.Ignore)
                && result.collider != null && !result.collider.transform.IsChildOf(transform) && HasTag(result.collider, wallTag))
                return true;
            result = default;
            return false;
        }

        // 根据障碍包围盒计算翻越后落点。
private Vector3 BuildTraversalTarget(Collider obstacle, float forwardOffset, float topOffset)
        {
            return BuildTraversalTarget(obstacle, forwardOffset, topOffset, transform.forward);
        }

        private Vector3 BuildTraversalTarget(
            Collider obstacle,
            float forwardOffset,
            float topOffset,
            Vector3 traversalDirection)
        {
            Vector3 forward = Vector3.ProjectOnPlane(traversalDirection, Vector3.up).normalized;
            if (forward.sqrMagnitude <= 0.0001f)
                forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

            Bounds bounds = obstacle.bounds;
            float centerDistance = Vector3.Dot(
                Vector3.ProjectOnPlane(bounds.center - transform.position, Vector3.up),
                forward);
            float distance = Mathf.Max(0f, centerDistance)
                + GetProjectedBoundsSize(bounds, forward) * 0.5f
                + characterController.radius
                + forwardOffset;
            Vector3 target = transform.position + forward * distance;
            target.y = bounds.max.y + topOffset;
            return target;
        }

        // 根据障碍包围盒计算攀爬后站到顶部的位置。
        private Vector3 BuildClimbTarget(RaycastHit hit)
        {
            Collider obstacle = hit.collider;
            Bounds bounds = obstacle.bounds;
            Vector3 intoSurface = Vector3.ProjectOnPlane(-hit.normal, Vector3.up).normalized;
            if (intoSurface.sqrMagnitude <= 0.0001f)
                intoSurface = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

            float ontoTopDistance = Mathf.Max(characterController.radius + Skin, climbForwardOffset);
            Vector3 target = hit.point + intoSurface * ontoTopDistance;

            float controllerBottomOffset = GetCharacterControllerBottom(characterController).y
                - transform.position.y;
            target.y = bounds.max.y - controllerBottomOffset;
            return target;
        }

        // 计算Bounds在指定方向上的投影尺寸，用于估算障碍宽度/深度。
        private float GetProjectedBoundsSize(Bounds bounds, Vector3 direction)
        {
            Vector3 axis = new Vector3(Mathf.Abs(direction.x), Mathf.Abs(direction.y), Mathf.Abs(direction.z)).normalized;
            Vector3 extents = bounds.extents;
            return 2f * (extents.x * axis.x + extents.y * axis.y + extents.z * axis.z);
        }

        private bool IsCross(Collider collider) { return HasTag(collider, "Cross"); }
        private bool IsClimbable(Collider collider) { return HasTag(collider, climbTag) || HasTag(collider, "Climb"); }
        private int GetWallRunAnimatorLayer()
        {
            if (animator == null) return 0;
            int layer = animator.GetLayerIndex(WallRunLayerName);
            return layer >= 0 ? layer : 0;
        }

        private bool HasTag(Collider collider, string tag)
        {
            return collider != null && !string.IsNullOrEmpty(tag) && string.Equals(collider.tag, tag, System.StringComparison.Ordinal);
        }

        // 汇总当前移动和动作状态，写入Animator参数。
        private void UpdateAnimator(Vector2 moveInput)
        {
            if (animator == null) return;
            bool forceGrounded = isCrouchActive || isSliding || isStandingUp || isClimbing || isVaulting || isWallRunning || isFrontFlipVaulting || isDoubleJumpRolling;
            bool animatorGrounded = forceGrounded || !airborne; bool animatorMoving = !isClimbing && (isVaulting || isWallRunning || isFrontFlipVaulting || isDoubleJumpRolling || moveInput.sqrMagnitude > 0.01f);
            Vector3 localVelocity = transform.InverseTransformDirection(horizontalVelocity); float maxAnimatorSpeed = Mathf.Max(0.01f, sprintSpeed);
            float animatorInputX = (isClimbing || isVaulting || isWallRunning || isFrontFlipVaulting || isDoubleJumpRolling) ? 0f : Mathf.Clamp(localVelocity.x / maxAnimatorSpeed, -1f, 1f);
            float animatorInputY = isClimbing ? 0f : (isVaulting || isWallRunning || isFrontFlipVaulting || isDoubleJumpRolling) ? 1f : Mathf.Clamp(localVelocity.z / maxAnimatorSpeed, -1f, 1f);
            float turn = 0f; float absInputX = Mathf.Abs(moveInput.x); float absInputY = Mathf.Abs(moveInput.y);
            if (!forceGrounded && absInputY < 0.1f && absInputX >= 0.2f && horizontalVelocity.sqrMagnitude < 0.5f && moveInput.x != previousTurnInput) turn = moveInput.x;
            previousTurnInput = (absInputY < 0.1f && absInputX >= 0.2f) ? moveInput.x : 0f;
            animator.SetBool(GroundedHash, animatorGrounded); animator.SetBool(MovingHash, animatorMoving);
            animator.SetFloat(InputXHash, animatorInputX, animationDampTime, Time.deltaTime);
            animator.SetFloat(InputYHash, animatorInputY, animationDampTime, Time.deltaTime);
            if (Mathf.Abs(turn) < 0.001f) animator.SetFloat(TurnHash, 0f); else animator.SetFloat(TurnHash, turn, animationDampTime, Time.deltaTime);
            if (isDoubleJumpRolling && (Time.time >= doubleJumpRollReleaseTime || grounded))
            {
                animator.SetBool(RollHash, false);
                isDoubleJumpRolling = false;
            }

            if (isJumpParameterActive && (Time.time >= jumpParameterReleaseTime || (jumpLeftGround && grounded)))
            {
                animator.SetBool(JumpHash, false);
                if (jumpLeftGround && (grounded || verticalVelocity <= 0f))
                {
                    isJumpParameterActive = false;
                    jumpLeftGround = false;
                }
            }
            BlendAnimatorLayers();
        }

        // 平滑切换行走、冲刺、蹲伏和滑铲动画层权重。
private void BlendAnimatorLayers()
        {
            currentWalkLayerWeight = Mathf.MoveTowards(
                currentWalkLayerWeight,
                0f,
                layerBlendSpeed * Time.deltaTime);

            bool wallActionActive = isWallRunning || isClimbing;
            float sprintTargetWeight = (IsSprinting
                || isVaulting
                || isFrontFlipVaulting
                || isDoubleJumpRolling) && !wallActionActive
                ? 1f
                : 0f;

            currentSprintLayerWeight = wallActionActive
                ? 0f
                : Mathf.MoveTowards(
                    currentSprintLayerWeight,
                    sprintTargetWeight,
                    layerBlendSpeed * Time.deltaTime);

            if (walkLayer >= 0 && walkLayer < animator.layerCount)
                animator.SetLayerWeight(walkLayer, currentWalkLayerWeight);
            if (sprintLayer >= 0 && sprintLayer < animator.layerCount)
                animator.SetLayerWeight(sprintLayer, currentSprintLayerWeight);

            int wallActionLayer = GetWallRunAnimatorLayer();
            if (wallActionLayer > 0 && wallActionLayer < animator.layerCount)
                animator.SetLayerWeight(wallActionLayer, wallActionActive ? 1f : 0f);

            bool keepCrouchLayer = isCrouchActive || isSliding || isStandingUp;
            float crouchTargetWeight = keepCrouchLayer ? 1f : 0f;
            currentCrouchLayerWeight = Mathf.MoveTowards(
                currentCrouchLayerWeight,
                crouchTargetWeight,
                layerBlendSpeed * Time.deltaTime);
            if (crouchLayer >= 0 && crouchLayer < animator.layerCount)
                animator.SetLayerWeight(crouchLayer, currentCrouchLayerWeight);

            if (slideLayer >= 0 && slideLayer < animator.layerCount)
            {
                float slideWeight = isSliding ? 1f : 0f;
                animator.SetLayerWeight(
                    slideLayer,
                    Mathf.MoveTowards(
                        animator.GetLayerWeight(slideLayer),
                        slideWeight,
                        layerBlendSpeed * Time.deltaTime));
            }
        }
    

private void OnDrawGizmos()
        {
            CharacterController controller = characterController != null
                ? characterController
                : GetComponent<CharacterController>();
            if (controller == null)
                return;

            float radius = Mathf.Max(0f, frontFlipTriggerRadius * 2f);
            Vector3 center = GetCharacterControllerBottom(controller);
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

            const int segments = 48;
            Gizmos.color = Color.red;

            Vector3 previous = center - right * radius;
            for (int i = 1; i <= segments; i++)
            {
                float angle = Mathf.Lerp(-90f, 90f, i / (float)segments) * Mathf.Deg2Rad;
                Vector3 point = center
                    + (forward * Mathf.Cos(angle) + right * Mathf.Sin(angle)) * radius;
                Gizmos.DrawLine(previous, point);
                previous = point;
            }

            Gizmos.DrawLine(center - right * radius, center + right * radius);
            Gizmos.DrawLine(center, center + forward * radius);
        }


private Vector3 GetCharacterControllerBottom(CharacterController controller)
        {
            Vector3 worldCenter = transform.TransformPoint(controller.center);
            float scaledHeight = controller.height * Mathf.Abs(transform.lossyScale.y);
            return worldCenter - transform.up * (scaledHeight * 0.5f) + transform.up * Skin;
        }


private bool TryFindClimbObstacle(out RaycastHit result)
        {
            float probeRadius = Mathf.Min(
                climbProbeRadius,
                Mathf.Max(0.05f, characterController.radius - Skin));
            float probeHeight = Mathf.Max(
                characterController.height * 0.65f,
                characterController.height * 0.5f + probeRadius);
            Vector3 origin = GetCharacterControllerBottom(characterController)
                + transform.up * probeHeight;

            RaycastHit[] hits = Physics.SphereCastAll(
                origin,
                probeRadius,
                transform.forward,
                climbCheckDistance,
                climbObstacleMask,
                QueryTriggerInteraction.Ignore);
            float nearest = float.MaxValue;
            result = default;

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null
                    || hit.collider.transform.IsChildOf(transform)
                    || hit.distance >= nearest)
                    continue;

                nearest = hit.distance;
                result = hit;
            }

            return result.collider != null;
        }
}
}
