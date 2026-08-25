using System;
using TPS.BulletTime.Domain;
using TPS.BulletTime.Infrastructure;
using TPS.Combat.Application;
using TPS.Combat.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TPS.BulletTime.Application
{
    [DefaultExecutionOrder(500)]
    [DisallowMultipleComponent]
    public sealed class BulletTimeController : MonoBehaviour
    {
        private static BulletTimeController current;

        private BulletTimeConfig config;
        private BulletTimeEnergyModel energyModel;
        private PerfectDodgeBulletTimeModel dodgeEnergyModel;
        private BulletTimeSource currentSource = BulletTimeSource.None;
        private float defaultFixedDeltaTime;
        private float appliedTimeScale = 1f;
        private float lookSensitivityMultiplier = 1f;
        private float recoveryDelayRemaining;

        public static BulletTimeController Current => current;
        public BulletTimeConfig Config => config;
        public BulletTimeState State { get; private set; } = BulletTimeState.Inactive;
        public float CurrentEnergy => energyModel != null ? energyModel.CurrentEnergy : 0f;
        public float MaxEnergy => energyModel != null ? energyModel.MaxEnergy : 0f;
        public BulletTimeSource CurrentSource => currentSource;
        public float DodgeCost => config != null ? config.DodgeCost : 0f;
        public bool IsActive => State == BulletTimeState.Active || State == BulletTimeState.Entering;
        public float LookSensitivityMultiplier => lookSensitivityMultiplier;
        public float EffectWeight => config == null
            ? 0f
            : Mathf.InverseLerp(1f, config.TimeScale, appliedTimeScale);

        public event Action<float, float, BulletTimeEnergyChangeReason> EnergyChanged;
        public event Action<BulletTimeState> StateChanged;
        public event Action<BulletTimeSource> SourceChanged;
        public event Action ActivationRejected;

        public static BulletTimeController EnsureRuntimeInstance()
        {
            if (current != null)
            {
                return current;
            }

            current = FindFirstObjectByType<BulletTimeController>();
            if (current != null)
            {
                return current;
            }

            GameObject gameRoot = GameObject.Find("GameRoot") ?? new GameObject("GameRoot");
            Transform runtimeSystems = gameRoot.transform.Find("RuntimeSystems");
            if (runtimeSystems == null)
            {
                GameObject systemsObject = new GameObject("RuntimeSystems");
                runtimeSystems = systemsObject.transform;
                runtimeSystems.SetParent(gameRoot.transform, false);
            }

            Transform systemTransform = runtimeSystems.Find("BulletTimeSystem");
            if (systemTransform == null)
            {
                GameObject systemObject = new GameObject("BulletTimeSystem");
                systemTransform = systemObject.transform;
                systemTransform.SetParent(runtimeSystems, false);
            }

            DontDestroyOnLoad(gameRoot);
            current = systemTransform.gameObject.AddComponent<BulletTimeController>();
            return current;
        }

        private void Awake()
        {
            if (current != null && current != this)
            {
                Destroy(gameObject);
                return;
            }

            current = this;
            config = BulletTimeConfigProvider.Load();
            energyModel = new BulletTimeEnergyModel(config.MaxEnergy);
            dodgeEnergyModel = new PerfectDodgeBulletTimeModel(config.DodgeBulletTimeMaxEnergy);
            defaultFixedDeltaTime = Time.fixedDeltaTime / Mathf.Max(0.0001f, Time.timeScale);
            appliedTimeScale = 1f;
            ApplyTimeScale(1f);
        }

        private void OnEnable()
        {
            CombatEventHub.EnemyKilled += OnEnemyKilled;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnDisable()
        {
            CombatEventHub.EnemyKilled -= OnEnemyKilled;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            RestoreTimeScaleImmediate();
        }

        private void OnDestroy()
        {
            RestoreTimeScaleImmediate();
            if (current == this)
            {
                current = null;
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                ForceExit();
            }
        }

        private void OnApplicationQuit()
        {
            RestoreTimeScaleImmediate();
        }

        private void Update()
        {
            Tick(Time.unscaledDeltaTime);
        }

internal void Tick(float deltaTime)
        {
            deltaTime = Mathf.Max(0f, deltaTime);
            switch (State)
            {
                case BulletTimeState.Entering:
                    if (MoveTimeScale(config.TimeScale, config.EnterTransitionDuration, deltaTime))
                    {
                        SetState(BulletTimeState.Active);
                    }
                    break;

                case BulletTimeState.Active:
                    ApplyTimeScale(config.TimeScale);
                    if (currentSource == BulletTimeSource.PerfectDodge)
                    {
                        dodgeEnergyModel.Consume(config.DodgeBulletTimeConsumePerSecond * deltaTime);
                        if (dodgeEnergyModel.CurrentEnergy <= 0f)
                        {
                            BeginExit();
                        }
                    }
                    else
                    {
                        ChangeEnergy(
                            -config.ConsumePerSecond * deltaTime,
                            BulletTimeEnergyChangeReason.Consume);
                        if (CurrentEnergy <= 0f)
                        {
                            BeginExit();
                        }
                    }
                    break;

                case BulletTimeState.Exiting:
                    if (MoveTimeScale(1f, config.ExitTransitionDuration, deltaTime))
                    {
                        recoveryDelayRemaining = config.RecoveryDelay;
                        SetState(BulletTimeState.Inactive);
                    }
                    break;

                case BulletTimeState.Inactive:
                    UpdateRecovery(deltaTime);
                    break;

                case BulletTimeState.Disabled:
                    ApplyTimeScale(1f);
                    break;
            }

            UpdateLookSensitivity(deltaTime);
        }

        public void Toggle()
        {
            if (State == BulletTimeState.Inactive)
            {
                EnterBulletTime(BulletTimeSource.Normal);
            }
            else if (currentSource == BulletTimeSource.Normal)
            {
                BeginExit();
            }
        }

        public bool EnterBulletTime(BulletTimeSource source)
        {
            switch (source)
            {
                case BulletTimeSource.Normal:
                    return TryEnterNormal();
                case BulletTimeSource.PerfectDodge:
                    return TriggerPerfectDodge();
                default:
                    return false;
            }
        }

        private bool TryEnterNormal()
        {
            if (State != BulletTimeState.Inactive)
            {
                return false;
            }

            if (CurrentEnergy < config.MinimumActivationEnergy)
            {
                ActivationRejected?.Invoke();
                return false;
            }

            SetSource(BulletTimeSource.Normal);
            SetState(BulletTimeState.Entering);
            return true;
        }

        public bool TryConsumeNormalEnergy(float amount)
        {
            if (energyModel == null || CurrentEnergy < amount)
            {
                return false;
            }

            ChangeEnergy(-amount, BulletTimeEnergyChangeReason.Consume);
            return true;
        }

public bool TriggerPerfectDodge()
        {
            if (config == null || dodgeEnergyModel == null || State == BulletTimeState.Disabled)
            {
                return false;
            }

            dodgeEnergyModel.Reset(config.PerfectDodgeEnergy);
            SetSource(BulletTimeSource.PerfectDodge);

            if (State == BulletTimeState.Inactive || State == BulletTimeState.Exiting)
            {
                SetState(BulletTimeState.Entering);
            }

            return true;
        }


        public void BeginExit()
        {
            if (State != BulletTimeState.Active && State != BulletTimeState.Entering)
            {
                return;
            }

            SetState(BulletTimeState.Exiting);
        }

        public void SetDisabled(bool disabled)
        {
            if (disabled)
            {
                RestoreTimeScaleImmediate();
                SetState(BulletTimeState.Disabled);
                return;
            }

            if (State == BulletTimeState.Disabled)
            {
                recoveryDelayRemaining = config.RecoveryDelay;
                SetState(BulletTimeState.Inactive);
            }
        }

        public void ForceExit()
        {
            RestoreTimeScaleImmediate();
            recoveryDelayRemaining = config != null ? config.RecoveryDelay : 0f;
            if (State != BulletTimeState.Disabled)
            {
                SetState(BulletTimeState.Inactive);
            }
        }

        private void UpdateRecovery(float deltaTime)
        {
            if (CurrentEnergy >= MaxEnergy)
            {
                return;
            }

            if (recoveryDelayRemaining > 0f)
            {
                recoveryDelayRemaining = Mathf.Max(0f, recoveryDelayRemaining - deltaTime);
                return;
            }

            float percent = CurrentEnergy <= config.LowEnergyThreshold
                ? config.LowRangeRecoveryPercentPerSecond
                : config.HighRangeRecoveryPercentPerSecond;
            ChangeEnergy(MaxEnergy * percent * deltaTime, BulletTimeEnergyChangeReason.NaturalRecovery);
        }

        private void OnEnemyKilled(EnemyKilledEvent killedEvent)
        {
            if (killedEvent.Source != DamageSourceKind.Player || killedEvent.Instigator == null)
            {
                return;
            }

            ChangeEnergy(
                MaxEnergy * config.KillRecoveryPercent,
                BulletTimeEnergyChangeReason.KillRecovery);
        }

        private void ChangeEnergy(float delta, BulletTimeEnergyChangeReason reason)
        {
            bool changed = delta < 0f
                ? energyModel.Consume(-delta)
                : energyModel.Recover(delta);
            if (changed)
            {
                EnergyChanged?.Invoke(CurrentEnergy, MaxEnergy, reason);
            }
        }

        private bool MoveTimeScale(float target, float duration, float unscaledDeltaTime)
        {
            float distance = Mathf.Abs(1f - config.TimeScale);
            float maxDelta = distance * unscaledDeltaTime / Mathf.Max(0.01f, duration);
            ApplyTimeScale(Mathf.MoveTowards(appliedTimeScale, target, maxDelta));
            return Mathf.Abs(appliedTimeScale - target) <= 0.0001f;
        }

        private void ApplyTimeScale(float value)
        {
            appliedTimeScale = Mathf.Clamp(value, 0.01f, 1f);
            Time.timeScale = appliedTimeScale;
            Time.fixedDeltaTime = defaultFixedDeltaTime * appliedTimeScale;
        }

        private void RestoreTimeScaleImmediate()
        {
            appliedTimeScale = 1f;
            lookSensitivityMultiplier = 1f;
            Time.timeScale = 1f;
            if (defaultFixedDeltaTime > 0f)
            {
                Time.fixedDeltaTime = defaultFixedDeltaTime;
            }
        }

        private void SetState(BulletTimeState nextState)
        {
            if (State == nextState)
            {
                return;
            }

            State = nextState;
            if (nextState == BulletTimeState.Inactive || nextState == BulletTimeState.Disabled)
            {
                SetSource(BulletTimeSource.None);
            }
            StateChanged?.Invoke(State);
        }

        private void SetSource(BulletTimeSource source)
        {
            if (currentSource == source)
            {
                return;
            }

            currentSource = source;
            SourceChanged?.Invoke(currentSource);
        }

        private void OnSceneUnloaded(Scene scene)
        {
            ForceExit();
        }
    

private void UpdateLookSensitivity(float unscaledDeltaTime)
        {
            bool useBulletTimeSensitivity = State == BulletTimeState.Entering
                || State == BulletTimeState.Active;
            float targetMultiplier = useBulletTimeSensitivity
                ? config.BulletTimeLookSensitivityMultiplier
                : 1f;
            float transitionRange = Mathf.Max(
                0.0001f,
                1f - config.BulletTimeLookSensitivityMultiplier);
            float maxDelta = transitionRange
                * unscaledDeltaTime
                / Mathf.Max(0.01f, config.LookSensitivityTransitionDuration);

            lookSensitivityMultiplier = Mathf.MoveTowards(
                lookSensitivityMultiplier,
                targetMultiplier,
                maxDelta);
        }
}
}
