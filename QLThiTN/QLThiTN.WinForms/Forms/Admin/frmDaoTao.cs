using QLThiTN.WinForms.Models;
using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms.Forms.Admin;

/// <summary>Quan ly khoa hoc, lop hoc, xep hoc vien, phan cong giang day — Giao vu = GiaoVien; QuanTriVien cung truy cap duoc.</summary>
public class frmDaoTao : Form
{
    private readonly TrainingService _service = new();

    // Tab khoa hoc
    private readonly DataGridView _gridKhoa = new();
    private readonly TextBox _txtTimKhoa = Ui.Input(200);
    private readonly ComboBox _cboLocMonKhoa = Ui.Combo(180);

    // Tab lop hoc
    private readonly DataGridView _gridLop = new();
    private readonly TextBox _txtTimLop = Ui.Input(200);
    private readonly ComboBox _cboLocKhoaLop = Ui.Combo(220);

    // Tab xep hoc vien
    private readonly ComboBox _cboLopXep = Ui.Combo(280);
    private readonly DataGridView _gridTrongLop = new();
    private readonly DataGridView _gridChuaLop = new();
    private readonly TextBox _txtTimHv = Ui.Input(200);
    private readonly ComboBox _cboTrangThaiHv = Ui.Combo(140);

    // Tab phan cong
    private readonly ComboBox _cboLopPc = Ui.Combo(280);
    private readonly DataGridView _gridPc = new();

    // Tab lich hoc
    private readonly ComboBox _cboLopLh = Ui.Combo(280);
    private readonly DataGridView _gridLh = new();

    // Tab tong hop: danh sach khoa + bang chi tiet khi chon
    private readonly DataGridView _gridDanhSachKhoa = new();
    private readonly DataGridView _gridChiTietKhoa = new();
    private readonly TextBox _txtTimTongHop = Ui.Input(240);
    private readonly Label _lblTongHopStats = Ui.Label("", Ui.Small, Ui.TextMuted);
    private readonly Label _lblChiTietKhoa = Ui.Label("Chọn một khóa học ở danh sách bên trái để xem chi tiết.", Ui.H2, Ui.Navy);
    private List<TongHopKhoaHoc> _tongHopCache = [];
    private TabControl? _tabs;
    private const int TabTongHopIndex = 0;

    public frmDaoTao()
    {
        Text = "Quản lý khóa học & lớp";
        Font = Ui.Body;

        _tabs = new TabControl { Dock = DockStyle.Fill, Font = Ui.Body, Padding = new Point(12, 8) };
        _tabs.TabPages.Add(TaoTabTongHop());
        _tabs.TabPages.Add(TaoTabKhoaHoc());
        _tabs.TabPages.Add(TaoTabLopHoc());
        _tabs.TabPages.Add(TaoTabXepHocVien());
        _tabs.TabPages.Add(TaoTabPhanCong());
        _tabs.TabPages.Add(TaoTabLichHoc());
        _tabs.SelectedIndexChanged += (_, _) =>
        {
            if (_tabs.SelectedIndex == TabTongHopIndex) TaiTongHop();
        };
        Controls.Add(_tabs);

        NapLocMonKhoa();
        NapLocKhoaLop();
        NapComboLop(_cboLopXep);
        NapComboLop(_cboLopPc);
        NapComboLop(_cboLopLh);
        TaiKhoaHoc();
        TaiLopHoc();
        TaiTongHop();
    }

    private void CapNhatTongHop() => TaiTongHop();

