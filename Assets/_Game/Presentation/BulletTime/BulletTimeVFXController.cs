using TPS.BulletTime.Application;
using TPS.BulletTime.Infrastructure;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TPS.BulletTime.Presentation
{
    [DisallowMultipleComponent]
    public sealed class BulletTimeVFXController : MonoBehaviour
    {
        private BulletTimeController controller;
        private Volume volume;
        private VolumeProfile runtimeProfile;

        private void Awake()
        {
            controller = GetComponent<BulletTimeController>();
            CreateRuntimeVolume();
        }

        private void Update()
        {
            if (volume != null && controller != null)
            {
                volume.weight = controller.EffectWeight;
            }
        }

        private void OnDisable()
        {
            if (volume != null)
            {
                volume.weight = 0f;
            }
        }

        private void OnDestroy()
        {
            if (runtimeProfile != null)
            {
                Destroy(runtimeProfile);
            }
        }

        private void CreateRuntimeVolume()
        {
            BulletTimeConfig config = controller != null ? controller.Config : null;
            if (config == null)
            {
                return;
            }

            volume = gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1000f;
            volume.weight = 0f;

            runtimeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            runtimeProfile.name = "Runtime Bullet Time Volume";
            runtimeProfile.hideFlags = HideFlags.DontSave;
            volume.profile = runtimeProfile;

            ColorAdjustments color = runtimeProfile.Add<ColorAdjustments>(true);
            color.saturation.Override(config.Saturation);
            color.colorFilter.Override(config.ColorFilter);

            Vignette vignette = runtimeProfile.Add<Vignette>(true);
            vignette.intensity.Override(config.VignetteIntensity);
            vignette.smoothness.Override(0.45f);

            MotionBlur blur = runtimeProfile.Add<MotionBlur>(true);
            blur.intensity.Override(config.MotionBlurIntensity);

            DepthOfField depth = runtimeProfile.Add<DepthOfField>(true);
            depth.mode.Override(DepthOfFieldMode.Bokeh);
            depth.focusDistance.Override(config.DepthOfFieldFocusDistance);
            depth.aperture.Override(config.DepthOfFieldAperture);
        }
    }
}
