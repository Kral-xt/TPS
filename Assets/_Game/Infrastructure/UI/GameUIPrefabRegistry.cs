using System;
using UnityEngine;

namespace TPS.Startup.Infrastructure
{
    public sealed class GameUIPrefabRegistry : ScriptableObject
    {
        [SerializeField] private GameObject startGameView;
        [SerializeField] private GameObject loadingView;
        [SerializeField] private GameObject battleView;
        [SerializeField] private GameObject bagDialog;
        [SerializeField] private GameObject gmView;
        [SerializeField] private GameObject itemCell;

        public GameObject Resolve(Type panelType, string prefabName)
        {
            string panelName = !string.IsNullOrEmpty(prefabName)
                ? prefabName
                : panelType?.Name;

            switch (panelName)
            {
                case "StartGameView":
                    return startGameView;
                case "LoadingView":
                    return loadingView;
                case "BattleView":
                    return battleView;
                case "BagDialog":
                    return bagDialog;
                case "GMView":
                    return gmView;
                case "ItemCell":
                    return itemCell;
                default:
                    return null;
            }
        }
    }
}
