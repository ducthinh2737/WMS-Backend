
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wms.Application.DTOS.Warehouse;
using Wms.Application.Interfaces.Services.Warehouse;
using Wms.Application.Services.Warehouses;
using Wms.Domain.Entity.Warehouses;
using Wms.Domain.Enums.location;
using Wms.Infrastructure.Persistence.Context;


namespace Wms.Application.Services.Warehouses
{
    public class WarehouseService : IWarehouseService
    {
        private readonly AppDbContext _db;


        public WarehouseService(AppDbContext db)
        {
            _db = db;
        }


        public async Task<WarehouseDto> CreateAsync(WarehouseCreateDto dto)
        {
            // check duplicate code
            var exists = await _db.Warehouses.AnyAsync(w => w.Code == dto.Code);
            if (exists) throw new InvalidOperationException("Warehouse code already exists.");


            var entity = new Warehouse
            {
                Code = dto.Code.Trim().ToUpperInvariant(),
                Name = dto.Name.Trim(),
                WarehouseType = dto.WarehouseType,
                Address = dto.Address
            };


            _db.Warehouses.Add(entity);
            await _db.SaveChangesAsync();


            return Map(entity);
        }

        public async Task<List<Warehouse>> GetWarehousesByProduct(WarehousesbyProduct dto)
        {
            var listwarehouses = new List<Warehouse>();
            var inventory = _db.Inventories.Where(s => s.ProductId == dto.ProductId);
            foreach (var item in inventory)
            {
                var warehouse = await _db.Warehouses.FirstOrDefaultAsync(s => s.Id == item.WarehouseId);
                listwarehouses.Add(warehouse);
            }
            return listwarehouses;
        }

        public async Task<List<Warehouse>> GetByWarehouseType(WarehousesbyTypeDto dto)
        {
            var warehouselist = _db.Warehouses.Where(s=>s.WarehouseType == dto.warehousetype).ToList();
            if (warehouselist == null) throw new Exception("Không có kho nào thuộc loại trên");
            return warehouselist;
        }

        public async Task<WarehouseDto> UpdateAsync(WarehouseUpdateDto dto)
        {
            var entity = await _db.Warehouses.FindAsync(dto.Id);
            if (entity == null) throw new KeyNotFoundException("Warehouse not found.");

            // cập nhật code
            if (!string.IsNullOrWhiteSpace(dto.Code))
            {
                var newCode = dto.Code.Trim().ToUpperInvariant();

                if (newCode != entity.Code)
                {
                    // check duplicate
                    var exists = await _db.Warehouses.AnyAsync(w => w.Code == newCode && w.Id != dto.Id);
                    if (exists) throw new InvalidOperationException("Warehouse code already exists.");

                    entity.Code = newCode;
                }
            }

            // cập nhật name + address
            entity.Name = dto.Name.Trim();
            entity.Address = dto.Address;

            if (dto.Status.HasValue) entity.Status = dto.Status.Value;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Map(entity);
        }



        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _db.Warehouses.Include(w => w.Locations).FirstOrDefaultAsync(w => w.Id == id);
            if (entity == null) return false;


            // business rule: cannot delete if locations exist
            if (entity.Locations != null && entity.Locations.Any()) throw new InvalidOperationException("Cannot delete a warehouse that still has locations.");


            _db.Warehouses.Remove(entity);
            await _db.SaveChangesAsync();
            return true; 
        }
        public async Task<WarehouseDto> GetByIdAsync(Guid id)
        {
            var entity = await _db.Warehouses.Include(w => w.Locations).FirstOrDefaultAsync(w => w.Id == id);
            if (entity == null) return null;
            return Map(entity);
        }


        public async Task<(IEnumerable<WarehouseDto> Items, int Total)> QueryAsync(
    int page,
    int pageSize,
    string q,
    string sortBy,
    bool asc)
        {
            page = page < 1 ? 1 : page;
            var query = _db.Warehouses.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var t = q.Trim();
                query = query.Where(w => w.Name.Contains(t) || w.Code.Contains(t));
            }

            var total = await query.CountAsync();

            // sorting
            query = sortBy?.ToLower() switch
            {
                "name" => asc
                    ? query.OrderBy(w => w.Name)
                    : query.OrderByDescending(w => w.Name),

                "warehousetype" => asc
                    ? query.OrderBy(w => w.WarehouseType)
                    : query.OrderByDescending(w => w.WarehouseType),

                _ => asc
                    ? query.OrderBy(w => w.CreatedAt)
                    : query.OrderByDescending(w => w.CreatedAt),
            };

            // 👉 pageSize = 0 => lấy ALL
            if (pageSize > 0)
            {
                query = query.Skip((page - 1) * pageSize).Take(pageSize);
            }

