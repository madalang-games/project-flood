#nullable enable

using System.Collections.Generic;

namespace ProjectFlood.Contracts.Inventory
{
    public sealed class InventoryItemDto
    {
        public int ItemId { get; set; }
        public int Count { get; set; }
    }

    public sealed class InventorySnapshot
    {
        public List<InventoryItemDto> Items { get; set; } = new List<InventoryItemDto>();
    }

    public sealed class SpendItemRequest
    {
        public int ItemId { get; set; }
        public int Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
