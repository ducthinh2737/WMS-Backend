ALTER DATABASE CHARACTER SET utf8mb4;


CREATE TABLE `Brands` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Code` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `Name` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `IsActive` tinyint(1) NOT NULL DEFAULT TRUE,
    `Description` longtext CHARACTER SET utf8mb4 NULL,
    `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT `PK_Brands` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `Categories` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Code` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `Name` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `IsActive` tinyint(1) NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_Categories` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `Customers` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Code` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `Name` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `Email` varchar(200) CHARACTER SET utf8mb4 NULL,
    `Phone` varchar(20) CHARACTER SET utf8mb4 NULL,
    `Address` varchar(500) CHARACTER SET utf8mb4 NULL,
    `IsActive` tinyint(1) NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_Customers` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `Lots` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `Code` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `productId` int NOT NULL,
    `ExpiryDate` datetime(6) NULL,
    `ManufacturingDate` datetime(6) NULL,
    `CreatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_Lots` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `Permissions` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Code` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `Description` longtext CHARACTER SET utf8mb4 NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `CreatedBy` int NULL,
    `UpdatedAt` datetime(6) NULL,
    `UpdatedBy` int NULL,
    `IsDeleted` tinyint(1) NOT NULL,
    CONSTRAINT `PK_Permissions` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `Roles` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `RoleName` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `CreatedBy` int NULL,
    `UpdatedAt` datetime(6) NULL,
    `UpdatedBy` int NULL,
    `IsDeleted` tinyint(1) NOT NULL,
    CONSTRAINT `PK_Roles` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `Suppliers` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Code` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `Name` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `Email` varchar(200) CHARACTER SET utf8mb4 NULL,
    `Phone` varchar(20) CHARACTER SET utf8mb4 NULL,
    `Address` varchar(500) CHARACTER SET utf8mb4 NULL,
    `IsActive` tinyint(1) NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_Suppliers` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `Units` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Code` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `Name` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `IsActive` tinyint(1) NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_Units` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `Users` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `FullName` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `Email` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `PasswordHash` longtext CHARACTER SET utf8mb4 NOT NULL,
    `IsActive` tinyint(1) NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `CreatedBy` int NULL,
    `UpdatedAt` datetime(6) NULL,
    `UpdatedBy` int NULL,
    `IsDeleted` tinyint(1) NOT NULL,
    CONSTRAINT `PK_Users` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `Warehouses` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `Code` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `Name` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `Address` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
    `Status` int NOT NULL,
    `WarehouseType` int NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NULL,
    CONSTRAINT `PK_Warehouses` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `OutboundOrders` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `Code` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `CustomerId` int NOT NULL,
    `CreatedBy` int NOT NULL,
    `Status` int NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NULL,
    `ApproveBy` int NULL,
    `ApprovedAt` datetime(6) NULL,
    CONSTRAINT `PK_OutboundOrders` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_OutboundOrders_Customers_CustomerId` FOREIGN KEY (`CustomerId`) REFERENCES `Customers` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;


