using Microsoft.EntityFrameworkCore;
using Wms.Domain.Entity.Auth;
using Wms.Domain.Entity.Outbound;
using Wms.Infrastructure.Persistence.Context;

namespace Wms.Infrastructure.Seed;

public static class AuthSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        // 1. Seed Roles
        if (!db.Roles.Any())
        {
            db.Roles.AddRange(
                new Role { Id = 1, RoleName = "Admin" },
                new Role { Id = 2, RoleName = "Manager" },
                new Role { Id = 3, RoleName = "Staff" }
            );
        }

        // 2. Seed Permissions
        var permissions = new List<Permission>
        {
            // Users
            new Permission { Code = "user.view", Description = "View users" },
            new Permission { Code = "user.create", Description = "Create users" },
            new Permission { Code = "user.update", Description = "Update users" },
            new Permission { Code = "user.delete", Description = "Delete users" },
            new Permission { Code = "user.assign-role", Description = "Assign roles to users" },
            new Permission { Code = "user.assign-permission", Description = "Assign permissions to users" },

            // Brands
            new Permission { Code = "brand.view", Description = "View brands" },
            new Permission { Code = "brand.create", Description = "Create brands" },
            new Permission { Code = "brand.update", Description = "Update brands" },
            new Permission { Code = "brand.delete", Description = "Delete brands" },

            // Categories
            new Permission { Code = "category.view", Description = "View categories" },
            new Permission { Code = "category.create", Description = "Create categories" },
            new Permission { Code = "category.update", Description = "Update categories" },
            new Permission { Code = "category.delete", Description = "Delete categories" },

            // Customers
            new Permission { Code = "customer.view", Description = "View customers" },
            new Permission { Code = "customer.create", Description = "Create customers" },
            new Permission { Code = "customer.update", Description = "Update customers" },
            new Permission { Code = "customer.delete", Description = "Delete customers" },

            // Inventory
            new Permission { Code = "inventory.view", Description = "View inventory" },
            new Permission { Code = "inventory.history", Description = "View inventory history" },
            new Permission { Code = "inventory.adjust", Description = "Adjust inventory" },
            new Permission { Code = "inventory.lock", Description = "Lock inventory" },
            new Permission { Code = "inventory.unlock", Description = "Unlock inventory" },
            new Permission { Code = "inventory.putaway", Description = "putaway inventory" },

            // Locations
            new Permission { Code = "location.view", Description = "View locations" },
            new Permission { Code = "location.create", Description = "Create locations" },
            new Permission { Code = "location.update", Description = "Update locations" },
            new Permission { Code = "location.delete", Description = "Delete locations" },

            // Permissions
            new Permission { Code = "permission.view", Description = "View permissions" },
            new Permission { Code = "permission.create", Description = "Create permissions" },
            new Permission { Code = "permission.update", Description = "Update permissions" },
            new Permission { Code = "permission.delete", Description = "Delete permissions" },

            // Products
            new Permission { Code = "product.view", Description = "View products" },
            new Permission { Code = "product.create", Description = "Create products" },
            new Permission { Code = "product.update", Description = "Update products" },
            new Permission { Code = "product.delete", Description = "Delete products" },

            // Inbound
            new Permission { Code = "inbound.order.create", Description = "Create inbound orders" },
            new Permission { Code = "inbound.gr.receive", Description = "Receive goods receipts" },
            new Permission { Code = "inbound.order.view", Description = "View inbound orders" },
            new Permission { Code = "inbound.gr.counting", Description = "production counting" },
            new Permission { Code = "inbound.gr.approve", Description = "production approve" },
            new Permission { Code = "inbound.order.approve", Description = "Approve inbound orders" },
            new Permission { Code = "inbound.order.reject", Description = "Reject inbound orders" },
            new Permission { Code = "inbound.gr.create", Description = "Create goods receipts" },
            new Permission { Code = "inbound.gr.view", Description = "View goods receipts" },
            new Permission { Code = "inbound.gr.cancel", Description = "Cancel goods receipts" },

            // Roles
            new Permission { Code = "role.view", Description = "View roles" },
            new Permission { Code = "role.create", Description = "Create roles" },
            new Permission { Code = "role.update", Description = "Update roles" },
            new Permission { Code = "role.delete", Description = "Delete roles" },
            new Permission { Code = "role.assign-permission", Description = "Assign permissions to roles" },
            new Permission { Code = "role.remove-permission", Description = "Remove permissions from roles" },

            // Suppliers
            new Permission { Code = "supplier.view", Description = "View suppliers" },
            new Permission { Code = "supplier.create", Description = "Create suppliers" },
            new Permission { Code = "supplier.update", Description = "Update suppliers" },
            new Permission { Code = "supplier.delete", Description = "Delete suppliers" },

            // Units
            new Permission { Code = "unit.view", Description = "View units" },
            new Permission { Code = "unit.create", Description = "Create units" },
            new Permission { Code = "unit.update", Description = "Update units" },
            new Permission { Code = "unit.delete", Description = "Delete units" },

            // Warehouses
            new Permission { Code = "warehouse.view", Description = "View warehouses" },
            new Permission { Code = "warehouse.create", Description = "Create warehouses" },
            new Permission { Code = "warehouse.update", Description = "Update warehouses" },
            new Permission { Code = "warehouse.delete", Description = "Delete warehouses" },
            new Permission { Code = "warehouse.lock", Description = "Lock warehouses" },
            new Permission { Code = "warehouse.unlock", Description = "Unlock warehouses" },

            // Transfer
            new Permission { Code = "transfer.view", Description = "View transfer orders" },
            new Permission { Code = "transfer.create", Description = "Create transfer orders" },
            new Permission { Code = "transfer.approve", Description = "Approve transfer orders" },
            new Permission { Code = "transfer.cancel", Description = "Cancel transfer orders" },

            // Outbound
            new Permission { Code = "outbound.order.view", Description = "View outbound orders" },
            new Permission { Code = "outbound.order.create", Description = "Create outbound orders" },
            new Permission { Code = "outbound.order.update", Description = "Update outbound orders" },
            new Permission { Code = "outbound.order.approve", Description = "Approve outbound orders" },
            new Permission { Code = "outbound.order.reject", Description = "Reject outbound orders" },
            new Permission { Code = "outbound.order.picking", Description = "Picking outbound orders" },
            new Permission { Code = "outbound.order.issue", Description = "Issue outbound orders" },
        };

        var existingPermissions = db.Permissions.ToList();
        var existingCodes = existingPermissions.Select(p => p.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        
        // Add missing permissions
        var missingPermissions = permissions.Where(p => !existingCodes.Contains(p.Code)).ToList();
        if (missingPermissions.Any())
        {
            db.Permissions.AddRange(missingPermissions);
            await db.SaveChangesAsync();
        }

        // Migrate old names if any left
        bool changed = false;
        foreach (var p in db.Permissions.ToList())
        {
            if (p.Code.StartsWith("purchase."))
            {
                var newCode = p.Code.Replace("purchase.", "inbound.");
                // If it's something like purchase.view -> inbound.order.view (structural change)
                if (newCode == "inbound.view") newCode = "inbound.order.view";
                if (newCode == "inbound.create") newCode = "inbound.order.create";

                if (!existingCodes.Contains(newCode))
                {
                    p.Code = newCode;
                    changed = true;
                }
            }
            else if (p.Code.StartsWith("sales."))
            {
                var newCode = p.Code.Replace("sales.", "outbound.");
                if (newCode == "outbound.view") newCode = "outbound.order.view";
                if (newCode == "outbound.create") newCode = "outbound.order.create";

                if (!existingCodes.Contains(newCode))
                {
                    p.Code = newCode;
                    changed = true;
                }
            }
        }
        if (changed) await db.SaveChangesAsync();

        // 4. Seed Admin User
        if (!db.Users.Any())
        {
            var admin = new User
            {
                FullName = "Administrator",
                Email = "admin@wms.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123")
            };

            db.Users.Add(admin);
            await db.SaveChangesAsync();

            // assign admin role
            db.UserRoles.Add(new UserRole
            {
                UserId = admin.Id,
                RoleId = 1
            });
        }

        // 5. Ensure Admin has all permissions
        var allPermissionIds = await db.Permissions.Select(p => p.Id).ToListAsync();
        var existingAdminPermissionIds = await db.RolePermissions
            .Where(rp => rp.RoleId == 1)
            .Select(rp => rp.PermissionId)
            .ToListAsync();

        var missingPermissionIds = allPermissionIds.Except(existingAdminPermissionIds).ToList();
        if (missingPermissionIds.Any())
        {
            db.RolePermissions.AddRange(missingPermissionIds.Select(pid => new RolePermission
            {
                RoleId = 1,
                PermissionId = pid
            }));
            await db.SaveChangesAsync();
        }

        // 6. Cleanup: Map existing UserPermissions from old codes to new ones if they were explicitly assigned
        // This is a safety measure for direct user-permission assignments
        var oldToNewMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "purchase.view", "inbound.order.view" },
            { "purchase.create", "inbound.order.create" },
            { "purchase.approve", "inbound.order.approve" },
            { "sales.view", "outbound.order.view" },
            { "sales.create", "outbound.order.create" },
            { "sales.approve", "outbound.order.approve" }
        };

        foreach (var mapping in oldToNewMap)
        {
            var oldPerm = await db.Permissions.FirstOrDefaultAsync(p => p.Code == mapping.Key);
            var newPerm = await db.Permissions.FirstOrDefaultAsync(p => p.Code == mapping.Value);

            if (oldPerm != null && newPerm != null)
            {
                // Find users who have the old permission but not the new one
                var usersWithOld = await db.UserPermissions
                    .Where(up => up.PermissionId == oldPerm.Id)
                    .Select(up => up.UserId)
                    .ToListAsync();
                
                var usersWithNew = await db.UserPermissions
                    .Where(up => up.PermissionId == newPerm.Id)
                    .Select(up => up.UserId)
                    .ToListAsync();

                var usersToUpdate = usersWithOld.Except(usersWithNew).ToList();
                foreach (var userId in usersToUpdate)
                {
                    db.UserPermissions.Add(new UserPermission { UserId = userId, PermissionId = newPerm.Id });
                }
            }
        }
        await db.SaveChangesAsync();
    }
}
