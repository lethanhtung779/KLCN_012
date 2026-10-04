using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms.Forms.Auth;

/// <summary>Man hinh dang nhap cho Quan tri vien / Giao vien.</summary>
public class frmLogin : Form
{
    private readonly AuthService _auth = new();
    private readonly TextBox _txtUser = Ui.Input(280);
    private readonly TextBox _txtPass = Ui.Input(280);
    private readonly Label _lblError = Ui.Label("", Ui.Small, Ui.Danger);

    public frmLogin()
    {
        Text = "Đăng nhập — Hệ thống quản lý & thi trắc nghiệm THPT";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        Size = new Size(900, 560);
        BackColor = Color.White;

        BuildUi();
    }

    private void BuildUi()
    {
        // Panel trai: nhan dien thuong hieu — van ban ve truc tiep trong Paint
        // de khong co "hop mau" cua Label dua tren nen gradient.
        var side = new Panel { Dock = DockStyle.Left, Width = 340 };
        side.Paint += (s, e) =>
        {
            var rect = side.ClientRectangle;
            using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                rect, Ui.NavyDark, Color.FromArgb(45, 74, 143), 135f);
            e.Graphics.FillRectangle(brush, rect);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            TextRenderer.DrawText(e.Graphics, "🎓", new Font("Segoe UI Emoji", 30f),
                new Point(40, 78), Color.White);
            TextRenderer.DrawText(e.Graphics, "QUẢN LÝ & THI\nTRẮC NGHIỆM THPT", Ui.H1,
                new Point(40, 150), Color.White);
            TextRenderer.DrawText(e.Graphics, "Phân hệ dành cho\nQuản trị viên và Giáo viên", Ui.Body,
                new Point(40, 224), Color.FromArgb(165, 178, 208));

            TextRenderer.DrawText(e.Graphics,
                "✓  Quản lý tài khoản người dùng\n" +
                "✓  Biên soạn ngân hàng câu hỏi\n" +
                "✓  Tạo đề thi từ ngân hàng câu hỏi\n" +
                "✓  Tổ chức kỳ thi trực tuyến\n" +
                "✓  Thống kê – báo cáo kết quả",
                Ui.Body, new Point(40, 318), Color.FromArgb(200, 210, 230));
        };

        // Panel phai: form dang nhap
        var lblTitle = Ui.Label("Chào mừng trở lại!", Ui.H1, Ui.Navy);
        var lblSub = Ui.Label("Đăng nhập để quản lý hệ thống", Ui.Body, Ui.TextMuted);

        var lblUser = Ui.Label("Tên đăng nhập", Ui.H2, Ui.Navy);
        var lblPass = Ui.Label("Mật khẩu", Ui.H2, Ui.Navy);
        _txtPass.UseSystemPasswordChar = true;
        _txtUser.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) _txtPass.Focus(); };
        _txtPass.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) DangNhap(); };

        var btnLogin = Ui.ButtonAccent("Đăng nhập", 280, 44);
        Ui.GradientButton(btnLogin, Ui.Accent, Ui.Accent2);
        btnLogin.Click += (s, e) => DangNhap();

        var lblNote = Ui.Label("Thí sinh vui lòng đăng nhập trên website để làm bài thi.", Ui.Small, Ui.TextMuted);

        Controls.AddRange([side, lblTitle, lblSub, lblUser, _txtUser, lblPass, _txtPass, _lblError, btnLogin, lblNote]);

        // Bo cuc panel phai (form FixedDialog nen toa do tinh mot lan la du)
        var x = 380;
        lblTitle.Location = new Point(x, 90);
        lblSub.Location = new Point(x, 128);
        lblUser.Location = new Point(x, 180);
        _txtUser.Location = new Point(x, 208);
        lblPass.Location = new Point(x, 258);
        _txtPass.Location = new Point(x, 286);
        _lblError.Location = new Point(x, 320);
        btnLogin.Location = new Point(x, 350);
        lblNote.Location = new Point(x, 420);
    }

    private void DangNhap()
    {
        _lblError.Text = "";
        var (user, error) = _auth.Login(_txtUser.Text, _txtPass.Text);
        if (user == null)
        {
            _lblError.Text = "⚠ " + error;
            return;
        }

        Hide();
        var main = new Forms.Common.frmMain();
        main.FormClosed += (s, e) => Close();
        main.Show();
    }
}
