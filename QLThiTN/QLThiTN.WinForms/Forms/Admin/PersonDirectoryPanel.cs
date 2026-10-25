using System.Security.Cryptography;
using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms.Forms.Admin;

/// <summary>Danh sách hồ sơ theo vai trò, dùng chung cho tab học viên và giáo viên.</summary>
public sealed class PersonDirectoryPanel : UserControl
{
    private readonly string _role;
    private readonly PersonManagementService _people = new();
    private readonly AccountService _accounts = new();
    private readonly DataGridView _grid = new();
    private readonly TextBox _search = Ui.Input(260);
    private readonly ComboBox _status = Ui.Combo(135);
    private readonly Label _summary = Ui.Label("", Ui.Small, Ui.TextMuted);

    public PersonDirectoryPanel(string role)
    {
        Session.YeuCauQuanTriVien();
        if (role is not ("HocVien" or "GiaoVien")) throw new ArgumentException("Vai trò không hợp lệ.");
        _role = role;
        Dock = DockStyle.Fill;
        BackColor = Color.White;
        Font = Ui.Body;
        _search.PlaceholderText = role == "HocVien"
            ? "Mã TK / tên / email / điện thoại..." : "Mã TK / tên / môn / email...";
        _search.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; RefreshList(); } };
        _status.DropDownStyle = ComboBoxStyle.DropDownList;
        _status.Items.AddRange(["Tất cả", "Hoạt động", "Đã khóa"]);
        _status.SelectedIndex = 0;
        _status.SelectedIndexChanged += (_, _) => RefreshList();

        var filters = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 58,
            WrapContents = false, Padding = new Padding(12, 10, 0, 8), BackColor = Color.White };
        var actions = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 56,
            WrapContents = false, Padding = new Padding(12, 4, 0, 8), BackColor = Color.White };
        var find = Ui.ButtonPrimary("Tìm", 75);
        find.Click += (_, _) => RefreshList();
        var add = Ui.ButtonAccent(role == "HocVien" ? "＋ Thêm học viên" : "＋ Thêm giáo viên", 170);
        add.Click += (_, _) => Edit(0);
        var edit = Ui.ButtonPrimary("Sửa hồ sơ", 115);
        edit.Click += (_, _) => Edit(SelectedId());
        var toggle = Ui.ButtonOutline("Khóa / Mở", 115);
        toggle.Click += (_, _) => Toggle();
        var reset = Ui.ButtonOutline("Đặt lại mật khẩu", 155);
        reset.Click += (_, _) => ResetPassword();
        var delete = Ui.ButtonDanger("Xóa", 85);
        delete.Click += (_, _) => Delete();
        var refresh = Ui.ButtonOutline("Làm mới", 100);
        refresh.Click += (_, _) => { _search.Clear(); _status.SelectedIndex = 0; RefreshList(); };
        foreach (Control c in new Control[] { _search, _status, find })
        {
            c.Margin = new Padding(0, 3, 9, 0);
            filters.Controls.Add(c);
        }
        foreach (Control c in new Control[] { add, edit, toggle, reset, delete, refresh })
        {
            c.Margin = new Padding(0, 3, 9, 0);
            actions.Controls.Add(c);
        }

        Ui.StyleGrid(_grid);
        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) Edit(SelectedId()); };
        var footer = new Panel { Dock = DockStyle.Bottom, Height = 36, BackColor = Color.White };
        _summary.Location = new Point(16, 9);
        footer.Controls.Add(_summary);
        Controls.Add(_grid);
        Controls.Add(footer);
        Controls.Add(actions);
        Controls.Add(filters);
        RefreshList();
    }

    private int _loadVersion;

    public async void RefreshList()
    {
        var version = ++_loadVersion;
        var selected = SelectedId();
        var status = _status.SelectedIndex switch { 1 => "HoatDong", 2 => "Khoa", _ => "" };
        var search = _search.Text;
        _summary.Text = "Đang tải danh sách...";
        try
        {
        var data = await Task.Run(() => _people.LayDanhSach(_role, search, status));
        if (IsDisposed || Disposing || version != _loadVersion) return;
        _grid.DataSource = data;
        Ui.RenameColumns(_grid,
            ("MaTaiKhoan", "Mã tài khoản"),
            ("HoTen", "Họ và tên"), ("TenDangNhap", "Tên đăng nhập"),
            ("Email", "Email"), ("SoDienThoai", "Điện thoại"),
            ("NgaySinh", "Ngày sinh"), ("LoaiHocVienHienThi", "Đối tượng"),
            ("LoaiGoiHienThi", "Gói"), ("TrangThaiHienThi", "Trạng thái"),
            ("SoLop", "Số lớp"), ("MonPhuTrach", "Môn phụ trách"),
            ("SoCauHoi", "Câu hỏi"), ("NgayTao", "Ngày tạo"));
        Ui.HideColumns(_grid, "TaiKhoanID", "HocVienID", "GiaoVienID", "LoaiHocVien", "LoaiGoi", "TrangThai");
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        foreach (DataGridViewColumn col in _grid.Columns)
            col.Width = col.Name switch
            {
                "MaTaiKhoan" => 125, "HoTen" => 165, "TenDangNhap" => 135, "Email" => 220,
                "SoDienThoai" => 125, "NgaySinh" => 105, "LoaiHocVienHienThi" => 100,
                "LoaiGoiHienThi" => 120, "TrangThaiHienThi" => 110,
                "SoLop" or "SoCauHoi" => 76, "MonPhuTrach" => 210,
                "NgayTao" => 150, _ => 100
            };
        if (_grid.Columns.Contains("NgaySinh")) _grid.Columns["NgaySinh"]!.DefaultCellStyle.Format = "dd/MM/yyyy";
        if (_grid.Columns.Contains("NgayTao")) _grid.Columns["NgayTao"]!.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
        foreach (DataGridViewRow row in _grid.Rows)
            if (Convert.ToInt32(row.Cells["TaiKhoanID"].Value) == selected)
            { row.Selected = true; _grid.CurrentCell = row.Cells["HoTen"]; break; }
        _summary.Text = $"{_grid.Rows.Count} {(_role == "HocVien" ? "học viên" : "giáo viên")} trong danh sách";
        }
        catch (Exception ex)
        {
            if (!IsDisposed && !Disposing && version == _loadVersion)
                _summary.Text = "Không tải được danh sách: " + ex.Message;
        }
    }

    private int SelectedId() => _grid.CurrentRow?.Cells["TaiKhoanID"].Value is { } id ? Convert.ToInt32(id) : 0;
    private string SelectedName() => _grid.CurrentRow?.Cells["HoTen"].Value?.ToString() ?? "";

    private void Edit(int id)
    {
        using var dialog = new frmPersonEdit(_role, id);
        if (dialog.ShowDialog(FindForm()) == DialogResult.OK) RefreshList();
    }

    private void Toggle()
    {
        var id = SelectedId();
        if (id == 0) { MessageBox.Show("Hãy chọn một hồ sơ."); return; }
        var locked = _grid.CurrentRow!.Cells["TrangThai"].Value?.ToString() == "Khoa";
        if (MessageBox.Show($"{(locked ? "Mở" : "Khóa")} tài khoản của {SelectedName()}?", "Xác nhận",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        _accounts.DoiTrangThai(id, !locked);
        RefreshList();
    }

    private void ResetPassword()
    {
        var id = SelectedId();
        if (id == 0) { MessageBox.Show("Hãy chọn một hồ sơ."); return; }
        if (MessageBox.Show($"Tạo mật khẩu mới cho {SelectedName()}?\nMật khẩu cũ sẽ không dùng được nữa. Hệ thống không tự gửi email; bạn cần gửi riêng cho người dùng.", "Xác nhận",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        var password = "Tn@" + Convert.ToHexString(RandomNumberGenerator.GetBytes(6));
        _accounts.DatLaiMatKhau(id, password);
        using var result = new PasswordResetResult(DirectoryRules.AccountCode(id), SelectedName(), password);
        result.ShowDialog(FindForm());
    }

    private void Delete()
    {
        var id = SelectedId();
        if (id == 0) { MessageBox.Show("Hãy chọn một hồ sơ."); return; }
        if (MessageBox.Show($"Xóa hồ sơ và tài khoản của {SelectedName()}? Không thể hoàn tác.", "Xác nhận xóa",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        var (ok, error) = _accounts.Xoa(id);
        if (!ok) MessageBox.Show(error, "Không thể xóa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        else RefreshList();
    }
}
