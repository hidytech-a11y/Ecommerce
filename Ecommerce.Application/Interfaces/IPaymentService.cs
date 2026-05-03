using Ecommerce.Application.DTOs.Payments;
using Microsoft.AspNetCore.Http;

namespace Ecommerce.Application.Interfaces;

public interface IPaymentService
{
    Task<InitializePaymentResponse> InitializePaymentAsync(
        Guid userId,
        InitializePaymentRequest request);

    Task VerifyPaymentAsync(string reference);
    Task HandleWebhookAsync(HttpRequest request);
}