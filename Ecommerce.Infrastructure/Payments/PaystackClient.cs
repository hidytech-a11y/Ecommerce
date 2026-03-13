using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Ecommerce.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Ecommerce.Infrastructure.Payments;

public class PaystackClient : IPaystackClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public PaystackClient(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task<PaystackInitializeResponse> InitializeTransactionAsync(
        decimal amount,
        string reference)
    {
        var secret = _config["Paystack:SecretKey"];

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", secret);

        var payload = new
        {
            email = "customer@email.com",
            amount = (int)(amount * 100),
            reference = reference
        };

        var content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync(
            "https://api.paystack.co/transaction/initialize",
            content);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);

        var url = doc.RootElement
            .GetProperty("data")
            .GetProperty("authorization_url")
            .GetString();

        return new PaystackInitializeResponse(url!);
    }

    public async Task<PaystackVerifyResponse> VerifyTransactionAsync(string reference)
    {
        var secret = _config["Paystack:SecretKey"];

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", secret);

        var response = await _httpClient.GetAsync(
            $"https://api.paystack.co/transaction/verify/{reference}");

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);

        var data = doc.RootElement.GetProperty("data");

        var status = data.GetProperty("status").GetString();
        var amount = data.GetProperty("amount").GetInt32();

        return new PaystackVerifyResponse(
            status == "success",
            amount / 100m
        );
    }
}