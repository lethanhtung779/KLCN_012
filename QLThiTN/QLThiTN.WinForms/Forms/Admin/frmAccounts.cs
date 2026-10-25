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
    private int _loadVersion;

    public frmAccounts()
    {
        Session.YeuCauQuanTriVien();
        Text = "Quản lý tài khoản";
        Font = Ui.Body;

        // Thanh cong cu tren cung
        var lblTim = Ui.Label("Tìm kiếm:", Ui.Body, Ui.TextMuted);
        _txtTim.PlaceholderText = "Mã TK / tên / email / họ tên...";
        _txtTim.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) TaiLai(); };

        _cboVaiTro.Items.Add("Tất cả");
        _cboVaiTro.Items.AddRange(new[] { "GiaoVien", "HocVien", "QuanTriVien" }.Select(c => new DirectoryRules.RoleOption(c)).ToArray());
        _cboVaiTro.SelectedIndex = 0;
        _cboVaiTro.SelectedIndexChanged += (s, e) => TaiLai();

        var btnTim = Ui.ButtonPrimary("Tìm kiếm", 110);
        btnTim.Click += (s, e) => TaiLai();

        var btnLamMoi = Ui.ButtonOutline("Làm mới", 100);
        btnLamMoi.Click += (s, e) => { _txtTim.Text = ""; _cboVaiTro.SelectedIndex = 0; TaiLai(); };

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

        var btnGoi = Ui.ButtonAccent("★ Cấp quyền gói", 170);
        btnGoi.Click += (s, e) => CapQuyenGoi();

        // Grid
        Ui.StyleGrid(_grid);
        _grid.CellFormatting += (_, e) =>
        {
            if (e.ColumnIndex < 0) return;
            var column = _grid.Columns[e.ColumnIndex].Name;
            if (column == "VaiTro") { e.Value = DirectoryRules.Role(e.Value?.ToString()); e.FormattingApplied = true; }
            if (column == "TrangThai") { e.Value = DirectoryRules.Status(e.Value?.ToString()); e.FormattingApplied = true; }
        };
        _grid.SelectionChanged += (s, e) => _lblInfo.Text = CapNhatInfo();

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(16, 12, 0, 8),
            BackColor = Color.White,
            WrapContents = true
        };
        foreach (Control c in new Control[] { lblTim, _txtTim, _cboVaiTro, btnTim, btnLamMoi, btnThem, btnSua, btnKhoa, btnMatKhau, btnGoi, btnXoa })
        {
            c.Margin = new Padding(0, 3, 10, 0);
            toolbar.Controls.Add(c);
        }

        var panelInfo = new Panel { Dock = DockStyle.Bottom, Height = 34, BackColor = Color.White };
        _lblInfo.Location = new Point(16, 8);
        panelInfo.Controls.Add(_lblInfo);

        var tabs = new TabControl { Dock = DockStyle.Fill, Font = Ui.Body };
        var accountsTab = new TabPage("Tài khoản") { BackColor = Color.White };
        accountsTab.Controls.Add(_grid);
        accountsTab.Controls.Add(panelInfo);
        accountsTab.Controls.Add(toolbar);
        tabs.TabPages.Add(accountsTab);
        var studentsTab = new TabPage("Danh sách học viên") { BackColor = Color.White };
        tabs.TabPages.Add(studentsTab);
        var teachersTab = new TabPage("Danh sách giáo viên") { BackColor = Color.White };
        tabs.TabPages.Add(teachersTab);
        tabs.SelectedIndexChanged += (_, _) =>
        {
            var page = tabs.SelectedTab;
            if (page == studentsTab || page == teachersTab)
            {
                if (page.Controls.Count == 0)
                    page.Controls.Add(new PersonDirectoryPanel(page == studentsTab ? "HocVien" : "GiaoVien"));
                else
                    ((PersonDirectoryPanel)page.Controls[0]).RefreshList();
            }
        };
        Controls.Add(tabs);

        _grid.Dock = DockStyle.Fill;
        _grid.CellDoubleClick += (s, e) => Sua();

        Load += (_, _) => TaiLai();
    }

    private async void TaiLai()
    {
        var version = ++_loadVersion;
        var vaiTro = (_cboVaiTro.SelectedItem as DirectoryRules.RoleOption)?.Code ?? "";
        var search = _txtTim.Text;
        _lblInfo.Text = "Đang tải danh sách...";
        try
        {
        var data = await Task.Run(() => _service.LayDanhSach(search, vaiTro));
        if (IsDisposed || Disposing || version != _loadVersion) return;
        _grid.DataSource = data;
        Ui.RenameColumns(_grid,
            ("MaTaiKhoan", "Mã tài khoản"), ("TaiKhoanID", "ID"), ("TenDangNhap", "Tên đăng nhập"), ("Email", "Email"),
            ("VaiTro", "Vai trò"), ("TrangThai", "Trạng thái"), ("HoTen", "Họ và tên"),
            ("TrangThaiGoi", "Gói"), ("NgayTao", "Ngày tạo"));
        Ui.HideColumns(_grid, "TaiKhoanID", "LoaiGoi");
        _lblInfo.Text = CapNhatInfo();
        }
        catch (Exception ex)
        {
            if (!IsDisposed && !Disposing && version == _loadVersion)
                _lblInfo.Text = "Không tải được danh sách: " + ex.Message;
        }
    }

    private string CapNhatInfo()
    {
        if (_grid.Rows.Count == 0) return "Không có tài khoản nào khớp điều kiện.";
        var row = _grid.CurrentRow;
        if (row == null) return $"{_grid.Rows.Count} tài khoản";
        var goi = row.Cells["TrangThaiGoi"].Value;
        return $"{row.Cells["MaTaiKhoan"].Value} · {row.Cells["TenDangNhap"].Value} — {row.Cells["HoTen"].Value} [{DirectoryRules.Status(row.Cells["TrangThai"].Value?.ToString())}] — Gói: {goi}";
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

    private void CapQuyenGoi()
    {
        var id = IdDangChon();
        if (id == 0)
        {
            MessageBox.Show("Hãy chọn một tài khoản.", "Thông báo");
            return;
        }

        var row = _grid.CurrentRow!;
        if (row.Cells["VaiTro"].Value?.ToString() != "HocVien")
        {
            MessageBox.Show("Chỉ học viên mới được cấp gói miễn phí hoặc đóng phí.", "Thông báo");
            return;
        }

        var ten = row.Cells["TenDangNhap"].Value?.ToString() ?? "";
        var email = row.Cells["Email"].Value?.ToString() ?? "";
        var hoTen = row.Cells["HoTen"].Value?.ToString() ?? "";
        var hienTai = row.Cells["LoaiGoi"].Value?.ToString() ?? "MienPhi";
        using var dlg = new frmCapQuyenGoi(ten, hoTen, email, hienTai);
        if (dlg.ShowDialog(FindForm()) != DialogResult.OK)
            return;

        var (ok, message) = _service.CapQuyenGoi(id, dlg.LoaiGoiChon);
        MessageBox.Show(message, ok ? "Cấp quyền gói" : "Không cấp được",
            MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        if (ok) TaiLai();
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
        if (MessageBox.Show($"Tạo mật khẩu mới cho '{ten}'?\nMật khẩu cũ sẽ không dùng được nữa. Hệ thống không tự gửi email; bạn cần gửi mật khẩu mới riêng cho người dùng.",
                "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        var temporaryPassword = "Tn@" + Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(6));
        _service.DatLaiMatKhau(id, temporaryPassword);
        using var result = new PasswordResetResult(DirectoryRules.AccountCode(id), ten?.ToString() ?? "", temporaryPassword);
        result.ShowDialog(this);
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

/// <summary>Hai lua chon goi cho mot tai khoan hoc vien.</summary>
public class frmCapQuyenGoi : Form
{
    public string LoaiGoiChon { get; private set; } = "MienPhi";

    public frmCapQuyenGoi(string tenDangNhap, string hoTen, string email, string loaiHienTai)
    {
        Text = "Cấp quyền gói";
        Font = Ui.Body;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(460, 280);

        var tieuDe = Ui.Label($"{hoTen} ({tenDangNhap})", Ui.H2, Ui.Navy);
        tieuDe.Location = new Point(20, 16);
        tieuDe.AutoSize = true;

        var mail = Ui.Label("Email nhận thông báo: " + (string.IsNullOrWhiteSpace(email) ? "(chưa có)" : email), Ui.Small, Ui.TextMuted);
        mail.Location = new Point(20, 48);
        mail.AutoSize = true;

        var rbMien = new RadioButton
        {
            Text = "Miễn phí — chỉ làm được các đề thi miễn phí",
            Location = new Point(20, 90),
            AutoSize = true,
            Checked = loaiHienTai != "DongPhi"
        };
        var rbPhi = new RadioButton
        {
            Text = "Đóng phí (VIP) — được thi toàn bộ đề",
            Location = new Point(20, 124),
            AutoSize = true,
            Checked = loaiHienTai == "DongPhi"
        };
        var goiY = Ui.Label("Chọn đóng phí (VIP) chỉ khi học viên đã đóng phí — hệ thống gửi email riêng: «Tài khoản của bạn đã được nâng lên vip». Email đăng ký web là thư xác minh, không phải thư này.", Ui.Small, Ui.TextMuted);
        goiY.Location = new Point(20, 164);
        goiY.MaximumSize = new Size(420, 0);
        goiY.AutoSize = true;

        var btnLuu = Ui.ButtonAccent("Lưu", 110);
        btnLuu.Location = new Point(20, 220);
        btnLuu.Click += (_, _) =>
        {
            LoaiGoiChon = rbPhi.Checked ? "DongPhi" : "MienPhi";
            DialogResult = DialogResult.OK;
        };
        var btnHuy = Ui.ButtonOutline("Hủy", 100);
        btnHuy.Location = new Point(140, 220);
        btnHuy.DialogResult = DialogResult.Cancel;

        Controls.AddRange([tieuDe, mail, rbMien, rbPhi, goiY, btnLuu, btnHuy]);
        AcceptButton = btnLuu;
        CancelButton = btnHuy;
    }
}
