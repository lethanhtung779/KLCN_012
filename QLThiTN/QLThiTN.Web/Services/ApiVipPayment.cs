namespace QLThiTN.Web.Services;

public class ApiVipPayment
{
    public bool Ready { get; set; }
    public List<ApiVipPlan> Plans { get; set; } = [];
    public DateTime? VipExpiresUtc { get; set; }
    public bool IsVip { get; set; }
    public List<ApiVipOrder> Orders { get; set; } = [];
}
public class ApiVipOrder
{
    public string Id { get; set; } = "";
    public long Amount { get; set; }
    public string Status { get; set; } = "";
    public string PlanId { get; set; } = "";
    public DateTime? GrantedUntilUtc { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime ExpiresUtc { get; set; }
    public DateTime? PaidUtc { get; set; }
    public string Label => Status == "Paid" ? "Đã thanh toán" : Status == "Failed" ? "Không thành công" : ExpiresUtc <= DateTime.UtcNow ? "Hết hạn thanh toán" : "Chờ xác nhận";
}
public class ApiVipCheckout { public string PaymentUrl { get; set; } = ""; }
public class ApiVipPlan
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public long PriceVnd { get; set; }
}
