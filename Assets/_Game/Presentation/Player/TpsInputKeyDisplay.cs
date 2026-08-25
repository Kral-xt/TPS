using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TPS.Player
{
    /// <summary>
    /// 角色头顶实时按键显示，自动附加到 TpsPrototypePlayerController 所在对象
    /// </summary>
    public class TpsInputKeyDisplay : MonoBehaviour
    {
        [SerializeField] private Vector3 headOffset = new Vector3(0f, 2.3f, 0f);
        [SerializeField] private float fontSize = 3f;
        [SerializeField] private Color textColor = Color.white;

        private TextMeshPro mText;
        private Transform mTextTransform;
        private Camera mMainCamera;
        private string mLastText;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneCallback()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            AutoAttach();
        }

        private static void AutoAttach()
        {
            var players = FindObjectsByType<TpsPrototypePlayerController>(FindObjectsSortMode.None);
            foreach (var player in players)
            {
                if (player.GetComponent<TpsInputKeyDisplay>() == null)
                    player.gameObject.AddComponent<TpsInputKeyDisplay>();
            }
        }

        private void Awake()
        {
            // 创建子对象并添加 TextMeshPro
            var go = new GameObject("InputKeyDisplay");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = headOffset;

            mText = go.AddComponent<TextMeshPro>();
            mTextTransform = go.transform;

            mText.fontSize = fontSize;
            mText.color = textColor;
            mText.alignment = TextAlignmentOptions.Center;
            mText.text = "";
            mLastText = "";

            // 黑色描边
            mText.outlineWidth = 0.2f;
            mText.outlineColor = Color.black;
        }

        private void Update()
        {
            string current = BuildKeyString();
            if (current != mLastText)
            {
                mText.text = current;
                mLastText = current;
            }
        }

        private void LateUpdate()
        {
            // 始终面向摄像机
            if (mMainCamera == null)
            {
                mMainCamera = Camera.main;
                if (mMainCamera == null) return;
            }

            Vector3 dir = mTextTransform.position - mMainCamera.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                mTextTransform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }

        private static string BuildKeyString()
        {
            string result = "";
            if (Input.GetKey(KeyCode.W)) result += "W ";
            if (Input.GetKey(KeyCode.A)) result += "A ";
            if (Input.GetKey(KeyCode.S)) result += "S ";
            if (Input.GetKey(KeyCode.D)) result += "D ";
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) result += "Shift ";
            if (Input.GetKey(KeyCode.C)) result += "C ";
            if (Input.GetKey(KeyCode.Space)) result += "Space ";
            return result.TrimEnd();
        }
    }
}
