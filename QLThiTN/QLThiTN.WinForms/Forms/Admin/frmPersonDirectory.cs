using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms.Forms.Admin;

/// <summary>Màn hình quản lý hồ sơ chuyên biệt cho học viên hoặc giáo viên.</summary>
public sealed class frmPersonDirectory : Form
{
    public frmPersonDirectory(string role)
    {
        if (role is not ("HocVien" or "GiaoVien"))
            throw new ArgumentException("Vai trò không hợp lệ.", nameof(role));

        Text = role == "HocVien" ? "Quản lý học viên" : "Quản lý giáo viên";
        BackColor = Color.White;
        Font = Ui.Body;

        var tabs = new TabControl { Dock = DockStyle.Fill, Font = Ui.Body };
        var management = new TabPage(role == "HocVien" ? "Danh sách học viên" : "Danh sách giáo viên")
        {
            BackColor = Color.White
        };
        management.Controls.Add(new PersonDirectoryPanel(role));
        var personalInfo = new TabPage("Thông tin chi tiết") { BackColor = Color.White };
        PersonPersonalInfoPanel? personalPanel = null;
        tabs.TabPages.Add(management);
        tabs.TabPages.Add(personalInfo);
        tabs.SelectedIndexChanged += (_, _) =>
        {
            if (tabs.SelectedTab != personalInfo) return;
            if (personalPanel == null)
            {
                personalPanel = new PersonPersonalInfoPanel(role);
                personalInfo.Controls.Add(personalPanel);
            }
            else
            {
                personalPanel.RefreshList();
            }
        };
        Controls.Add(tabs);
    }
}
