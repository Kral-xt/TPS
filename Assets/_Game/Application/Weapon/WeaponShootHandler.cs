using TPS.Combat.Domain;
using UnityEngine;

namespace TPS.Weapon.Application
{
    public interface IAimAssistTarget
    {
        bool IsValidAimTarget { get; }
        Transform AimPoint { get; }
    }

    public sealed class WeaponShootHandler
    {
        private const int HitBufferSize = 32;

        private readonly WeaponConfig config;
        private readonly Transform ownerRoot;
        private readonly RaycastHit[] hitBuffer = new RaycastHit[HitBufferSize];
        private float nextFireTime;

        public WeaponShootHandler(WeaponConfig weaponConfig, Transform shootOwnerRoot)
        {
            config = weaponConfig;
            ownerRoot = shootOwnerRoot;
        }

        public Ray GetAimRay(Camera gameCamera)
        {
            if (gameCamera == null)
            {
                Vector3 origin = ownerRoot != null ? ownerRoot.position : Vector3.zero;
                Vector3 direction = ownerRoot != null ? ownerRoot.forward : Vector3.forward;
                return new Ray(origin, direction);
            }

            return gameCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        }


public Vector3 GetAimPoint(Camera gameCamera)
        {
            return GetAimPoint(gameCamera, out _);
        }

        public Vector3 GetAimPoint(Camera gameCamera, out RaycastHit hit)
        {
            Ray aimRay = GetAimRay(gameCamera);
            return TryGetClosestValidHit(aimRay, out hit)
                ? hit.point
                : aimRay.GetPoint(config.AttackRange);
        }

        public bool TryShoot(Camera gameCamera, out RaycastHit hit)
        {
            hit = default;
            if (gameCamera == null || Time.time < nextFireTime)
            {
                return false;
            }

            float shotsPerSecond = Mathf.Max(0.01f, config.AttackSpeed);
            nextFireTime = Time.time + 1f / shotsPerSecond;
            TryGetClosestValidHit(GetAimRay(gameCamera), out hit);
            return true;
        }

public bool IsValidEnemyHit(RaycastHit hit)
        {
            if (hit.collider == null)
            {
                return false;
            }

            Transform current = hit.collider.transform;
            while (current != null && !current.CompareTag("Enemy"))
            {
                current = current.parent;
            }

            if (current == null)
            {
                return false;
            }

            Component damageComponent = current.GetComponent(typeof(IDamageable))
                ?? current.GetComponentInChildren(typeof(IDamageable), true);
            return damageComponent is IDamageable damageable && !damageable.IsDead;
        }


        private bool TryGetClosestValidHit(Ray ray, out RaycastHit closestHit)
        {
            int hitCount = Physics.RaycastNonAlloc(
                ray,
                hitBuffer,
                config.AttackRange,
                ~0,
                QueryTriggerInteraction.Collide);
            RaycastHit closestSolidHit = default;
            RaycastHit closestWeakPointHit = default;
            float closestSolidDistance = float.PositiveInfinity;
            float closestWeakPointDistance = float.PositiveInfinity;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit candidate = hitBuffer[i];
                if (candidate.collider == null
                    || IsOwnerCollider(candidate.collider)
                    || !IsValidShotCollider(candidate.collider))
                {
                    continue;
                }

                if (candidate.collider.isTrigger)
                {
                    if (candidate.distance < closestWeakPointDistance)
                    {
                        closestWeakPointDistance = candidate.distance;
                        closestWeakPointHit = candidate;
                    }
                }
                else if (candidate.distance < closestSolidDistance)
                {
                    closestSolidDistance = candidate.distance;
                    closestSolidHit = candidate;
                }
            }

            bool weakPointIsVisible = closestWeakPointHit.collider != null
                && (closestSolidHit.collider == null
                    || closestWeakPointDistance <= closestSolidDistance
                    || BelongsToSameDamageable(
                        closestWeakPointHit.collider,
                        closestSolidHit.collider));
            closestHit = weakPointIsVisible
                ? closestWeakPointHit
                : closestSolidHit;
            return closestHit.collider != null;
        }

