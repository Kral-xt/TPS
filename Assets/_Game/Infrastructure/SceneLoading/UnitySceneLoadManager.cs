using System;
using System.Collections;
using TPS.Application.Abstractions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TPS.Startup.Infrastructure
{
    public sealed class UnitySceneLoadManager : ISceneLoadService
    {
        public IEnumerator LoadSceneAsync(
            string sceneName,
            Action<float> onProgress,
            Action<bool> onCompleted)
        {
            onProgress?.Invoke(0f);
            if (string.IsNullOrWhiteSpace(sceneName)
                || !UnityEngine.Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[UnitySceneLoadManager] 场景未加入 Build Settings：{sceneName}");
                onCompleted?.Invoke(false);
                yield break;
            }

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (operation == null)
            {
                Debug.LogError($"[UnitySceneLoadManager] 无法创建异步加载操作：{sceneName}");
                onCompleted?.Invoke(false);
                yield break;
            }

            while (!operation.isDone)
            {
                float progress = Mathf.Clamp01(operation.progress / 0.9f);
                onProgress?.Invoke(progress);
                yield return null;
            }

            onProgress?.Invoke(1f);
            yield return null;
            onCompleted?.Invoke(true);
        }
    }
}
