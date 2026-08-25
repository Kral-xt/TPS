using UnityEngine;
using UnityEngine.UI;
using QFramework;
using TPS.BulletTime.Application;
using TPS.BulletTime.Domain;
using TPS.Combat.Application;
using TPS.Player.Application;
using TPS.Player.Domain;
using TPS.Player.Presentation;

namespace QFramework.Example
{
    public enum CrosshairState
    {
        Normal,
        TargetingEnemy,
        HitEnemy
    }
    public class BattleViewData : UIPanelData
    {
    }

    public partial class BattleView : UIPanel
    {
        private float fpsTimer;
        private int fpsFrameCount;
        private float fpsValue;
        private TPS.Player.TpsPrototypePlayerController mCachedPlayer;
        private float mPlayerSearchTimer;
        
        private Image mCrosshairBackground;
        private bool mCrosshairEventsRegistered;
        private bool mIsTargetingEnemy;
        private float mCrosshairHitRemaining;

        [Header("准星反馈")]
        [SerializeField] private Color normalCrosshairColor = Color.green;
        [SerializeField] private Color targetCrosshairColor = Color.yellow;
        [SerializeField] private Color hitCrosshairColor = Color.red;
        [SerializeField, Min(0.01f)] private float hitCrosshairDuration = 0.3f;
        private bool? mCrosshairVisible;
        private PlayerAttributeController mPlayerAttributes;

        private BulletTimeController mBulletTimeController;
        private Slider mBulletTimeSlider;
        private Slider mBulletTimeEffectSlider;
        private Image mBulletTimeFill;
        private Color mBulletTimeBaseColor = Color.white;
        private Color mBulletTimeFlashColor = Color.white;
        private float mBulletTimeFlashRemaining;

        [Header("子弹时间 UI 反馈")]
        [SerializeField] private Color mBulletTimeRecoverColor = new Color(0.35f, 1f, 0.9f, 1f);
        [SerializeField, Min(0.01f)] private float mBulletTimeRecoverStopDelay = 0.35f;

        private float mPreviousBulletTimeEnergy;
        private float mBulletTimeRecoverStopTimer;
        private float mBulletTimePreviewTarget;
        private bool mBulletTimeEnergyInitialized;
        private bool mBulletTimeRecoverEffectPlaying;

        private TPS.Player.Presentation.EmojiBar mEmojiBar;
        private TPS.Player.Presentation.EmojiBarController mEmojiBarController;
        private bool mBulletTimePreviewActive;

        protected override void OnInit(IUIData uiData = null)
        {
            mData = uiData as BattleViewData ?? new BattleViewData();
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            ResetDisplayBeforeBinding();

            fpsTimer = 0f;
            fpsFrameCount = 0;
            fpsValue = 0f;
            mPlayerSearchTimer = 0f;
            mCachedPlayer = FindFirstObjectByType<TPS.Player.TpsPrototypePlayerController>();
            
            ResolveCrosshairBackground();
            RegisterCrosshairEvents();
            mIsTargetingEnemy = CombatPresentationEvents.IsTargetingEnemy;
            mCrosshairHitRemaining = 0f;
            ApplyCrosshairState();
            mCrosshairVisible = null;

            BindPlayerAttributes();
            UpdateCrosshairVisibility();
            BindBulletTime();
            try
            {
                BindEmojiBar();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BattleView] BindEmojiBar exception: {e}");
            }
        }

        
        protected override void OnShow()
        {
            RegisterCrosshairEvents();
            mIsTargetingEnemy = CombatPresentationEvents.IsTargetingEnemy;
            ApplyCrosshairState();
            if (mPlayerAttributes == null)
            {
                BindPlayerAttributes();
            }

            if (mBulletTimeController == null)
            {
                BindBulletTime();
            }

            if (mEmojiBar == null)
            {
                BindEmojiBar();
            }
        }

