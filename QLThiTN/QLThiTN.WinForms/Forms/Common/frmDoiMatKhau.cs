using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms.Forms.Common;

/// <summary>Hop thoai doi mat khau cua nguoi dang nhap.</summary>
public class frmDoiMatKhau : Form
{
    private readonly AuthService _auth = new();
    private readonly TextBox _txtCu = Ui.Input(300);
    private readonly TextBox _txtMoi = Ui.Input(300);
    private readonly TextBox _txtXacNhan = Ui.Input(300);

    public frmDoiMatKhau()
    {
        Text = "Đổi mật khẩu";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(420, 250);
        BackColor = Color.White;

        var lblCu = Ui.Label("Mật khẩu hiện tại", Ui.H2, Ui.Navy);
        _txtCu.UseSystemPasswordChar = true;
        var lblMoi = Ui.Label("Mật khẩu mới (tối thiểu 6 ký tự)", Ui.H2, Ui.Navy);
        _txtMoi.UseSystemPasswordChar = true;
        var lblXacNhan = Ui.Label("Xác nhận mật khẩu mới", Ui.H2, Ui.Navy);
        _txtXacNhan.UseSystemPasswordChar = true;

        var btnLuu = Ui.ButtonAccent("Lưu mật khẩu", 150, 40);
        Ui.GradientButton(btnLuu, Ui.Accent, Ui.Accent2);
        btnLuu.Click += (s, e) => Luu();
        var btnHuy = Ui.ButtonOutline("Hủy", 100, 40);
        btnHuy.Click += (s, e) => Close();

        Controls.AddRange([lblCu, _txtCu, lblMoi, _txtMoi, lblXacNhan, _txtXacNhan, btnLuu, btnHuy]);

        lblCu.Location = new Point(30, 20);
        _txtCu.Location = new Point(30, 46);
        lblMoi.Location = new Point(30, 86);
        _txtMoi.Location = new Point(30, 112);
        lblXacNhan.Location = new Point(30, 152);
        _txtXacNhan.Location = new Point(30, 178);
        btnLuu.Location = new Point(30, 218);
        btnHuy.Location = new Point(190, 218);
        ClientSize = new Size(420, 275);
    }

    private void Luu()
    {
        var (ok, error) = _auth.DoiMatKhau(_txtCu.Text, _txtMoi.Text, _txtXacNhan.Text);
        if (!ok)
        {
            MessageBox.Show(this, error, "Không thể đổi mật khẩu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        MessageBox.Show(this, "Đổi mật khẩu thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        Close();
    }
}
