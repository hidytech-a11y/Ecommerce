namespace Ecommerce.Application.DTOs.Orders;

public class OrderQueryParameters
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public string? Status { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? CustomerEmail { get; set; }
    public string? PaymentReference { get; set; }
}