        private static bool BelongsToSameDamageable(Collider first, Collider second)
        {
            if (first == null || second == null)
            {
                return false;
            }

            Component firstDamageable = first.GetComponentInParent(
                typeof(IDamageable),
                true);
            Component secondDamageable = second.GetComponentInParent(
                typeof(IDamageable),
                true);
            return firstDamageable != null && firstDamageable == secondDamageable;
        }
        private static bool IsValidShotCollider(Collider candidate)
        {
            if (candidate == null || !candidate.isTrigger)
            {
                return candidate != null;
            }

            Component resolverComponent = candidate.GetComponentInParent(
                typeof(IHitPartResolver),
                true);
            return resolverComponent is IHitPartResolver resolver
                && resolver.TryResolveHitPart(candidate, out _);
        }
        private bool IsOwnerCollider(Collider candidate)
        {
            if (ownerRoot == null || candidate == null)
            {
                return false;
            }

            Transform candidateTransform = candidate.transform;
            return candidateTransform == ownerRoot
                || candidateTransform.IsChildOf(ownerRoot);
        }
    }

    internal sealed class WeaponAimAssistResolver
    {
        private const int TargetBufferSize = 64;
        private const int VisibilityBufferSize = 32;
        private const string EnemyTag = "Enemy";

        private readonly WeaponConfig config;
        private readonly Transform ownerRoot;
        private readonly Collider[] targetBuffer = new Collider[TargetBufferSize];
        private readonly RaycastHit[] visibilityBuffer = new RaycastHit[VisibilityBufferSize];

        private Transform currentTargetRoot;
        private Collider currentTargetCollider;
        private float targetHoldUntil;
        public WeaponAimAssistResolver(WeaponConfig weaponConfig, Transform shootOwnerRoot)
        {
            config = weaponConfig;
            ownerRoot = shootOwnerRoot;
        }

        public bool TryGetTargetPoint(Camera gameCamera, out Vector3 targetPoint)
        {
            targetPoint = default;
            if (gameCamera == null || Mathf.Clamp01(config.AimAssistStrength) <= 0f)
            {
                ClearTarget();
                return false;
            }

            Ray originalRay = gameCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            bool found = TryResolveTarget(gameCamera, originalRay, out targetPoint);
            Ray targetRay = found && (targetPoint - originalRay.origin).sqrMagnitude > 0.000001f
                ? new Ray(originalRay.origin, (targetPoint - originalRay.origin).normalized)
                : originalRay;
            DrawDebug(gameCamera, originalRay, targetRay);
            return found;
        }


        private bool TryResolveTarget(
            Camera gameCamera,
            Ray originalRay,
            out Vector3 targetPoint)
        {
            if (currentTargetRoot != null
                && Time.unscaledTime < targetHoldUntil
                && TryEvaluateCandidate(
                    gameCamera,
                    originalRay,
                    currentTargetRoot,
                    currentTargetCollider,
                    out targetPoint,
                    out _))
            {
                return true;
            }

            float searchDistance = Mathf.Min(
                Mathf.Max(0f, config.AimAssistMaxDistance),
                Mathf.Max(0f, config.AttackRange));
            if (searchDistance <= 0f
                || config.AimAssistScreenRadius <= 0f
                || config.AimAssistMaxAngle <= 0f)
            {
                ClearTarget();
                targetPoint = default;
                return false;
            }

            int count = Physics.OverlapSphereNonAlloc(
                originalRay.origin,
                searchDistance,
                targetBuffer,
                config.AimAssistTargetMask,
                QueryTriggerInteraction.Ignore);

            float bestScore = float.PositiveInfinity;
            Transform bestRoot = null;
            Collider bestCollider = null;
            Vector3 bestPoint = default;
            for (int i = 0; i < count; i++)
            {
                Collider candidate = targetBuffer[i];
                if (candidate == null || IsOwnerCollider(candidate))
                {
                    continue;
                }

                Transform enemyRoot = FindTaggedAncestor(candidate.transform);
                if (enemyRoot == null
                    || !TryEvaluateCandidate(
                        gameCamera,
                        originalRay,
                        enemyRoot,
                        candidate,
                        out Vector3 candidatePoint,
                        out float score)
                    || score >= bestScore)
                {
                    continue;
                }

                bestScore = score;
                bestRoot = enemyRoot;
                bestCollider = candidate;
                bestPoint = candidatePoint;
            }

            if (bestRoot == null)
            {
                ClearTarget();
                targetPoint = default;
                return false;
            }

            currentTargetRoot = bestRoot;
            currentTargetCollider = bestCollider;
            targetHoldUntil = Time.unscaledTime
                + Mathf.Max(0f, config.AimAssistTargetHoldTime);
            targetPoint = bestPoint;
            return true;
        }

        private bool TryEvaluateCandidate(
            Camera gameCamera,
            Ray originalRay,
            Transform enemyRoot,
            Collider candidateCollider,
            out Vector3 targetPoint,
            out float score)
        {
            targetPoint = default;
            score = float.PositiveInfinity;
            if (enemyRoot == null
                || !enemyRoot.gameObject.activeInHierarchy
                || candidateCollider == null
                || !candidateCollider.enabled)
            {
                return false;
            }

            Component aimComponent = enemyRoot.GetComponent(typeof(IAimAssistTarget))
                ?? enemyRoot.GetComponentInChildren(typeof(IAimAssistTarget), true);
            if (aimComponent is IAimAssistTarget aimTarget
                && !aimTarget.IsValidAimTarget)
            {
                return false;
            }

            Component damageComponent = enemyRoot.GetComponent(typeof(IDamageable))
                ?? enemyRoot.GetComponentInChildren(typeof(IDamageable), true);
            if (damageComponent is IDamageable damageable && damageable.IsDead)
            {
                return false;
            }

            Transform namedAimPoint = ResolveNamedAimPoint(enemyRoot);
            namedAimPoint ??= aimComponent is IAimAssistTarget configuredTarget
                ? configuredTarget.AimPoint
                : null;
            targetPoint = namedAimPoint != null
                ? namedAimPoint.position
                : candidateCollider.bounds.center;

            Vector3 toTarget = targetPoint - originalRay.origin;
            float distance = toTarget.magnitude;
            if (distance <= 0.001f
                || distance > config.AimAssistMaxDistance
                || Vector3.Dot(originalRay.direction, toTarget) <= 0f)
            {
                return false;
            }

            float angle = Vector3.Angle(originalRay.direction, toTarget);
            if (angle > config.AimAssistMaxAngle)
            {
                return false;
            }

            Vector3 screenPoint = gameCamera.WorldToScreenPoint(targetPoint);
            if (screenPoint.z <= 0f)
            {
                return false;
            }

            Vector2 screenCenter = new Vector2(
                gameCamera.pixelWidth * 0.5f,
                gameCamera.pixelHeight * 0.5f);
            float screenDistanceSq =
                ((Vector2)screenPoint - screenCenter).sqrMagnitude;
            float screenRadius = config.AimAssistScreenRadius;
            if (screenDistanceSq > screenRadius * screenRadius
                || !HasLineOfSight(originalRay.origin, targetPoint, enemyRoot))
            {
                return false;
            }

            float distanceWeight = distance
                / Mathf.Max(0.01f, config.AimAssistMaxDistance);
            score = screenDistanceSq
                + distanceWeight * screenRadius * screenRadius * 0.05f;
            return true;
        }

        private bool HasLineOfSight(
            Vector3 origin,
            Vector3 targetPoint,
            Transform targetRoot)
        {
            Vector3 toTarget = targetPoint - origin;
            float distance = toTarget.magnitude;
            if (distance <= 0.001f)
            {
                return true;
            }

            int hitCount = Physics.RaycastNonAlloc(
                origin,
                toTarget / distance,
                visibilityBuffer,
                distance,
                config.AimAssistObstacleMask,
                QueryTriggerInteraction.Ignore);

            float closestDistance = float.PositiveInfinity;
            Collider closestCollider = null;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = visibilityBuffer[i];
                if (hit.collider == null
                    || IsOwnerCollider(hit.collider)
                    || hit.distance >= closestDistance)
                {
                    continue;
                }

                closestDistance = hit.distance;
                closestCollider = hit.collider;
            }

            return closestCollider == null
                || FindTaggedAncestor(closestCollider.transform) == targetRoot;
        }

        private bool IsOwnerCollider(Collider candidate)
        {
            if (ownerRoot == null || candidate == null)
            {
                return false;
            }

            Transform candidateTransform = candidate.transform;
            return candidateTransform == ownerRoot
                || candidateTransform.IsChildOf(ownerRoot);
        }

        private static Transform FindTaggedAncestor(Transform start)
        {
            Transform current = start;
            while (current != null)
            {
                if (current.CompareTag(EnemyTag))
                {
                    return current;
                }

                current = current.parent;
            }

            return null;
        }

        private static Transform ResolveNamedAimPoint(Transform enemyRoot)
        {
            Transform result = FindDescendant(enemyRoot, "HeadShot");
            if (result != null)
            {
                return result;
            }

            result = FindDescendant(enemyRoot, "AimPoint");
            if (result != null)
            {
                return result;
            }

            result = FindDescendant(enemyRoot, "HeadPoint");
            return result != null
                ? result
                : FindDescendant(enemyRoot, "ChestPoint");
        }


        private static Transform FindDescendant(Transform root, string targetName)
        {
            if (root == null)
            {
                return null;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == targetName)
                {
                    return child;
                }

                Transform nested = FindDescendant(child, targetName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private void ClearTarget()
        {
            currentTargetRoot = null;
            currentTargetCollider = null;
            targetHoldUntil = 0f;
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void DrawDebug(Camera gameCamera, Ray originalRay, Ray finalRay)
        {
            if (!config.ShowAimAssistDebug)
            {
                return;
            }

            Debug.DrawRay(
                originalRay.origin,
                originalRay.direction * config.AimAssistMaxDistance,
                Color.cyan);
            Debug.DrawRay(
                finalRay.origin,
                finalRay.direction * config.AimAssistMaxDistance,
                Color.green);

            float debugDistance = config.AimAssistMaxDistance;
            float debugAngle = config.AimAssistMaxAngle;
            Vector3 cameraUp = gameCamera.transform.up;
            Vector3 cameraRight = gameCamera.transform.right;
            Debug.DrawRay(
                originalRay.origin,
                Quaternion.AngleAxis(debugAngle, cameraUp)
                    * originalRay.direction * debugDistance,
                Color.gray);
            Debug.DrawRay(
                originalRay.origin,
                Quaternion.AngleAxis(-debugAngle, cameraUp)
                    * originalRay.direction * debugDistance,
                Color.gray);
            Debug.DrawRay(
                originalRay.origin,
                Quaternion.AngleAxis(debugAngle, cameraRight)
                    * originalRay.direction * debugDistance,
                Color.gray);
            Debug.DrawRay(
                originalRay.origin,
                Quaternion.AngleAxis(-debugAngle, cameraRight)
                    * originalRay.direction * debugDistance,
                Color.gray);
            if (currentTargetRoot != null)
            {
                Debug.DrawLine(
                    originalRay.origin,
                    currentTargetCollider != null
                        ? currentTargetCollider.bounds.center
                        : currentTargetRoot.position,
                    Color.yellow);
            }
        }
    }
}