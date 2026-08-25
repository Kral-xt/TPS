using System.Collections;
using QFramework;
using QFramework.Example;
using TPS.Application.Abstractions;
using TPS.Player.Presentation;
using TPS.Startup.Application;
using TPS.Startup.Infrastructure;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TPS.Startup.Presentation
{
    [DisallowMultipleComponent]
    public sealed class GameStartupPresenter : MonoBehaviour
    {
        private GameStartFlow startFlow;
        private StartGameView startGameView;
        private LoadingView loadingView;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (SceneManager.GetActiveScene().name != GameSceneNames.Startup
                || FindFirstObjectByType<GameStartupPresenter>() != null)
            {
                return;
            }

            GameUIRuntimeEnvironment.Ensure();
            GamePanelLoaderPool.Install();
            CursorStateController cursorController =
                CursorStateController.EnsureRuntimeInstance();
            cursorController.SetEscapeToggleEnabled(false);
            PlayerInputGate.SetUI();

            GameObject presenterObject = new GameObject("GameStartupFlow");
            DontDestroyOnLoad(presenterObject);
            presenterObject.AddComponent<GameStartupPresenter>();
        }

        private void Awake()
        {
            IGameAudioService audioService = GameAudioManager.EnsureRuntimeInstance();
            ISceneLoadService sceneLoadService = new UnitySceneLoadManager();
            startFlow = new GameStartFlow(sceneLoadService, audioService);
        }

        private void Start()
        {
            OpenStartGameView();
        }

        private void OpenStartGameView()
        {
            startGameView = UIKit.OpenPanel<StartGameView>(UILevel.Common);
            if (startGameView == null)
            {
                LogPanelOpenFailure("StartGameView", "UIKit.OpenPanel returned null.");
                return;
            }

            startGameView.StartRequested += OnStartRequested;
            GameAudioManager.EnsureRuntimeInstance().PlayMenuBGM();
        }

        private void OnStartRequested()
        {
            if (startFlow == null || startFlow.IsRunning)
            {
                return;
            }

            GameAudio.Current?.PlayUIClickSound();
            startGameView?.SetStartInteractable(false);
            loadingView = UIKit.OpenPanel<LoadingView>(UILevel.PopUI);
            if (loadingView == null)
            {
                LogPanelOpenFailure("LoadingView", "UIKit.OpenPanel returned null.");
                startGameView?.SetStartInteractable(true);
                return;
            }

            loadingView.UpdateProgress(0f);
            CloseStartGameView();
            StartCoroutine(RunStartFlow());
        }

        private IEnumerator RunStartFlow()
        {
            yield return startFlow.EnterGameplay(
                UpdateLoadingProgress,
                CloseLoadingView);

            if (startFlow.LastLoadSucceeded)
            {
                Destroy(gameObject);
            }
            else
            {
                OpenStartGameView();
            }
        }

        private void UpdateLoadingProgress(float progress)
        {
            loadingView?.UpdateProgress(progress);
        }

        private void CloseStartGameView()
        {
            if (startGameView == null)
            {
                return;
            }

            startGameView.StartRequested -= OnStartRequested;
            StartGameView panel = startGameView;
            startGameView = null;
            UIKit.ClosePanel(panel);
        }

        private void CloseLoadingView()
        {
            if (loadingView == null)
            {
                return;
            }

            LoadingView panel = loadingView;
            loadingView = null;
            UIKit.ClosePanel(panel);
        }

        private void OnDestroy()
        {
            if (startGameView != null)
            {
                startGameView.StartRequested -= OnStartRequested;
            }
        }

        private void LogPanelOpenFailure(string panelName, string reason)
        {
            Debug.LogError(
                "[GameStartupPresenter] Panel open failed. "
                + $"Panel={panelName}, "
                + $"Path=Resources/{GamePanelLoaderPool.RegistryResourcePath}, "
                + $"UIReady={GameUIRuntimeEnvironment.IsReady}, "
                + $"LoaderReady={GamePanelLoaderPool.IsReady}, "
                + $"Reason={reason}",
                this);
        }

    }
}
