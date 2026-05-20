using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wms.Domain.Enums.location;

namespace Wms.Domain.Entity.Warehouses
{
    public class Location
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();


        [Required]
        public Guid WarehouseId { get; set; }


        [ForeignKey(nameof(WarehouseId))]
        public Warehouse Warehouse { get; set; }


        // Example code: A1-01-03
        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;


        [MaxLength(250)]
        public string Description { get; set; } = string.Empty;

        public LocationType Type { get; set; }
        public bool IsActive { get; set; } = true;  


        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        public DateTime? UpdatedAt { get; set; }
    }
}
