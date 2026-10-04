using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms.Forms.Admin;

/// <summary>Quan ly tai khoan nguoi dung: them/sua/xoa, khoa/mo, tim kiem, loc vai tro.</summary>
public class frmAccounts : Form
{
    private readonly AccountService _service = new();
    private readonly DataGridView _grid = new();
    private readonly TextBox _txtTim = Ui.Input(220);
    private readonly ComboBox _cboVaiTro = Ui.Combo(150);
    private readonly Label _lblInfo = Ui.Label("", Ui.Small, Ui.TextMuted);

    public frmAccounts()
    {
        Text = "Quản lý tài khoản";
        Font = Ui.Body;

        // Thanh cong cu tren cung
        var lblTim = Ui.Label("Tìm kiếm:", Ui.Body, Ui.TextMuted);
        _txtTim.PlaceholderText = "Tên đăng nhập / email / họ tên...";
        _txtTim.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) TaiLai(); };

        _cboVaiTro.Items.AddRange(["Tất cả", "GiaoVien", "HocVien", "QuanTriVien"]);
        _cboVaiTro.SelectedIndex = 0;
        _cboVaiTro.SelectedIndexChanged += (s, e) => TaiLai();

        var btnTim = Ui.ButtonPrimary("Tìm kiếm", 110);
        btnTim.Click += (s, e) => TaiLai();

        var btnThem = Ui.ButtonAccent("＋ Thêm tài khoản", 170);
        btnThem.Click += (s, e) => { using var f = new frmAccountEdit(); if (f.ShowDialog(FindForm()) == DialogResult.OK) TaiLai(); };

        var btnSua = Ui.ButtonPrimary("✎ Sửa", 100);
        btnSua.Click += (s, e) => Sua();

        var btnKhoa = Ui.ButtonOutline("🔒 Khóa / Mở", 140);
        btnKhoa.Click += (s, e) => KhoaMo();

        var btnMatKhau = Ui.ButtonOutline("🔑 Reset mật khẩu", 170);
        btnMatKhau.Click += (s, e) => ResetMatKhau();

        var btnXoa = Ui.ButtonDanger("🗑 Xóa", 100);
        btnXoa.Click += (s, e) => Xoa();

        // Grid
        Ui.StyleGrid(_grid);
        _grid.SelectionChanged += (s, e) => _lblInfo.Text = CapNhatInfo();

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 62,
            Padding = new Padding(16, 12, 0, 0),
            BackColor = Color.White,
            WrapContents = false
        };
        foreach (Control c in new Control[] { lblTim, _txtTim, _cboVaiTro, btnTim, btnThem, btnSua, btnKhoa, btnMatKhau, btnXoa })
        {
            c.Margin = new Padding(0, 3, 10, 0);
            toolbar.Controls.Add(c);
        }

        var panelInfo = new Panel { Dock = DockStyle.Bottom, Height = 34, BackColor = Color.White };
        _lblInfo.Location = new Point(16, 8);
        panelInfo.Controls.Add(_lblInfo);

        Controls.Add(_grid);
        Controls.Add(panelInfo);
        Controls.Add(toolbar);

        _grid.Dock = DockStyle.Fill;
        _grid.CellDoubleClick += (s, e) => Sua();

        TaiLai();
    }

    private void TaiLai()
    {
        var vaiTro = _cboVaiTro.SelectedIndex is > 0 ? _cboVaiTro.Text : "";
        _grid.DataSource = _service.LayDanhSach(_txtTim.Text, vaiTro);
        Ui.RenameColumns(_grid,
            ("TaiKhoanID", "ID"), ("TenDangNhap", "Tên đăng nhập"), ("Email", "Email"),
            ("VaiTro", "Vai trò"), ("TrangThai", "Trạng thái"), ("HoTen", "Họ và tên"), ("NgayTao", "Ngày tạo"));
        Ui.HideColumns(_grid, "TaiKhoanID");
        _lblInfo.Text = CapNhatInfo();
    }

    private string CapNhatInfo()
    {
        if (_grid.Rows.Count == 0) return "Không có tài khoản nào khớp điều kiện.";
        var row = _grid.CurrentRow;
        if (row == null) return $"{_grid.Rows.Count} tài khoản";
        return $"Đang chọn: {row.Cells["TenDangNhap"].Value} — {row.Cells["HoTen"].Value} [{row.Cells["TrangThai"].Value}]";
    }

    private int IdDangChon() =>
        _grid.CurrentRow != null && _grid.CurrentRow.Cells["TaiKhoanID"].Value != null
            ? Convert.ToInt32(_grid.CurrentRow.Cells["TaiKhoanID"].Value)
            : 0;

    private void Sua()
    {
        if (IdDangChon() == 0) { MessageBox.Show("Hãy chọn một tài khoản trong danh sách.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        var row = _grid.CurrentRow!;
        var vaiTro = row.Cells["VaiTro"].Value?.ToString() ?? "HocVien";
        using var f = new frmAccountEdit(
            IdDangChon(),
            row.Cells["TenDangNhap"].Value?.ToString() ?? "",
            row.Cells["Email"].Value?.ToString() ?? "",
            row.Cells["HoTen"].Value?.ToString() ?? "",
            vaiTro);
        if (f.ShowDialog(FindForm()) == DialogResult.OK) TaiLai();
    }

    private void KhoaMo()
    {
        var id = IdDangChon();
        if (id == 0) { MessageBox.Show("Hãy chọn một tài khoản.", "Thông báo"); return; }
        var trangThai = _grid.CurrentRow!.Cells["TrangThai"].Value?.ToString();
        var khoa = trangThai == "HoatDong";
        if (khoa && id == Session.Current?.TaiKhoanID)
        { MessageBox.Show("Không thể khóa chính tài khoản đang đăng nhập.", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

        var ten = _grid.CurrentRow.Cells["TenDangNhap"].Value;
        if (MessageBox.Show($"Bạn chắc chắn muốn {(khoa ? "KHÓA" : "MỞ")} tài khoản '{ten}'?",
                "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        _service.DoiTrangThai(id, khoa);
        TaiLai();
    }

    private void ResetMatKhau()
    {
        var id = IdDangChon();
        if (id == 0) { MessageBox.Show("Hãy chọn một tài khoản.", "Thông báo"); return; }
        var ten = _grid.CurrentRow!.Cells["TenDangNhap"].Value;
        if (MessageBox.Show($"Đặt lại mật khẩu cho '{ten}' thành 'Abc@123456'?",
                "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        _service.DatLaiMatKhau(id, "Abc@123456");
        MessageBox.Show("Đã đặt lại mật khẩu thành 'Abc@123456'. Học viên/giáo viên nên đổi lại sau khi đăng nhập.", "Hoàn tất");
    }

    private void Xoa()
    {
        var id = IdDangChon();
        if (id == 0) { MessageBox.Show("Hãy chọn một tài khoản.", "Thông báo"); return; }
        var ten = _grid.CurrentRow!.Cells["TenDangNhap"].Value;
        if (MessageBox.Show($"Bạn chắc chắn muốn XÓA tài khoản '{ten}'? Hành động này không thể hoàn tác.",
                "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        var (ok, error) = _service.Xoa(id);
        if (!ok) { MessageBox.Show(error, "Không thể xóa", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        TaiLai();
    }
}
