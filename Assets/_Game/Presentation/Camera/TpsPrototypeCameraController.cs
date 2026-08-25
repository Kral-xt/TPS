using UnityEngine;
using TPS.Player.Presentation;
using TPS.BulletTime.Application;
using TPS.CameraSystem;

using TPS.CameraSystem.Presentation;
using TPS.CameraSystem.Infrastructure;


namespace TPS.Player
{
    /// <summary>
    /// TPS 原型镜头控制器。
    /// 负责第三人称镜头旋转、跟随、碰撞修正以及冲刺时的 FOV 变化。
    /// </summary>
    public class TpsPrototypeCameraController : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private Transform target; // 镜头跟随目标，通常是玩家角色
        [SerializeField] private TpsPrototypePlayerController playerController; // 玩家控制器，用于读取冲刺等状态

        [SerializeField] private Camera controlledCamera;
        [SerializeField] private CameraConfig config;
        private Vector3 targetOffset;
        private float followDistance;
        private float followHeight;
        private float followSmoothTime;
        private float mouseSensitivityX;
        private float mouseSensitivityY;
        private float minPitch;
        private float maxPitch;
        private LayerMask collisionMask;
        private float collisionRadius;
        private float collisionPadding;
        private float normalFov;
        private float sprintFov;
        private float fovLerpSpeed;
        private float aimFov;

        
        private float currentPivotHeight;
        private float pivotHeightVelocity;
        private CharacterController targetCharacterController;
private Vector3 followVelocity; // SmoothDamp 使用的速度缓存
        private float yaw; // 镜头水平旋转角
        private float pitch; // 镜头垂直俯仰角
        private readonly RaycastHit[] collisionHitBuffer = new RaycastHit[16];
        private readonly Collider[] pivotOverlapBuffer = new Collider[8];

        private CameraAimAssistController aimAssistController;
        private CameraFeedbackManager feedbackManager;
        private bool wasSprinting;


        /// <summary>
        /// 当前镜头水平旋转角，供其他系统读取。
        /// </summary>
        public float Yaw => yaw;

        private void Awake()
        {
            config ??= CameraConfigProvider.Load();
            ApplyConfig(config);
            // 如果没有手动指定跟随目标，则自动查找 Player 标签对象
            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    target = player.transform;
                }
            }

            // 如果没有手动指定玩家控制器，则从目标对象上获取
            

            if (target != null)
            {
                targetCharacterController = target.GetComponent<CharacterController>();
            }

            currentPivotHeight = targetOffset.y;
if (playerController == null && target != null)
            {
                playerController = target.GetComponent<TpsPrototypePlayerController>();
            }

            // 如果没有手动指定摄像机，则从子物体中查找
            if (controlledCamera == null)
            {
                controlledCamera = GetComponentInChildren<Camera>();
            }

            // 初始化当前镜头角度，避免运行时角度突然跳变
            Vector3 euler = transform.eulerAngles;
            yaw = euler.y;
            pitch = NormalizePitch(euler.x);


            aimAssistController = GetComponent<CameraAimAssistController>();
            if (aimAssistController == null)
            {
                aimAssistController = gameObject.AddComponent<CameraAimAssistController>();
            }
            aimAssistController.Initialize(target);
            feedbackManager = GetComponent<CameraFeedbackManager>();
            if (feedbackManager == null)
            {
                feedbackManager = gameObject.AddComponent<CameraFeedbackManager>();
            }

            wasSprinting = playerController != null && playerController.IsSprinting;

        }


        private void ApplyConfig(CameraConfig value)
        {
            targetOffset = value.TargetOffset;
            followDistance = value.FollowDistance;
            followHeight = value.FollowHeight;
            followSmoothTime = value.FollowSmoothTime;
            mouseSensitivityX = value.MouseSensitivityX;
            mouseSensitivityY = value.MouseSensitivityY;
            minPitch = value.MinPitch;
            maxPitch = value.MaxPitch;
            collisionMask = value.CollisionMask;
            collisionRadius = value.CollisionRadius;
            collisionPadding = value.CollisionPadding;
            normalFov = value.NormalFov;
            sprintFov = value.SprintFov;
            fovLerpSpeed = value.FovLerpSpeed;
            aimFov = value.AimFov;
        }