        private void Update()
        {
            fpsFrameCount++;
            fpsTimer += Time.unscaledDeltaTime;
            if (fpsTimer >= 0.25f)
            {
                fpsValue = fpsFrameCount / fpsTimer;
                if (FPS != null)
                {
                    FPS.text = $"FPS: {fpsValue:F1}";
                }

                fpsFrameCount = 0;
                fpsTimer = 0f;
            }

            if (mCachedPlayer == null)
            {
                mPlayerSearchTimer += Time.unscaledDeltaTime;
                if (mPlayerSearchTimer >= 2f)
                {
                    mCachedPlayer = FindFirstObjectByType<TPS.Player.TpsPrototypePlayerController>();
                    mPlayerSearchTimer = 0f;
                    BindPlayerAttributes();
                }
            }

            if (Speed != null)
            {
                Speed.text = mCachedPlayer != null
                    ? $"Speed: {mCachedPlayer.CurrentSpeed:F1}"
                    : "Speed: 0.0";
            }

            
            UpdateCrosshairFeedback();
            UpdateCrosshairVisibility();
            UpdateBulletTimeFeedback();
        }

        private void ResetDisplayBeforeBinding()
        {
            if (Slider_HP != null)
            {
                Slider_HP.minValue = 0f;
                Slider_HP.maxValue = 1f;
                Slider_HP.value = 0f;
            }

            if (Text_Hp != null)
            {
                Text_Hp.text = string.Empty;
            }

            if (Slider_EXP != null)
            {
                Slider_EXP.minValue = 0f;
                Slider_EXP.maxValue = 1f;
                Slider_EXP.value = 0f;
            }

            if (Text_Level != null)
            {
                Text_Level.text = string.Empty;
            }

            if (FPS != null)
            {
                FPS.text = "FPS: 0";
            }

            if (Speed != null)
            {
                Speed.text = "Speed: 0.0";
            }
        }

        private void UpdateCrosshairVisibility()
        {
            bool visible = mCachedPlayer != null && mCachedPlayer.IsAiming;
            if (crosshair == null || mCrosshairVisible == visible)
            {
                return;
            }

            crosshair.gameObject.SetActive(visible);
            mCrosshairVisible = visible;
        }

        private void ResolveCrosshairBackground()
        {
            mCrosshairBackground = null;
            if (crosshair == null)
            {
                return;
            }

            Transform background = crosshair.transform.Find("Bg");
            mCrosshairBackground = background != null
                ? background.GetComponent<Image>()
                : crosshair;
        }

        private void RegisterCrosshairEvents()
        {
            if (mCrosshairEventsRegistered)
            {
                return;
            }

            CombatPresentationEvents.CrosshairTargetChanged += OnCrosshairTargetChanged;
            CombatPresentationEvents.EnemyHit += OnEnemyHit;
            mCrosshairEventsRegistered = true;
        }

        private void UnregisterCrosshairEvents()
        {
            if (!mCrosshairEventsRegistered)
            {
                return;
            }

            CombatPresentationEvents.CrosshairTargetChanged -= OnCrosshairTargetChanged;
            CombatPresentationEvents.EnemyHit -= OnEnemyHit;
            mCrosshairEventsRegistered = false;
            mCrosshairHitRemaining = 0f;
            mIsTargetingEnemy = false;
            ApplyCrosshairState();
        }

        private void OnCrosshairTargetChanged(bool isTargetingEnemy)
        {
            mIsTargetingEnemy = isTargetingEnemy;
            if (mCrosshairHitRemaining <= 0f)
            {
                ApplyCrosshairState();
            }
        }

        private void OnEnemyHit()
        {
            mCrosshairHitRemaining = hitCrosshairDuration;
            ApplyCrosshairState();
        }

        private void UpdateCrosshairFeedback()
        {
            if (mCrosshairHitRemaining <= 0f)
            {
                return;
            }

            mCrosshairHitRemaining = Mathf.Max(
                0f,
                mCrosshairHitRemaining - Time.unscaledDeltaTime);
            if (mCrosshairHitRemaining <= 0f)
            {
                ApplyCrosshairState();
            }
        }







        private void ApplyCrosshairState()
        {
            if (mCrosshairBackground == null)
            {
                ResolveCrosshairBackground();
            }

            if (mCrosshairBackground == null)
            {
                return;
            }

            CrosshairState state = mCrosshairHitRemaining > 0f
                ? CrosshairState.HitEnemy
                : mIsTargetingEnemy
                    ? CrosshairState.TargetingEnemy
                    : CrosshairState.Normal;

            switch (state)
            {
                case CrosshairState.HitEnemy:
                    mCrosshairBackground.color = hitCrosshairColor;
                    break;
                case CrosshairState.TargetingEnemy:
                    mCrosshairBackground.color = targetCrosshairColor;
                    break;
                default:
                    mCrosshairBackground.color = normalCrosshairColor;
                    break;
            }
        }

