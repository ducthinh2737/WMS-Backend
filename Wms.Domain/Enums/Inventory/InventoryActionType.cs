namespace Wms.Domain.Enums.Inventory
{
    public enum InventoryActionType
    {
        Receive = 1,          // Nhập kho (PO)
        Issue = 2,            // Xuất kho (SO)
        TransferIn = 3,       // Nhập vị trí khi chuyển kho/transfer
        TransferOut = 4,      // Xuất vị trí khi chuyển kho/transfer
        AdjustIncrease = 5,   // Điều chỉnh + 
        AdjustDecrease = 6,   // Điều chỉnh -
        StockCount = 7,       // Kiểm kê chênh lệch
        Lock = 8,             // Khóa tồn
        Unlock = 9,
        StockTakeAdjustment = 10,
        Pick = 11,
        Stage = 12,
        ReleaseReservation = 13
    }
}
