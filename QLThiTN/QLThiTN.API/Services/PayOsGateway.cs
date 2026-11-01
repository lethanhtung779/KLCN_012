using System.Text.Json;
using Microsoft.Extensions.Options;
using QLThiTN.API.Entities;

namespace QLThiTN.API.Services;

public class PayOsGateway(HttpClient http, IOptions<VipPaymentSettings> options)
{
    public sealed record Link(string Id, string Url);
    private readonly VipPaymentSettings settings = options.Value;
    public async Task<Link> CreateAsync(VipPayment order)
    {
        var fields = JsonSerializer.SerializeToElement(new {
            amount = order.Amount, cancelUrl = settings.CancelUrl, description = "VIP" + order.Id[..6],
            orderCode = order.OrderCode, returnUrl = settings.ReturnUrl
        });
        using var request = Request(HttpMethod.Post, "v2/payment-requests");
        request.Content = JsonContent.Create(new {
            orderCode = order.OrderCode, amount = order.Amount, description = "VIP" + order.Id[..6],
            cancelUrl = settings.CancelUrl, returnUrl = settings.ReturnUrl,
            expiredAt = new DateTimeOffset(DateTime.SpecifyKind(order.ExpiresUtc, DateTimeKind.Utc)).ToUnixTimeSeconds(),
            signature = VipPaymentGateway.Sign(fields, settings.ChecksumKey)
        });
        using var response = await http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = json.RootElement;
        if (Text(root, "code") != "00")
        {
            // A previous request may have reached payOS before its response was lost.
            // Recover the SAME persisted order code; never create a second payable order.
            using var lookup = Request(HttpMethod.Get, "v2/payment-requests/" + order.OrderCode);
            using var recovered = await http.SendAsync(lookup);
            recovered.EnsureSuccessStatusCode();
            using var recoveryJson = await JsonDocument.ParseAsync(await recovered.Content.ReadAsStreamAsync());
            var recovery = recoveryJson.RootElement;
            if (Text(recovery, "code") != "00") throw new HttpRequestException("payOS did not create the payment link.");
            var data = recovery.GetProperty("data");
            ValidateOrder(data, order);
            if (Text(data, "status") != "PENDING") throw new HttpRequestException("Payment link is no longer pending.");
            var id = Text(data, "id");
            if (!Guid.TryParseExact(id, "N", out _)) throw new HttpRequestException("Invalid payment link ID.");
            return new(id, "https://pay.payos.vn/web/" + id);
        }
        var result = root.GetProperty("data");
        if (!VipPaymentGateway.Verify(result, Text(root, "signature"), settings.ChecksumKey))
            throw new HttpRequestException("Invalid payOS response signature.");
        ValidateOrder(result, order);
        var linkId = Text(result, "paymentLinkId");
        var url = Text(result, "checkoutUrl");
        if (!Guid.TryParseExact(linkId, "N", out _) || !Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || uri.Scheme != "https" || uri.Host != "pay.payos.vn") throw new HttpRequestException("Invalid checkout URL.");
        return new(linkId, url);
    }
    private HttpRequestMessage Request(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("x-client-id", settings.ClientId);
        request.Headers.Add("x-api-key", settings.ApiKey);
        return request;
    }
    private static void ValidateOrder(JsonElement data, VipPayment order)
    {
        if (data.GetProperty("orderCode").GetInt64() != order.OrderCode || data.GetProperty("amount").GetInt64() != order.Amount)
            throw new HttpRequestException("Payment order does not match.");
    }
    public static string Text(JsonElement data, string name) => data.TryGetProperty(name, out var field) && field.ValueKind == JsonValueKind.String ? field.GetString() ?? "" : "";
}