        private void BindPlayerAttributes()
        {
            UnbindPlayerAttributes();
            if (mCachedPlayer == null)
            {
                BindEmojiPlayer();
                return;
            }

            PlayerAttributeRuntimeBootstrap.EnsureFor(mCachedPlayer.gameObject);
            BindEmojiPlayer();
            mPlayerAttributes = mCachedPlayer.GetComponent<PlayerAttributeController>();
            if (mPlayerAttributes == null)
            {
                return;
            }

            mPlayerAttributes.HpChanged += OnPlayerHpChanged;
            mPlayerAttributes.ExpChanged += OnPlayerExpChanged;
            mPlayerAttributes.LevelChanged += OnPlayerLevelChanged;
            mPlayerAttributes.Died += OnPlayerDied;

            UpdateHpDisplay(mPlayerAttributes.CurrentHp, mPlayerAttributes.MaxHp);
            UpdateExpDisplay(
                mPlayerAttributes.Level,
                mPlayerAttributes.CurrentExp,
                mPlayerAttributes.RequiredExp);
        }

        private void UnbindPlayerAttributes()
        {
            if (mPlayerAttributes != null)
            {
                mPlayerAttributes.HpChanged -= OnPlayerHpChanged;
                mPlayerAttributes.ExpChanged -= OnPlayerExpChanged;
                mPlayerAttributes.LevelChanged -= OnPlayerLevelChanged;
                mPlayerAttributes.Died -= OnPlayerDied;
            }

            mPlayerAttributes = null;
        }

        private void OnPlayerHpChanged(PlayerHpChangedEvent hpEvent)
        {
            UpdateHpDisplay(hpEvent.CurrentHp, hpEvent.MaxHp);
        }

        private void OnPlayerExpChanged(PlayerExpChangedEvent expEvent)
        {
            UpdateExpDisplay(expEvent.Level, expEvent.CurrentExp, expEvent.RequiredExp);
        }

        private void OnPlayerLevelChanged(PlayerLevelChangedEvent levelEvent)
        {
            UpdateLevelDisplay(levelEvent.CurrentLevel);
        }

        private void OnPlayerDied(PlayerDiedEvent diedEvent)
        {
            float maxHp = mPlayerAttributes != null ? mPlayerAttributes.MaxHp : 1f;
            UpdateHpDisplay(0f, maxHp);
        }

        private void UpdateHpDisplay(float currentHp, float maxHp)
        {
            maxHp = Mathf.Max(1f, maxHp);
            currentHp = Mathf.Clamp(currentHp, 0f, maxHp);

            if (Slider_HP != null)
            {
                Slider_HP.minValue = 0f;
                Slider_HP.maxValue = maxHp;
                Slider_HP.value = currentHp;
            }

            if (Text_Hp != null)
            {
                Text_Hp.text = $"{Mathf.CeilToInt(currentHp)}/{Mathf.CeilToInt(maxHp)}";
            }
        }

        private void UpdateExpDisplay(int level, int currentExp, int requiredExp)
        {
            requiredExp = Mathf.Max(1, requiredExp);
            currentExp = Mathf.Clamp(currentExp, 0, requiredExp);

            if (Slider_EXP != null)
            {
                Slider_EXP.minValue = 0f;
                Slider_EXP.maxValue = requiredExp;
                Slider_EXP.value = currentExp;
            }

            UpdateLevelDisplay(level);
        }

        private void UpdateLevelDisplay(int level)
        {
            if (Text_Level != null)
            {
                Text_Level.text = level.ToString();
            }
        }

        private void BindBulletTime()
        {
            UnbindBulletTime();
            mBulletTimeController = BulletTimeController.EnsureRuntimeInstance();
            if (mBulletTimeController == null)
            {
                return;
            }

            mBulletTimeController.EnergyChanged += OnBulletTimeEnergyChanged;
            mBulletTimeController.StateChanged += OnBulletTimeStateChanged;
            mBulletTimeController.ActivationRejected += OnBulletTimeActivationRejected;

            ResolveBulletTimeSliders();
            if (mBulletTimeSlider != null && mBulletTimeSlider.fillRect != null)
            {
                mBulletTimeFill = mBulletTimeSlider.fillRect.GetComponent<Image>();
                if (mBulletTimeFill != null)
                {
                    mBulletTimeBaseColor = mBulletTimeFill.color;
                }
            }

            InitializeBulletTimeUI(
                mBulletTimeController.CurrentEnergy,
                mBulletTimeController.MaxEnergy);
        }

