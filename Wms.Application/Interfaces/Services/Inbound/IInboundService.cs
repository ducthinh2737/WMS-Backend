using Wms.Application.DTOS.Inbound;
using Wms.Domain.Entity.Inbound;

namespace Wms.Application.Interfaces.Services.Inbound;

public interface IInboundService
{
    // INBOUND ORDER
    Task<InboundOrderDto> CreateInboundOrderAsync(InboundOrderDto dto);
    Task<InboundOrderDto> ApproveInboundOrderAsync(Guid orderId);
    Task<InboundOrderDto> RejectInboundOrderAsync(Guid orderId);
    Task<List<InboundOrderDto>> GetInboundOrdersAsync(int page = 1, int pageSize = 20, string? status = null);
    Task<InboundOrderDto> GetInboundOrderAsync(Guid orderId);
    Task<ScanReceiveResultDto> ScanInboundOrderInfoAsync(string orderCode);
    Task UpdateGRStatusAsync(Guid grId, InboundStatus status);
    Task<ScanReceiveResultDto> ConfirmAndReceiveAsync(string orderCode);
    Task<ScanReceiveResultDto> ScanAndProcessAsync(ScanQRPayloadDto payload);


    // GOODS RECEIPT
    Task<GoodsReceiptDto> CreateGRAsync(GoodsReceiptDto dto);
    Task<GoodsReceiptDto> ApproveProductionReceipt(GoodsReceiptDto dto);
    Task<GoodsReceiptDto> CountingReceiptProduction(GoodsReceiptDto dto);
    Task IncomingStockCount(GoodsReceiptItem1Dto dto);
    Task<List<GoodsReceiptDto>> GetGRsAsync(Guid? orderId = null, int page = 1, int pageSize = 20);
    Task CancelGRAsync(Guid grId);
    Task<List<GoodsReceipt>> getGRbytype(GRByTypeDto dto);
}

