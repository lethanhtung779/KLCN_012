using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace QLThiTN.Web.Filters;

/// <summary>
/// Chan truy cap cac trang can dang nhap (làm bài, nộp bài, kết quả, hồ sơ).
/// Website chỉ dành cho thí sinh nên mọi hành động thi đều yêu cầu phiên đăng nhập.
/// </summary>
public class RequireLoginAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.HttpContext.Session.GetString("UserFullName") != null)
            return;

        var request = context.HttpContext.Request;
        var returnUrl = request.Path + request.QueryString;
        context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
    }
}
