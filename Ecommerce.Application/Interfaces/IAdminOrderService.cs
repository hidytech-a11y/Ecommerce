using Ecommerce.Application.Common.Pagination;
using Ecommerce.Application.DTOs.Orders;

namespace Ecommerce.Application.Interfaces;

public interface IAdminOrderService
{
    Task<PagedResult<AdminOrderResponse>> GetAllOrdersAsync(OrderQueryParameters query);

    
    Task<AdminOrderResponse> GetOrderByIdAsync(Guid orderId);

   
    Task<AdminOrderResponse> UpdateOrderStatusAsync(Guid orderId, UpdateOrderStatusRequest request);


    Task<AdminOrderStatsResponse> GetStatsAsync();
}