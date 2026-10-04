using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms.Forms.Common;

/// <summary>Man hinh chinh: sidebar dieu huong theo vai tro + vung noi dung.</summary>
public class frmMain : Form
{
    private readonly Panel _content = new();
    private readonly StatisticsService _stats = new();
    private readonly Button _btnDoiMatKhau = new();
    private readonly Label _lblUserInfo = new();

    public frmMain()
    {
        Text = "Hệ thống quản lý & thi trắc nghiệm THPT — Phân hệ Quản trị & Giáo viên";
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1100, 680);
        BackColor = Ui.BgSoft;

        BuildSidebar();
        BuildContent();
        MoManHinh("Tổng quan", TaoTongQuan);
    }

    // ------------------------------------------------------------------ sidebar
    private void BuildSidebar()
    {
        var sidebar = new Panel { Dock = DockStyle.Left, Width = 232, BackColor = Ui.Navy };

        var lblBrand = new Label
        {
            Text = "🎓  QLTHI TN-THPT",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = false,
            Size = new Size(232, 40),
            Location = new Point(0, 18),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(20, 0, 0, 0)
        };

        var lblRole = new Label
        {
            Text = "PHÂN HỆ QUẢN TRỊ & GIÁO VIÊN",
            Font = Ui.Small,
            ForeColor = Color.FromArgb(120, 140, 180),
            AutoSize = false,
            Size = new Size(232, 22),
            Location = new Point(0, 56),
            Padding = new Padding(20, 0, 0, 0)
        };

        _lblUserInfo.Text = $"👤 {Session.HoTen}\n{Session.Current?.VaiTro switch { "QuanTriVien" => "Quản trị viên", "GiaoVien" => "Giáo viên", _ => "" }}";
        _lblUserInfo.Font = Ui.Body;
        _lblUserInfo.ForeColor = Color.White;
        _lblUserInfo.AutoSize = false;
        _lblUserInfo.Size = new Size(232, 46);
        _lblUserInfo.Padding = new Padding(20, 0, 0, 0);

        var flow = new FlowLayoutPanel
        {
            Location = new Point(0, 120),
            Size = new Size(232, 400),
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Ui.Navy
        };

        if (Session.IsQuanTriVien)
        {
            flow.Controls.Add(NavButton("👥  Quản lý tài khoản", () => MoManHinh("Quản lý tài khoản", () => new Forms.Admin.frmAccounts())));
            flow.Controls.Add(NavButton("🗄️  Backup & Restore", () => MoManHinh("Sao lưu & phục hồi dữ liệu", () => new Forms.Admin.frmBackupRestore())));
        }

        if (Session.IsGiaoVien)
        {
            flow.Controls.Add(NavButton("📚  Ngân hàng câu hỏi", () => MoManHinh("Ngân hàng câu hỏi", () => new Forms.Teacher.frmQuestions())));
            flow.Controls.Add(NavButton("📝  Đề thi & Kỳ thi", () => MoManHinh("Đề thi & Kỳ thi", () => new Forms.Teacher.frmExams())));
            flow.Controls.Add(NavButton("📊  Thống kê kết quả", () => MoManHinh("Thống kê kết quả", () => new Forms.Teacher.frmStatistics())));
        }

        flow.Controls.Add(NavButton("🔄  Tổng quan", () => MoManHinh("Tổng quan", TaoTongQuan)));

        _btnDoiMatKhau.Text = "🔑  Đổi mật khẩu";
        _btnDoiMatKhau.Font = Ui.Body;
        _btnDoiMatKhau.ForeColor = Color.White;
        _btnDoiMatKhau.BackColor = Ui.Navy;
        _btnDoiMatKhau.FlatStyle = FlatStyle.Flat;
        _btnDoiMatKhau.FlatAppearance.BorderSize = 0;
        _btnDoiMatKhau.Size = new Size(232, 44);
        _btnDoiMatKhau.TextAlign = ContentAlignment.MiddleLeft;
        _btnDoiMatKhau.Padding = new Padding(20, 0, 0, 0);
        _btnDoiMatKhau.Cursor = Cursors.Hand;
        _btnDoiMatKhau.Click += (s, e) =>
        {
            using var f = new frmDoiMatKhau();
            f.ShowDialog(this);
        };

        var btnLogout = new Button
        {
            Text = "⏻  Đăng xuất",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Ui.NavyDark,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(232, 46),
            Dock = DockStyle.Bottom,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(20, 0, 0, 0),
            Cursor = Cursors.Hand
        };
        btnLogout.FlatAppearance.BorderSize = 0;
        btnLogout.Click += (s, e) =>
        {
            Session.Clear();
            Close();
        };

        sidebar.Controls.AddRange([lblBrand, lblRole, _lblUserInfo, flow, _btnDoiMatKhau, btnLogout]);
        _btnDoiMatKhau.Location = new Point(0, ClientSize.Height - 90);

        Controls.Add(sidebar);
        sidebar.Paint += (s, e) =>
        {
            using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                sidebar.ClientRectangle, Ui.NavyDark, Ui.Navy, 90f);
            e.Graphics.FillRectangle(brush, sidebar.ClientRectangle);
        };
    }

    private Button NavButton(string text, Action onClick)
    {
        var b = new Button
        {
            Text = text,
            Font = new Font("Segoe UI", 10f),
            ForeColor = Color.FromArgb(200, 210, 230),
            BackColor = Ui.Navy,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(232, 46),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(20, 0, 0, 0),
            Cursor = Cursors.Hand
        };
        b.FlatAppearance.BorderSize = 0;
        b.FlatAppearance.MouseOverBackColor = Ui.NavyDark;
        b.Click += (s, e) => onClick();
        return b;
    }

    // ------------------------------------------------------------------ content
    private void BuildContent()
    {
        _content.Dock = DockStyle.Fill;
        _content.BackColor = Ui.BgSoft;
        Controls.Add(_content);
    }

    private Label? _lblTitle;

    /// <summary>Do mot form con (TopLevel=false) vao vung noi dung.</summary>
    private void MoManHinh(string tieuDe, Func<Form> tao)
    {
        _content.SuspendLayout();
        foreach (Control c in _content.Controls) c.Dispose();
        _content.Controls.Clear();

        var header = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Color.White };
        _lblTitle = new Label
        {
            Text = tieuDe,
            Font = Ui.H1,
            ForeColor = Ui.Navy,
            AutoSize = true,
            Location = new Point(24, 16)
        };
        header.Controls.Add(_lblTitle);

        var child = tao();
        child.TopLevel = false;
        child.FormBorderStyle = FormBorderStyle.None;
        child.Dock = DockStyle.Fill;
        child.BackColor = Ui.BgSoft;

        _content.Controls.Add(child);
        _content.Controls.Add(header);
        child.Show();
        _content.ResumeLayout();
    }

    /// <summary>Dashboard tong quan: 4 the so lieu that tu DB.</summary>
    private Form TaoTongQuan()
    {
        var f = new Form { Dock = DockStyle.Fill };
        var (cauHoi, deThi, hocVien, baiThi) = _stats.ThongKeTongQuan();

        (string Title, string Value, Color Color)[] cards =
        [
            ("Câu hỏi hoạt động", cauHoi.ToString("N0"), Ui.Accent),
            ("Đề thi", deThi.ToString("N0"), Ui.Accent2),
            ("Học viên", hocVien.ToString("N0"), Ui.Success),
            ("Bài thi đã nộp", baiThi.ToString("N0"), Ui.Warning)
        ];

        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Ui.BgSoft, AutoScroll = true };

        var x = 24;
        foreach (var (title, value, color) in cards)
        {
            var card = new Panel { Size = new Size(250, 130), Location = new Point(x, 24), BackColor = Color.White };
            var bar = new Panel { Dock = DockStyle.Top, Height = 6, BackColor = color };
            var lblValue = new Label { Text = value, Font = new Font("Segoe UI", 22f, FontStyle.Bold), ForeColor = Ui.Navy, AutoSize = true, Location = new Point(20, 28) };
            var lblTitle = new Label { Text = title, Font = Ui.Body, ForeColor = Ui.TextMuted, AutoSize = true, Location = new Point(20, 84) };
            card.Controls.AddRange([lblValue, lblTitle, bar]);
            panel.Controls.Add(card);
            x += 266;
        }

        var note = Ui.Label("Số liệu đồng bộ trực tiếp với cơ sở dữ liệu ThiTracNghiem dùng chung cho website.", Ui.Small, Ui.TextMuted);
        note.Location = new Point(28, 172);
        panel.Controls.Add(note);

        f.Controls.Add(panel);
        return f;
    }
}