CREATE TABLE `RolePermissions` (
    `RoleId` int NOT NULL,
    `PermissionId` int NOT NULL,
    CONSTRAINT `PK_RolePermissions` PRIMARY KEY (`RoleId`, `PermissionId`),
    CONSTRAINT `FK_RolePermissions_Permissions_PermissionId` FOREIGN KEY (`PermissionId`) REFERENCES `Permissions` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_RolePermissions_Roles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `Roles` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;


CREATE TABLE `InboundOrders` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `Code` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `SupplierId` int NOT NULL,
    `Status` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `CreateBy` int NULL,
    `UpdatedAt` datetime(6) NOT NULL,
    `ApprovedAt` datetime(6) NULL,
    `ApprovedBy` int NULL,
    CONSTRAINT `PK_InboundOrders` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_InboundOrders_Suppliers_SupplierId` FOREIGN KEY (`SupplierId`) REFERENCES `Suppliers` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;


CREATE TABLE `Products` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Code` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `Name` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `Description` longtext CHARACTER SET utf8mb4 NULL,
    `IsActive` tinyint(1) NOT NULL,
    `Type` int NOT NULL,
    `CategoryId` int NOT NULL,
    `UnitId` int NOT NULL,
    `BrandId` int NOT NULL,
    `SupplierId` int NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_Products` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Products_Brands_BrandId` FOREIGN KEY (`BrandId`) REFERENCES `Brands` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_Products_Categories_CategoryId` FOREIGN KEY (`CategoryId`) REFERENCES `Categories` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_Products_Suppliers_SupplierId` FOREIGN KEY (`SupplierId`) REFERENCES `Suppliers` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_Products_Units_UnitId` FOREIGN KEY (`UnitId`) REFERENCES `Units` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;


CREATE TABLE `UserPermissions` (
    `UserId` int NOT NULL,
    `PermissionId` int NOT NULL,
    CONSTRAINT `PK_UserPermissions` PRIMARY KEY (`UserId`, `PermissionId`),
    CONSTRAINT `FK_UserPermissions_Permissions_PermissionId` FOREIGN KEY (`PermissionId`) REFERENCES `Permissions` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_UserPermissions_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;


CREATE TABLE `UserRoles` (
    `UserId` int NOT NULL,
    `RoleId` int NOT NULL,
    CONSTRAINT `PK_UserRoles` PRIMARY KEY (`UserId`, `RoleId`),
    CONSTRAINT `FK_UserRoles_Roles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `Roles` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_UserRoles_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;


CREATE TABLE `Locations` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `WarehouseId` char(36) COLLATE ascii_general_ci NOT NULL,
    `Code` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `Description` varchar(250) CHARACTER SET utf8mb4 NOT NULL,
    `Type` int NOT NULL,
    `IsActive` tinyint(1) NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NULL,
    CONSTRAINT `PK_Locations` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Locations_Warehouses_WarehouseId` FOREIGN KEY (`WarehouseId`) REFERENCES `Warehouses` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;


CREATE TABLE `StockTakes` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `Code` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `WarehouseId` char(36) COLLATE ascii_general_ci NOT NULL,
    `Status` int NOT NULL,
    `Description` varchar(500) CHARACTER SET utf8mb4 NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `CreatedBy` int NULL,
    `CompletedAt` datetime(6) NULL,
    `CompletedBy` int NULL,
    CONSTRAINT `PK_StockTakes` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_StockTakes_Warehouses_WarehouseId` FOREIGN KEY (`WarehouseId`) REFERENCES `Warehouses` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;


CREATE TABLE `transfer_orders` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `Code` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `FromWarehouseId` char(36) COLLATE ascii_general_ci NOT NULL,
    `ToWarehouseId` char(36) COLLATE ascii_general_ci NOT NULL,
    `Status` int NOT NULL,
    `Note` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
    `ApprovedBy` int NULL,
    `ApprovedAt` datetime NULL,
    `CreatedBy` int NULL,
    `CreatedAt` datetime NOT NULL,
    `UpdatedBy` int NULL,
    `UpdatedAt` datetime(6) NULL,
    CONSTRAINT `PK_transfer_orders` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_transfer_orders_Warehouses_FromWarehouseId` FOREIGN KEY (`FromWarehouseId`) REFERENCES `Warehouses` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_transfer_orders_Warehouses_ToWarehouseId` FOREIGN KEY (`ToWarehouseId`) REFERENCES `Warehouses` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;


CREATE TABLE `GoodsIssues` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `OutboundOrderId` char(36) COLLATE ascii_general_ci NULL,
    `Type` int NOT NULL,
    `Code` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `WarehouseId` char(36) COLLATE ascii_general_ci NOT NULL,
    `Status` int NOT NULL,
    `IssuedAt` datetime(6) NOT NULL,
    `CreateAt` datetime(6) NOT NULL,
    `UpdateAt` datetime(6) NULL,
    CONSTRAINT `PK_GoodsIssues` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_GoodsIssues_OutboundOrders_OutboundOrderId` FOREIGN KEY (`OutboundOrderId`) REFERENCES `OutboundOrders` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_GoodsIssues_Warehouses_WarehouseId` FOREIGN KEY (`WarehouseId`) REFERENCES `Warehouses` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;


CREATE TABLE `GoodsReceipts` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `Code` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `InboundOrderId` char(36) COLLATE ascii_general_ci NULL,
    `WarehouseId` char(36) COLLATE ascii_general_ci NOT NULL,
    `ReceiptType` int NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NOT NULL,
    `ReceivedAt` datetime(6) NOT NULL,
    `Status` int NOT NULL,
    CONSTRAINT `PK_GoodsReceipts` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_GoodsReceipts_InboundOrders_InboundOrderId` FOREIGN KEY (`InboundOrderId`) REFERENCES `InboundOrders` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;


CREATE TABLE `InboundOrderItems` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `InboundOrderId` char(36) COLLATE ascii_general_ci NOT NULL,
    `ProductId` int NOT NULL,
    `Quantity` int NOT NULL,
    `Received_qty` int NOT NULL,
    `Status` int NOT NULL,
    `Price` decimal(18,2) NOT NULL,
    `WarehouseId` char(36) COLLATE ascii_general_ci NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_InboundOrderItems` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_InboundOrderItems_InboundOrders_InboundOrderId` FOREIGN KEY (`InboundOrderId`) REFERENCES `InboundOrders` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_InboundOrderItems_Warehouses_WarehouseId` FOREIGN KEY (`WarehouseId`) REFERENCES `Warehouses` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;


CREATE TABLE `OutboundOrderItems` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `OutboundOrderId` char(36) COLLATE ascii_general_ci NOT NULL,
    `ProductId` int NOT NULL,
    `Status` int NOT NULL,
    `Quantity` int NOT NULL,
    `Issued_Qty` int NOT NULL,
    `WarehouseId` char(36) COLLATE ascii_general_ci NOT NULL,
    `Price` decimal(65,30) NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NULL,
    CONSTRAINT `PK_OutboundOrderItems` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_OutboundOrderItems_OutboundOrders_OutboundOrderId` FOREIGN KEY (`OutboundOrderId`) REFERENCES `OutboundOrders` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_OutboundOrderItems_Products_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `Products` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_OutboundOrderItems_Warehouses_WarehouseId` FOREIGN KEY (`WarehouseId`) REFERENCES `Warehouses` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;


CREATE TABLE `Inventories` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `WarehouseId` char(36) COLLATE ascii_general_ci NOT NULL,
    `LocationId` char(36) COLLATE ascii_general_ci NULL,
    `LotId` char(36) COLLATE ascii_general_ci NOT NULL,
    `ProductId` int NOT NULL,
    `OnHandQuantity` decimal(18,4) NOT NULL,
    `LockedQuantity` decimal(18,4) NOT NULL,
    `InTransitQuantity` decimal(18,4) NOT NULL DEFAULT 0.0,
    `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` datetime NULL,
    CONSTRAINT `PK_Inventories` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Inventories_Locations_LocationId` FOREIGN KEY (`LocationId`) REFERENCES `Locations` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Inventories_Lots_LotId` FOREIGN KEY (`LotId`) REFERENCES `Lots` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_Inventories_Products_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `Products` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_Inventories_Warehouses_WarehouseId` FOREIGN KEY (`WarehouseId`) REFERENCES `Warehouses` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;


CREATE TABLE `InventoryHistories` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `WarehouseId` char(36) COLLATE ascii_general_ci NOT NULL,
    `LocationId` char(36) COLLATE ascii_general_ci NULL,
    `ProductId` int NOT NULL,
    `QuantityChange` decimal(18,4) NOT NULL,
    `Note` varchar(200) CHARACTER SET utf8mb4 NULL,
    `ActionType` int NOT NULL,
    `ReferenceCode` varchar(50) CHARACTER SET utf8mb4 NULL,
    `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT `PK_InventoryHistories` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_InventoryHistories_Locations_LocationId` FOREIGN KEY (`LocationId`) REFERENCES `Locations` (`Id`),
    CONSTRAINT `FK_InventoryHistories_Products_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `Products` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_InventoryHistories_Warehouses_WarehouseId` FOREIGN KEY (`WarehouseId`) REFERENCES `Warehouses` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;


CREATE TABLE `StockTakeItems` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `StockTakeId` char(36) COLLATE ascii_general_ci NOT NULL,
    `LocationId` char(36) COLLATE ascii_general_ci NOT NULL,
    `ProductId` int NOT NULL,
    `LotId` char(36) COLLATE ascii_general_ci NULL,
    `SystemQty` decimal(18,2) NOT NULL,
    `CountedQty` decimal(18,2) NOT NULL,
    `Note` varchar(255) CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_StockTakeItems` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_StockTakeItems_Locations_LocationId` FOREIGN KEY (`LocationId`) REFERENCES `Locations` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_StockTakeItems_Products_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `Products` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_StockTakeItems_StockTakes_StockTakeId` FOREIGN KEY (`StockTakeId`) REFERENCES `StockTakes` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;


CREATE TABLE `transfer_order_items` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `TransferOrderId` char(36) COLLATE ascii_general_ci NOT NULL,
    `ProductId` int NOT NULL,
    `FromLocationId` char(36) COLLATE ascii_general_ci NOT NULL,
    `ToLocationId` char(36) COLLATE ascii_general_ci NOT NULL,
    `Quantity` decimal(18,2) NOT NULL,
    `Note` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_transfer_order_items` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_transfer_order_items_Locations_FromLocationId` FOREIGN KEY (`FromLocationId`) REFERENCES `Locations` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_transfer_order_items_Locations_ToLocationId` FOREIGN KEY (`ToLocationId`) REFERENCES `Locations` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_transfer_order_items_Products_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `Products` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_transfer_order_items_transfer_orders_TransferOrderId` FOREIGN KEY (`TransferOrderId`) REFERENCES `transfer_orders` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;


CREATE TABLE `GoodsReceiptItems` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `GoodsReceiptId` char(36) COLLATE ascii_general_ci NOT NULL,
    `ProductId` int NOT NULL,
    `Quantity` int NOT NULL,
    `InboundOrderItemId` char(36) COLLATE ascii_general_ci NOT NULL,
    `Received_Qty` int NOT NULL,
    `Status` int NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_GoodsReceiptItems` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_GoodsReceiptItems_GoodsReceipts_GoodsReceiptId` FOREIGN KEY (`GoodsReceiptId`) REFERENCES `GoodsReceipts` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;


CREATE TABLE `ProductionReceiptItems` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `GoodsReceiptId` char(36) COLLATE ascii_general_ci NOT NULL,
    `ProductId` int NOT NULL,
    `Quantity` int NOT NULL,
    `ExpiryDate` datetime(6) NULL,
    `ManufacturingDate` datetime(6) NULL,
    `Receipt_Qty` int NOT NULL,
    `Status` int NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_ProductionReceiptItems` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ProductionReceiptItems_GoodsReceipts_GoodsReceiptId` FOREIGN KEY (`GoodsReceiptId`) REFERENCES `GoodsReceipts` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;


CREATE TABLE `GoodsIssueItems` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `GoodsIssueId` char(36) COLLATE ascii_general_ci NOT NULL,
    `OutboundOrderItemId` char(36) COLLATE ascii_general_ci NULL,
    `ProductId` int NOT NULL,
    `Status` int NOT NULL,
    `LocationId` char(36) COLLATE ascii_general_ci NULL,
    `Quantity` int NOT NULL,
    `Issued_Qty` int NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NULL,
    CONSTRAINT `PK_GoodsIssueItems` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_GoodsIssueItems_GoodsIssues_GoodsIssueId` FOREIGN KEY (`GoodsIssueId`) REFERENCES `GoodsIssues` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_GoodsIssueItems_Locations_LocationId` FOREIGN KEY (`LocationId`) REFERENCES `Locations` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_GoodsIssueItems_OutboundOrderItems_OutboundOrderItemId` FOREIGN KEY (`OutboundOrderItemId`) REFERENCES `OutboundOrderItems` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_GoodsIssueItems_Products_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `Products` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;


CREATE TABLE `GoodsIssueAllocates` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `GoodsIssueItemId` char(36) COLLATE ascii_general_ci NOT NULL,
    `LocationId` char(36) COLLATE ascii_general_ci NULL,
    `LotId` char(36) COLLATE ascii_general_ci NOT NULL,
    `AllocatedQty` decimal(65,30) NOT NULL,
    `PickedQty` decimal(65,30) NOT NULL,
    `Status` int NOT NULL,
    CONSTRAINT `PK_GoodsIssueAllocates` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_GoodsIssueAllocates_GoodsIssueItems_GoodsIssueItemId` FOREIGN KEY (`GoodsIssueItemId`) REFERENCES `GoodsIssueItems` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_GoodsIssueAllocates_Locations_LocationId` FOREIGN KEY (`LocationId`) REFERENCES `Locations` (`Id`)
) CHARACTER SET=utf8mb4;


CREATE UNIQUE INDEX `IX_Brands_Code` ON `Brands` (`Code`);


CREATE UNIQUE INDEX `IX_Brands_Name` ON `Brands` (`Name`);


CREATE UNIQUE INDEX `IX_Categories_Code` ON `Categories` (`Code`);


CREATE UNIQUE INDEX `IX_Categories_Name` ON `Categories` (`Name`);


CREATE UNIQUE INDEX `IX_Customers_Code` ON `Customers` (`Code`);


CREATE UNIQUE INDEX `IX_Customers_Name` ON `Customers` (`Name`);


CREATE INDEX `IX_GoodsIssueAllocates_GoodsIssueItemId` ON `GoodsIssueAllocates` (`GoodsIssueItemId`);


CREATE INDEX `IX_GoodsIssueAllocates_LocationId` ON `GoodsIssueAllocates` (`LocationId`);


CREATE INDEX `IX_GoodsIssueItems_GoodsIssueId` ON `GoodsIssueItems` (`GoodsIssueId`);


CREATE INDEX `IX_GoodsIssueItems_LocationId` ON `GoodsIssueItems` (`LocationId`);


CREATE INDEX `IX_GoodsIssueItems_OutboundOrderItemId` ON `GoodsIssueItems` (`OutboundOrderItemId`);


CREATE INDEX `IX_GoodsIssueItems_ProductId` ON `GoodsIssueItems` (`ProductId`);


CREATE UNIQUE INDEX `IX_GoodsIssues_Code` ON `GoodsIssues` (`Code`);


CREATE INDEX `IX_GoodsIssues_OutboundOrderId` ON `GoodsIssues` (`OutboundOrderId`);


CREATE INDEX `IX_GoodsIssues_WarehouseId` ON `GoodsIssues` (`WarehouseId`);


CREATE INDEX `IX_GoodsReceiptItems_GoodsReceiptId` ON `GoodsReceiptItems` (`GoodsReceiptId`);


CREATE INDEX `IX_GoodsReceipts_InboundOrderId` ON `GoodsReceipts` (`InboundOrderId`);


CREATE INDEX `IX_InboundOrderItems_InboundOrderId` ON `InboundOrderItems` (`InboundOrderId`);


CREATE INDEX `IX_InboundOrderItems_WarehouseId` ON `InboundOrderItems` (`WarehouseId`);


CREATE INDEX `IX_InboundOrders_SupplierId` ON `InboundOrders` (`SupplierId`);


CREATE INDEX `IX_Inventories_LocationId` ON `Inventories` (`LocationId`);


CREATE INDEX `IX_Inventories_LotId` ON `Inventories` (`LotId`);


CREATE INDEX `IX_Inventories_ProductId` ON `Inventories` (`ProductId`);


CREATE INDEX `IX_Inventories_WarehouseId` ON `Inventories` (`WarehouseId`);


CREATE UNIQUE INDEX `IX_Inventories_WarehouseId_LocationId_ProductId_LotId` ON `Inventories` (`WarehouseId`, `LocationId`, `ProductId`, `LotId`);


CREATE INDEX `IX_InventoryHistories_LocationId` ON `InventoryHistories` (`LocationId`);


CREATE INDEX `IX_InventoryHistories_ProductId` ON `InventoryHistories` (`ProductId`);


CREATE INDEX `IX_InventoryHistories_WarehouseId` ON `InventoryHistories` (`WarehouseId`);


CREATE UNIQUE INDEX `UX_Location_Warehouse_Code` ON `Locations` (`WarehouseId`, `Code`);


CREATE INDEX `IX_Lot_Product_ExpiryDate` ON `Lots` (`productId`, `ExpiryDate`);


CREATE UNIQUE INDEX `UX_Lot_Product_LotCode` ON `Lots` (`productId`, `Code`);


CREATE INDEX `IX_OutboundOrderItems_OutboundOrderId` ON `OutboundOrderItems` (`OutboundOrderId`);


CREATE INDEX `IX_OutboundOrderItems_ProductId` ON `OutboundOrderItems` (`ProductId`);


CREATE INDEX `IX_OutboundOrderItems_WarehouseId` ON `OutboundOrderItems` (`WarehouseId`);


CREATE UNIQUE INDEX `IX_OutboundOrders_Code` ON `OutboundOrders` (`Code`);


CREATE INDEX `IX_OutboundOrders_CustomerId` ON `OutboundOrders` (`CustomerId`);


CREATE UNIQUE INDEX `IX_Permissions_Code` ON `Permissions` (`Code`);


CREATE INDEX `IX_ProductionReceiptItems_GoodsReceiptId` ON `ProductionReceiptItems` (`GoodsReceiptId`);


CREATE INDEX `IX_Products_BrandId` ON `Products` (`BrandId`);


CREATE INDEX `IX_Products_CategoryId` ON `Products` (`CategoryId`);


CREATE UNIQUE INDEX `IX_Products_Code` ON `Products` (`Code`);


CREATE INDEX `IX_Products_SupplierId` ON `Products` (`SupplierId`);


CREATE INDEX `IX_Products_UnitId` ON `Products` (`UnitId`);


CREATE INDEX `IX_RolePermissions_PermissionId` ON `RolePermissions` (`PermissionId`);


CREATE INDEX `IX_StockTakeItems_LocationId` ON `StockTakeItems` (`LocationId`);


CREATE INDEX `IX_StockTakeItems_ProductId` ON `StockTakeItems` (`ProductId`);


CREATE INDEX `IX_StockTakeItems_StockTakeId` ON `StockTakeItems` (`StockTakeId`);


CREATE INDEX `IX_StockTakes_WarehouseId` ON `StockTakes` (`WarehouseId`);


CREATE UNIQUE INDEX `IX_Suppliers_Code` ON `Suppliers` (`Code`);


CREATE UNIQUE INDEX `IX_Suppliers_Name` ON `Suppliers` (`Name`);


CREATE INDEX `idx_transfer_item_lookup` ON `transfer_order_items` (`TransferOrderId`, `ProductId`, `FromLocationId`, `ToLocationId`);


CREATE INDEX `IX_transfer_order_items_FromLocationId` ON `transfer_order_items` (`FromLocationId`);


CREATE INDEX `IX_transfer_order_items_ProductId` ON `transfer_order_items` (`ProductId`);


CREATE INDEX `IX_transfer_order_items_ToLocationId` ON `transfer_order_items` (`ToLocationId`);


CREATE UNIQUE INDEX `IX_transfer_orders_Code` ON `transfer_orders` (`Code`);


CREATE INDEX `IX_transfer_orders_CreatedAt` ON `transfer_orders` (`CreatedAt`);


CREATE INDEX `IX_transfer_orders_FromWarehouseId` ON `transfer_orders` (`FromWarehouseId`);


CREATE INDEX `IX_transfer_orders_Status` ON `transfer_orders` (`Status`);


CREATE INDEX `IX_transfer_orders_ToWarehouseId` ON `transfer_orders` (`ToWarehouseId`);


CREATE UNIQUE INDEX `IX_Units_Code` ON `Units` (`Code`);


CREATE UNIQUE INDEX `IX_Units_Name` ON `Units` (`Name`);


CREATE INDEX `IX_UserPermissions_PermissionId` ON `UserPermissions` (`PermissionId`);


CREATE INDEX `IX_UserRoles_RoleId` ON `UserRoles` (`RoleId`);


CREATE UNIQUE INDEX `IX_Users_Email` ON `Users` (`Email`);


CREATE UNIQUE INDEX `IX_Warehouses_Code` ON `Warehouses` (`Code`);


