using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using QLThiTN.API.Entities;

namespace QLThiTN.API.Services;

public class VipPaymentSettings
{
    public bool Enabled { get; set; }
    public string ClientId { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string ChecksumKey { get; set; } = "";
    public string ReturnUrl { get; set; } = "";
    public string CancelUrl { get; set; } = "";
    public bool Ready => Enabled && !string.IsNullOrWhiteSpace(ClientId)
        && !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(ChecksumKey)
        && ValidUrl(ReturnUrl) && ValidUrl(CancelUrl);
    private static bool ValidUrl(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == "https" || (uri.Scheme == "http" && uri.IsLoopback));
}

public static class VipPaymentGateway
{
    public sealed record Plan(string Id, string Name, long PriceVnd);
    public static readonly IReadOnlyList<Plan> Plans = Array.AsReadOnly(new[] {
        new Plan("30days", "VIP 30 ngày", 300000), new Plan("1year", "VIP 1 năm", 1500000)
    });
    public static bool HasVip(HocVien student) => student.LoaiGoi == GoiThi.DongPhi && (student.VipExpiresUtc is null || student.VipExpiresUtc > DateTime.UtcNow);
    public static bool CanBuy(HocVien? student) => student?.NguonTaiKhoan == GoiThi.VangLai && !HasVip(student);
    public static DateTime Expiry(string planId, DateTime startUtc) => planId switch {
        "30days" => startUtc.AddDays(30), "1year" => startUtc.AddYears(1),
        _ => throw new ArgumentException("Unknown VIP plan", nameof(planId))
    };

    public static string Canonical(System.Text.Json.JsonElement data)
    {
        var properties = data.EnumerateObject().ToList();
        if (properties.Select(x => x.Name).Distinct(StringComparer.Ordinal).Count() != properties.Count)
            throw new FormatException("Duplicate signature field");
        return string.Join("&", properties.OrderBy(x => x.Name, StringComparer.Ordinal).Select(x => x.Name + "=" + Scalar(x.Value)));
    }
    private static string Scalar(System.Text.Json.JsonElement value) => value.ValueKind switch
    {
        System.Text.Json.JsonValueKind.Null => "",
        System.Text.Json.JsonValueKind.String => value.GetString() is null or "null" or "undefined" ? "" : value.GetString()!,
        System.Text.Json.JsonValueKind.Number => value.GetRawText(),
        System.Text.Json.JsonValueKind.True => "true",
        System.Text.Json.JsonValueKind.False => "false",
        _ => throw new FormatException("Unsupported signature field")
    };
    public static string Sign(System.Text.Json.JsonElement data, string key) => Convert.ToHexString(
        HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(Canonical(data)))).ToLowerInvariant();
    public static bool Verify(System.Text.Json.JsonElement data, string? signature, string key)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(signature) || data.ValueKind != System.Text.Json.JsonValueKind.Object) return false;
        try { return CryptographicOperations.FixedTimeEquals(Convert.FromHexString(Sign(data, key)), Convert.FromHexString(signature)); }
        catch (FormatException) { return false; }
    }
    public const string SchemaSql = """
        IF OBJECT_ID(N'dbo.VipPayment', N'U') IS NULL
        BEGIN
            CREATE TABLE dbo.VipPayment (
                Id nvarchar(32) NOT NULL PRIMARY KEY,
                HocVienID int NOT NULL REFERENCES dbo.HocVien(HocVienID),
                Amount bigint NOT NULL CHECK (Amount > 0),
                PlanId nvarchar(20) NOT NULL, GrantedUntilUtc datetime2 NULL,
                Status nvarchar(20) NOT NULL CHECK (Status IN ('Pending','Paid','Failed')),
                CreatedUtc datetime2 NOT NULL, ExpiresUtc datetime2 NOT NULL, PaidUtc datetime2 NULL,
                TransactionNo nvarchar(100) NULL, PaymentUrl nvarchar(2000) NOT NULL);
            CREATE INDEX IX_VipPayment_Student ON dbo.VipPayment(HocVienID, CreatedUtc);
            CREATE UNIQUE INDEX IX_VipPayment_Transaction ON dbo.VipPayment(TransactionNo) WHERE TransactionNo IS NOT NULL;
        END;
        IF COL_LENGTH('VipPayment','PlanId') IS NULL
            ALTER TABLE dbo.VipPayment ADD PlanId nvarchar(20) NOT NULL CONSTRAINT DF_VipPayment_Plan DEFAULT N'legacy';
        IF COL_LENGTH('VipPayment','GrantedUntilUtc') IS NULL
            ALTER TABLE dbo.VipPayment ADD GrantedUntilUtc datetime2 NULL;
        IF COL_LENGTH('VipPayment','Provider') IS NULL
            ALTER TABLE dbo.VipPayment ADD Provider nvarchar(20) NOT NULL CONSTRAINT DF_VipPayment_Provider DEFAULT N'VNPay';
        IF COL_LENGTH('VipPayment','OrderCode') IS NULL
            ALTER TABLE dbo.VipPayment ADD OrderCode bigint NULL;
        IF COL_LENGTH('VipPayment','PaymentLinkId') IS NULL
            ALTER TABLE dbo.VipPayment ADD PaymentLinkId nvarchar(100) NULL;
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.VipPayment') AND name='IX_VipPayment_OrderCode')
            EXEC(N'CREATE UNIQUE INDEX IX_VipPayment_OrderCode ON dbo.VipPayment(OrderCode) WHERE OrderCode IS NOT NULL');
        """;
}
