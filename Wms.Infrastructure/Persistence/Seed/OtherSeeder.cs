using Microsoft.EntityFrameworkCore;
using Wms.Domain.Entity.Inventorys;
using Wms.Domain.Entity.MasterData;
using Wms.Domain.Entity.Warehouses;
using Wms.Domain.Enums;
using Wms.Domain.Enums.Inventory;
using Wms.Domain.Enums.location;
using Wms.Infrastructure.Persistence.Context;

namespace Wms.Infrastructure.Seed;

/// <summary>
/// Seed dữ liệu thực tế cho hệ thống kho nhựa kỹ thuật.
/// - 16 kho (4 loại × 4 kho/loại): RawMaterial, FinishedGoods, Auxiliary, Chemical
/// - Mỗi kho: đủ 6 loại location (Receiving, Storage×10, Shipping, Picking×2, Return, Damage)
/// - 50 sản phẩm nhựa kỹ thuật thực tế (vật liệu + thành phẩm)
/// - Lot + Inventory phân bổ đúng loại kho, số lượng sát thực tế
/// </summary>
public static class TechnicalPlasticWarehouseSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        var rnd = new Random(42);
        var now = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc);

        // ══════════════════════════════════════════════
        // 1. UNITS
        // ══════════════════════════════════════════════
        if (!await db.Units.AnyAsync())
        {
            db.Units.AddRange(
                new Unit { Code = "KG", Name = "Kilogram", IsActive = true, CreatedAt = now },
                new Unit { Code = "TON", Name = "Tấn (1000kg)", IsActive = true, CreatedAt = now },
                new Unit { Code = "BAG", Name = "Bao 25kg", IsActive = true, CreatedAt = now },
                new Unit { Code = "PAL", Name = "Pallet", IsActive = true, CreatedAt = now },
                new Unit { Code = "PCS", Name = "Cái / Chiếc", IsActive = true, CreatedAt = now },
                new Unit { Code = "SET", Name = "Bộ", IsActive = true, CreatedAt = now },
                new Unit { Code = "L", Name = "Lít", IsActive = true, CreatedAt = now }
            );
            await db.SaveChangesAsync();
            Console.WriteLine("→ Units seeded (7)");
        }
        var unitId = await db.Units.ToDictionaryAsync(u => u.Code, u => u.Id);

        // ══════════════════════════════════════════════
        // 2. BRANDS
        // ══════════════════════════════════════════════
        if (!await db.Brands.AnyAsync())
        {
            db.Brands.AddRange(
                new Brand { Code = "BR001", Name = "BASF SE", IsActive = true, Description = "Tập đoàn hóa chất BASF – Đức", CreatedAt = now },
                new Brand { Code = "BR002", Name = "Covestro", IsActive = true, Description = "Nhựa kỹ thuật cao cấp – Đức", CreatedAt = now },
                new Brand { Code = "BR003", Name = "Sabic", IsActive = true, Description = "Saudi Basic Industries Corporation – Ả Rập Saudi", CreatedAt = now },
                new Brand { Code = "BR004", Name = "DuPont", IsActive = true, Description = "Vật liệu kỹ thuật cao cấp – Mỹ", CreatedAt = now },
                new Brand { Code = "BR005", Name = "LG Chem", IsActive = true, Description = "Hóa chất LG – Hàn Quốc", CreatedAt = now },
                new Brand { Code = "BR006", Name = "Toray Industries", IsActive = true, Description = "Vật liệu tiên tiến – Nhật Bản", CreatedAt = now },
                new Brand { Code = "BR007", Name = "Teijin", IsActive = true, Description = "Nhựa kỹ thuật Teijin – Nhật Bản", CreatedAt = now },
                new Brand { Code = "BR008", Name = "Lanxess", IsActive = true, Description = "Hóa chất đặc biệt – Đức", CreatedAt = now },
                new Brand { Code = "BR009", Name = "Victrex", IsActive = true, Description = "PEEK và polymer hiệu suất cao – Anh", CreatedAt = now },
                new Brand { Code = "BR010", Name = "Celanese (Ticona)", IsActive = true, Description = "Nhựa kỹ thuật Celanese – Mỹ", CreatedAt = now },
                new Brand { Code = "BR011", Name = "Solvay", IsActive = true, Description = "Chất dẻo kỹ thuật cao cấp – Bỉ", CreatedAt = now },
                new Brand { Code = "BR012", Name = "Clariant", IsActive = true, Description = "Masterbatch & phụ gia – Thụy Sĩ", CreatedAt = now }
            );
            await db.SaveChangesAsync();
            Console.WriteLine("→ Brands seeded (12)");
        }
        var brandId = await db.Brands.ToDictionaryAsync(b => b.Code, b => b.Id);

        // ══════════════════════════════════════════════
        // 3. CATEGORIES
        // ══════════════════════════════════════════════
        if (!await db.Categories.AnyAsync())
        {
            db.Categories.AddRange(
                new Category { Code = "ABS", Name = "ABS – Acrylonitrile Butadiene Styrene", IsActive = true, CreatedAt = now },
                new Category { Code = "PC", Name = "PC – Polycarbonate", IsActive = true, CreatedAt = now },
                new Category { Code = "PA66", Name = "PA66 – Polyamide 66 (Nylon 66)", IsActive = true, CreatedAt = now },
                new Category { Code = "PA6", Name = "PA6 – Polyamide 6 (Nylon 6)", IsActive = true, CreatedAt = now },
                new Category { Code = "PP", Name = "PP – Polypropylene", IsActive = true, CreatedAt = now },
                new Category { Code = "POM", Name = "POM – Polyoxymethylene (Acetal)", IsActive = true, CreatedAt = now },
                new Category { Code = "PBT", Name = "PBT – Polybutylene Terephthalate", IsActive = true, CreatedAt = now },
                new Category { Code = "TPU", Name = "TPU – Thermoplastic Polyurethane", IsActive = true, CreatedAt = now },
                new Category { Code = "PEEK", Name = "PEEK – Polyether Ether Ketone", IsActive = true, CreatedAt = now },
                new Category { Code = "PPS", Name = "PPS – Polyphenylene Sulfide", IsActive = true, CreatedAt = now },
                new Category { Code = "PCABS", Name = "PC/ABS – Hợp kim Polycarbonate/ABS", IsActive = true, CreatedAt = now },
                new Category { Code = "ADD", Name = "Phụ Gia & Masterbatch", IsActive = true, CreatedAt = now },
                new Category { Code = "CHEM", Name = "Hóa Chất Xử Lý & Dung Môi", IsActive = true, CreatedAt = now },
                new Category { Code = "PKG", Name = "Bao Bì & Vật Liệu Đóng Gói", IsActive = true, CreatedAt = now },
                new Category { Code = "FG_CON", Name = "Thành Phẩm – Connector & Đầu Nối", IsActive = true, CreatedAt = now },
                new Category { Code = "FG_HSG", Name = "Thành Phẩm – Housing & Vỏ Máy", IsActive = true, CreatedAt = now }
            );
            await db.SaveChangesAsync();
            Console.WriteLine("→ Categories seeded (16)");
        }
        var catId = await db.Categories.ToDictionaryAsync(c => c.Code, c => c.Id);

        // ══════════════════════════════════════════════
        // 4. SUPPLIERS
        // ══════════════════════════════════════════════
        if (!await db.Suppliers.AnyAsync())
        {
            db.Suppliers.AddRange(
                new Supplier { Code = "SUP001", Name = "BASF Vietnam Co., Ltd", Email = "procurement@basf.com.vn", Phone = "02838231200", Address = "Tòa nhà Bitexco, 2 Hải Triều, Q.1, TP.HCM", IsActive = true, CreatedAt = now },
                new Supplier { Code = "SUP002", Name = "Covestro (Việt Nam) Co., Ltd", Email = "info.vn@covestro.com", Phone = "02422001200", Address = "Tầng 12, HM Town, 412 Nguyễn Thị Minh Khai, Q.3, TP.HCM", IsActive = true, CreatedAt = now },
                new Supplier { Code = "SUP003", Name = "Sabic Vietnam Pte. Ltd", Email = "contact@sabic.com.vn", Phone = "02838251900", Address = "Tầng 20, Keangnam Landmark, E6 Cầu Giấy, Hà Nội", IsActive = true, CreatedAt = now },
                new Supplier { Code = "SUP004", Name = "DuPont Vietnam Co., Ltd", Email = "vn.info@dupont.com", Phone = "02439411200", Address = "Tầng 15, Times City, 458 Minh Khai, Hai Bà Trưng, Hà Nội", IsActive = true, CreatedAt = now },
                new Supplier { Code = "SUP005", Name = "LG Chem Vietnam Co., Ltd", Email = "sales.vn@lgchem.com", Phone = "02513836000", Address = "Lô C-2A, KCN Amata, Long Bình, Biên Hòa, Đồng Nai", IsActive = true, CreatedAt = now },
                new Supplier { Code = "SUP006", Name = "Công ty CP Nhựa KT Miền Nam", Email = "info@nhuakythuatmn.com.vn", Phone = "02513830120", Address = "Lô B5, KCN Biên Hòa 2, TP. Biên Hòa, Đồng Nai", IsActive = true, CreatedAt = now },
                new Supplier { Code = "SUP007", Name = "Toray Plastics Vietnam Co., Ltd", Email = "contact@toray.com.vn", Phone = "02513818888", Address = "Lô E-8, KCN Long Thành, Long Thành, Đồng Nai", IsActive = true, CreatedAt = now },
                new Supplier { Code = "SUP008", Name = "Lanxess Vietnam Co., Ltd", Email = "vietnam@lanxess.com", Phone = "02838101200", Address = "Tầng 6, Saigon Centre Tower 2, 67 Lê Lợi, Q.1, TP.HCM", IsActive = true, CreatedAt = now },
                new Supplier { Code = "SUP009", Name = "Victrex Vietnam Representative", Email = "apac@victrex.com", Phone = "02438252000", Address = "Tầng 11, Lotte Center Hanoi, 54 Liễu Giai, Ba Đình, HN", IsActive = true, CreatedAt = now },
                new Supplier { Code = "SUP010", Name = "Celanese Vietnam Pte. Ltd", Email = "info.asia@celanese.com", Phone = "02836369900", Address = "Tầng 8, Deutsches Haus, 33 Lê Duẩn, Q.1, TP.HCM", IsActive = true, CreatedAt = now },
                new Supplier { Code = "SUP011", Name = "Công ty TNHH Hóa Chất Đại Á", Email = "sales@hoachataida.vn", Phone = "02513810200", Address = "45 Nguyễn Ái Quốc, P.Tân Tiến, TP. Biên Hòa, Đồng Nai", IsActive = true, CreatedAt = now },
                new Supplier { Code = "SUP012", Name = "Công ty CP Nhựa Bình Minh", Email = "info@binhminhplastic.vn", Phone = "02838140024", Address = "208 Nguyễn Văn Linh, P.Bình Thuận, Q.7, TP.HCM", IsActive = true, CreatedAt = now },
                new Supplier { Code = "SUP013", Name = "Clariant Vietnam Co., Ltd", Email = "vn.sales@clariant.com", Phone = "02838238888", Address = "Tầng 10, VietcomBank Tower, 5 Công Trường Mê Linh, Q.1, TP.HCM", IsActive = true, CreatedAt = now },
                new Supplier { Code = "SUP014", Name = "Solvay Vietnam Representative", Email = "vietnam@solvay.com", Phone = "02438256000", Address = "Tầng 14, Hanoi Towers, 49 Hai Bà Trưng, Hoàn Kiếm, HN", IsActive = true, CreatedAt = now },
                new Supplier { Code = "SUP015", Name = "Công ty TNHH Bao Bì Tân Phú", Email = "sales@baobitanphu.com.vn", Phone = "02838762000", Address = "Lô 5, Đường số 8, KCX Tân Thuận, Q.7, TP.HCM", IsActive = true, CreatedAt = now }
            );
            await db.SaveChangesAsync();
            Console.WriteLine("→ Suppliers seeded (15)");
        }
        var supId = await db.Suppliers.ToDictionaryAsync(s => s.Code, s => s.Id);

        // ══════════════════════════════════════════════
        // 5. CUSTOMERS
        // ══════════════════════════════════════════════
        if (!await db.Customers.AnyAsync())
        {
            db.Customers.AddRange(
                new Customer { Code = "CUS001", Name = "Samsung Electronics Vietnam (SEV)", Email = "procurement@sev.samsung.com", Phone = "02203840067", Address = "KCN Yên Phong, Yên Phong, Bắc Ninh", IsActive = true, CreatedAt = now },
                new Customer { Code = "CUS002", Name = "LG Electronics Việt Nam (LGEVN)", Email = "purchase@lgvina.com.vn", Phone = "02253742559", Address = "KCN Tràng Duệ, An Dương, Hải Phòng", IsActive = true, CreatedAt = now },
                new Customer { Code = "CUS003", Name = "Foxconn Industrial Park VN", Email = "material@foxconn.com.vn", Phone = "02203829666", Address = "KCN Đình Vũ, Hải An, Hải Phòng", IsActive = true, CreatedAt = now },
                new Customer { Code = "CUS004", Name = "Canon Vietnam Co., Ltd (CAVN)", Email = "supply@canon.com.vn", Phone = "02432186888", Address = "Lô A-5, KCN Thăng Long, Đông Anh, Hà Nội", IsActive = true, CreatedAt = now },
                new Customer { Code = "CUS005", Name = "VinFast Manufacturing LLC", Email = "logistics@vinfast.vn", Phone = "02253710999", Address = "Khu Kinh Tế Đình Vũ – Cát Hải, Hải Phòng", IsActive = true, CreatedAt = now },
                new Customer { Code = "CUS006", Name = "Panasonic Manufacturing Vietnam", Email = "purchase@panasonic.com.vn", Phone = "02432865000", Address = "Lô G-9, KCN Thăng Long, Đông Anh, Hà Nội", IsActive = true, CreatedAt = now },
                new Customer { Code = "CUS007", Name = "Robert Bosch Vietnam Co., Ltd", Email = "procurement.vn@bosch.com", Phone = "02838140000", Address = "Tầng 18, CornerStone Building, 16 Phan Chu Trinh, HN", IsActive = true, CreatedAt = now },
                new Customer { Code = "CUS008", Name = "Cty CP Cơ Điện Tử Miền Nam (EMC)", Email = "sales@emc.com.vn", Phone = "02513820200", Address = "Lô D1, KCN Amata, Long Bình, Biên Hòa, Đồng Nai", IsActive = true, CreatedAt = now },
                new Customer { Code = "CUS009", Name = "Thaco Trường Hải Auto Corp", Email = "material@thaco.com.vn", Phone = "02353504888", Address = "KCN Chu Lai, Núi Thành, Quảng Nam", IsActive = true, CreatedAt = now },
                new Customer { Code = "CUS010", Name = "Cty TNHH Nhựa Đại Đồng Tiến", Email = "purchase@daidongtienplas.vn", Phone = "02513890200", Address = "Lô B4, KCN Sóng Thần 2, Dĩ An, Bình Dương", IsActive = true, CreatedAt = now },
                new Customer { Code = "CUS011", Name = "Datalogic Vietnam Co., Ltd", Email = "supply.vn@datalogic.com", Phone = "02513831999", Address = "Lô E-3, KCN Long Thành, Long Thành, Đồng Nai", IsActive = true, CreatedAt = now },
                new Customer { Code = "CUS012", Name = "Rạng Đông Holding Corp.", Email = "logistics@rangdong.com.vn", Phone = "02435532204", Address = "87-89 Hạ Đình, Thanh Xuân, Hà Nội", IsActive = true, CreatedAt = now },
                new Customer { Code = "CUS013", Name = "Toyota Motor Vietnam (TMV)", Email = "supply@toyota.com.vn", Phone = "04836887272", Address = "Phú Thị, Gia Lâm, Hà Nội", IsActive = true, CreatedAt = now },
                new Customer { Code = "CUS014", Name = "Schneider Electric Vietnam", Email = "vn.purchasing@schneider-electric.com", Phone = "02438375000", Address = "Tầng 15, Hà Nội Towerland, 49 Hai Bà Trưng, HN", IsActive = true, CreatedAt = now }
            );
            await db.SaveChangesAsync();
            Console.WriteLine("→ Customers seeded (14)");
        }

        // ══════════════════════════════════════════════
        // 6. WAREHOUSES – 4 kho × 4 loại = 16 kho
        // ══════════════════════════════════════════════
        if (!await db.Warehouses.AnyAsync())
        {
            db.Warehouses.AddRange(

                // ── RawMaterial – Kho nguyên vật liệu nhựa đầu vào ──
                new Warehouse { Code = "WH-RM01", Name = "Kho Nguyên Liệu Nhựa A – Amata", Address = "Lô C5, KCN Amata, Long Bình, Biên Hòa, Đồng Nai", WarehouseType = WarehouseType.RawMaterial, Status = WarehouseStatus.Active, CreatedAt = now.AddMonths(-36) },
                new Warehouse { Code = "WH-RM02", Name = "Kho Nguyên Liệu Nhựa B – Biên Hòa", Address = "Lô D12, KCN Biên Hòa 2, TP. Biên Hòa, Đồng Nai", WarehouseType = WarehouseType.RawMaterial, Status = WarehouseStatus.Active, CreatedAt = now.AddMonths(-30) },
                new Warehouse { Code = "WH-RM03", Name = "Kho Nguyên Liệu Nhập Khẩu – Long Thành", Address = "Lô A3, KCN Long Thành, Long Thành, Đồng Nai", WarehouseType = WarehouseType.RawMaterial, Status = WarehouseStatus.Active, CreatedAt = now.AddMonths(-18) },
                new Warehouse { Code = "WH-RM04", Name = "Kho Nguyên Liệu Dự Trữ – Nhơn Trạch", Address = "Lô B9, KCN Nhơn Trạch 3, Nhơn Trạch, Đồng Nai", WarehouseType = WarehouseType.RawMaterial, Status = WarehouseStatus.Active, CreatedAt = now.AddMonths(-12) },

                // ── FinishedGoods – Kho thành phẩm ──
                new Warehouse { Code = "WH-FG01", Name = "Kho Thành Phẩm Xuất Khẩu – Amata", Address = "Lô B7, KCN Amata, Long Bình, Biên Hòa, Đồng Nai", WarehouseType = WarehouseType.FinishedGoods, Status = WarehouseStatus.Active, CreatedAt = now.AddMonths(-36) },
                new Warehouse { Code = "WH-FG02", Name = "Kho Thành Phẩm Nội Địa – Bình Dương", Address = "Lô E2, KCN Sóng Thần 2, Dĩ An, Bình Dương", WarehouseType = WarehouseType.FinishedGoods, Status = WarehouseStatus.Active, CreatedAt = now.AddMonths(-28) },
                new Warehouse { Code = "WH-FG03", Name = "Kho Thành Phẩm Phân Phối – Đồng An", Address = "Lô F9, KCN Đồng An 2, Thuận An, Bình Dương", WarehouseType = WarehouseType.FinishedGoods, Status = WarehouseStatus.Active, CreatedAt = now.AddMonths(-20) },
                new Warehouse { Code = "WH-FG04", Name = "Kho Thành Phẩm Cảng – Cát Lái", Address = "Lô G3, KCX Linh Trung 2, Thủ Đức, TP.HCM", WarehouseType = WarehouseType.FinishedGoods, Status = WarehouseStatus.Active, CreatedAt = now.AddMonths(-14) },

                // ── Auxiliary – Kho vật tư phụ trợ ──
                new Warehouse { Code = "WH-AX01", Name = "Kho Vật Tư Phụ Trợ – Biên Hòa 1", Address = "Lô G4, KCN Biên Hòa 1, TP. Biên Hòa, Đồng Nai", WarehouseType = WarehouseType.Auxiliary, Status = WarehouseStatus.Active, CreatedAt = now.AddMonths(-30) },
                new Warehouse { Code = "WH-AX02", Name = "Kho Bao Bì & Đóng Gói – Nhơn Trạch", Address = "Lô H1, KCN Nhơn Trạch 2, Nhơn Trạch, Đồng Nai", WarehouseType = WarehouseType.Auxiliary, Status = WarehouseStatus.Active, CreatedAt = now.AddMonths(-24) },
                new Warehouse { Code = "WH-AX03", Name = "Kho Phụ Tùng Máy Ép – Long Đức", Address = "Lô I6, KCN Long Đức, Long Toàn, Trà Vinh", WarehouseType = WarehouseType.Auxiliary, Status = WarehouseStatus.Active, CreatedAt = now.AddMonths(-15) },
                new Warehouse { Code = "WH-AX04", Name = "Kho Vật Tư Tiêu Hao – Sóng Thần", Address = "Lô K2, KCN Sóng Thần 1, Dĩ An, Bình Dương", WarehouseType = WarehouseType.Auxiliary, Status = WarehouseStatus.Active, CreatedAt = now.AddMonths(-9) },

                // ── Chemical – Kho hóa chất ──
                new Warehouse { Code = "WH-CH01", Name = "Kho Hóa Chất Phụ Gia – Amata Mở Rộng", Address = "Lô J2, KCN Amata Expansion, Long Bình, Biên Hòa, Đồng Nai", WarehouseType = WarehouseType.Chemical, Status = WarehouseStatus.Active, CreatedAt = now.AddMonths(-28) },
                new Warehouse { Code = "WH-CH02", Name = "Kho Dung Môi & Chất Tạo Màu – Nhơn Trạch 5", Address = "Lô K5, KCN Nhơn Trạch 5, Nhơn Trạch, Đồng Nai", WarehouseType = WarehouseType.Chemical, Status = WarehouseStatus.Active, CreatedAt = now.AddMonths(-20) },
                new Warehouse { Code = "WH-CH03", Name = "Kho Chất Chống Cháy – Hiệp Phước", Address = "Lô L8, KCN Hiệp Phước, Nhà Bè, TP.HCM", WarehouseType = WarehouseType.Chemical, Status = WarehouseStatus.Active, CreatedAt = now.AddMonths(-14) },
                new Warehouse { Code = "WH-CH04", Name = "Kho Hóa Chất Xử Lý Bề Mặt – Tân Đông Hiệp", Address = "Lô M3, KCN Tân Đông Hiệp, Dĩ An, Bình Dương", WarehouseType = WarehouseType.Chemical, Status = WarehouseStatus.Active, CreatedAt = now.AddMonths(-8) }
            );
            await db.SaveChangesAsync();
            Console.WriteLine("→ Warehouses seeded (16)");
        }

        var allWarehouses = await db.Warehouses.OrderBy(w => w.Code).ToListAsync();

        // ══════════════════════════════════════════════
        // 7. LOCATIONS – mỗi kho có đầy đủ 6 loại vị trí
        //    Cấu trúc mỗi kho:
        //    - 1 × Receiving  (cổng nhận hàng đầu vào)
        //    - 10 × Storage   (dãy A-J, mỗi dãy 1 kệ – tổng 10 vị trí lưu trữ)
        //    - 1 × Shipping   (cổng xuất hàng)
        //    - 2 × Picking    (khu lấy hàng cho sản xuất)
        //    - 1 × Return     (khu QC / hàng trả về)
        //    - 1 × Damage     (khu cách ly hàng hỏng/hết hạn – inactive)
        //    → Tổng 16 vị trí/kho × 16 kho = 256 vị trí
        // ══════════════════════════════════════════════
        if (!await db.Locations.AnyAsync())
        {
            var locations = new List<Location>();

            foreach (var wh in allWarehouses)
            {
                var c = wh.Code;

                // Receiving
                locations.Add(Loc(wh.Id, $"{c}-RCV-01", "Cổng nhận hàng – kiểm tra đầu vào", LocationType.Receiving, true));

                // Storage 10 vị trí: dãy A-E, kệ 01-02 mỗi dãy
                string[] rows = { "A", "B", "C", "D", "E" };
                foreach (var row in rows)
                    for (int s = 1; s <= 2; s++)
                        locations.Add(Loc(wh.Id, $"{c}-{row}{s:D2}", $"Dãy {row}, Kệ {s:D2} – lưu trữ", LocationType.Storage, true));

                // Shipping
                locations.Add(Loc(wh.Id, $"{c}-SHP-01", "Cổng xuất hàng – staging khu đóng gói", LocationType.Shipping, true));

                // Picking ×2
                locations.Add(Loc(wh.Id, $"{c}-PCK-01", "Khu lấy hàng – Line 1", LocationType.Picking, true));
                locations.Add(Loc(wh.Id, $"{c}-PCK-02", "Khu lấy hàng – Line 2", LocationType.Picking, true));

                // Return
                locations.Add(Loc(wh.Id, $"{c}-RET-01", "Khu QC & tiếp nhận hàng trả về", LocationType.Return, true));

                // Damage (inactive – vị trí cách ly)
                locations.Add(Loc(wh.Id, $"{c}-DMG-01", "Khu cách ly hàng hỏng / hết hạn / không đạt QC", LocationType.Damage, false));
            }

            db.Locations.AddRange(locations);
            await db.SaveChangesAsync();
            Console.WriteLine($"→ Locations seeded ({locations.Count} vị trí trên {allWarehouses.Count} kho)");
        }

        // ══════════════════════════════════════════════
        // 8. PRODUCTS – 50 sản phẩm đầy đủ các dòng
        // ══════════════════════════════════════════════
        if (!await db.Products.AnyAsync())
        {
            Product P(string code, string name, string desc, ProductType type,
                      string cat, string brand, string unit, string sup) => new()
                      {
                          Code = code,
                          Name = name,
                          Description = desc,
                          IsActive = true,
                          Type = type,
                          CategoryId = catId[cat],
                          BrandId = brandId[brand],
                          UnitId = unitId[unit],
                          SupplierId = supId[sup],
                          CreatedAt = now.AddMonths(-rnd.Next(1, 36)),
                      };

            db.Products.AddRange(

                // ── ABS (4 SKU) ──
                P("P0001", "ABS HI-ABS 750 – LG Chem", "High Impact ABS, MFI 20 g/10min, tiêu chuẩn ngành điện tử", ProductType.Material, "ABS", "BR005", "KG", "SUP005"),
                P("P0002", "ABS GF10 – BASF Terluran GP-22 GF10", "ABS gia cường 10% sợi thủy tinh, chịu va đập cao hơn 40%", ProductType.Material, "ABS", "BR001", "KG", "SUP001"),
                P("P0003", "ABS V0 FR – Sabic Cycolac MG47", "ABS chống cháy UL94 V-0, không halogen, dùng cho thiết bị điện", ProductType.Material, "ABS", "BR003", "KG", "SUP003"),
                P("P0004", "ABS ESD – Covestro Bayblend ESD", "ABS chống tĩnh điện, điện trở bề mặt 10^6–10^9 Ω, ngành điện tử", ProductType.Material, "ABS", "BR002", "KG", "SUP002"),

                // ── PC (4 SKU) ──
                P("P0005", "PC Makrolon 2205 – Covestro", "PC đa năng, trong suốt, MFI 10 g/10min (300°C/1.2kg)", ProductType.Material, "PC", "BR002", "KG", "SUP002"),
                P("P0006", "PC Makrolon 2405 – Covestro", "PC chịu nhiệt cao, Tg=147°C, dùng đèn xe, nắp đèn LED", ProductType.Material, "PC", "BR002", "KG", "SUP002"),
                P("P0007", "PC GF20 – Sabic Lexan 500R", "PC gia cường 20% GF, độ cứng uốn 6,500 MPa, chi tiết cơ khí", ProductType.Material, "PC", "BR003", "KG", "SUP003"),
                P("P0008", "PC FR V0 – Covestro Makrolon FR2020", "PC chống cháy V-0, dày 3mm, vỏ thiết bị điện – viễn thông", ProductType.Material, "PC", "BR002", "KG", "SUP002"),

                // ── PA66 (4 SKU) ──
                P("P0009", "PA66 Ultramid A3WG6 GF30 – BASF", "PA66/GF30, chịu nhiệt 220°C liên tục, tiêu chuẩn ô tô", ProductType.Material, "PA66", "BR001", "KG", "SUP001"),
                P("P0010", "PA66 Zytel 101L – DuPont", "PA66 nguyên chất chưa gia cường, dẻo dai, dùng sợi & màng", ProductType.Material, "PA66", "BR004", "KG", "SUP004"),
                P("P0011", "PA66 GF25 FR V0 – Lanxess Durethan", "PA66/GF25 chống cháy V-0, cho relay, circuit breaker", ProductType.Material, "PA66", "BR008", "KG", "SUP008"),
                P("P0012", "PA66 GF15 – DuPont Zytel 72G15", "PA66/GF15 cân bằng cứng-dẻo, dùng clip, bracket ô tô", ProductType.Material, "PA66", "BR004", "KG", "SUP004"),

                // ── PA6 (3 SKU) ──
                P("P0013", "PA6 Ultramid B3WG6 GF30 – BASF", "PA6/GF30, module kéo 11,000 MPa, tiêu chuẩn Nylon kỹ thuật ô tô", ProductType.Material, "PA6", "BR001", "KG", "SUP001"),
                P("P0014", "PA6 Grilon F34 – EMS", "PA6 nguyên chất, độ hấp thụ ẩm 2.5% (điều kiện cân bằng)", ProductType.Material, "PA6", "BR005", "KG", "SUP005"),
                P("P0015", "PA6 GF30 FR V2 – Lanxess Durethan BKV30 FR", "PA6/GF30 flame retardant V-2, dùng vỏ motor, connector", ProductType.Material, "PA6", "BR008", "KG", "SUP008"),

                // ── PP (4 SKU) ──
                P("P0016", "PP Homopolymer HH440FB – LG Chem", "PP Homo, MFI 20 g/10min, dùng sản xuất bao bì công nghiệp", ProductType.Material, "PP", "BR005", "KG", "SUP005"),
                P("P0017", "PP GF20 – Sabic Purell PP GF20", "PP gia cường 20% GF, độ cứng uốn 3,800 MPa, linh kiện ô tô", ProductType.Material, "PP", "BR003", "KG", "SUP003"),
                P("P0018", "PP Copolymer BI848MO – Toray", "PP copoly, chịu va đập thấp nhiệt, dùng nắp động cơ, tản nhiệt", ProductType.Material, "PP", "BR006", "KG", "SUP007"),
                P("P0019", "PP TD20 – Sabic Stamax 20YM240E", "PP gia cường 20% bột talc, giảm co rút, dùng dashboard ô tô", ProductType.Material, "PP", "BR003", "KG", "SUP003"),

                // ── POM (3 SKU) ──
                P("P0020", "POM Delrin 500NC – DuPont", "POM homopolymer, độ bền mòn cao, dùng bánh răng, cam, trục", ProductType.Material, "POM", "BR004", "KG", "SUP004"),
                P("P0021", "POM Ultraform N2320 – BASF", "POM copolymer ổn định nhiệt tốt hơn, dùng cơ cấu truyền động", ProductType.Material, "POM", "BR001", "KG", "SUP001"),
                P("P0022", "POM GF25 – Celanese Hostaform GF25", "POM/GF25, độ cứng cao 130 MPa, dùng linh kiện cơ khí chính xác", ProductType.Material, "POM", "BR010", "KG", "SUP010"),

                // ── PBT (3 SKU) ──
                P("P0023", "PBT Pocan B3235 GF30 – Lanxess", "PBT/GF30, chịu nhiệt 220°C ngắn hạn, dùng connector điện tử", ProductType.Material, "PBT", "BR008", "KG", "SUP008"),
                P("P0024", "PBT GF15 FR V0 – Lanxess Pocan TP221", "PBT/GF15 chống cháy V-0, cho micro connector, rơ le", ProductType.Material, "PBT", "BR008", "KG", "SUP008"),
                P("P0025", "PBT Valox 325 – Sabic", "PBT nguyên chất, co rút thấp, bề mặt bóng, dùng vỏ điện tử", ProductType.Material, "PBT", "BR003", "KG", "SUP003"),

                // ── TPU (3 SKU) ──
                P("P0026", "TPU Desmopan 85A – Covestro", "TPU Shore 85A mềm dẻo, chịu dầu tốt, dùng gioăng, ống mềm", ProductType.Material, "TPU", "BR002", "KG", "SUP002"),
                P("P0027", "TPU Desmopan 95A – Covestro", "TPU Shore 95A bán cứng, dùng bánh xe, đế giày bảo hộ", ProductType.Material, "TPU", "BR002", "KG", "SUP002"),
                P("P0028", "TPU FR 87A – Covestro Desmopan DP9370AU", "TPU chống cháy V-0, Shore 87A, dùng vỏ bọc cáp điện công nghiệp", ProductType.Material, "TPU", "BR002", "KG", "SUP002"),

                // ── PEEK (2 SKU) ──
                P("P0029", "PEEK Victrex 150G", "PEEK tiêu chuẩn, Tg=143°C, dùng chi tiết bơm, van, linh kiện y tế", ProductType.Material, "PEEK", "BR009", "KG", "SUP009"),
                P("P0030", "PEEK Victrex 450GL30 GF30", "PEEK/GF30, bền nhiệt 250°C liên tục, dùng linh kiện bán dẫn, hàng không", ProductType.Material, "PEEK", "BR009", "KG", "SUP009"),

                // ── PPS (2 SKU) ──
                P("P0031", "PPS Fortron 1140L4 GF40 – Celanese", "PPS/GF40, chịu hóa chất xuất sắc, dùng linh kiện ô tô dưới nắp capo", ProductType.Material, "PPS", "BR010", "KG", "SUP010"),
                P("P0032", "PPS Ryton R4-220 – Solvay", "PPS nguyên chất, chịu nhiệt 220°C, dùng linh kiện điện cao tần", ProductType.Material, "PPS", "BR011", "KG", "SUP014"),

                // ── PC/ABS (2 SKU) ──
                P("P0033", "PC/ABS Bayblend T65 – Covestro", "PC/ABS 65%PC cân bằng độ bền-gia công, dùng vỏ máy tính, tivi", ProductType.Material, "PCABS", "BR002", "KG", "SUP002"),
                P("P0034", "PC/ABS FR2000 V0 – Covestro", "PC/ABS chống cháy V-0, tương thích sơn UV, dùng vỏ điện tử ô tô", ProductType.Material, "PCABS", "BR002", "KG", "SUP002"),

                // ── Phụ Gia & Masterbatch ADD (6 SKU) ──
                P("P0035", "MB Màu Đen Carbon Black 40% – Clariant", "Masterbatch màu đen, tỷ lệ dùng 1-3%, cho ABS/PP/PA", ProductType.Material, "ADD", "BR012", "KG", "SUP013"),
                P("P0036", "MB Màu Trắng TiO2 60% – Clariant", "Masterbatch màu trắng, độ phủ cao, tỷ lệ 2-5%, ngành điện tử", ProductType.Material, "ADD", "BR012", "KG", "SUP013"),
                P("P0037", "Phụ Gia Chống UV HALS – BASF Tinuvin", "UV stabilizer HALS dạng hạt, bảo vệ màu sắc & cơ tính ngoài trời", ProductType.Material, "ADD", "BR001", "KG", "SUP011"),
                P("P0038", "Phụ Gia Chống Cháy Halogen-Free – Lanxess", "FR Exolit OP 1312 không halogen, cho PA/PP, đạt UL94 V-0", ProductType.Material, "ADD", "BR008", "KG", "SUP011"),
                P("P0039", "Phụ Gia Bôi Trơn Nội Tại – DuPont", "Lube PA/POM grade, giảm ma sát 30-40%, cải thiện tốc độ xuất", ProductType.Material, "ADD", "BR004", "KG", "SUP011"),
                P("P0040", "Phụ Gia Hút Ẩm Masterbatch – Clariant", "Desiccant masterbatch, hấp thụ ẩm khi gia công PA/PBT/PET", ProductType.Material, "ADD", "BR012", "KG", "SUP013"),

                // ── Hóa Chất CHEM (4 SKU) ──
                P("P0041", "Dung Môi IPA (Isopropanol) 99.9%", "IPA công nghiệp làm sạch khuôn, linh kiện trước sơn, 200L/phuy", ProductType.Material, "CHEM", "BR011", "L", "SUP011"),
                P("P0042", "Chất Tháo Khuôn Wax Silicone – Chem-Trend", "Mold release wax silicone, dùng cho khuôn nhựa tất cả vật liệu", ProductType.Material, "CHEM", "BR012", "KG", "SUP011"),
                P("P0043", "Chất Làm Sạch Khuôn BC-100", "Purging compound dạng hạt, làm sạch nòng vít máy ép phun", ProductType.Material, "CHEM", "BR010", "KG", "SUP011"),
                P("P0044", "Dầu Truyền Nhiệt Therminol 55", "Thermal oil cho hệ thống gia nhiệt khuôn 150-250°C, 20L/can", ProductType.Material, "CHEM", "BR011", "L", "SUP014"),

                // ── Bao Bì PKG (2 SKU) ──
                P("P0045", "Túi PE Chống Ẩm 3-Lớp 25kg", "Bao gói PE 3 lớp chống ẩm, chống tĩnh điện, đựng hạt nhựa 25kg", ProductType.Material, "PKG", "BR012", "PCS", "SUP015"),
                P("P0046", "Pallet Nhựa HDPE 1200×1000mm", "Pallet nhựa tái sinh HDPE, tải 1.5 tấn, dùng trong kho nhiệt cao", ProductType.Material, "PKG", "BR012", "PCS", "SUP015"),

                // ── Thành Phẩm FG_CON (2 SKU) ──
                P("P0047", "Connector PA66/GF30 Ô Tô – 2-Pin", "Đầu nối 2 chân PA66/GF30+FR, IP67, kháng xăng dầu, tiêu chuẩn AEC-Q200", ProductType.Production, "FG_CON", "BR001", "PCS", "SUP001"),
                P("P0048", "Connector PBT/GF30 Điện Tử – 4-Pin", "Micro connector 4 chân PBT/GF30, pitch 2.54mm, kháng hồi lưu", ProductType.Production, "FG_CON", "BR008", "PCS", "SUP008"),

                // ── Thành Phẩm FG_HSG (2 SKU) ──
                P("P0049", "Housing ABS/FR Điện Tử – 120×80×30mm", "Vỏ thiết bị điện tử ABS V-0, độ dày vách 2mm, lắp snap-fit", ProductType.Production, "FG_HSG", "BR002", "PCS", "SUP002"),
                P("P0050", "Housing PC/ABS Ô Tô – 200×150×50mm", "Vỏ ECU ô tô PC/ABS FR V-0, cản UV, chịu nhiệt 120°C, sơn tĩnh điện", ProductType.Production, "FG_HSG", "BR002", "PCS", "SUP002")
            );
            await db.SaveChangesAsync();
            Console.WriteLine("→ Products seeded (50)");
        }

        var allProducts = await db.Products.OrderBy(p => p.Code).ToListAsync();

        // ══════════════════════════════════════════════
        // 9. LOTS – gán lot thực tế cho mỗi sản phẩm
        //    Nguyên liệu: 2-3 lot/sản phẩm (nhiều lô nhập khác nhau)
        //    Thành phẩm: 1-2 lot/sản phẩm (lô sản xuất theo PO)
        // ══════════════════════════════════════════════
        if (!await db.Lots.AnyAsync())
        {
            var lots = new List<Lot>();
            int lotSeq = 1;

            foreach (var prod in allProducts)
            {
                bool isFG = prod.Type == ProductType.Production;
                int lotCount = isFG ? rnd.Next(1, 3) : rnd.Next(2, 4);

                for (int j = 1; j <= lotCount; j++)
                {
                    // Nguyên liệu: nhập 1-18 tháng trước | Thành phẩm: sản xuất 1-6 tháng trước
                    var mfg = now.AddMonths(-rnd.Next(1, isFG ? 7 : 19))
                                 .AddDays(-rnd.Next(0, 28));

                    // Hạn sử dụng: nguyên liệu 24-60 tháng | thành phẩm 18-36 tháng
                    var expiry = mfg.AddMonths(isFG ? rnd.Next(18, 37) : rnd.Next(24, 61));

                    // Mã lot theo format thực tế: PROD-LOTSEQ-YYYYMM
                    string lotCode = $"{prod.Code}-L{lotSeq:D4}-{mfg:yyyyMM}";
                    lotSeq++;

                    lots.Add(new Lot
                    {
                        Id = Guid.NewGuid(),
                        Code = lotCode,
                        productId = prod.Id,
                        ManufacturingDate = mfg,
                        ExpiryDate = expiry,
                        CreatedAt = mfg,
                    });
                }
            }

            db.Lots.AddRange(lots);
            await db.SaveChangesAsync();
            Console.WriteLine($"→ Lots seeded ({lots.Count} lots)");
        }

        var lotByProduct = await db.Lots
            .GroupBy(l => l.productId)
            .ToDictionaryAsync(g => g.Key, g => g.Select(l => l.Id).ToList());

        // ══════════════════════════════════════════════
        // 10. INVENTORIES – phân bổ tồn kho đúng loại kho, số lượng sát thực tế
        //     Nguyên liệu nhựa: phân vào kho RawMaterial  (500–8,000 kg/vị trí)
        //     Phụ gia/hóa chất: phân vào kho Chemical     (50–1,500 kg/vị trí)
        //     Bao bì/vật tư:    phân vào kho Auxiliary    (100–5,000 pcs/pallet)
        //     Thành phẩm:       phân vào kho FinishedGoods (500–20,000 pcs/vị trí)
        // ══════════════════════════════════════════════
        if (!await db.Inventories.AnyAsync())
        {
            var storageLocs = await db.Locations
                .Where(l => l.Type == LocationType.Storage && l.IsActive)
                .GroupBy(l => l.WarehouseId)
                .ToDictionaryAsync(g => g.Key, g => g.Select(l => l.Id).ToList());

            var whByType = await db.Warehouses
                .GroupBy(w => w.WarehouseType)
                .ToDictionaryAsync(g => g.Key, g => g.Select(w => w.Id).ToList());

            var rmWhs = whByType.GetValueOrDefault(WarehouseType.RawMaterial, new List<Guid>());
            var fgWhs = whByType.GetValueOrDefault(WarehouseType.FinishedGoods, new List<Guid>());
            var axWhs = whByType.GetValueOrDefault(WarehouseType.Auxiliary, new List<Guid>());
            var chWhs = whByType.GetValueOrDefault(WarehouseType.Chemical, new List<Guid>());

            // Phân loại sản phẩm → loại kho tương ứng
            var chemCodes = new HashSet<string> { "P0041", "P0042", "P0043", "P0044" };
            var pkgCodes = new HashSet<string> { "P0045", "P0046" };
            var addCodes = new HashSet<string> { "P0035", "P0036", "P0037", "P0038", "P0039", "P0040" };

            var inventories = new List<Inventory>();

            foreach (var prod in allProducts)
            {
                if (!lotByProduct.ContainsKey(prod.Id)) continue;

                // Chọn loại kho phù hợp
                List<Guid> targetWhs;
                if (prod.Type == ProductType.Production)
                    targetWhs = fgWhs;
                else if (chemCodes.Contains(prod.Code))
                    targetWhs = chWhs;
                else if (pkgCodes.Contains(prod.Code))
                    targetWhs = axWhs;
                else if (addCodes.Contains(prod.Code))
                    // Phụ gia: lưu 1 kho Chemical + 1 kho RawMaterial (vì nhiều factory dùng)
                    targetWhs = chWhs.Concat(rmWhs.Take(1)).ToList();
                else
                    // Nguyên liệu chính: phân bổ 1-3 kho RawMaterial
                    targetWhs = rmWhs;

                if (!targetWhs.Any()) continue;

                // Số kho được phân bổ tồn kho
                int whCount = prod.Type == ProductType.Production ? rnd.Next(1, 3)
                            : pkgCodes.Contains(prod.Code) ? 1
                            : chemCodes.Contains(prod.Code) ? rnd.Next(1, 3)
                            : rnd.Next(1, 4); // nguyên liệu chính: 1-3 kho

                foreach (var whId in targetWhs.OrderBy(_ => rnd.Next()).Take(whCount))
                {
                    if (!storageLocs.ContainsKey(whId)) continue;

                    var locs = storageLocs[whId];
                    var locId = locs[rnd.Next(locs.Count)];

                    // Có thể phân bổ nhiều lot trên cùng 1 kho (FIFO)
                    int lotCountForWh = rnd.Next(1, Math.Min(3, lotByProduct[prod.Id].Count + 1));
                    var selectedLots = lotByProduct[prod.Id]
                        .OrderBy(_ => rnd.Next())
                        .Take(lotCountForWh)
                        .ToList();

                    foreach (var lotId in selectedLots)
                    {
                        // Số lượng tồn thực tế theo loại sản phẩm (toàn bộ số nguyên)
                        decimal qty;
                        if (prod.Type == ProductType.Production)
                            qty = (decimal)(rnd.Next(1000, 20001));       // 1,000–20,000 PCS
                        else if (chemCodes.Contains(prod.Code))
                            qty = (decimal)(rnd.Next(100, 1501));         // 100–1,500 kg/L
                        else if (pkgCodes.Contains(prod.Code))
                            qty = (decimal)(rnd.Next(100, 5001));         // 100–5,000 PCS/PAL
                        else if (addCodes.Contains(prod.Code))
                            qty = (decimal)(rnd.Next(100, 2001));         // 100–2,000 kg
                        else
                            qty = (decimal)(rnd.Next(500, 8001));         // 500–8,000 kg

                        // Một phần nhỏ bị lock (đã lên PO xuất nhưng chưa xuất)
                        decimal locked = qty > 500m
                            ? (decimal)(rnd.Next(0, (int)(qty * 0.15m))) // 0-15% locked, số nguyên
                            : 0m;

                        inventories.Add(new Inventory
                        {
                            Id = Guid.NewGuid(),
                            WarehouseId = whId,
                            LocationId = locId,
                            LotId = lotId,
                            ProductId = prod.Id,
                            OnHandQuantity = qty,
                            LockedQuantity = locked,
                            InTransitQuantity = 0m,
                            CreatedAt = now.AddDays(-rnd.Next(1, 180)),
                        });
                    }
                }
            }

            db.Inventories.AddRange(inventories);
            await db.SaveChangesAsync();
            Console.WriteLine($"→ Inventories seeded ({inventories.Count} bản ghi)");
        }
        // ══════════════════════════════════════════════
        // 11. BACKFILL BASE PRODUCT UOMS
        // ══════════════════════════════════════════════
        var productsWithoutBaseUom = await db.Products
            .Where(p => !db.ProductUoms.Any(u => u.ProductId == p.Id && u.UnitId == p.UnitId && u.IsBaseUnit))
            .ToListAsync();

        if (productsWithoutBaseUom.Any())
        {
            var backfilledUoms = productsWithoutBaseUom.Select(p => new ProductUom
            {
                ProductId = p.Id,
                UnitId = p.UnitId,
                Factor = 1,
                IsBaseUnit = true,
                CreatedAt = DateTime.UtcNow
            }).ToList();
            db.ProductUoms.AddRange(backfilledUoms);
            await db.SaveChangesAsync();
            Console.WriteLine($"→ Backfilled base ProductUom for {productsWithoutBaseUom.Count} products");
        }

        // ══════════════════════════════════════════════
        // 12. BACKFILL TRANSACTION ITEMS' UNITID AND BASEQUANTITY
        // ══════════════════════════════════════════════
        // InboundOrderItems
        var inboundItemsToFix = await db.InboundOrderItems
            .Where(i => i.UnitId <= 0 || i.BaseQuantity <= 0)
            .ToListAsync();
        foreach (var item in inboundItemsToFix)
        {
            var prod = await db.Products.FindAsync(item.ProductId);
            if (prod != null)
            {
                if (item.UnitId <= 0) item.UnitId = prod.UnitId;
                if (item.BaseQuantity <= 0) item.BaseQuantity = item.Quantity;
            }
        }

        // OutboundOrderItems
        var outboundItemsToFix = await db.OutboundOrderItems
            .Where(i => i.UnitId <= 0 || i.BaseQuantity <= 0)
            .ToListAsync();
        foreach (var item in outboundItemsToFix)
        {
            var prod = await db.Products.FindAsync(item.ProductId);
            if (prod != null)
            {
                if (item.UnitId <= 0) item.UnitId = prod.UnitId;
                if (item.BaseQuantity <= 0) item.BaseQuantity = item.Quantity;
            }
        }

        // GoodsReceiptItems
        var grItemsToFix = await db.GoodsReceiptItems
            .Where(i => i.UnitId <= 0 || i.BaseQuantity <= 0)
            .ToListAsync();
        foreach (var item in grItemsToFix)
        {
            var prod = await db.Products.FindAsync(item.ProductId);
            if (prod != null)
            {
                if (item.UnitId <= 0) item.UnitId = prod.UnitId;
                if (item.BaseQuantity <= 0) item.BaseQuantity = item.Quantity;
            }
        }

        // ProductionReceiptItems
        var prItemsToFix = await db.ProductionReceiptItems
            .Where(i => i.UnitId <= 0 || i.BaseQuantity <= 0)
            .ToListAsync();
        foreach (var item in prItemsToFix)
        {
            var prod = await db.Products.FindAsync(item.ProductId);
            if (prod != null)
            {
                if (item.UnitId <= 0) item.UnitId = prod.UnitId;
                if (item.BaseQuantity <= 0) item.BaseQuantity = item.Quantity;
            }
        }

        // GoodsIssueItems
        var giItemsToFix = await db.GoodsIssueItems
            .Where(i => i.UnitId <= 0 || i.BaseQuantity <= 0)
            .ToListAsync();
        foreach (var item in giItemsToFix)
        {
            var prod = await db.Products.FindAsync(item.ProductId);
            if (prod != null)
            {
                if (item.UnitId <= 0) item.UnitId = prod.UnitId;
                if (item.BaseQuantity <= 0) item.BaseQuantity = item.Quantity;
            }
        }

        // InventoryTransactions
        var invTxToFix = await db.InventoryTransactions
            .Where(i => i.UnitId <= 0 || i.BaseQuantity <= 0)
            .ToListAsync();
        foreach (var item in invTxToFix)
        {
            var prod = await db.Products.FindAsync(item.ProductId);
            if (prod != null)
            {
                if (item.UnitId <= 0) item.UnitId = prod.UnitId;
                if (item.BaseQuantity <= 0) item.BaseQuantity = item.Quantity;
            }
        }

        // InventoryHistories
        var invHistToFix = await db.InventoryHistories
            .Where(i => i.UnitId <= 0 || i.BaseQuantityChange <= 0)
            .ToListAsync();
        foreach (var item in invHistToFix)
        {
            var prod = await db.Products.FindAsync(item.ProductId);
            if (prod != null)
            {
                if (item.UnitId <= 0) item.UnitId = prod.UnitId;
                if (item.BaseQuantityChange <= 0) item.BaseQuantityChange = item.QuantityChange;
            }
        }

        await db.SaveChangesAsync();
        Console.WriteLine($"→ Programmatically healed legacy UnitId & BaseQuantity records");

        Console.WriteLine();
        Console.WriteLine("══════════════════════════════════════════════════════════════");
        Console.WriteLine("✅  SEED HOÀN TẤT – Kho Nhựa Kỹ Thuật v2.0");
        Console.WriteLine($"    16 kho  |  256 locations  |  50 sản phẩm");
        Console.WriteLine("    RawMaterial×4 | FinishedGoods×4 | Auxiliary×4 | Chemical×4");
        Console.WriteLine("══════════════════════════════════════════════════════════════");
    }

    // ── Helper tạo Location ──
    private static Location Loc(Guid warehouseId, string code, string desc,
                                 LocationType type, bool isActive) => new()
                                 {
                                     Id = Guid.NewGuid(),
                                     WarehouseId = warehouseId,
                                     Code = code,
                                     Description = desc,
                                     Type = type,
                                     IsActive = isActive,
                                     CreatedAt = DateTime.UtcNow,
                                 };
}