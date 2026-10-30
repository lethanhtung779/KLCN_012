using System.Data;
using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms.Forms.Admin;

/// <summary>Hồ sơ lớp học: admin xem mọi lớp, giáo viên chỉ xem lớp được phân công.</summary>
public class frmChiTietLopHoc : Form
{
    private readonly TrainingService _service = new();
    private readonly int _lopBanDau;
    private int _lopDangXem;
    private bool _dangNapDanhSach;
    private int _listVersion;
    private int _detailVersion;
    private readonly TextBox _search = Ui.Input(280);
    private readonly DataGridView _classes = new();
    private readonly DataGridView _students = new();
    private readonly DataGridView _teachers = new();
    private readonly DataGridView _schedule = new();
    private readonly Label _title = Ui.Label("Chọn một lớp học", Ui.H1, Ui.Navy);
    private readonly Label _meta = Ui.Label("", Ui.Body, Ui.TextMuted);
    private readonly Label _summary = Ui.Label("", Ui.Body, Ui.Navy);
    private readonly Label _classHint = Ui.Label("", Ui.Small, Ui.TextMuted);
    private readonly Label _studentHint = Ui.Label("", Ui.Small, Ui.TextMuted);
    private readonly Label _teacherHint = Ui.Label("", Ui.Small, Ui.TextMuted);
    private readonly Label _scheduleHint = Ui.Label("", Ui.Small, Ui.TextMuted);
    private readonly FlowLayoutPanel _actions = new() { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(8, 4, 0, 0) };
    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill, Font = Ui.Body };
    private TabPage _tabHocVien = null!;
    private TabPage _tabGiaoVien = null!;
    private TabPage _tabLichHoc = null!;

    public frmChiTietLopHoc(int lopHocId = 0)
    {
        if (!Session.IsQuanTriVien && !Session.IsGiaoVien)
            throw new UnauthorizedAccessException("Bạn không có quyền xem lớp học.");

        _lopBanDau = lopHocId;
        Text = Session.IsQuanTriVien ? "Chi tiết lớp học" : "Lớp phụ trách";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(900, 620);
        ClientSize = new Size(1160, 760);
        BackColor = Ui.BgSoft;

        Ui.StyleGrid(_classes);
        Ui.StyleGrid(_students);
        _students.CellFormatting += (_, e) =>
        {
            if (e.ColumnIndex >= 0 && _students.Columns[e.ColumnIndex].Name == "TrangThai")
            { e.Value = DirectoryRules.Status(e.Value?.ToString()); e.FormattingApplied = true; }
        };
        Ui.StyleGrid(_teachers);
        Ui.StyleGrid(_schedule);
        _classes.SelectionChanged += (_, _) => ChonLop();
        _search.PlaceholderText = "Mã lớp LH000001 / tên lớp / khóa học...";
        _search.TextChanged += (_, _) => NapDanhSach(debounce: true);

        var split = new SplitContainer { Dock = DockStyle.Fill, BackColor = Ui.BorderSoft };
        split.SizeChanged += (_, _) =>
        {
            if (split.Width > 850)
                split.SplitterDistance = Math.Clamp(split.Width / 3, 280, split.Width - 550);
        };
        var left = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.White, ColumnCount = 1, RowCount = 3 };
        left.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        _search.Dock = DockStyle.Fill;
        _search.Margin = new Padding(12, 14, 12, 10);
        _classes.Dock = DockStyle.Fill;
        _classHint.Dock = DockStyle.Fill;
        _classHint.Padding = new Padding(12, 8, 0, 0);
        left.Controls.Add(_search, 0, 0);
        left.Controls.Add(_classes, 0, 1);
        left.Controls.Add(_classHint, 0, 2);
        split.Panel1.Controls.Add(left);

        var right = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.White, ColumnCount = 1, RowCount = 4 };
        right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _title.Dock = DockStyle.Fill;
        _title.AutoSize = false;
        _title.Padding = new Padding(18, 15, 0, 0);
        _meta.Dock = DockStyle.Fill;
        _meta.AutoSize = false;
        _meta.Padding = new Padding(18, 4, 0, 0);
        _summary.Dock = DockStyle.Bottom;
        _summary.AutoSize = false;
        _summary.Height = 30;
        _summary.Padding = new Padding(18, 3, 0, 0);
        var info = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        info.Controls.Add(_summary);
        info.Controls.Add(_meta);
        _meta.Dock = DockStyle.Top;
        _meta.Height = 33;

        if (Session.IsQuanTriVien)
        {
            ThemNut("Sửa lớp", SuaLop);
            ThemNut("Thêm học viên", ThemHocVien);
            ThemNut("Thêm lịch học", ThemLich);
        }
        ThemNut("Làm mới", () => NapDanhSach(_lopDangXem));

        _tabHocVien = TaoTab("Học viên", _students, _studentHint);
        _tabGiaoVien = TaoTabGiaoVien();
        _tabLichHoc = TaoTab("Lịch học", _schedule, _scheduleHint);
        if (Session.IsQuanTriVien)
        {
            var repeat = Ui.ButtonAccent("Tạo lịch lặp theo tuần", 220);
            repeat.Dock = DockStyle.Top;
            repeat.Click += (_, _) =>
            {
                if (_lopDangXem <= 0) return;
                using var form = new frmLichHocLap(_lopDangXem);
                if (form.ShowDialog(this) == DialogResult.OK) NapDanhSach(_lopDangXem);
            };
            var body = _tabLichHoc.Controls[0];
            _tabLichHoc.Controls.Add(repeat);
            body.BringToFront();
        }
        _tabs.TabPages.AddRange([_tabHocVien, _tabGiaoVien, _tabLichHoc]);
        _schedule.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0 && Session.IsQuanTriVien) SuaLich(); };
        right.Controls.Add(_title, 0, 0);
        right.Controls.Add(info, 0, 1);
        right.Controls.Add(_actions, 0, 2);
        right.Controls.Add(_tabs, 0, 3);
        split.Panel2.Controls.Add(right);
        Controls.Add(split);

        Load += (_, _) => NapDanhSach(_lopBanDau);
    }

    private static TabPage TaoTab(string name, DataGridView grid, Label hint)
    {
        var page = new TabPage(name) { BackColor = Color.White };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
        grid.Dock = DockStyle.Fill;
        hint.Dock = DockStyle.Fill;
        hint.Padding = new Padding(12, 8, 0, 0);
        layout.Controls.Add(grid, 0, 0);
        layout.Controls.Add(hint, 0, 1);
        page.Controls.Add(layout);
        return page;
    }

    private TabPage TaoTabGiaoVien()
    {
        if (!Session.IsQuanTriVien) return TaoTab("Giáo viên", _teachers, _teacherHint);

        var page = new TabPage("Giáo viên") { BackColor = Color.White };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(8, 4, 0, 0) };
        var add = Ui.ButtonAccent("Phân công", 130, 38);
        var edit = Ui.ButtonOutline("Đổi giáo viên", 145, 38);
        var remove = Ui.ButtonDanger("Gỡ phân công", 145, 38);
        add.Click += (_, _) => PhanCong();
        edit.Click += (_, _) => SuaPhanCong();
        remove.Click += (_, _) => XoaPhanCong();
        foreach (var button in new[] { add, edit, remove })
        {
            button.AutoSize = true;
            button.AutoSizeMode = AutoSizeMode.GrowOnly;
            button.Padding = new Padding(12, 0, 12, 0);
            button.Margin = new Padding(4);
            toolbar.Controls.Add(button);
        }
        _teachers.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) SuaPhanCong(); };
        _teachers.Dock = DockStyle.Fill;
        _teacherHint.Dock = DockStyle.Fill;
        _teacherHint.Padding = new Padding(12, 8, 0, 0);
        layout.Controls.Add(toolbar, 0, 0);
        layout.Controls.Add(_teachers, 0, 1);
        layout.Controls.Add(_teacherHint, 0, 2);
        page.Controls.Add(layout);
        return page;
    }

    private void ThemNut(string text, Action action)
    {
        var button = Ui.ButtonOutline(text, 150, 38);
        button.AutoSize = true;
        button.AutoSizeMode = AutoSizeMode.GrowOnly;
        button.MinimumSize = new Size(110, 38);
        button.Padding = new Padding(14, 0, 14, 0);
        button.Margin = new Padding(4);
        button.Click += (_, _) => { if (_lopDangXem > 0) action(); };
        _actions.Controls.Add(button);
    }

    private async void NapDanhSach(int preferId = 0, bool debounce = false)
    {
        var version = ++_listVersion;
        var selectedId = preferId > 0 ? preferId : _lopDangXem;
        var search = _search.Text;
        try
        {
        if (debounce) await Task.Delay(300);
        if (IsDisposed || Disposing || version != _listVersion) return;
        _classHint.Text = "Đang tải danh sách lớp...";
        var table = await Task.Run(() => _service.LayLopDuocXem(search));
        if (IsDisposed || Disposing || version != _listVersion) return;
        _dangNapDanhSach = true;
        _classes.DataSource = table;
        Ui.RenameColumns(_classes, ("MaLop", "Mã lớp"), ("TenLop", "Lớp học"),
            ("TenGiaoVienPhuTrach", "Giáo viên phụ trách"), ("SoHocVien", "HV"),
            ("TrangThaiPhanCong", "Phân công GV"));
        Ui.HideColumns(_classes, "LopHocID", "TenKhoaHoc", "TenMon");
        if (_classes.Columns.Contains("TenLop")) _classes.Columns["TenLop"]!.FillWeight = 150;
        if (_classes.Columns.Contains("TenGiaoVienPhuTrach")) _classes.Columns["TenGiaoVienPhuTrach"]!.FillWeight = 135;
        if (_classes.Columns.Contains("SoHocVien")) _classes.Columns["SoHocVien"]!.FillWeight = 35;
        if (_classes.Columns.Contains("TrangThaiPhanCong")) _classes.Columns["TrangThaiPhanCong"]!.FillWeight = 105;
        var unassigned = 0;
        foreach (DataGridViewRow row in _classes.Rows)
        {
            if (row.Cells["TrangThaiPhanCong"].Value?.ToString() != "Chưa phân công") continue;
            unassigned++;
            row.DefaultCellStyle.BackColor = Color.FromArgb(255, 248, 232);
            row.Cells["TrangThaiPhanCong"].Style.ForeColor = Color.FromArgb(160, 88, 0);
        }
        _classHint.Text = table.Rows.Count == 0
            ? (Session.IsGiaoVien ? "Chưa có lớp được phân công." : "Không tìm thấy lớp học.")
            : Session.IsQuanTriVien ? $"{table.Rows.Count:N0} lớp · {unassigned:N0} chưa phân công"
              : $"{table.Rows.Count:N0} lớp phụ trách";
        if (table.Rows.Count == 0)
        {
            _dangNapDanhSach = false;
            XoaChiTiet();
            return;
        }

        var target = _classes.Rows.Cast<DataGridViewRow>()
            .FirstOrDefault(r => Convert.ToInt32(r.Cells["LopHocID"].Value) == selectedId)
            ?? _classes.Rows[0];
        _classes.CurrentCell = target.Cells["TenLop"];
        _lopDangXem = 0;
        _dangNapDanhSach = false;
        ChonLop();
        }
        catch (Exception ex)
        {
            if (IsDisposed || Disposing || version != _listVersion) return;
            _dangNapDanhSach = false;
            _classes.DataSource = null;
            XoaChiTiet();
            _classHint.Text = "Không tải được lớp: " + ex.Message;
        }
    }

    private void ChonLop()
    {
        if (_dangNapDanhSach) return;
        if (_classes.CurrentRow?.Cells["LopHocID"].Value is null) return;
        var id = Convert.ToInt32(_classes.CurrentRow.Cells["LopHocID"].Value);
        if (id == _lopDangXem) return;
        _lopDangXem = id;
        TaiChiTiet();
    }

    private async void TaiChiTiet()
    {
        if (_lopDangXem <= 0) { XoaChiTiet(); return; }
        var version = ++_detailVersion;
        var classId = _lopDangXem;
        _actions.Enabled = false;
        _tabs.Enabled = false;
        _students.DataSource = null;
        _teachers.DataSource = null;
        _schedule.DataSource = null;
        _title.Text = "Đang tải thông tin lớp...";
        _meta.Text = _summary.Text = "";
        _studentHint.Text = _teacherHint.Text = _scheduleHint.Text = "";
        try
        {
        var data = await Task.Run(() =>
        {
            var info = _service.LayThongTinLopDuocXem(classId);
            // Keep the permission checks on each data access.
            if (info.Rows.Count == 0)
                return (info, students: new DataTable(), teachers: new DataTable(), schedule: new DataTable());
            return (info, students: _service.LayHocVienLopDuocXem(classId),
                teachers: _service.LayPhanCongLopDuocXem(classId),
                schedule: _service.LayLichHocLopDuocXem(classId));
        });
        if (IsDisposed || Disposing || version != _detailVersion || classId != _lopDangXem) return;
        var (info, students, teachers, schedule) = data;
        if (info.Rows.Count == 0) { XoaChiTiet(); return; }
        var row = info.Rows[0];
        _title.Text = $"{DirectoryRules.ClassCode(classId)} · {row["TenLop"]}";
        var dates = row["NgayBatDau"] is DateTime start && row["NgayKetThuc"] is DateTime end
            ? $" · {start:dd/MM/yyyy}–{end:dd/MM/yyyy}" : "";
        _meta.Text = $"{row["TenKhoaHoc"]} · {row["TenMon"]}{dates}";
        _summary.Text = teachers.Rows.Count == 0
            ? $"{row["SoHocVien"]} học viên đang học  ·  CHƯA PHÂN CÔNG GIÁO VIÊN  ·  {row["SoBuoiSapToi"]} buổi sắp tới"
            : $"{row["SoHocVien"]} học viên đang học  ·  {teachers.Rows.Count} giáo viên  ·  {row["SoBuoiSapToi"]} buổi sắp tới";
        _summary.ForeColor = teachers.Rows.Count == 0 ? Color.FromArgb(160, 88, 0) : Ui.Navy;

        _students.DataSource = students;
        Ui.RenameColumns(_students, ("MaTaiKhoan", "Mã tài khoản"), ("HoTen", "Họ tên"), ("TenDangNhap", "Tài khoản"),
            ("Email", "Email"), ("NgayThamGia", "Ngày vào lớp"), ("TrangThai", "Trạng thái"));
        Ui.HideColumns(_students, "HocVienID");
        if (_students.Columns.Contains("NgayThamGia")) _students.Columns["NgayThamGia"]!.DefaultCellStyle.Format = "dd/MM/yyyy";
        _studentHint.Text = students.Rows.Count == 0 ? "Lớp chưa có học viên." : $"{students.Rows.Count} học viên trong danh sách";

        _teachers.DataSource = teachers;
        Ui.RenameColumns(_teachers, ("TenMon", "Môn học"), ("TenGiaoVien", "Giáo viên phụ trách"));
        Ui.HideColumns(_teachers, "LopHocID", "MonHocID", "GiaoVienID");
        _teacherHint.Text = teachers.Rows.Count == 0 ? "Lớp chưa được phân công giáo viên." : $"{teachers.Rows.Count} phân công giảng dạy";

        _schedule.DataSource = schedule;
        Ui.RenameColumns(_schedule, ("NoiDung", "Nội dung"), ("ThoiGianBatDau", "Bắt đầu"),
            ("ThoiGianKetThuc", "Kết thúc"), ("DiaDiem", "Địa điểm"));
        Ui.HideColumns(_schedule, "LichHocID", "LopHocID");
        if (_schedule.Columns.Contains("ThoiGianBatDau")) _schedule.Columns["ThoiGianBatDau"]!.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
        if (_schedule.Columns.Contains("ThoiGianKetThuc")) _schedule.Columns["ThoiGianKetThuc"]!.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
        _scheduleHint.Text = schedule.Rows.Count == 0 ? "Lớp chưa có lịch học." : $"{schedule.Rows.Count} buổi học đã lên lịch";
        }
        catch (Exception ex)
        {
            if (IsDisposed || Disposing || version != _detailVersion) return;
            _title.Text = "Không tải được thông tin lớp";
            _meta.Text = ex.Message;
        }
        finally
        {
            if (!IsDisposed && !Disposing && version == _detailVersion)
            {
                _actions.Enabled = true;
                _tabs.Enabled = true;
            }
        }
    }

    private void XoaChiTiet()
    {
        ++_detailVersion;
        _actions.Enabled = true;
        _tabs.Enabled = true;
        _lopDangXem = 0;
        _title.Text = "Chọn một lớp học";
        _meta.Text = "";
        _summary.Text = "";
        _students.DataSource = null;
        _teachers.DataSource = null;
        _schedule.DataSource = null;
        _studentHint.Text = _teacherHint.Text = _scheduleHint.Text = "";
    }

    private void SuaLop()
    {
        var row = _service.LayThongTinLopDuocXem(_lopDangXem).Rows[0];
        using var edit = new frmLopHocEdit(_lopDangXem, row["TenLop"].ToString()!, Convert.ToInt32(row["KhoaHocID"]));
        if (edit.ShowDialog(FindForm()) == DialogResult.OK) NapDanhSach(_lopDangXem);
    }

    private void ThemHocVien()
    {
        using var picker = new frmThemHocVienVaoLop(_lopDangXem);
        if (picker.ShowDialog(FindForm()) == DialogResult.OK)
        {
            TaiChiTiet();
            _tabs.SelectedTab = _tabHocVien;
        }
    }

    private void PhanCong()
    {
        if (_lopDangXem <= 0) return;
        using var edit = new frmPhanCongEdit(_lopDangXem);
        if (edit.ShowDialog(FindForm()) == DialogResult.OK)
        {
            NapDanhSach(_lopDangXem);
            _tabs.SelectedTab = _tabGiaoVien;
        }
    }

    private void SuaPhanCong()
    {
        if (_lopDangXem <= 0 || _teachers.CurrentRow?.Cells["MonHocID"].Value is null)
        {
            MessageBox.Show(this, "Hãy chọn một phân công giáo viên.", "Thông báo");
            return;
        }
        var monId = Convert.ToInt32(_teachers.CurrentRow.Cells["MonHocID"].Value);
        var giaoVienId = Convert.ToInt32(_teachers.CurrentRow.Cells["GiaoVienID"].Value);
        using var edit = new frmPhanCongEdit(_lopDangXem, sua: true, monHocId: monId, giaoVienId: giaoVienId);
        if (edit.ShowDialog(FindForm()) == DialogResult.OK)
        {
            NapDanhSach(_lopDangXem);
            _tabs.SelectedTab = _tabGiaoVien;
        }
    }

    private void XoaPhanCong()
    {
        if (_lopDangXem <= 0 || _teachers.CurrentRow?.Cells["MonHocID"].Value is null)
        {
            MessageBox.Show(this, "Hãy chọn một phân công giáo viên.", "Thông báo");
            return;
        }
        var monId = Convert.ToInt32(_teachers.CurrentRow.Cells["MonHocID"].Value);
        var tenMon = _teachers.CurrentRow.Cells["TenMon"].Value?.ToString();
        var tenGiaoVien = _teachers.CurrentRow.Cells["TenGiaoVien"].Value?.ToString();
        if (MessageBox.Show(this, $"Gỡ {tenGiaoVien} khỏi phân công môn {tenMon}?",
                "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        var result = _service.XoaPhanCong(_lopDangXem, monId);
        if (!result.Ok)
        {
            MessageBox.Show(this, result.Error, "Không thể gỡ phân công", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        NapDanhSach(_lopDangXem);
        _tabs.SelectedTab = _tabGiaoVien;
    }

    private void ThemLich()
    {
        using var edit = new frmLichHocEdit(lopHocId: _lopDangXem);
        if (edit.ShowDialog(FindForm()) == DialogResult.OK)
        {
            TaiChiTiet();
            _tabs.SelectedTab = _tabLichHoc;
        }
    }

    private void SuaLich()
    {
        if (_schedule.CurrentRow?.Cells["LichHocID"].Value is null) return;
        var lichId = Convert.ToInt32(_schedule.CurrentRow.Cells["LichHocID"].Value);
        var nd = _schedule.CurrentRow.Cells["NoiDung"].Value?.ToString();
        var bd = _schedule.CurrentRow.Cells["ThoiGianBatDau"].Value is DateTime dt1 ? dt1 : (DateTime?)null;
        var kt = _schedule.CurrentRow.Cells["ThoiGianKetThuc"].Value is DateTime dt2 ? dt2 : (DateTime?)null;
        var dd = _schedule.CurrentRow.Cells["DiaDiem"].Value?.ToString();
        using var edit = new frmLichHocEdit(id: lichId, lopHocId: _lopDangXem, noiDung: nd, batDau: bd, ketThuc: kt, diaDiem: dd);
        if (edit.ShowDialog(FindForm()) == DialogResult.OK)
        {
            TaiChiTiet();
            _tabs.SelectedTab = _tabLichHoc;
        }
    }
}

internal class frmThemHocVienVaoLop : Form
{
    private readonly TrainingService _service = new();
    private readonly int _lopId;
    private readonly TextBox _search = Ui.Input(320);
    private readonly DataGridView _grid = new();

    public frmThemHocVienVaoLop(int lopId)
    {
        Session.YeuCauQuanTriVien();
        _lopId = lopId;
        Text = "Thêm học viên vào lớp";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(680, 500);
        MinimumSize = new Size(560, 400);
        BackColor = Color.White;
        Ui.StyleGrid(_grid);
        _search.PlaceholderText = "Tìm họ tên, tài khoản hoặc email...";
        _search.TextChanged += (_, _) => Nap();
        _search.Dock = DockStyle.Fill;
        _grid.Dock = DockStyle.Fill;
        _grid.CellDoubleClick += (_, _) => Luu();
        var add = Ui.ButtonAccent("Thêm học viên", 150);
        add.AutoSize = true;
        add.MinimumSize = new Size(150, 38);
        add.Padding = new Padding(14, 0, 14, 0);
        add.Click += (_, _) => Luu();
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        buttons.Controls.Add(add);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(12) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        layout.Controls.Add(_search, 0, 0);
        layout.Controls.Add(_grid, 0, 1);
        layout.Controls.Add(buttons, 0, 2);
        Controls.Add(layout);
        Load += (_, _) => Nap();
    }

    private void Nap()
    {
        _grid.DataSource = _service.LayHocVienChuaTrongLop(_lopId, _search.Text);
        Ui.RenameColumns(_grid, ("MaTaiKhoan", "Mã tài khoản"), ("HoTen", "Họ tên"), ("TenDangNhap", "Tài khoản"), ("Email", "Email"));
        Ui.HideColumns(_grid, "HocVienID");
    }

    private void Luu()
    {
        if (_grid.CurrentRow?.Cells["HocVienID"].Value is null) return;
        var id = Convert.ToInt32(_grid.CurrentRow.Cells["HocVienID"].Value);
        var result = _service.ThemHocVienVaoLop(_lopId, id);
        if (!result.Ok)
        {
            MessageBox.Show(this, result.Error, "Không thể thêm học viên", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        DialogResult = DialogResult.OK;
        Close();
    }
}
