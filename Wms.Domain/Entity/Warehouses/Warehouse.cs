using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wms.Domain.Entity.MasterData;

namespace Wms.Domain.Entity.Warehouses
{
    
    public class Warehouse
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(100)]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public WarehouseStatus Status { get; set; } = WarehouseStatus.Active;

        public WarehouseType WarehouseType { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public ICollection<Location> Locations { get; set; }
    }

}
public enum WarehouseType
{
    RawMaterial = 0,     // Kho nguyên vật liệu
    FinishedGoods = 1,   // Kho thành phẩm
    Auxiliary = 2,       // Kho phụ liệu
    Chemical = 3         // Kho hóa chất
}
public enum WarehouseStatus
{
    Active = 1,
    Inactive = 2,
    Locked = 3,
    Maintenance = 4
}

