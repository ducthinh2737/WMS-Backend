using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Wms.Domain.Enums.Inventory;

namespace Wms.Domain.Entity.Inventorys
{
    [Table("InventoryTransactions")]
    public class InventoryTransaction
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(50)]
        public string ReferenceCode { get; set; }

        public Guid WarehouseId { get; set; }
        public Guid? LocationId { get; set; }
        public int ProductId { get; set; }
        public Guid LotId { get; set; }

        public InventoryActionType ActionType { get; set; }
        public decimal Quantity { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
    }
}
