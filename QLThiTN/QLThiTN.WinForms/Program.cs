using QLThiTN.WinForms.Forms.Auth;
using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        try
        {
            Database.LoadConfig(AppContext.BaseDirectory);
            Database.EnsureGoiColumns();
            Database.Execute(QLThiTN.Shared.WorkflowSchema.Sql);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Không thể khởi tạo cơ sở dữ liệu. Kiểm tra SQL Server và appsettings.json tại:\n"
                + AppContext.BaseDirectory + "\n\n" + ex.Message,
                "Cơ sở dữ liệu",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        if (args is ["--hash", var plain, ..])
        {
            Console.WriteLine(PasswordHelper.Hash(plain));
            return;
        }

        bool loggedIn = false;
        // Che do kiem tra nhanh: --autologin dang nhap bang tai khoan admin mac dinh
        if (args.Contains("--autologin"))
        {
            new AuthService().Login("admin", "admin123");
            loggedIn = true;
        }

        while (true)
        {
            if (!loggedIn)
            {
                using var login = new frmLogin();
                if (login.ShowDialog() != DialogResult.OK)
                {
                    break;
                }
            }
            loggedIn = false;

            using var main = new Forms.Common.frmMain();
            Application.Run(main);

            if (!main.IsLogout)
            {
                break;
            }
        }
    }
}