private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            UpdateFeedbackTriggers();

            UpdateAimAssist();
            UpdateRotationInput();
            UpdateCameraPosition();
            UpdateFov();
        }

        /// <summary>
        /// 读取鼠标输入并更新镜头旋转角度。
        /// 同时处理鼠标锁定和解锁逻辑。
        /// </summary>
private void UpdateRotationInput()
        {
            BulletTimeController bulletTimeController = BulletTimeController.Current;
            float lookSensitivityMultiplier = bulletTimeController != null
                ? bulletTimeController.LookSensitivityMultiplier
                : 1f;

            if (PlayerInputGate.IsGameplay && Cursor.lockState == CursorLockMode.Locked)
            {
                yaw += Input.GetAxisRaw("Mouse X")
                    * mouseSensitivityX
                    * lookSensitivityMultiplier;
                pitch -= Input.GetAxisRaw("Mouse Y")
                    * mouseSensitivityY
                    * lookSensitivityMultiplier;
            }

            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        }

        private void UpdateAimAssist()
        {
            if (aimAssistController == null
                || controlledCamera == null
                || playerController == null
                || !playerController.IsAiming
                || !PlayerInputGate.IsGameplay
                || !aimAssistController.TryGetAssistAngles(
                    controlledCamera,
                    out float targetYaw,
                    out float targetPitch))
            {
                return;
            }

            float strength = aimAssistController.Strength;
            float blend = strength >= 1f
                ? 1f
                : 1f - Mathf.Pow(
                    1f - strength,
                    Mathf.Max(0f, Time.unscaledDeltaTime) * 60f);
            yaw = Mathf.LerpAngle(yaw, targetYaw, blend);
            pitch = Mathf.LerpAngle(pitch, targetPitch, blend);
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }


        /// <summary>
        /// 更新镜头位置和旋转。
        /// 根据当前 yaw / pitch 计算目标位置，并进行碰撞修正。
        /// </summary>
