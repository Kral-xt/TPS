using TPS.Audio.Infrastructure;
using UnityEditor;
using UnityEngine;

namespace TPS.Audio.Editor
{
    [CustomEditor(typeof(KillAudioConfig))]
    public sealed class KillAudioConfigEditor : UnityEditor.Editor
    {

        private static readonly string[] KillClipLabels =
        {
            "Kill1", "Kill2", "Kill3", "Kill4",
            "Kill5", "Kill6", "Kill7", "Kill8"
        };
        private const int KillClipCount = 8;

        private SerializedProperty killClips;
        private SerializedProperty headShotClip;

        private void OnEnable()
        {
            killClips = serializedObject.FindProperty("killClips");
            headShotClip = serializedObject.FindProperty("headShotClip");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (killClips.arraySize != KillClipCount)
            {
                killClips.arraySize = KillClipCount;
            }

            EditorGUILayout.LabelField("Kill Audio Clips", EditorStyles.boldLabel);
            for (int i = 0; i < KillClipCount; i++)
            {
                EditorGUILayout.PropertyField(
                    killClips.GetArrayElementAtIndex(i),
                    new GUIContent(KillClipLabels[i]));
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("HeadShot Audio", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(headShotClip, GUIContent.none);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
