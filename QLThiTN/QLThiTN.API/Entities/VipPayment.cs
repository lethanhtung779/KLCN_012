using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLThiTN.API.Entities;

[Table("VipPayment")]
public class VipPayment
{
    [Key, MaxLength(32)] public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public int HocVienID { get; set; }
    public long Amount { get; set; }
    [MaxLength(20)] public string Provider { get; set; } = "payOS";
    public long? OrderCode { get; set; }
    [MaxLength(100)] public string? PaymentLinkId { get; set; }
    [MaxLength(20)] public string PlanId { get; set; } = "30days";
    public DateTime? GrantedUntilUtc { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "Pending";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresUtc { get; set; }
    public DateTime? PaidUtc { get; set; }
    [MaxLength(100)] public string? TransactionNo { get; set; }
    [MaxLength(2000)] public string PaymentUrl { get; set; } = "";
}
