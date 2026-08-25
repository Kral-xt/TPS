using TPS.Infrastructure.Persistence;
using TPS.Inventory.Infrastructure;
using TPS.ItemSystem.Infrastructure;
using UnityEngine;

namespace TPS.Inventory.Presentation
{
    public static class PlayerInventoryRuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            PlayerInventoryController controller = PlayerInventoryController.EnsureRuntimeInstance();
            controller.Initialize(
                new InventoryJsonStore(new JsonSaveRepository()),
                new InventoryItemConfigProvider());
        }
    }
}
