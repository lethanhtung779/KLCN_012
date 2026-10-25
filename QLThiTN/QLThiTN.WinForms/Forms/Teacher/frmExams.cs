using System.Data;
using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms.Forms.Teacher;

/// <summary>Quan ly de thi & ky thi: danh sach de, tao de tu dong/thu cong, tao dot thi.</summary>
public class frmExams : Form
{
    private readonly ExamService _service = new();
    private readonly DataGridView _gridDe = new();
    private readonly DataGridView _gridDot = new();
    private readonly SplitContainer _split = new();

    public frmExams()
    {
        ExamService.YeuCauQuanTriVien();
        Text = "Đề thi & Kỳ thi";
        Font = Ui.Body;

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(16, 10, 0, 0),
            BackColor = Color.White,
            WrapContents = true
        };
        var btnTHPTQG = Ui.ButtonAccent("🎓 Tạo đề chuẩn THPTQG", 220);
        btnTHPTQG.Click += (s, e) => TaoDe(giaoVienId => new frmTaoDeTHPTQG(giaoVienId));
        var btnTuDong = Ui.ButtonPrimary("Tạo đề ngẫu nhiên", 190);
        btnTuDong.Click += (s, e) => TaoDe(giaoVienId => new frmTaoDeTuDong(giaoVienId));
        var btnThuCong = Ui.ButtonPrimary("✎ Tạo đề thủ công", 190);
        btnThuCong.Click += (s, e) => TaoDe(giaoVienId => new frmTaoDeThuCong(giaoVienId));
        var btnDotThi = Ui.ButtonPrimary("📅 Tạo đợt thi", 160);
        btnDotThi.Click += (s, e) => TaoDotThi();
        var btnTronDe = Ui.ButtonPrimary("🔀 Trộn mã đề", 160);
        btnTronDe.Click += (s, e) => MoManHinhTronDe();
        var btnXoaDe = Ui.ButtonDanger("🗑 Xóa đề", 110);
        btnXoaDe.Click += (s, e) => XoaDe();
        var btnGoiDe = Ui.ButtonAccent("★ Miễn phí / VIP", 170);
        btnGoiDe.Click += (s, e) => DoiGoiDe();
        var btnLamMoi = Ui.ButtonOutline("🔄 Làm mới", 120);
        btnLamMoi.Click += (s, e) => TaiLai();

        foreach (var c in new Control[] { btnTHPTQG, btnTuDong, btnThuCong, btnDotThi, btnTronDe, btnGoiDe, btnLamMoi, btnXoaDe })
        {
            c.Margin = new Padding(0, 3, 10, 0);
            toolbar.Controls.Add(c);
        }

        _split.Dock = DockStyle.Fill;
        _split.Orientation = Orientation.Horizontal;
        _split.SplitterDistance = 300;

        // Grid de thi (tren)
        var headerDe = new Panel { Dock = DockStyle.Top, Height = 38, BackColor = Color.White };
        var lblDe = Ui.Label("Danh sách đề thi", Ui.H2, Ui.Navy);
        lblDe.Location = new Point(16, 8);
        lblDe.AutoSize = true;
        headerDe.Controls.Add(lblDe);

        Ui.StyleGrid(_gridDe);
        _gridDe.Dock = DockStyle.Fill;
        _gridDe.SelectionChanged += (s, e) => TaiDotThi();
        _gridDe.DataError += (s, e) => { e.ThrowException = false; };

        var panelDe = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        panelDe.Controls.Add(_gridDe);
        panelDe.Controls.Add(headerDe);
        _gridDe.BringToFront();
        _split.Panel1.Controls.Add(panelDe);

        // Grid dot thi (duoi)
        var headerDot = new TableLayoutPanel
        {
            Dock = DockStyle.Top, Height = 52, ColumnCount = 3, RowCount = 1,
            Padding = new Padding(16, 6, 12, 6), BackColor = Color.White
        };
        headerDot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        headerDot.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        headerDot.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        var lblDot = Ui.Label("Các đợt thi của đề đang chọn (chọn đề phía trên)", Ui.H2, Ui.Navy);
        lblDot.Dock = DockStyle.Fill;
        lblDot.AutoSize = false;
        lblDot.AutoEllipsis = true;
        lblDot.TextAlign = ContentAlignment.MiddleLeft;
        lblDot.Margin = new Padding(0, 0, 12, 0);
        var btnXemDiem = Ui.ButtonAccent("📊 Xem bảng điểm", 160, 32);
        btnXemDiem.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        btnXemDiem.Margin = new Padding(0, 3, 10, 0);
        btnXemDiem.Click += (s, e) => XemBangDiemDot();
        var btnDongDot = Ui.ButtonOutline("Đóng đợt thi", 130, 32);
        btnDongDot.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        btnDongDot.Margin = new Padding(0, 3, 0, 0);
        btnDongDot.Click += (s, e) => DongDot();
        headerDot.Controls.Add(lblDot, 0, 0);
        headerDot.Controls.Add(btnXemDiem, 1, 0);
        headerDot.Controls.Add(btnDongDot, 2, 0);