        private void ResolveBulletTimeSliders()
        {
            mBulletTimeSlider = null;
            mBulletTimeEffectSlider = null;
            if (BulletTimeSlider == null)
            {
                return;
            }

            Slider[] sliders = BulletTimeSlider.GetComponentsInChildren<Slider>(true);
            foreach (Slider slider in sliders)
            {
                if (slider == null)
                {
                    continue;
                }

                if (string.Equals(slider.name, "Slider 2", System.StringComparison.Ordinal))
                {
                    mBulletTimeEffectSlider = slider;
                }
                else if (string.Equals(slider.name, "Slider", System.StringComparison.Ordinal))
                {
                    mBulletTimeSlider = slider;
                }
            }

            if (mBulletTimeSlider == null)
            {
                foreach (Slider slider in sliders)
                {
                    if (slider != null
                        && slider != mBulletTimeEffectSlider
                        && slider.fillRect != null)
                    {
                        mBulletTimeSlider = slider;
                        break;
                    }
                }
            }

            if (mBulletTimeSlider == null)
            {
                Debug.LogError("[BattleView] 未找到子弹时间主 Slider。");
            }
        }

        private void InitializeBulletTimeUI(float currentEnergy, float maxEnergy)
        {
            maxEnergy = Mathf.Max(1f, maxEnergy);
            currentEnergy = Mathf.Clamp(currentEnergy, 0f, maxEnergy);

            if (mBulletTimeSlider != null)
            {
                mBulletTimeSlider.minValue = 0f;
                mBulletTimeSlider.maxValue = maxEnergy;
                mBulletTimeSlider.value = currentEnergy;
            }
            EndBulletTimeRecoveryPreview(currentEnergy, maxEnergy);

            mPreviousBulletTimeEnergy = currentEnergy;
            mBulletTimeEnergyInitialized = true;
            mBulletTimeRecoverStopTimer = 0f;
            SetBulletTimeRecoverEffect(false);
        }

        private void UnbindBulletTime()
        {
            if (mBulletTimeController != null)
            {
                mBulletTimeController.EnergyChanged -= OnBulletTimeEnergyChanged;
                mBulletTimeController.StateChanged -= OnBulletTimeStateChanged;
                mBulletTimeController.ActivationRejected -= OnBulletTimeActivationRejected;
            }

            EndBulletTimeRecoveryPreview(
                mBulletTimeController != null ? mBulletTimeController.CurrentEnergy : 0f,
                mBulletTimeController != null ? mBulletTimeController.MaxEnergy : 1f);

            if (mBulletTimeFill != null)
            {
                mBulletTimeFill.color = mBulletTimeBaseColor;
            }

            mBulletTimeController = null;
            mBulletTimeSlider = null;
            mBulletTimeEffectSlider = null;
            mBulletTimeFill = null;
            mBulletTimeFlashRemaining = 0f;
            mBulletTimeEnergyInitialized = false;
            mBulletTimeRecoverStopTimer = 0f;
            mBulletTimeRecoverEffectPlaying = false;
        }

        private void OnBulletTimeEnergyChanged(
            float currentEnergy,
            float maxEnergy,
            BulletTimeEnergyChangeReason reason)
        {
            RefreshBulletTimeEnergy(currentEnergy, maxEnergy, reason);
        }

