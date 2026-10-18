using System.Security.Claims;

namespace QLThiTN.API.Common;

public static class CurrentUser
{
    public static int AccountId(this ClaimsPrincipal user) => Id(user, ClaimTypes.NameIdentifier);
    public static int StudentId(this ClaimsPrincipal user) => Id(user, "HocVienID");
    public static int TeacherId(this ClaimsPrincipal user) => Id(user, "GiaoVienID");
    private static int Id(ClaimsPrincipal user, string claim) =>
        int.TryParse(user.FindFirstValue(claim), out var id) ? id : 0;
}
