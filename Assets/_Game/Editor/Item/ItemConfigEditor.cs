using System;
using TPS.ItemSystem;
using UnityEditor;
using UnityEngine;

namespace TPS.Editor.ItemSystem
{
    [CustomEditor(typeof(ItemConfig), true)]
    public sealed class ItemConfigEditor : UnityEditor.Editor
    {
        private SerializedProperty itemID;
        private SerializedProperty itemIcon;
        private SerializedProperty itemName;
        private SerializedProperty description;
        private SerializedProperty quality;
        private SerializedProperty itemTypes;
        private ItemQualityConfig qualityConfig;

        private void OnEnable()
        {
            itemID = serializedObject.FindProperty("itemID");
            itemIcon = serializedObject.FindProperty("itemIcon");
            itemName = serializedObject.FindProperty("itemName");
            description = serializedObject.FindProperty("description");
            quality = serializedObject.FindProperty("quality");
            itemTypes = serializedObject.FindProperty("itemTypes");
            qualityConfig = Resources.Load<ItemQualityConfig>("Config/Item/ItemQualityConfig");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Item Config", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);
            EditorGUILayout.PropertyField(itemID, new GUIContent("ID", "物品唯一索引，不允许重复"));
            EditorGUILayout.PropertyField(itemIcon, new GUIContent("Icon", "背包、掉落提示和商店使用的图片"));
            EditorGUILayout.PropertyField(itemName, new GUIContent("Name", "物品显示名称"));
            EditorGUILayout.PropertyField(
                description,
                new GUIContent("Description", "物品详情页面显示的描述文本"),
                true);

            DrawQuality();
            DrawTypes();

            serializedObject.ApplyModifiedProperties();
            DrawValidationMessages();
        }

        private void DrawQuality()
        {
            int qualityValue = Mathf.Clamp(quality.intValue, 1, 5);
            string displayName = qualityConfig != null
                ? qualityConfig.GetQualityDisplayName(qualityValue)
                : "Missing Config";

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("物品品阶", EditorStyles.boldLabel);

            Rect row = EditorGUILayout.GetControlRect();
            Rect colorRect = new Rect(row.xMax - 20f, row.y + 2f, 18f, row.height - 4f);
            Rect sliderRect = new Rect(row.x, row.y, row.width - 26f, row.height);
            quality.intValue = EditorGUI.IntSlider(
                sliderRect,
                new GUIContent($"Quality ({displayName})"),
                qualityValue,
                1,
                5);

            Color color = qualityConfig != null
                ? qualityConfig.GetQualityColor(quality.intValue)
                : Color.white;
            EditorGUI.DrawRect(colorRect, color);
        }

        private void DrawTypes()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Types", EditorStyles.boldLabel);

            foreach (ItemType itemType in Enum.GetValues(typeof(ItemType)))
            {
                int index = FindTypeIndex(itemType);
                bool selected = index >= 0;
                bool nextSelected = EditorGUILayout.ToggleLeft(itemType.ToString(), selected);

                if (nextSelected && !selected)
                {
                    int newIndex = itemTypes.arraySize;
                    itemTypes.InsertArrayElementAtIndex(newIndex);
                    itemTypes.GetArrayElementAtIndex(newIndex).enumValueIndex = (int)itemType;
                }
                else if (!nextSelected && selected)
                {
                    itemTypes.DeleteArrayElementAtIndex(index);
                }
            }
        }

        private int FindTypeIndex(ItemType itemType)
        {
            for (int index = 0; index < itemTypes.arraySize; index++)
            {
                if (itemTypes.GetArrayElementAtIndex(index).enumValueIndex == (int)itemType)
                {
                    return index;
                }
            }

            return -1;
        }

        private void DrawValidationMessages()
        {
            ItemConfig current = (ItemConfig)target;
            if (current.ItemID <= 0)
            {
                EditorGUILayout.HelpBox("ID 必须大于 0。", MessageType.Error);
            }

            if (string.IsNullOrWhiteSpace(current.ItemName))
            {
                EditorGUILayout.HelpBox("物品名称不能为空。", MessageType.Warning);
            }

            string[] guids = AssetDatabase.FindAssets("t:ItemConfig", new[] { "Assets/Resources/Config/Item" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ItemConfig other = AssetDatabase.LoadAssetAtPath<ItemConfig>(path);
                if (other != null && other != current && other.ItemID == current.ItemID)
                {
                    EditorGUILayout.HelpBox($"ID 与 {other.name} 重复。", MessageType.Error);
                    break;
                }
            }
        }
    }
}
