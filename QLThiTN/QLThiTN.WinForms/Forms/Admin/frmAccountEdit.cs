using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms.Forms.Admin;

/// <summary>Them moi / sua thong tin tai khoan.</summary>
public class frmAccountEdit : Form
{
    private readonly AccountService _service = new();
    private readonly int _id;                       // 0 = them moi
    private readonly string _tenDangNhap;
    private readonly string _vaiTro;
    private readonly TextBox _txtTen = Ui.Input(300);
    private readonly TextBox _txtEmail = Ui.Input(300);
    private readonly TextBox _txtHoTen = Ui.Input(300);
    private readonly TextBox _txtMatKhau = Ui.Input(300);
    private readonly ComboBox _cboVaiTro = Ui.Combo(300);
    private readonly Label _lblError = Ui.Label("", Ui.Small, Ui.Danger);

    public frmAccountEdit(int id = 0, string tenDangNhap = "", string email = "", string hoTen = "", string vaiTro = "HocVien")
    {
        _id = id;
        _tenDangNhap = tenDangNhap;
        _vaiTro = vaiTro;

        Text = id == 0 ? "Thêm tài khoản mới" : $"Sửa tài khoản: {tenDangNhap}";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false;
        ClientSize = new Size(440, id == 0 ? 400 : 310);
        BackColor = Color.White;

        var lblTen = Ui.Label("Tên đăng nhập *", Ui.H2, Ui.Navy);
        _txtTen.Text = tenDangNhap;
        _txtTen.Enabled = id == 0; // khong doi ten dang nhap

        var lblEmail = Ui.Label("Email *", Ui.H2, Ui.Navy);
        _txtEmail.Text = email;

        var lblHoTen = Ui.Label("Họ và tên *", Ui.H2, Ui.Navy);
        _txtHoTen.Text = hoTen;

        var lblVaiTro = Ui.Label("Vai trò", Ui.H2, Ui.Navy);
        _cboVaiTro.Items.AddRange(["HocVien", "GiaoVien", "QuanTriVien"]);
        _cboVaiTro.SelectedItem = vaiTro;
        _cboVaiTro.Enabled = id == 0;

        var lblMatKhau = Ui.Label(id == 0 ? "Mật khẩu * (tối thiểu 6 ký tự)" : "Đặt lại mật khẩu (để trống = giữ nguyên)", Ui.H2, Ui.Navy);
        _txtMatKhau.UseSystemPasswordChar = true;

        var btnLuu = Ui.ButtonAccent(id == 0 ? "Tạo tài khoản" : "Lưu thay đổi", 170, 40);
        Ui.GradientButton(btnLuu, Ui.Accent, Ui.Accent2);
        btnLuu.Click += (s, e) => Luu();
        var btnHuy = Ui.ButtonOutline("Hủy", 100, 40);
        btnHuy.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        Controls.AddRange([lblTen, _txtTen, lblEmail, _txtEmail, lblHoTen, _txtHoTen, lblVaiTro, _cboVaiTro, lblMatKhau, _txtMatKhau, _lblError, btnLuu, btnHuy]);

        var y = 20;
        lblTen.Location = new Point(30, y); _txtTen.Location = new Point(30, y + 26); y += 66;
        lblEmail.Location = new Point(30, y); _txtEmail.Location = new Point(30, y + 26); y += 66;
        lblHoTen.Location = new Point(30, y); _txtHoTen.Location = new Point(30, y + 26); y += 66;
        if (id == 0)
        {
            lblVaiTro.Location = new Point(30, y); _cboVaiTro.Location = new Point(30, y + 26); y += 66;
        }
        lblMatKhau.Location = new Point(30, y);
        _txtMatKhau.Location = new Point(30, y + 26);
        _lblError.Location = new Point(30, y + 56);
        btnLuu.Location = new Point(30, y + 78);
        btnHuy.Location = new Point(210, y + 78);
    }

    private void Luu()
    {
        _lblError.Text = "";
        try
        {
            if (_id == 0)
            {
                var (ok, error, _) = _service.Them(_txtTen.Text, _txtEmail.Text, _txtHoTen.Text, _cboVaiTro.Text, _txtMatKhau.Text);
                if (!ok) { _lblError.Text = "⚠ " + error; return; }
            }
            else
            {
                var (ok, error) = _service.Sua(_id, _txtEmail.Text, _txtHoTen.Text, _vaiTro);
                if (!ok) { _lblError.Text = "⚠ " + error; return; }
                if (!string.IsNullOrWhiteSpace(_txtMatKhau.Text))
                {
                    if (_txtMatKhau.Text.Length < 6) { _lblError.Text = "⚠ Mật khẩu tối thiểu 6 ký tự."; return; }
                    _service.DatLaiMatKhau(_id, _txtMatKhau.Text);
                }
            }
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _lblError.Text = "⚠ Lỗi: " + ex.Message;
        }
    }
}
