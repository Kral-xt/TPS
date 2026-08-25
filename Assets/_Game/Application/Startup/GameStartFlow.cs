using System;
using System.Collections;
using TPS.Application.Abstractions;

namespace TPS.Startup.Application
{
    public sealed class GameStartFlow
    {
        private readonly ISceneLoadService sceneLoadService;
        private readonly IGameAudioService audioService;

        public bool IsRunning { get; private set; }
        public bool LastLoadSucceeded { get; private set; }

        public GameStartFlow(
            ISceneLoadService sceneLoadService,
            IGameAudioService audioService)
        {
            this.sceneLoadService = sceneLoadService;
            this.audioService = audioService;
        }

        public IEnumerator EnterGameplay(
            Action<float> onProgress,
            Action onLoadFinished)
        {
            if (IsRunning)
            {
                yield break;
            }

            IsRunning = true;
            LastLoadSucceeded = false;
            audioService.StopMenuBGM();

            yield return sceneLoadService.LoadSceneAsync(
                GameSceneNames.Gameplay,
                onProgress,
                succeeded => LastLoadSucceeded = succeeded);

            onLoadFinished?.Invoke();
            if (LastLoadSucceeded)
            {
                audioService.PlayGamingBGM();
            }
            else
            {
                audioService.PlayMenuBGM();
            }

            IsRunning = false;
        }
    }
}
