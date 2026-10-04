using QLThiTN.WinForms.Forms.Auth;
using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Database.LoadConfig(AppContext.BaseDirectory);

        // Che do kiem tra nhanh: --autologin dang nhap bang tai khoan admin mac dinh
        if (args.Contains("--autologin"))
        {
            new AuthService().Login("admin", "admin123");
            Application.Run(new Forms.Common.frmMain());
            return;
        }

        Application.Run(new frmLogin());
    }
}
