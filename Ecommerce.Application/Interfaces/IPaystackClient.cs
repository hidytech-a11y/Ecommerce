namespace Ecommerce.Application.Interfaces;

public interface IPaystackClient
{
    Task<PaystackInitializeResponse> InitializeTransactionAsync(
        string email,
        decimal amount,
        string reference,
        string? callbackUrl = null);

    Task<PaystackVerifyResponse> VerifyTransactionAsync(string reference);
}