        private void RefreshBulletTimeEnergy(
            float currentEnergy,
            float maxEnergy,
            BulletTimeEnergyChangeReason reason)
        {
            maxEnergy = Mathf.Max(1f, maxEnergy);
            currentEnergy = Mathf.Clamp(currentEnergy, 0f, maxEnergy);

            if (mBulletTimeSlider != null)
            {
                mBulletTimeSlider.minValue = 0f;
                mBulletTimeSlider.maxValue = maxEnergy;
                mBulletTimeSlider.value = currentEnergy;
            }
            if (!mBulletTimeEnergyInitialized)
            {
                mPreviousBulletTimeEnergy = currentEnergy;
                mBulletTimeEnergyInitialized = true;
                EndBulletTimeRecoveryPreview(currentEnergy, maxEnergy);
                SetBulletTimeRecoverEffect(false);
                return;
            }

            bool increased = currentEnergy > mPreviousBulletTimeEnergy + 0.001f;
            bool decreased = currentEnergy < mPreviousBulletTimeEnergy - 0.001f;
            bool cancelsPreview = reason == BulletTimeEnergyChangeReason.Consume
                || reason == BulletTimeEnergyChangeReason.Reset
                || reason == BulletTimeEnergyChangeReason.PerfectDodgeRecovery;
            bool isNormalRecovery = reason == BulletTimeEnergyChangeReason.NaturalRecovery
                || reason == BulletTimeEnergyChangeReason.KillRecovery;
            bool shouldPlay = increased && isNormalRecovery && currentEnergy < maxEnergy;

            if (decreased || cancelsPreview)
            {
                EndBulletTimeRecoveryPreview(currentEnergy, maxEnergy);
            }
            else if (increased && reason == BulletTimeEnergyChangeReason.NaturalRecovery)
            {
                if (!mBulletTimePreviewActive)
                {
                    BeginBulletTimeRecoveryPreview(currentEnergy, maxEnergy);
                }

                if (mBulletTimePreviewActive
                    && currentEnergy >= mBulletTimePreviewTarget - 0.001f)
                {
                    EndBulletTimeRecoveryPreview(currentEnergy, maxEnergy);
                }
            }
            else if (mBulletTimePreviewActive
                && currentEnergy >= mBulletTimePreviewTarget - 0.001f)
            {
                EndBulletTimeRecoveryPreview(currentEnergy, maxEnergy);
            }

            if (shouldPlay)
            {
                mBulletTimeRecoverStopTimer = mBulletTimeRecoverStopDelay;
            }

            SetBulletTimeRecoverEffect(shouldPlay);
            mPreviousBulletTimeEnergy = currentEnergy;
        }

        private void BeginBulletTimeRecoveryPreview(float currentEnergy, float maxEnergy)
        {
            if (mBulletTimeEffectSlider == null)
            {
                return;
            }

            float targetEnergy = GetBulletTimeRecoveryPreviewTarget(currentEnergy, maxEnergy);
            if (targetEnergy <= currentEnergy + 0.001f)
            {
                EndBulletTimeRecoveryPreview(currentEnergy, maxEnergy);
                return;
            }

            mBulletTimeEffectSlider.minValue = 0f;
            mBulletTimeEffectSlider.maxValue = maxEnergy;
            mBulletTimeEffectSlider.value = targetEnergy;
            mBulletTimeEffectSlider.gameObject.SetActive(true);
            mBulletTimePreviewTarget = targetEnergy;
            mBulletTimePreviewActive = true;
        }

        private float GetBulletTimeRecoveryPreviewTarget(float currentEnergy, float maxEnergy)
        {
            float threshold = mBulletTimeController != null
                && mBulletTimeController.Config != null
                    ? mBulletTimeController.Config.LowEnergyThreshold
                    : maxEnergy;
            threshold = Mathf.Clamp(threshold, 0f, maxEnergy);
            return currentEnergy < threshold ? threshold : maxEnergy;
        }

        private void EndBulletTimeRecoveryPreview(float currentEnergy, float maxEnergy)
        {
            mBulletTimePreviewActive = false;
            mBulletTimePreviewTarget = 0f;
            if (mBulletTimeEffectSlider == null)
            {
                return;
            }

            maxEnergy = Mathf.Max(1f, maxEnergy);
            mBulletTimeEffectSlider.minValue = 0f;
            mBulletTimeEffectSlider.maxValue = maxEnergy;
            mBulletTimeEffectSlider.value = Mathf.Clamp(currentEnergy, 0f, maxEnergy);
            mBulletTimeEffectSlider.gameObject.SetActive(false);
        }

        private void OnBulletTimeStateChanged(BulletTimeState state)
        {
            if (state == BulletTimeState.Entering
                || state == BulletTimeState.Active
                || state == BulletTimeState.Exiting
                || state == BulletTimeState.Disabled)
            {
                EndBulletTimeRecoveryPreview(
                    mBulletTimeController != null ? mBulletTimeController.CurrentEnergy : 0f,
                    mBulletTimeController != null ? mBulletTimeController.MaxEnergy : 1f);
            }
        }

