using System.ComponentModel.DataAnnotations;

namespace QLThiTN.Web.ViewModels;

public class ProfileViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [Display(Name = "Số điện thoại")]
    public string? Phone { get; set; }

    [Display(Name = "Tên đăng nhập")]
    public string Username { get; set; } = string.Empty;
}