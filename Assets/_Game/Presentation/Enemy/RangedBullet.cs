using TPS.Combat.Application;
using TPS.Combat.Domain;
using UnityEngine;

namespace TPS.Enemy.Presentation
{
    [DisallowMultipleComponent]
    public sealed class RangedBullet : MonoBehaviour, IDodgeDetectableProjectile
    {
        private const int HitBufferSize = 16;

        private readonly RaycastHit[] hitBuffer = new RaycastHit[HitBufferSize];
        private ParticleSystem[] particleSystems;
        private PoolItem poolItem;
        private TrailRenderer trail;
        private Material trailMaterial;
        private Transform ownerRoot;
        private Transform targetRoot;
        private Vector3 direction;
        private float speed;
        private float damage;
        private float hitRadius;
        private float remainingLifetime;
        private long attackId;
        private bool active;
        private bool canTriggerDodge;
        private bool dodgeRewarded;

        public long AttackId => attackId;
        public ProjectileType ProjectileType => ProjectileType.EnemyBullet;
        public Vector3 Position => transform.position;
        public Vector3 Velocity => direction * speed;
        public float HitRadius => hitRadius;
        public bool CanTriggerDodge => canTriggerDodge;
        public bool IsDodgeCandidateActive => active && gameObject.activeInHierarchy;

        public void Configure(
            Vector3 position,
            Vector3 flightDirection,
            float bulletSpeed,
            float bulletDamage,
            float bulletHitRadius,
            float lifetime,
            bool allowDodgeTrigger,
            Transform owner,
            Transform target,
            long sourceAttackId)
        {
            CacheComponents();
            ownerRoot = owner;
            targetRoot = target;
            direction = flightDirection.sqrMagnitude > 0.0001f
                ? flightDirection.normalized
                : transform.forward;
            speed = Mathf.Max(0.01f, bulletSpeed);
            damage = Mathf.Max(0f, bulletDamage);
            hitRadius = Mathf.Max(0.01f, bulletHitRadius);
            remainingLifetime = Mathf.Max(0.1f, lifetime);
            attackId = sourceAttackId;
            canTriggerDodge = allowDodgeTrigger;
            dodgeRewarded = false;
            active = true;

            transform.SetPositionAndRotation(
                position,
                Quaternion.LookRotation(direction, Vector3.up));
            trail.Clear();
            PlayParticles();
            ProjectileDodgeRegistry.Register(this);
        }

        public bool TryMarkDodgeRewarded()
        {
            if (!IsDodgeCandidateActive || dodgeRewarded)
            {
                return false;
            }

            dodgeRewarded = true;
            return true;
        }

        private void Update()
        {
            if (!active)
            {
                return;
            }

            float distance = speed * Time.deltaTime;
            if (distance > 0f && TryGetClosestHit(distance, out RaycastHit hit))
            {
                transform.position = hit.point;
                ApplyDamage(hit.collider);
                ReturnToPool();
                return;
            }

            transform.position += direction * distance;
            remainingLifetime -= Time.deltaTime;
            if (remainingLifetime <= 0f)
            {
                ReturnToPool();
            }
        }

        private void OnDisable()
        {
            ProjectileDodgeRegistry.Unregister(this);
            active = false;
            if (trail != null)
            {
                trail.Clear();
            }
        }

        private void OnDestroy()
        {
            ProjectileDodgeRegistry.Unregister(this);
            if (trailMaterial != null)
            {
                Destroy(trailMaterial);
            }
        }

        private bool TryGetClosestHit(float distance, out RaycastHit closestHit)
        {
            int count = Physics.SphereCastNonAlloc(
                transform.position,
                hitRadius,
                direction,
                hitBuffer,
                distance,
                ~0,
                QueryTriggerInteraction.Ignore);

            closestHit = default;
            float closestDistance = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                RaycastHit candidate = hitBuffer[i];
                if (candidate.collider == null
                    || IsOwnedCollider(candidate.collider)
                    || (dodgeRewarded && IsTargetCollider(candidate.collider))
                    || candidate.distance >= closestDistance)
                {
                    continue;
                }

                closestDistance = candidate.distance;
                closestHit = candidate;
            }

            return closestHit.collider != null;
        }

        private void ApplyDamage(Collider hitCollider)
        {
            if (dodgeRewarded || hitCollider == null || targetRoot == null)
            {
                return;
            }

            if (!IsTargetCollider(hitCollider))
            {
                return;
            }

            Component component = hitCollider.GetComponentInParent(
                typeof(IDamageable),
                true);
            if (component is not IDamageable damageable || damageable.IsDead)
            {
                return;
            }

            DamageInfo damageInfo = new(
                damage,
                DamageSourceKind.Other,
                ownerRoot);
            if (damageable is IIdentifiedAttackDamageable identifiedDamageable)
            {
                identifiedDamageable.ApplyDamage(damageInfo, attackId);
            }
            else if (damageable is IAttributedDamageable attributedDamageable)
            {
                attributedDamageable.ApplyDamage(damageInfo);
            }
            else
            {
                damageable.ApplyDamage(damage);
            }
        }

        private bool IsOwnedCollider(Collider candidate)
        {
            if (candidate == null || ownerRoot == null)
            {
                return false;
            }

            Transform candidateTransform = candidate.transform;
            return candidateTransform == ownerRoot
                || candidateTransform.IsChildOf(ownerRoot);
        }

        private bool IsTargetCollider(Collider candidate)
        {
            if (candidate == null || targetRoot == null)
            {
                return false;
            }

            Transform candidateTransform = candidate.transform;
            return candidateTransform == targetRoot
                || candidateTransform.IsChildOf(targetRoot);
        }

        private void CacheComponents()
        {
            poolItem ??= GetComponent<PoolItem>();
            particleSystems ??= GetComponentsInChildren<ParticleSystem>(true);
            trail ??= GetComponent<TrailRenderer>();
            if (trail == null)
            {
                trail = gameObject.AddComponent<TrailRenderer>();
                trail.time = 0.25f;
                trail.minVertexDistance = 0.03f;
                trail.widthCurve = AnimationCurve.Linear(0f, 0.12f, 1f, 0.015f);
                trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                trail.receiveShadows = false;
                trailMaterial = CreateTrailMaterial();
                trail.sharedMaterial = trailMaterial;
            }

            Gradient gradient = new();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.05f, 0.02f), 0f),
                    new GradientColorKey(new Color(0.45f, 0f, 0f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            trail.colorGradient = gradient;
        }

        private static Material CreateTrailMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Sprites/Default");
            return shader != null ? new Material(shader) : null;
        }

        private void PlayParticles()
        {
            if (particleSystems == null)
            {
                return;
            }

            Color enemyBulletColor = new(1f, 0.03f, 0.01f, 1f);
            for (int i = 0; i < particleSystems.Length; i++)
            {
                ParticleSystem particleSystem = particleSystems[i];
                ParticleSystem.MainModule main = particleSystem.main;
                main.startColor = enemyBulletColor;
                particleSystem.Stop(
                    false,
                    ParticleSystemStopBehavior.StopEmittingAndClear);
                particleSystem.Play(false);
            }
        }

        private void ReturnToPool()
        {
            ProjectileDodgeRegistry.Unregister(this);
            active = false;
            poolItem ??= GetComponent<PoolItem>();
            poolItem?.ReturnToPool();
        }
    }
}