        Ui.StyleGrid(_gridDot);
        _gridDot.ColumnHeadersHeight = 46;
        _gridDot.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _gridDot.Dock = DockStyle.Fill;
        _gridDot.DataError += (s, e) => { e.ThrowException = false; };

        var panelDot = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        panelDot.Controls.Add(_gridDot);
        panelDot.Controls.Add(headerDot);
        _gridDot.BringToFront();
        _split.Panel2.Controls.Add(panelDot);
        _split.Panel2.BackColor = Color.White;

        Controls.Add(_split);
        Controls.Add(toolbar);

        Load += (s, e) => { try { _service.DongDotThiQuaHan(); } catch { } TaiLai(); };
    }

    private void TaoDe(Func<int, Form> createForm)
    {
        using var chooser = new Form
        {
            Text = "Giáo viên phụ trách đề thi", ClientSize = new Size(510, 180),
            StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false, Font = Ui.Body
        };
        var teachers = Database.Query(@"SELECT gv.GiaoVienID, gv.HoTen + N' (' + tk.TenDangNhap + N')' AS Ten
            FROM GiaoVien gv JOIN TaiKhoan tk ON tk.TaiKhoanID = gv.TaiKhoanID
            WHERE tk.TrangThai = N'HoatDong' AND tk.VaiTro = N'GiaoVien' ORDER BY gv.HoTen");
        if (teachers.Rows.Count == 0) { MessageBox.Show("Chưa có giáo viên đang hoạt động để phụ trách đề thi."); return; }
        var label = Ui.Label("Chọn giáo viên phụ trách đề thi:", Ui.Body, Ui.Navy);
        label.Location = new Point(20, 20);
        var combo = Ui.Combo(460); combo.Location = new Point(20, 50);
        combo.DisplayMember = "Ten"; combo.ValueMember = "GiaoVienID"; combo.DataSource = teachers; combo.SelectedIndex = -1;
        var next = Ui.ButtonAccent("Tiếp tục", 130); next.Location = new Point(20, 108); next.Enabled = false;
        combo.SelectedIndexChanged += (_, _) => next.Enabled = combo.SelectedIndex >= 0;
        next.DialogResult = DialogResult.OK;
        var cancel = Ui.ButtonOutline("Hủy", 100); cancel.Location = new Point(165, 108); cancel.DialogResult = DialogResult.Cancel;
        chooser.Controls.AddRange([label, combo, next, cancel]); chooser.AcceptButton = next; chooser.CancelButton = cancel;
        if (chooser.ShowDialog(FindForm()) != DialogResult.OK || combo.SelectedValue == null) return;
        using var form = createForm(Convert.ToInt32(combo.SelectedValue));
        if (form.ShowDialog(FindForm()) == DialogResult.OK) TaiLai();
    }

    private void TaiLai()
    {
        _gridDe.DataSource = _service.LayDanhSachDe();
        Ui.RenameColumns(_gridDe,
            ("DeThiID", "ID"), ("TenDe", "Tên đề"), ("TenMon", "Môn"), ("LoaiDe", "Loại đề"),
            ("SoLuongCauHoi", "Số câu"), ("HinhThucTao", "Hình thức tạo"), ("QuyenThi", "Quyền thi"),
            ("SoDotThi", "Số đợt thi"), ("SoMaDe", "Số mã đề"));
        Ui.HideColumns(_gridDe, "DeThiID", "LaMienPhi");
        if (_gridDe.Columns.Contains("LoaiDe"))
            foreach (DataGridViewRow row in _gridDe.Rows)
                row.Cells["LoaiDe"].Value = row.Cells["LoaiDe"].Value switch
                {
                    "OnTap" => "Bài tập về nhà",
                    "ThiThu" => "Thi thử",
                    "ChinhThuc" => "Chính thức",
                    var x => x
                };
        if (_gridDe.Columns.Contains("HinhThucTao"))
            foreach (DataGridViewRow row in _gridDe.Rows)
                row.Cells["HinhThucTao"].Value = row.Cells["HinhThucTao"].Value switch
                {
                    "MaTranTHPTQG" => "Chuẩn THPTQG",
                    "TuDong" => "Tự động",
                    "ThuCong" => "Thủ công",
                    var x => x
                };

        if (_gridDe.Columns.Contains("TenDe")) _gridDe.Columns["TenDe"]!.FillWeight = 220;
        if (_gridDe.Columns.Contains("TenMon")) _gridDe.Columns["TenMon"]!.FillWeight = 90;
        if (_gridDe.Columns.Contains("LoaiDe")) _gridDe.Columns["LoaiDe"]!.FillWeight = 95;
        if (_gridDe.Columns.Contains("SoLuongCauHoi"))
        {
            _gridDe.Columns["SoLuongCauHoi"]!.FillWeight = 65;
            _gridDe.Columns["SoLuongCauHoi"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _gridDe.Columns["SoLuongCauHoi"]!.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
        }
        if (_gridDe.Columns.Contains("HinhThucTao")) _gridDe.Columns["HinhThucTao"]!.FillWeight = 110;
        if (_gridDe.Columns.Contains("QuyenThi")) _gridDe.Columns["QuyenThi"]!.FillWeight = 110;
        if (_gridDe.Columns.Contains("SoDotThi"))
        {
            _gridDe.Columns["SoDotThi"]!.FillWeight = 75;
            _gridDe.Columns["SoDotThi"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _gridDe.Columns["SoDotThi"]!.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
        }
        if (_gridDe.Columns.Contains("SoMaDe"))
        {
            _gridDe.Columns["SoMaDe"]!.FillWeight = 75;
            _gridDe.Columns["SoMaDe"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _gridDe.Columns["SoMaDe"]!.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
        }

        TaiDotThi();
    }

    private int DeIdDangChon() =>
        _gridDe.CurrentRow != null && _gridDe.CurrentRow.Cells["DeThiID"].Value != null
            ? Convert.ToInt32(_gridDe.CurrentRow.Cells["DeThiID"].Value)
            : 0;

    private void TaiDotThi()
    {
        var id = DeIdDangChon();
        _gridDot.DataSource = id == 0 ? new DataTable() : _service.LayDanhSachDotThi(id);
        Ui.RenameColumns(_gridDot,
            ("DotThiID", "ID"), ("TenDotThi", "Tên đợt thi"), ("TenDe", "Đề"),
            ("ThoiGianMoCong", "Mở cổng"), ("ThoiGianDongCong", "Đóng cổng"),
            ("ThoiLuongLamBai", "Thời lượng (phút)"), ("GioiHanSoLuong", "Giới hạn SL"),
            ("CongBoDiemSom", "Công bố điểm sớm"), ("TrangThai", "Trạng thái"),
            ("PhamVi", "Phạm vi"), ("SoLanThiToiDa", "Lần thi tối đa"),
            ("SoDangKy", "Đã đăng ký"), ("SoBaiDaNop", "Bài đã nộp"));
        Ui.HideColumns(_gridDot, "DotThiID", "TenDe");

        if (_gridDot.Columns.Contains("ThoiGianMoCong"))
            _gridDot.Columns["ThoiGianMoCong"]!.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
        if (_gridDot.Columns.Contains("ThoiGianDongCong"))
            _gridDot.Columns["ThoiGianDongCong"]!.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";

        if (_gridDot.Columns.Contains("TrangThai"))
            foreach (DataGridViewRow row in _gridDot.Rows)
                row.Cells["TrangThai"].Value = row.Cells["TrangThai"].Value switch
                {
                    "SapMo" => "Sắp mở",
                    "DangMo" => "Đang mở",
                    "DaDong" => "Đã đóng",
                    var x => x
                };

        if (_gridDot.Columns.Contains("PhamVi"))
            foreach (DataGridViewRow row in _gridDot.Rows)
                row.Cells["PhamVi"].Value = row.Cells["PhamVi"].Value switch
                {
                    "ToanTruong" => "Toàn trung tâm",
                    "TheoLop" => "Theo lớp",
                    "CongKhai" => "Công khai",
                    "NoiBo" => "Nội bộ",
                    var x => x
                };

        if (_gridDot.Columns.Contains("TenDotThi")) _gridDot.Columns["TenDotThi"]!.FillWeight = 160;
        if (_gridDot.Columns.Contains("ThoiGianMoCong")) _gridDot.Columns["ThoiGianMoCong"]!.FillWeight = 120;
        if (_gridDot.Columns.Contains("ThoiGianDongCong")) _gridDot.Columns["ThoiGianDongCong"]!.FillWeight = 120;
        if (_gridDot.Columns.Contains("ThoiLuongLamBai")) _gridDot.Columns["ThoiLuongLamBai"]!.FillWeight = 90;
        if (_gridDot.Columns.Contains("GioiHanSoLuong")) _gridDot.Columns["GioiHanSoLuong"]!.FillWeight = 75;
        if (_gridDot.Columns.Contains("CongBoDiemSom")) _gridDot.Columns["CongBoDiemSom"]!.FillWeight = 85;
        if (_gridDot.Columns.Contains("TrangThai")) _gridDot.Columns["TrangThai"]!.FillWeight = 80;
        if (_gridDot.Columns.Contains("PhamVi")) _gridDot.Columns["PhamVi"]!.FillWeight = 85;
        if (_gridDot.Columns.Contains("SoLanThiToiDa")) _gridDot.Columns["SoLanThiToiDa"]!.FillWeight = 75;
        if (_gridDot.Columns.Contains("SoDangKy")) _gridDot.Columns["SoDangKy"]!.FillWeight = 75;
        if (_gridDot.Columns.Contains("SoBaiDaNop")) _gridDot.Columns["SoBaiDaNop"]!.FillWeight = 75;

        foreach (var col in new[] { "ThoiLuongLamBai", "GioiHanSoLuong", "TrangThai", "PhamVi", "SoLanThiToiDa", "SoDangKy", "SoBaiDaNop" })
        {
            if (_gridDot.Columns.Contains(col))
            {
                _gridDot.Columns[col]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                _gridDot.Columns[col]!.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
        }
    }

    private int DotIdDangChon() =>
        _gridDot.CurrentRow != null && _gridDot.CurrentRow.Cells["DotThiID"].Value != null
            ? Convert.ToInt32(_gridDot.CurrentRow.Cells["DotThiID"].Value)
            : 0;

    private void TaoDotThi()
    {
        var deId = DeIdDangChon();
        if (deId == 0) { MessageBox.Show("Hãy chọn một đề thi phía trên.", "Thông báo"); return; }
        var tenDe = _gridDe.CurrentRow!.Cells["TenDe"].Value?.ToString() ?? "";
        using var f = new frmDotThiEdit(deId, tenDe);
        if (f.ShowDialog(FindForm()) == DialogResult.OK) TaiLai();
    }

    private void XemBangDiemDot()
    {
        var id = DotIdDangChon();
        if (id == 0) { MessageBox.Show("Hãy chọn một đợt thi phía dưới để xem bảng điểm.", "Thông báo"); return; }
        using var f = new frmStatistics(id);
        f.ShowDialog(FindForm());
    }

    private void DongDot()
    {
        var id = DotIdDangChon();
        if (id == 0) { MessageBox.Show("Hãy chọn một đợt thi.", "Thông báo"); return; }
        if (MessageBox.Show("Đóng đợt thi này ngay? Thí sinh sẽ không thể vào thi.", "Xác nhận",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        _service.CapNhatTrangThaiDot(id, "DaDong");
        TaiDotThi();
    }

    private static bool LaDeMienPhi(object? value)
    {
        if (value is bool b) return b;
        if (value is null || value == DBNull.Value) return false;
        return Convert.ToInt32(value) != 0;
    }

    private void DoiGoiDe()
    {
        var id = DeIdDangChon();
        if (id == 0) { MessageBox.Show("Hãy chọn một đề thi.", "Thông báo"); return; }
        var ten = _gridDe.CurrentRow!.Cells["TenDe"].Value?.ToString() ?? "";
        var dangMienPhi = LaDeMienPhi(_gridDe.CurrentRow.Cells["LaMienPhi"].Value);
        var sangMienPhi = !dangMienPhi;
        var hoi = sangMienPhi
            ? $"Đánh dấu đề '{ten}' là MIỄN PHÍ?\nMọi tài khoản đều làm được đề này."
            : $"Đánh dấu đề '{ten}' là ĐÓNG PHÍ (VIP)?\nChỉ tài khoản đã đóng phí mới làm được.";
        if (MessageBox.Show(hoi, "Quyền thi", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;
        _service.DatMienPhi(id, sangMienPhi);
        TaiLai();
    }

    private void XoaDe()
    {
        var id = DeIdDangChon();
        if (id == 0) { MessageBox.Show("Hãy chọn một đề thi.", "Thông báo"); return; }
        var ten = _gridDe.CurrentRow!.Cells["TenDe"].Value;
        if (MessageBox.Show($"Xóa đề '{ten}' cùng đợt thi và đăng ký liên quan? Đề đã có bài làm sẽ không thể xóa.\nHành động này KHÔNG THỂ hoàn tác!",
                "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        _service.XoaDe(id);
        TaiLai();
    }

    private void MoManHinhTronDe()
    {
        try
        {
            var deId = DeIdDangChon();
            if (deId == 0)
            {
                MessageBox.Show("Hãy nhấp chọn một đề thi trong bảng danh sách phía trên trước khi bấm 'Trộn mã đề'.", "Chưa chọn đề thi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var tenDe = _gridDe.CurrentRow!.Cells["TenDe"].Value?.ToString() ?? "";
            using var f = new frmTronDe(deId, tenDe);
            var owner = this.TopLevelControl as Form ?? FindForm() ?? this;
            f.ShowDialog(owner);
            TaiLai();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể mở màn hình trộn đề:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
