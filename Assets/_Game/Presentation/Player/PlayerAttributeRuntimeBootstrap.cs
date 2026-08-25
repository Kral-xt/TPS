using TPS.Player.Application;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TPS.Player.Presentation
{
    public static class PlayerAttributeRuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneHook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureCurrentScene()
        {
            EnsureAllPlayers();
        }

        public static void EnsureFor(GameObject playerObject)
        {
            if (playerObject == null)
            {
                return;
            }

            GetOrAdd<PlayerAttributeController>(playerObject);
            GetOrAdd<PlayerHealthController>(playerObject);
            GetOrAdd<PlayerLevelController>(playerObject);
            GetOrAdd<PlayerCombatAttributeController>(playerObject);
            GetOrAdd<PlayerAfterimageController>(playerObject);
            GetOrAdd<PlayerDodgeController>(playerObject);
            GetOrAdd<PerfectDodgeDetector>(playerObject);
            GetOrAdd<PlayerDodgeInput>(playerObject);
            GetOrAdd<PlayerEmojiController>(playerObject);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureAllPlayers();
        }

        private static void EnsureAllPlayers()
        {
            TpsPrototypePlayerController[] players =
                Object.FindObjectsByType<TpsPrototypePlayerController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            for (int i = 0; i < players.Length; i++)
            {
                EnsureFor(players[i].gameObject);
            }
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }
    }
}
