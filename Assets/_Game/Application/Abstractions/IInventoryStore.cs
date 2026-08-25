using TPS.Inventory.Domain;

namespace TPS.Application.Abstractions
{
    public interface IInventoryStore
    {
        bool TryLoad(out InventoryData data);
        bool Save(InventoryData data);
    }
}