        private void OnBulletTimeActivationRejected()
        {
            StartBulletTimeFlash(new Color(1f, 0.2f, 0.2f, 1f), 0.45f);
        }

        private void StartBulletTimeFlash(Color color, float duration)
        {
            mBulletTimeFlashColor = color;
            mBulletTimeFlashRemaining = duration;
        }

        private void SetBulletTimeRecoverEffect(bool shouldPlay)
        {
            if (mBulletTimeRecoverEffectPlaying == shouldPlay)
            {
                return;
            }

            mBulletTimeRecoverEffectPlaying = shouldPlay;
            if (!shouldPlay && mBulletTimeFill != null && mBulletTimeFlashRemaining <= 0f)
            {
                mBulletTimeFill.color = mBulletTimeBaseColor;
            }
        }

        private void UpdateBulletTimeFeedback()
        {
            if (mBulletTimeFill == null || mBulletTimeController == null)
            {
                return;
            }

            if (mBulletTimeFlashRemaining > 0f)
            {
                mBulletTimeFlashRemaining = Mathf.Max(
                    0f,
                    mBulletTimeFlashRemaining - Time.unscaledDeltaTime);
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 24f);
                mBulletTimeFill.color = Color.Lerp(
                    mBulletTimeBaseColor,
                    mBulletTimeFlashColor,
                    pulse);
                return;
            }

            if (mBulletTimeRecoverEffectPlaying)
            {
                mBulletTimeRecoverStopTimer = Mathf.Max(
                    0f,
                    mBulletTimeRecoverStopTimer - Time.unscaledDeltaTime);
                if (mBulletTimeRecoverStopTimer <= 0f)
                {
                    SetBulletTimeRecoverEffect(false);
                }
                else
                {
                    float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 18f);
                    mBulletTimeFill.color = Color.Lerp(
                        mBulletTimeBaseColor,
                        mBulletTimeRecoverColor,
                        pulse);
                    return;
                }
            }

            bool lowEnergy = mBulletTimeController.State == BulletTimeState.Active
                && mBulletTimeController.CurrentSource == BulletTimeSource.Normal
                && mBulletTimeController.CurrentEnergy
                    <= mBulletTimeController.Config.MinimumActivationEnergy;
            if (lowEnergy)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 12f);
                mBulletTimeFill.color = Color.Lerp(
                    mBulletTimeBaseColor,
                    new Color(1f, 0.15f, 0.15f, 1f),
                    pulse);
            }
            else
            {
                mBulletTimeFill.color = mBulletTimeBaseColor;
            }
        }

        

        private void BindEmojiBar()
        {
            if (EmojiBar == null)
            {
                Debug.LogError("[BattleView] EmojiBar Designer 引用为空。", this);
                return;
            }

            mEmojiBar = EmojiBar.GetComponent<TPS.Player.Presentation.EmojiBar>();
            mEmojiBarController = EmojiBar.GetComponent<TPS.Player.Presentation.EmojiBarController>();
            if (mEmojiBar == null || mEmojiBarController == null)
            {
                Debug.LogError(
                    "[BattleView] EmojiBar 缺少 EmojiBar 或 EmojiBarController 组件。",
                    EmojiBar);
                mEmojiBar = null;
                mEmojiBarController = null;
                return;
            }

            mEmojiBarController.Bind(mEmojiBar);
            BindEmojiPlayer();
        }

        private void BindEmojiPlayer()
        {
            if (mEmojiBarController == null)
            {
                return;
            }

            IPlayerEmojiController emojiController = mCachedPlayer != null
                ? mCachedPlayer.GetComponent<PlayerEmojiController>()
                : null;
            mEmojiBarController.BindPlayer(emojiController);
        }

        private void UnbindEmojiBar()
        {
            if (mEmojiBarController != null)
            {
                mEmojiBarController.Unbind();
            }
            mEmojiBar = null;
            mEmojiBarController = null;
        }

        protected override void OnHide()
        {
            UnregisterCrosshairEvents();
            UnbindPlayerAttributes();
            UnbindBulletTime();
            UnbindEmojiBar();
        }

        
        protected override void OnClose()
        {
            UnregisterCrosshairEvents();
            UnbindPlayerAttributes();
            UnbindBulletTime();
            UnbindEmojiBar();
        }
    }
}
