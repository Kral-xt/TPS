using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TPS.Application.Abstractions;
using TPS.Inventory.Application;
using TPS.Inventory.Domain;
using UnityEngine;

namespace TPS.Inventory.Presentation
{
    [DefaultExecutionOrder(-9000)]
    [DisallowMultipleComponent]
    public sealed class PlayerInventoryController : MonoBehaviour
    {
        private static readonly IReadOnlyDictionary<int, int> EmptyItems =
            new ReadOnlyDictionary<int, int>(new Dictionary<int, int>());
        private static readonly IReadOnlyList<InventoryDisplayItem> EmptyDisplayItems =
            Array.Empty<InventoryDisplayItem>();
        private static readonly IReadOnlyList<ItemData> EmptyItemEntries = Array.Empty<ItemData>();

        private static PlayerInventoryController instance;
        private PlayerInventoryService inventoryService;
        private InventoryDisplayService displayService;

        public static PlayerInventoryController Instance => instance;

        public IReadOnlyDictionary<int, int> Items => inventoryService?.Items ?? EmptyItems;
        public IReadOnlyList<ItemData> ItemEntries =>
            inventoryService?.ItemEntries ?? EmptyItemEntries;
        public int EntryCount => inventoryService?.EntryCount ?? 0;

        public event Action InventoryChanged;

        public static PlayerInventoryController EnsureRuntimeInstance()
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindFirstObjectByType<PlayerInventoryController>();
            if (instance != null)
            {
                DontDestroyOnLoad(instance.gameObject);
                return instance;
            }

            GameObject controllerObject = new GameObject("PlayerInventoryController");
            DontDestroyOnLoad(controllerObject);
            instance = controllerObject.AddComponent<PlayerInventoryController>();
            return instance;
        }

        internal void Initialize(IInventoryStore store)
        {
            if (inventoryService != null)
            {
                return;
            }

            inventoryService = new PlayerInventoryService(store);
            inventoryService.Initialize();
        }

        internal void Initialize(IInventoryStore store, IItemConfigProvider itemConfigProvider)
        {
            Initialize(store);
            if (displayService == null && inventoryService != null && itemConfigProvider != null)
            {
                displayService = new InventoryDisplayService(inventoryService, itemConfigProvider);
            }
        }

        public void AddItem(int itemID, int count)
        {
            if (!EnsureInitialized())
            {
                return;
            }

            try
            {
                inventoryService.AddItem(itemID, count);
            }
            catch (ArgumentException exception)
            {
                Debug.LogError($"[PlayerInventoryController] 添加物品失败。\n{exception}", this);
                return;
            }
            catch (OverflowException exception)
            {
                Debug.LogError($"[PlayerInventoryController] 添加物品失败。\n{exception}", this);
                return;
            }

            InventoryChanged?.Invoke();
        }

        public bool RemoveItem(int itemID, int count)
        {
            if (!EnsureInitialized() || !inventoryService.RemoveItem(itemID, count))
            {
                return false;
            }

            InventoryChanged?.Invoke();
            return true;
        }

        public IReadOnlyList<InventoryDisplayItem> GetDisplayItems()
        {
            return EnsureInitialized() && displayService != null
                ? displayService.GetItems()
                : EmptyDisplayItems;
        }

        public int GetItemCount(int itemID)
        {
            return EnsureInitialized() ? inventoryService.GetItemCount(itemID) : 0;
        }

        public bool HasItem(int itemID)
        {
            return EnsureInitialized() && inventoryService.HasItem(itemID);
        }

        public bool HasItemConfig(int itemID)
        {
            return EnsureInitialized()
                && displayService != null
                && displayService.HasItemConfig(itemID);
        }

        public void SaveInventory()
        {
            if (EnsureInitialized())
            {
                inventoryService.SaveInventory();
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private bool EnsureInitialized()
        {
            if (inventoryService != null)
            {
                return true;
            }

            Debug.LogError("[PlayerInventoryController] 背包服务尚未初始化。", this);
            return false;
        }

        private void OnApplicationQuit()
        {
            if (inventoryService != null)
            {
                inventoryService.SaveInventory();
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                InventoryChanged = null;
                instance = null;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            instance = null;
        }
    }
}
