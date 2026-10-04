using QLThiTN.WinForms.Forms.Auth;
using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Database.LoadConfig(AppContext.BaseDirectory);
        Application.Run(new frmLogin());
    }
}