    // ------------------------------------------------------------------ Tong hop
    private TabPage TaoTabTongHop()
    {
        var page = new TabPage("Tổng hợp") { BackColor = Ui.BgSoft };

        Ui.StyleGrid(_gridDanhSachKhoa);
        _gridDanhSachKhoa.Dock = DockStyle.Fill;
        _gridDanhSachKhoa.SelectionChanged += (_, _) => ChonKhoaXemChiTiet();
        _gridDanhSachKhoa.CellClick += (_, _) => ChonKhoaXemChiTiet();

        Ui.StyleGrid(_gridChiTietKhoa);
        _gridChiTietKhoa.Dock = DockStyle.Fill;
        _gridChiTietKhoa.CellFormatting += GridChiTietKhoa_CellFormatting;

        _txtTimTongHop.PlaceholderText = "Tìm tên khóa, môn, lớp, giáo viên, học viên...";
        _txtTimTongHop.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) TaiTongHop(); };

        var btnTim = Ui.ButtonPrimary("Tìm", 80);
        btnTim.Click += (_, _) => TaiTongHop();
        var btnLamMoi = Ui.ButtonOutline("Làm mới", 100);
        btnLamMoi.Click += (_, _) => TaiTongHop();

        var toolbar = TaoToolbar([
            Ui.Label("Tìm:", Ui.Body, Ui.TextMuted), _txtTimTongHop, btnTim, btnLamMoi
        ]);

        _lblTongHopStats.Dock = DockStyle.Bottom;
        _lblTongHopStats.Height = 32;
        _lblTongHopStats.Padding = new Padding(16, 8, 0, 0);
        _lblTongHopStats.BackColor = Color.White;

        var note = Ui.Label(
            "Danh sách bên trái chỉ hiển thị khóa học. Bấm một dòng để xem lớp, giáo viên phụ trách và học viên. Tự cập nhật khi bạn sửa ở các tab khác.",
            Ui.Small, Ui.TextMuted);
        note.Dock = DockStyle.Top;
        note.Height = 28;
        note.Padding = new Padding(16, 8, 0, 0);
        note.BackColor = Color.White;

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 380,
            BackColor = Ui.BgSoft
        };

        var lblDs = Ui.Label("Danh sách khóa học", Ui.H2, Ui.Navy);
        lblDs.Dock = DockStyle.Top;
        lblDs.Height = 36;
        lblDs.Padding = new Padding(12, 10, 0, 0);
        lblDs.BackColor = Color.White;

        var pnlTrai = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        pnlTrai.Controls.Add(_gridDanhSachKhoa);
        pnlTrai.Controls.Add(lblDs);

        _lblChiTietKhoa.Dock = DockStyle.Top;
        _lblChiTietKhoa.Height = 40;
        _lblChiTietKhoa.Padding = new Padding(12, 10, 0, 0);
        _lblChiTietKhoa.BackColor = Color.White;

        var pnlPhai = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        pnlPhai.Controls.Add(_gridChiTietKhoa);
        pnlPhai.Controls.Add(_lblChiTietKhoa);

        split.Panel1.Controls.Add(pnlTrai);
        split.Panel2.Controls.Add(pnlPhai);

        page.Controls.Add(split);
        page.Controls.Add(_lblTongHopStats);
        page.Controls.Add(note);
        page.Controls.Add(toolbar);
        return page;
    }

    private void GridChiTietKhoa_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
        var row = _gridChiTietKhoa.Rows[e.RowIndex];
        var loai = row.Cells["Loai"].Value?.ToString();
        if (_gridChiTietKhoa.Columns[e.ColumnIndex].Name == "Loai" && loai is "Lớp học")
            e.CellStyle.Font = new Font(Ui.Body, FontStyle.Bold);
        if (row.Cells["ChiTiet"].Value?.ToString()?.StartsWith('(') == true)
            e.CellStyle.ForeColor = Ui.TextMuted;
    }

    private int? IdKhoaTongHopDangChon()
    {
        if (_gridDanhSachKhoa.CurrentRow?.Cells["KhoaHocID"].Value is int id) return id;
        if (_gridDanhSachKhoa.CurrentRow?.Cells["KhoaHocID"].Value != null)
            return Convert.ToInt32(_gridDanhSachKhoa.CurrentRow.Cells["KhoaHocID"].Value);
        return null;
    }

    private void ChonKhoaXemChiTiet() => TaiChiTietKhoa(IdKhoaTongHopDangChon());

    private void TaiTongHop()
    {
        var selectedId = IdKhoaTongHopDangChon();
        _tongHopCache = _service.LayTongHopDaoTao(_txtTimTongHop.Text);

        var dtKhoa = new System.Data.DataTable();
        dtKhoa.Columns.Add("KhoaHocID", typeof(int));
        dtKhoa.Columns.Add("TenKhoaHoc", typeof(string));
        dtKhoa.Columns.Add("TenMon", typeof(string));
        dtKhoa.Columns.Add("TrangThai", typeof(string));
        dtKhoa.Columns.Add("SoLop", typeof(int));
        dtKhoa.Columns.Add("SoHocVien", typeof(int));
        dtKhoa.Columns.Add("SoGiaoVien", typeof(int));

        var tongLop = 0;
        var tongHv = 0;
        var tongGvPc = 0;

        foreach (var khoa in _tongHopCache)
        {
            var soLop = khoa.LopHocs.Count;
            var soHv = khoa.LopHocs.Sum(l => l.HocViens.Count);
            var soGv = khoa.LopHocs.Sum(l => l.GiaoViens.Count);
            tongLop += soLop;
            tongHv += soHv;
            tongGvPc += soGv;
            dtKhoa.Rows.Add(
                khoa.KhoaHocID,
                khoa.TenKhoaHoc,
                khoa.TenMon,
                TrangThaiKhoaHoc.Display(khoa.TrangThai),
                soLop,
                soHv,
                soGv);
        }

        _gridDanhSachKhoa.DataSource = dtKhoa;
        Ui.RenameColumns(_gridDanhSachKhoa,
            ("TenKhoaHoc", "Tên khóa học"),
            ("TenMon", "Môn"),
            ("TrangThai", "Trạng thái"),
            ("SoLop", "Số lớp"),
            ("SoHocVien", "Học viên"),
            ("SoGiaoVien", "GV phụ trách"));
        Ui.HideColumns(_gridDanhSachKhoa, "KhoaHocID");

        if (_gridDanhSachKhoa.Rows.Count > 0)
        {
            _gridDanhSachKhoa.ClearSelection();
            var rowIndex = 0;
            if (selectedId.HasValue)
            {
                for (var i = 0; i < _gridDanhSachKhoa.Rows.Count; i++)
                {
                    if (_gridDanhSachKhoa.Rows[i].Cells["KhoaHocID"].Value is int id && id == selectedId.Value)
                    {
                        rowIndex = i;
                        break;
                    }
                }
            }
            _gridDanhSachKhoa.Rows[rowIndex].Selected = true;
            _gridDanhSachKhoa.CurrentCell = _gridDanhSachKhoa.Rows[rowIndex].Cells["TenKhoaHoc"];
        }

        ChonKhoaXemChiTiet();

        _lblTongHopStats.Text =
            $"  {_tongHopCache.Count} khóa học · {tongLop} lớp · {tongHv} học viên · {tongGvPc} phân công giáo viên";
    }

    private void TaiChiTietKhoa(int? khoaHocId)
    {
        if (khoaHocId is not int id || id <= 0)
        {
            _lblChiTietKhoa.Text = "Chọn một khóa học ở danh sách bên trái để xem chi tiết.";
            _gridChiTietKhoa.DataSource = null;
            return;
        }

        var khoa = _tongHopCache.FirstOrDefault(k => k.KhoaHocID == id);
        if (khoa == null)
        {
            _lblChiTietKhoa.Text = "Không tìm thấy dữ liệu khóa học.";
            _gridChiTietKhoa.DataSource = null;
            return;
        }

        _lblChiTietKhoa.Text = $"Chi tiết: {khoa.TenKhoaHoc} — {khoa.TenMon} ({TrangThaiKhoaHoc.Display(khoa.TrangThai)})";

        var dt = new System.Data.DataTable();
        dt.Columns.Add("Loai", typeof(string));
        dt.Columns.Add("TenLop", typeof(string));
        dt.Columns.Add("ChiTiet", typeof(string));
        dt.Columns.Add("MonHoacTaiKhoan", typeof(string));
        dt.Columns.Add("TrangThai", typeof(string));

        if (khoa.LopHocs.Count == 0)
        {
            dt.Rows.Add("Thông báo", "—", "(Khóa học chưa có lớp nào)", "", "");
        }
        else
        {
            foreach (var lop in khoa.LopHocs)
            {
                dt.Rows.Add("Lớp học", lop.TenLop, lop.TenLop, "", "");

                if (lop.GiaoViens.Count == 0)
                    dt.Rows.Add("Giáo viên", lop.TenLop, "(Chưa phân công giáo viên)", "", "");
                else
                {
                    foreach (var gv in lop.GiaoViens)
                        dt.Rows.Add("Giáo viên", lop.TenLop, gv.TenGiaoVien, gv.TenMon, "Phụ trách");
                }

                if (lop.HocViens.Count == 0)
                    dt.Rows.Add("Học viên", lop.TenLop, "(Chưa có học viên)", "", "");
                else
                {
                    foreach (var hv in lop.HocViens)
                        dt.Rows.Add("Học viên", lop.TenLop, hv.HoTen, hv.TenDangNhap,
                            TrangThaiHocVienLop.Display(hv.TrangThai));
                }
            }
        }

        _gridChiTietKhoa.DataSource = dt;
        Ui.RenameColumns(_gridChiTietKhoa,
            ("Loai", "Loại"),
            ("TenLop", "Lớp"),
            ("ChiTiet", "Họ tên / Mô tả"),
            ("MonHoacTaiKhoan", "Môn / Tài khoản"),
            ("TrangThai", "Trạng thái"));
    }

    // ------------------------------------------------------------------ Khoa hoc
    private TabPage TaoTabKhoaHoc()
    {
        var page = new TabPage("Khóa học") { BackColor = Ui.BgSoft };
        Ui.StyleGrid(_gridKhoa);
        _gridKhoa.Dock = DockStyle.Fill;
        _gridKhoa.CellDoubleClick += (_, _) => SuaKhoaHoc();

        _txtTimKhoa.PlaceholderText = "Tên khóa / môn...";
        _txtTimKhoa.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) TaiKhoaHoc(); };
        _cboLocMonKhoa.SelectedIndexChanged += (_, _) => TaiKhoaHoc();

        var btnTim = Ui.ButtonPrimary("Tìm", 80);
        btnTim.Click += (_, _) => TaiKhoaHoc();
        var btnThem = Ui.ButtonAccent("＋ Mở khóa học", 150);
        btnThem.Click += (_, _) =>
        {
            using var f = new frmKhoaHocEdit();
            if (f.ShowDialog(FindForm()) == DialogResult.OK) { TaiKhoaHoc(); NapLocKhoaLop(); CapNhatTongHop(); }
        };
        var btnSua = Ui.ButtonPrimary("✎ Sửa", 90);
        btnSua.Click += (_, _) => SuaKhoaHoc();
        var btnXoa = Ui.ButtonDanger("🗑 Xóa", 90);
        btnXoa.Click += (_, _) => XoaKhoaHoc();

        var toolbar = TaoToolbar([Ui.Label("Tìm:", Ui.Body, Ui.TextMuted), _txtTimKhoa, _cboLocMonKhoa, btnTim, btnThem, btnSua, btnXoa]);
        page.Controls.Add(_gridKhoa);
        page.Controls.Add(toolbar);
        return page;
    }

    private void NapLocMonKhoa()
    {
        var dt = _service.LayDanhSachMonHoc();
        var all = dt.Clone();
        all.Rows.Add(0, "Tất cả môn");
        foreach (System.Data.DataRow r in dt.Rows) all.Rows.Add(r.ItemArray);
        _cboLocMonKhoa.DisplayMember = "TenMon";
        _cboLocMonKhoa.ValueMember = "MonHocID";
        _cboLocMonKhoa.DataSource = all;
    }

    private void TaiKhoaHoc()
    {
        int? monId = _cboLocMonKhoa.SelectedValue is int m && m > 0 ? m : null;
        _gridKhoa.DataSource = _service.LayDanhSachKhoaHoc(_txtTimKhoa.Text, monId);
        Ui.RenameColumns(_gridKhoa,
            ("KhoaHocID", "ID"), ("TenKhoaHoc", "Tên khóa học"), ("TenMon", "Môn học"),
            ("NgayBatDau", "Bắt đầu"), ("NgayKetThuc", "Kết thúc"), ("TrangThai", "Trạng thái"), ("SoLop", "Số lớp"));
        Ui.HideColumns(_gridKhoa, "KhoaHocID", "MonHocID");
        FormatTrangThaiKhoa(_gridKhoa);
    }

    private static void FormatTrangThaiKhoa(DataGridView grid)
    {
        if (!grid.Columns.Contains("TrangThai")) return;
        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.Cells["TrangThai"].Value is string code)
                row.Cells["TrangThai"].Value = TrangThaiKhoaHoc.Display(code);
        }
    }

    private int IdKhoaChon() => GridId(_gridKhoa, "KhoaHocID");

    private void SuaKhoaHoc()
    {
        var id = IdKhoaChon();
        if (id == 0) { ThongBaoChon(); return; }
        var row = _gridKhoa.CurrentRow!;
        var trangThai = row.Cells["TrangThai"].Value?.ToString() ?? TrangThaiKhoaHoc.HoatDong;
        foreach (var (code, display) in TrangThaiKhoaHoc.All)
            if (display == trangThai) { trangThai = code; break; }

        DateTime? bd = row.Cells["NgayBatDau"].Value is DateTime d1 ? d1 : null;
        DateTime? kt = row.Cells["NgayKetThuc"].Value is DateTime d2 ? d2 : null;
        var monId = row.Cells["MonHocID"].Value is int mi ? mi : Convert.ToInt32(row.Cells["MonHocID"].Value ?? 0);

        using var f = new frmKhoaHocEdit(id,
            row.Cells["TenKhoaHoc"].Value?.ToString() ?? "",
            monId, bd, kt, trangThai);
        if (f.ShowDialog(FindForm()) == DialogResult.OK) { TaiKhoaHoc(); NapLocKhoaLop(); CapNhatTongHop(); }
    }

    private void XoaKhoaHoc()
    {
        var id = IdKhoaChon();
        if (id == 0) { ThongBaoChon(); return; }
        var ten = _gridKhoa.CurrentRow!.Cells["TenKhoaHoc"].Value;
        if (MessageBox.Show($"Xóa khóa học '{ten}'?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;
        var (ok, error) = _service.XoaKhoaHoc(id);
        if (!ok) { MessageBox.Show(error, "Không thể xóa", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        TaiKhoaHoc();
        NapLocKhoaLop();
        CapNhatTongHop();
    }

    // ------------------------------------------------------------------ Lop hoc
    private TabPage TaoTabLopHoc()
    {
        var page = new TabPage("Lớp học") { BackColor = Ui.BgSoft };
        Ui.StyleGrid(_gridLop);
        _gridLop.Dock = DockStyle.Fill;
        _gridLop.CellDoubleClick += (_, _) => SuaLopHoc();

        _txtTimLop.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) TaiLopHoc(); };
        _cboLocKhoaLop.SelectedIndexChanged += (_, _) => TaiLopHoc();

        var btnTim = Ui.ButtonPrimary("Tìm", 80);
        btnTim.Click += (_, _) => TaiLopHoc();
        var btnThem = Ui.ButtonAccent("＋ Mở lớp", 120);
        btnThem.Click += (_, _) =>
        {
            using var f = new frmLopHocEdit();
            if (f.ShowDialog(FindForm()) == DialogResult.OK)
            {
                TaiLopHoc();
                NapComboLop(_cboLopXep);
                NapComboLop(_cboLopPc);
                NapComboLop(_cboLopLh);
                CapNhatTongHop();
            }
        };
        var btnSua = Ui.ButtonPrimary("✎ Sửa", 90);
        btnSua.Click += (_, _) => SuaLopHoc();
        var btnXoa = Ui.ButtonDanger("🗑 Xóa", 90);
        btnXoa.Click += (_, _) => XoaLopHoc();

        var toolbar = TaoToolbar([Ui.Label("Tìm:", Ui.Body, Ui.TextMuted), _txtTimLop, _cboLocKhoaLop, btnTim, btnThem, btnSua, btnXoa]);
        page.Controls.Add(_gridLop);
        page.Controls.Add(toolbar);
        return page;
    }

    private void NapLocKhoaLop()
    {
        var dt = _service.LayComboKhoaHoc();
        var all = dt.Clone();
        all.Rows.Add(0, "Tất cả khóa học");
        foreach (System.Data.DataRow r in dt.Rows) all.Rows.Add(r.ItemArray);
        _cboLocKhoaLop.DisplayMember = "TenHienThi";
        _cboLocKhoaLop.ValueMember = "KhoaHocID";
        _cboLocKhoaLop.DataSource = all;
    }

    private void TaiLopHoc()
    {
        int? khId = _cboLocKhoaLop.SelectedValue is int k && k > 0 ? k : null;
        _gridLop.DataSource = _service.LayDanhSachLopHoc(khId, _txtTimLop.Text);
        Ui.RenameColumns(_gridLop,
            ("LopHocID", "ID"), ("TenLop", "Tên lớp"), ("TenKhoaHoc", "Khóa học"),
            ("TenMon", "Môn"), ("SoHocVien", "Học viên đang học"));
        Ui.HideColumns(_gridLop, "LopHocID", "KhoaHocID");
    }

    private int IdLopChon(DataGridView grid) => GridId(grid, "LopHocID");

    private void SuaLopHoc()
    {
        var id = IdLopChon(_gridLop);
        if (id == 0) { ThongBaoChon(); return; }
        var row = _gridLop.CurrentRow!;
        using var f = new frmLopHocEdit(id,
            row.Cells["TenLop"].Value?.ToString() ?? "",
            Convert.ToInt32(row.Cells["KhoaHocID"].Value));
        if (f.ShowDialog(FindForm()) == DialogResult.OK)
        {
            TaiLopHoc();
            NapComboLop(_cboLopXep);
            NapComboLop(_cboLopPc);
            NapComboLop(_cboLopLh);
            CapNhatTongHop();
        }
    }

    private void XoaLopHoc()
    {
        var id = IdLopChon(_gridLop);
        if (id == 0) { ThongBaoChon(); return; }
        var ten = _gridLop.CurrentRow!.Cells["TenLop"].Value;
        if (MessageBox.Show($"Xóa lớp '{ten}'?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;
        var (ok, error) = _service.XoaLopHoc(id);
        if (!ok) { MessageBox.Show(error, "Không thể xóa", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        TaiLopHoc();
        NapComboLop(_cboLopXep);
        NapComboLop(_cboLopPc);
        NapComboLop(_cboLopLh);
        CapNhatTongHop();
    }

    // ------------------------------------------------------------------ Xep hoc vien
    private TabPage TaoTabXepHocVien()
    {
        var page = new TabPage("Xếp học viên") { BackColor = Ui.BgSoft };

        Ui.StyleGrid(_gridTrongLop);
        Ui.StyleGrid(_gridChuaLop);
        _gridTrongLop.Dock = DockStyle.Fill;
        _gridChuaLop.Dock = DockStyle.Fill;
        _gridTrongLop.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _gridChuaLop.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

        _cboLopXep.SelectedIndexChanged += (_, _) => TaiHocVienLop();
        _txtTimHv.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) TaiHocVienChuaLop(); };

        foreach (var (_, display) in TrangThaiHocVienLop.All)
            _cboTrangThaiHv.Items.Add(display);
        _cboTrangThaiHv.SelectedIndex = 0;

        var btnTim = Ui.ButtonPrimary("Tìm HV", 100);
        btnTim.Click += (_, _) => TaiHocVienChuaLop();
        var btnThem = Ui.ButtonAccent("→ Thêm vào lớp", 150);
        btnThem.Click += (_, _) => ThemHocVienVaoLop();
        var btnXoa = Ui.ButtonDanger("← Gỡ khỏi lớp", 140);
        btnXoa.Click += (_, _) => GoHocVienKhoiLop();
        var btnDoiTt = Ui.ButtonOutline("Đổi trạng thái", 130);
        btnDoiTt.Click += (_, _) => DoiTrangThaiHocVien();

        var top = TaoToolbar([
            Ui.Label("Lớp:", Ui.Body, Ui.TextMuted), _cboLopXep,
            Ui.Label("Tìm HV:", Ui.Body, Ui.TextMuted), _txtTimHv, btnTim,
            btnThem, btnXoa, btnDoiTt, Ui.Label("Trạng thái:", Ui.Body, Ui.TextMuted), _cboTrangThaiHv
        ]);
        top.Height = 62;

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 480,
            BackColor = Ui.BgSoft
        };
        var lblTrong = Ui.Label("Học viên trong lớp", Ui.H2, Ui.Navy);
        lblTrong.Dock = DockStyle.Top;
        lblTrong.Padding = new Padding(8, 8, 0, 4);
        split.Panel1.Controls.Add(_gridTrongLop);
        split.Panel1.Controls.Add(lblTrong);

        var lblChua = Ui.Label("Học viên có thể thêm", Ui.H2, Ui.Navy);
        lblChua.Dock = DockStyle.Top;
        lblChua.Padding = new Padding(8, 8, 0, 4);
        split.Panel2.Controls.Add(_gridChuaLop);
        split.Panel2.Controls.Add(lblChua);

        page.Controls.Add(split);
        page.Controls.Add(top);
        return page;
    }

    private void NapComboLop(ComboBox cbo)
    {
        cbo.DisplayMember = "TenHienThi";
        cbo.ValueMember = "LopHocID";
        cbo.DataSource = _service.LayComboLopHoc();
    }

    private int LopXepId() => _cboLopXep.SelectedValue is int i ? i : 0;

    private void TaiHocVienLop()
    {
        var lop = LopXepId();
        if (lop <= 0)
        {
            _gridTrongLop.DataSource = null;
            _gridChuaLop.DataSource = null;
            return;
        }
        _gridTrongLop.DataSource = _service.LayHocVienTrongLop(lop);
        Ui.RenameColumns(_gridTrongLop,
            ("HocVienID", "ID"), ("HoTen", "Họ tên"), ("TenDangNhap", "Tài khoản"),
            ("Email", "Email"), ("NgayThamGia", "Ngày tham gia"), ("TrangThai", "Trạng thái"));
        Ui.HideColumns(_gridTrongLop, "HocVienID");
        FormatTrangThaiHvLop(_gridTrongLop);
        TaiHocVienChuaLop();
    }

    private void TaiHocVienChuaLop()
    {
        var lop = LopXepId();
        if (lop <= 0) return;
        _gridChuaLop.DataSource = _service.LayHocVienChuaTrongLop(lop, _txtTimHv.Text);
        Ui.RenameColumns(_gridChuaLop,
            ("HocVienID", "ID"), ("HoTen", "Họ tên"), ("TenDangNhap", "Tài khoản"), ("Email", "Email"));
        Ui.HideColumns(_gridChuaLop, "HocVienID");
    }

    private static void FormatTrangThaiHvLop(DataGridView grid)
    {
        if (!grid.Columns.Contains("TrangThai")) return;
        foreach (DataGridViewRow row in grid.Rows)
            if (row.Cells["TrangThai"].Value is string code)
                row.Cells["TrangThai"].Value = TrangThaiHocVienLop.Display(code);
    }

    private void ThemHocVienVaoLop()
    {
        var lop = LopXepId();
        if (lop <= 0) { MessageBox.Show("Hãy chọn lớp học.", "Thông báo"); return; }
        if (_gridChuaLop.CurrentRow == null) { MessageBox.Show("Hãy chọn học viên bên danh sách có thể thêm.", "Thông báo"); return; }
        var hvId = Convert.ToInt32(_gridChuaLop.CurrentRow.Cells["HocVienID"].Value);
        var (ok, error) = _service.ThemHocVienVaoLop(lop, hvId);
        if (!ok) { MessageBox.Show(error, "Không thể thêm", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        TaiHocVienLop();
        TaiLopHoc();
        CapNhatTongHop();
    }

    private void GoHocVienKhoiLop()
    {
        var lop = LopXepId();
        if (lop <= 0 || _gridTrongLop.CurrentRow == null) { ThongBaoChon(); return; }
        var ten = _gridTrongLop.CurrentRow.Cells["HoTen"].Value;
        if (MessageBox.Show($"Gỡ '{ten}' khỏi lớp?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;
        var hvId = Convert.ToInt32(_gridTrongLop.CurrentRow.Cells["HocVienID"].Value);
        _service.XoaHocVienKhoiLop(lop, hvId);
        TaiHocVienLop();
        TaiLopHoc();
        CapNhatTongHop();
    }

    private void DoiTrangThaiHocVien()
    {
        var lop = LopXepId();
        if (lop <= 0 || _gridTrongLop.CurrentRow == null) { ThongBaoChon(); return; }
        var display = _cboTrangThaiHv.Text;
        var code = TrangThaiHocVienLop.All.FirstOrDefault(x => x.Display == display).Code;
        if (string.IsNullOrEmpty(code)) return;
        var hvId = Convert.ToInt32(_gridTrongLop.CurrentRow.Cells["HocVienID"].Value);
        _service.DoiTrangThaiHocVienLop(lop, hvId, code);
        TaiHocVienLop();
        CapNhatTongHop();
    }

    // ------------------------------------------------------------------ Phan cong
    private TabPage TaoTabPhanCong()
    {
        var page = new TabPage("Phân công GV") { BackColor = Ui.BgSoft };
        Ui.StyleGrid(_gridPc);
        _gridPc.Dock = DockStyle.Fill;
        _gridPc.CellDoubleClick += (_, _) => SuaPhanCong();

        _cboLopPc.SelectedIndexChanged += (_, _) => TaiPhanCong();

        var btnThem = Ui.ButtonAccent("＋ Phân công", 140);
        btnThem.Click += (_, _) => ThemPhanCong();
        var btnSua = Ui.ButtonPrimary("✎ Sửa", 90);
        btnSua.Click += (_, _) => SuaPhanCong();
        var btnXoa = Ui.ButtonDanger("🗑 Xóa", 90);
        btnXoa.Click += (_, _) => XoaPhanCong();

        var toolbar = TaoToolbar([
            Ui.Label("Lớp:", Ui.Body, Ui.TextMuted), _cboLopPc,
            btnThem, btnSua, btnXoa
        ]);

        var note = Ui.Label(
            "Mỗi lớp — mỗi môn chỉ một giáo viên. Khi phân công, hệ thống tự ghi nhận giáo viên dạy môn đó.",
            Ui.Small, Ui.TextMuted);
        note.Dock = DockStyle.Bottom;
        note.Padding = new Padding(16, 6, 0, 6);
        note.Height = 28;

        page.Controls.Add(_gridPc);
        page.Controls.Add(note);
        page.Controls.Add(toolbar);
        return page;
    }

    private int LopPcId() => _cboLopPc.SelectedValue is int i ? i : 0;

    private void TaiPhanCong()
    {
        var lop = LopPcId();
        if (lop <= 0) { _gridPc.DataSource = null; return; }
        _gridPc.DataSource = _service.LayPhanCongTheoLop(lop);
        Ui.RenameColumns(_gridPc,
            ("MonHocID", "Môn ID"), ("TenMon", "Môn học"), ("TenGiaoVien", "Giáo viên"));
        Ui.HideColumns(_gridPc, "LopHocID", "MonHocID", "GiaoVienID");
    }

    private void ThemPhanCong()
    {
        var lop = LopPcId();
        if (lop <= 0) { MessageBox.Show("Hãy chọn lớp.", "Thông báo"); return; }
        using var f = new frmPhanCongEdit(lop);
        if (f.ShowDialog(FindForm()) == DialogResult.OK) { TaiPhanCong(); CapNhatTongHop(); }
    }

    private void SuaPhanCong()
    {
        var lop = LopPcId();
        if (lop <= 0 || _gridPc.CurrentRow == null) { ThongBaoChon(); return; }
        var mon = Convert.ToInt32(_gridPc.CurrentRow.Cells["MonHocID"].Value);
        var gv = Convert.ToInt32(_gridPc.CurrentRow.Cells["GiaoVienID"].Value);
        using var f = new frmPhanCongEdit(lop, sua: true, monHocId: mon, giaoVienId: gv);
        if (f.ShowDialog(FindForm()) == DialogResult.OK) { TaiPhanCong(); CapNhatTongHop(); }
    }

    private void XoaPhanCong()
    {
        var lop = LopPcId();
        if (lop <= 0 || _gridPc.CurrentRow == null) { ThongBaoChon(); return; }
        var monTen = _gridPc.CurrentRow.Cells["TenMon"].Value;
        if (MessageBox.Show($"Xóa phân công môn '{monTen}'?", "Xác nhận", MessageBoxButtons.YesNo) != DialogResult.Yes)
            return;
        var mon = Convert.ToInt32(_gridPc.CurrentRow.Cells["MonHocID"].Value);
        _service.XoaPhanCong(lop, mon);
        TaiPhanCong();
        CapNhatTongHop();
    }

    // ------------------------------------------------------------------ Lich hoc
    private TabPage TaoTabLichHoc()
    {
        var page = new TabPage("Lịch học") { BackColor = Ui.BgSoft };
        Ui.StyleGrid(_gridLh);
        _gridLh.Dock = DockStyle.Fill;
        _gridLh.CellDoubleClick += (_, _) => SuaLichHoc();

        _cboLopLh.SelectedIndexChanged += (_, _) => TaiLichHoc();

        var btnThem = Ui.ButtonAccent("＋ Thêm buổi", 130);
        btnThem.Click += (_, _) =>
        {
            var lop = LopLhId();
            using var f = new frmLichHocEdit(lopHocId: lop);
            if (f.ShowDialog(FindForm()) == DialogResult.OK) TaiLichHoc();
        };
        var btnSua = Ui.ButtonPrimary("✎ Sửa", 90);
        btnSua.Click += (_, _) => SuaLichHoc();
        var btnXoa = Ui.ButtonDanger("🗑 Xóa", 90);
        btnXoa.Click += (_, _) => XoaLichHoc();

        var toolbar = TaoToolbar([
            Ui.Label("Lớp:", Ui.Body, Ui.TextMuted), _cboLopLh,
            btnThem, btnSua, btnXoa
        ]);

        var note = Ui.Label(
            "Lịch học hiển thị trên website mục «Lịch trình học tập» cho học viên thuộc lớp.",
            Ui.Small, Ui.TextMuted);
        note.Dock = DockStyle.Bottom;
        note.Padding = new Padding(16, 6, 0, 6);
        note.Height = 28;

        page.Controls.Add(_gridLh);
        page.Controls.Add(note);
        page.Controls.Add(toolbar);
        return page;
    }

    private int LopLhId() => _cboLopLh.SelectedValue is int i ? i : 0;

    private void TaiLichHoc()
    {
        var lop = LopLhId();
        if (lop <= 0) { _gridLh.DataSource = null; return; }
        _gridLh.DataSource = _service.LayLichHocTheoLop(lop);
        Ui.RenameColumns(_gridLh,
            ("LichHocID", "ID"), ("NoiDung", "Nội dung"), ("ThoiGianBatDau", "Bắt đầu"),
            ("ThoiGianKetThuc", "Kết thúc"), ("DiaDiem", "Địa điểm"));
        Ui.HideColumns(_gridLh, "LichHocID", "LopHocID");
    }

    private int IdLichHocChon() => GridId(_gridLh, "LichHocID");

    private void SuaLichHoc()
    {
        var id = IdLichHocChon();
        if (id == 0) { ThongBaoChon(); return; }
        var row = _gridLh.CurrentRow!;
        using var f = new frmLichHocEdit(
            id,
            Convert.ToInt32(row.Cells["LopHocID"].Value),
            row.Cells["NoiDung"].Value?.ToString(),
            row.Cells["ThoiGianBatDau"].Value is DateTime bd ? bd : null,
            row.Cells["ThoiGianKetThuc"].Value is DateTime kt ? kt : null,
            row.Cells["DiaDiem"].Value?.ToString());
        if (f.ShowDialog(FindForm()) == DialogResult.OK) TaiLichHoc();
    }

    private void XoaLichHoc()
    {
        var id = IdLichHocChon();
        if (id == 0) { ThongBaoChon(); return; }
        if (MessageBox.Show("Xóa buổi học này?", "Xác nhận", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
        _service.XoaLichHoc(id);
        TaiLichHoc();
    }

    // ------------------------------------------------------------------ helpers
    private static FlowLayoutPanel TaoToolbar(Control[] controls)
    {
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 58,
            Padding = new Padding(12, 10, 0, 0),
            BackColor = Color.White,
            WrapContents = false
        };
        foreach (var c in controls)
        {
            c.Margin = new Padding(0, 3, 8, 0);
            toolbar.Controls.Add(c);
        }
        return toolbar;
    }

    private static int GridId(DataGridView grid, string col)
    {
        if (grid.CurrentRow?.Cells[col].Value is int i) return i;
        if (grid.CurrentRow?.Cells[col].Value != null)
            return Convert.ToInt32(grid.CurrentRow.Cells[col].Value);
        return 0;
    }

    private static void ThongBaoChon() =>
        MessageBox.Show("Hãy chọn một dòng trong danh sách.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
}