            var items = await query.ToListAsync();
            return (items.Select(Map), total);
        }



        public async Task LockAsync(Guid id, string reason = null)
        {
            var entity = await _db.Warehouses.FindAsync(id);
            if (entity == null) throw new KeyNotFoundException("Warehouse not found.");
            entity.Status = WarehouseStatus.Locked;
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }


        public async Task UnlockAsync(Guid id)
        {
            var entity = await _db.Warehouses.FindAsync(id);
            if (entity == null) throw new KeyNotFoundException("Warehouse not found.");
            entity.Status = WarehouseStatus.Active;
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }


        // Locations
        public async Task<LocationDto> CreateLocationAsync(LocationCreateDto dto)
        {
            var wh = await _db.Warehouses.FindAsync(dto.WarehouseId);
            if (wh == null) throw new KeyNotFoundException("Warehouse not found.");


            // validate warehouse status
            if (wh.Status == WarehouseStatus.Locked || wh.Status == WarehouseStatus.Maintenance)
                throw new InvalidOperationException("Cannot create location while warehouse is locked or under maintenance.");


            // validate format
            if (!LocationCodeValidator.IsValid(dto.Code))
                throw new InvalidOperationException("Location code invalid. Expected pattern like A1-01-03.");


            // duplicate check
            var dup = await _db.Locations.AnyAsync(l => l.WarehouseId == dto.WarehouseId && l.Code == dto.Code);
            if (dup) throw new InvalidOperationException("Location code already exists in warehouse.");


            var entity = new Location
            {
                WarehouseId = dto.WarehouseId,
                Type = dto.Type,
                Code = dto.Code.Trim().ToUpperInvariant(),
                Description = dto.Description ?? ""
            };


            _db.Locations.Add(entity);
            await _db.SaveChangesAsync();
            return Map(entity);
        }
        public async Task<Guid> GetReceivingLocationId(Guid warehouseId)
        {
            var locationId = await _db.Locations
                .Where(l =>
                    l.WarehouseId == warehouseId &&
                    l.Type == LocationType.Receiving &&
                    l.IsActive)
                .Select(l => l.Id)
                .FirstOrDefaultAsync();

            if (locationId == Guid.Empty)
                throw new Exception(
                    $"Receiving location not configured for warehouse {warehouseId}");

            return locationId;
        }
        public async Task<Location> GetIssuedLocationId(Guid warehouseId)
        {
            var location = await _db.Locations
                .Where(l =>
                    l.WarehouseId == warehouseId &&
                    l.Type == LocationType.Shipping &&
                    l.IsActive)
                .FirstOrDefaultAsync();  // ← Lấy cả object Location

            if (location == null)  // ← Check null thay vì Guid.Empty
                throw new Exception(
                    $"Shipping location not configured for warehouse {warehouseId}");

            return location;
        }


        public async Task<LocationDto> UpdateLocationAsync(LocationUpdateDto dto)
        {
            var entity = await _db.Locations.FindAsync(dto.Id);
            if (entity == null) throw new KeyNotFoundException("Location not found.");

            var wh = await _db.Warehouses.FindAsync(entity.WarehouseId);
            if (wh.Status == WarehouseStatus.Locked || wh.Status == WarehouseStatus.Maintenance)
                throw new InvalidOperationException("Cannot update location while warehouse is locked or under maintenance.");

            // cập nhật code
            if (!string.IsNullOrWhiteSpace(dto.Code))
            {
                var newCode = dto.Code.Trim().ToUpperInvariant();

                if (newCode != entity.Code)
                {
                    // validate pattern
                    if (!LocationCodeValidator.IsValid(newCode))
                        throw new InvalidOperationException("Location code invalid. Expected pattern like A1-01-03.");

                    // check duplicate
                    var dup = await _db.Locations.AnyAsync(l =>
                        l.WarehouseId == entity.WarehouseId &&
                        l.Code == newCode &&
                        l.Id != entity.Id
                    );

                    if (dup) throw new InvalidOperationException("Location code already exists in warehouse.");

                    entity.Code = newCode;
                }
            }

            // cập nhật description + isActive
            if (dto.Description != null) entity.Description = dto.Description;
            if (dto.IsActive.HasValue) entity.IsActive = dto.IsActive.Value;
            entity.Type = dto.LocationType;

            entity.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Map(entity);
        }


        public async Task<bool> DeleteLocationAsync(Guid id)
        {
            var entity = await _db.Locations.FindAsync(id);
            if (entity == null) return false;


            var wh = await _db.Warehouses.FindAsync(entity.WarehouseId);
            if (wh.Status == WarehouseStatus.Locked || wh.Status == WarehouseStatus.Maintenance)
                throw new InvalidOperationException("Cannot delete location while warehouse is locked or under maintenance.");


            // TODO: check inventory in location before deleting (business rule)


            _db.Locations.Remove(entity);
            await _db.SaveChangesAsync();
            return true;
        }


        public async Task<IEnumerable<LocationDto>> GetLocationsByWarehouseAsync(
    Guid warehouseId,
    LocationType? type)
        {
            var all = await _db.Locations.ToListAsync();

            Console.WriteLine($"Total locations in DB: {all.Count}");
            Console.WriteLine($"WarehouseId filter: {warehouseId}");

            var filtered = all.Where(l => l.WarehouseId == warehouseId);

            Console.WriteLine($"After warehouse filter: {filtered.Count()}");

            return filtered.Select(Map);
        }


        public async Task<LocationDto> GetLocationByIdAsync(Guid id)
        {
            var entity = await _db.Locations.FindAsync(id);
            return entity == null ? null : Map(entity);
        }


        private WarehouseDto Map(Warehouse w) => new WarehouseDto
        {
            Id = w.Id,
            Code = w.Code,
            WarehouseType = w.WarehouseType,
            Name = w.Name,
            Address = w.Address,
            Status = w.Status,
            CreatedAt = w.CreatedAt,
            UpdatedAt = w.UpdatedAt,
            Locations = w.Locations?.Select(Map).ToList()
        };


        private LocationDto Map(Location l) => new LocationDto
        {
            Id = l.Id,
            WarehouseId = l.WarehouseId,
            Code = l.Code,
            Description = l.Description,
            IsActive = l.IsActive,
            Type    = l.Type,
            CreatedAt = l.CreatedAt,
            UpdatedAt = l.UpdatedAt
        };
    }
}
