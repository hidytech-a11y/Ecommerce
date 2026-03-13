namespace Ecommerce.Application.Interfaces;

public interface IPaystackClient
{
    Task<PaystackInitializeResponse> InitializeTransactionAsync(
        decimal amount,
        string reference);

    Task<PaystackVerifyResponse> VerifyTransactionAsync(string reference);
}