private void UpdateCameraPosition()
        {
            bool isCrouching = playerController != null && playerController.IsCrouching;
            bool isSliding = playerController != null && playerController.IsSliding;
            float desiredPivotHeight = targetOffset.y;

            if (isCrouching)
            {
                float crouchControllerHeight = targetCharacterController != null
                    ? targetCharacterController.height
                    : 1f;
                float crouchPivotHeight = Mathf.Min(
                    targetOffset.y,
                    crouchControllerHeight * 0.65f);
                desiredPivotHeight = isSliding
                    ? Mathf.Lerp(targetOffset.y, crouchPivotHeight, 1f / 3f)
                    : crouchPivotHeight;
            }

            float pivotSmoothTime = desiredPivotHeight < currentPivotHeight
                ? followSmoothTime * 3f
                : followSmoothTime;
            currentPivotHeight = Mathf.SmoothDamp(
                currentPivotHeight,
                desiredPivotHeight,
                ref pivotHeightVelocity,
                pivotSmoothTime);

            Vector3 activeTargetOffset = targetOffset;
            activeTargetOffset.y = currentPivotHeight;
            Vector3 pivot = target.position + activeTargetOffset;

            if (feedbackManager != null
                && feedbackManager.TryGetLockRotation(
                    pivot,
                    out Quaternion lockRotation,
                    out float lockBlend))
            {
                Vector3 lockEuler = lockRotation.eulerAngles;
                yaw = Mathf.LerpAngle(yaw, lockEuler.y, lockBlend);
                pitch = Mathf.LerpAngle(
                    pitch,
                    NormalizePitch(lockEuler.x),
                    lockBlend);
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            }

            Vector3 feedbackRotation = feedbackManager != null
                ? feedbackManager.RotationOffset
                : Vector3.zero;
            Quaternion rotation = Quaternion.Euler(
                pitch + feedbackRotation.x,
                yaw + feedbackRotation.y,
                feedbackRotation.z);

            bool isSprinting = playerController != null && playerController.IsSprinting;
            float distanceMultiplier = feedbackManager != null
                ? feedbackManager.FollowDistanceMultiplier
                : 1f;
            float activeFollowDistance =
                followDistance * (isSprinting ? 1.2f : 1f) * distanceMultiplier;

            Vector3 localFeedbackOffset = feedbackManager != null
                ? feedbackManager.LocalPositionOffset
                : Vector3.zero;
            Vector3 desiredPosition = pivot
                - rotation * Vector3.forward * activeFollowDistance
                + Vector3.up * followHeight
                + rotation * localFeedbackOffset;

            if (isCrouching && !isSliding)
            {
                desiredPosition.y = pivot.y + followHeight
                    + (rotation * localFeedbackOffset).y;
            }

            Vector3 finalPosition = ResolveCollision(pivot, desiredPosition);
            float dampingMultiplier = feedbackManager != null
                ? feedbackManager.DampingMultiplier
                : 1f;
            float activeSmoothTime = Mathf.Max(
                0.001f,
                followSmoothTime * dampingMultiplier);

            transform.position = Vector3.SmoothDamp(
                transform.position,
                finalPosition,
                ref followVelocity,
                activeSmoothTime);
            transform.rotation = rotation;
        }

        /// <summary>
        /// 处理镜头碰撞。
        /// 如果观察点和期望镜头位置之间有障碍，则将镜头拉近到障碍物前方。
        /// </summary>
        private Vector3 ResolveCollision(Vector3 pivot, Vector3 desiredPosition)
        {
            Vector3 direction = desiredPosition - pivot;
            float distance = direction.magnitude;
            if (distance <= 0.001f)
            {
                return desiredPosition;
            }

            direction /= distance;
            float hitDistance;
            if (TryGetClosestCollision(pivot, direction, distance, out hitDistance))
            {
                return pivot + direction * Mathf.Max(0f, hitDistance - collisionPadding);
            }

            if (IsPivotObstructed(pivot) && TryGetClosestCollision(desiredPosition, -direction, distance, out hitDistance))
            {
                return desiredPosition - direction * Mathf.Max(0f, hitDistance - collisionPadding);
            }

            return desiredPosition;
        }

        private bool TryGetClosestCollision(Vector3 origin, Vector3 direction, float distance, out float closestDistance)
        {
            int hitCount = Physics.SphereCastNonAlloc(origin, collisionRadius, direction, collisionHitBuffer, distance, collisionMask, QueryTriggerInteraction.Ignore);
            closestDistance = distance;
            bool found = false;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = collisionHitBuffer[i];
                if (IsTargetCollider(hit.collider) || hit.distance >= closestDistance)
                {
                    continue;
                }

                closestDistance = hit.distance;
                found = true;
            }

            return found;
        }

        private bool IsPivotObstructed(Vector3 pivot)
        {
            int overlapCount = Physics.OverlapSphereNonAlloc(pivot, collisionRadius, pivotOverlapBuffer, collisionMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < overlapCount; i++)
            {
                if (!IsTargetCollider(pivotOverlapBuffer[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsTargetCollider(Collider collider)
        {
            return collider != null && target != null && (collider.transform == target || collider.transform.IsChildOf(target));
        }

        /// <summary>
        /// 根据玩家是否冲刺动态调整摄像机 FOV。
        /// 冲刺时扩大视野角，增强高速移动的速度感。
        /// </summary>
private void UpdateFov()
        {
            if (controlledCamera == null)
            {
                return;
            }

            bool isAiming = playerController != null && playerController.IsAiming;
            float targetFov = isAiming
                ? aimFov
                : playerController != null && playerController.IsSprinting
                    ? sprintFov
                    : normalFov;
            if (feedbackManager != null)
            {
                targetFov += feedbackManager.FovOffset;
            }

            controlledCamera.fieldOfView = Mathf.Lerp(
                controlledCamera.fieldOfView,
                targetFov,
                fovLerpSpeed * Time.unscaledDeltaTime);
        }

        /// <summary>
        /// 将 Unity 的 0~360 欧拉角转换为 -180~180 范围，便于 Clamp 俯仰角。
        /// </summary>
        private static float NormalizePitch(float rawPitch)
        {
            return rawPitch > 180f ? rawPitch - 360f : rawPitch;
        }
    

private void UpdateFeedbackTriggers()
        {
            bool isSprinting = playerController != null && playerController.IsSprinting;
            if (isSprinting && !wasSprinting)
            {
                feedbackManager?.PlayDash();
            }

            wasSprinting = isSprinting;
        }
}
}
