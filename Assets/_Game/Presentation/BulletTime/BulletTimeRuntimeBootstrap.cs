using TPS.BulletTime.Application;
using TPS.Application.Abstractions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TPS.BulletTime.Presentation
{
    public static class BulletTimeRuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneCallback()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            if (scene.name == GameSceneNames.Gameplay)
            {
                Install();
            }
        }

        private static void Install()
        {
            BulletTimeController controller = BulletTimeController.EnsureRuntimeInstance();
            GameObject host = controller.gameObject;
            if (host.GetComponent<BulletTimeInput>() == null)
            {
                host.AddComponent<BulletTimeInput>();
            }

            if (host.GetComponent<BulletTimeVFXController>() == null)
            {
                host.AddComponent<BulletTimeVFXController>();
            }

            if (host.GetComponent<BulletTimeAudioController>() == null)
            {
                host.AddComponent<BulletTimeAudioController>();
            }
        }
    }
}
