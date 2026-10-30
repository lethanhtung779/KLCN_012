using System.Data;
using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms.Forms.Teacher;

/// <summary>Quản lý đề thi và lịch thi theo quy trình tạo → trộn → duyệt → mở lịch.</summary>
public class frmExams : Form
{
    private readonly ExamService _service = new();
    private readonly DataGridView _gridDe = new();
    private readonly DataGridView _gridDot = new();
    private readonly SplitContainer _split = new();
    private readonly Label _lblDeDangChon = Ui.Label("Chọn một đề trong bảng để xem bước cần làm tiếp theo.", Ui.Small, Ui.TextMuted);
    private readonly Label _lblDot = Ui.Label("Lịch thi của đề đang chọn", Ui.H2, Ui.Navy);
    private readonly Button _btnTronDe = Ui.ButtonPrimary("Trộn mã đề", 145);
    private readonly Button _btnDuyet = Ui.ButtonAccent("Xem và duyệt", 170);
    private readonly Button _btnDotThi = Ui.ButtonPrimary("Tạo lịch thi", 150);
    private readonly Button _btnGoiDe = Ui.ButtonOutline("Đổi quyền làm bài", 165);
    private readonly Button _btnXoaDe = Ui.ButtonDanger("Xóa đề", 100);
    private readonly ToolTip _tips = new();

    public frmExams()
    {
        ExamService.YeuCauQuanTriVien();
        Text = "Đề thi & Lịch thi";
        Font = Ui.Body;

        var topArea = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.White,
            Padding = new Padding(16, 10, 12, 8)
        };
        var intro = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.White };
        var title = Ui.Label("Quản lý đề thi và lịch thi", Ui.H1, Ui.Navy);
        title.Location = new Point(0, 0);
        var subtitle = Ui.Label("Chọn một đề trong bảng để xem trạng thái, kiểm duyệt hoặc tạo lịch cho thí sinh.", Ui.Small, Ui.TextMuted);
        subtitle.Location = new Point(2, 31);
        intro.Controls.Add(title);
        intro.Controls.Add(subtitle);

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.White,
            WrapContents = true
        };
        var btnTHPTQG = Ui.ButtonAccent("Tạo đề chuẩn THPT", 195);
        btnTHPTQG.Click += (s, e) => TaoDe(giaoVienId => new frmTaoDeTHPTQG(giaoVienId));
        var btnTuDong = Ui.ButtonOutline("Tạo ngẫu nhiên", 145);
        btnTuDong.Click += (s, e) => TaoDe(giaoVienId => new frmTaoDeTuDong(giaoVienId));
        var btnThuCong = Ui.ButtonOutline("Tạo thủ công", 135);
        btnThuCong.Click += (s, e) => TaoDe(giaoVienId => new frmTaoDeThuCong(giaoVienId));
        _btnDotThi.Click += (s, e) => TaoDotThi();
        _btnTronDe.Click += (s, e) => MoManHinhTronDe();
        _btnXoaDe.Click += (s, e) => XoaDe();
        _btnGoiDe.Click += (s, e) => DoiGoiDe();
        var btnLamMoi = Ui.ButtonOutline("Làm mới", 105);
        _btnDuyet.Click += (_, _) => MoXemCacMaDe();
        btnLamMoi.Click += (s, e) => TaiLai();

        foreach (var c in new Control[] { btnTHPTQG, btnTuDong, btnThuCong, _btnTronDe, _btnDuyet, _btnDotThi, _btnGoiDe, btnLamMoi, _btnXoaDe })
        {
            c.Margin = new Padding(0, 3, 10, 0);
            toolbar.Controls.Add(c);
        }
        _tips.SetToolTip(btnTHPTQG, "Tạo đề theo đúng cấu trúc THPT của môn học. Đây là lựa chọn nên dùng.");
        _tips.SetToolTip(btnTuDong, "Tạo nhanh một đề bằng cách lấy ngẫu nhiên câu hỏi trong ngân hàng.");
        _tips.SetToolTip(btnThuCong, "Tự chọn từng câu hỏi đưa vào đề.");
        _tips.SetToolTip(_btnTronDe, "Tạo các mã đề khác nhau từ đề đang chọn.");
        _tips.SetToolTip(_btnDuyet, "Xem toàn bộ câu hỏi, đáp án và mã đề trước khi cho phép hiển thị trên Web.");
        _tips.SetToolTip(_btnDotThi, "Đặt thời gian mở, đóng và phạm vi thí sinh được làm đề.");
        _tips.SetToolTip(_btnGoiDe, "Chọn đề miễn phí cho mọi tài khoản hoặc chỉ dành cho tài khoản VIP.");

        topArea.Controls.Add(intro, 0, 0);
        topArea.Controls.Add(toolbar, 0, 1);

        _split.Dock = DockStyle.Fill;
        _split.Orientation = Orientation.Horizontal;
        _split.SplitterDistance = 300;

        // Grid de thi (tren)
        var headerDe = new Panel { Dock = DockStyle.Top, Height = 62, BackColor = Color.White };
        var lblDe = Ui.Label("Chọn đề cần thao tác", Ui.H2, Ui.Navy);
        lblDe.Location = new Point(16, 6);
        lblDe.AutoSize = true;
        _lblDeDangChon.Location = new Point(18, 34);
        _lblDeDangChon.AutoSize = true;
        headerDe.Controls.Add(lblDe);
        headerDe.Controls.Add(_lblDeDangChon);

        Ui.StyleGrid(_gridDe);
        _gridDe.Dock = DockStyle.Fill;
        _gridDe.SelectionChanged += (s, e) => TaiDotThi();
        _gridDe.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) MoXemCacMaDe(); };
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
        headerDot.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));
        headerDot.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        _lblDot.Dock = DockStyle.Fill;
        _lblDot.AutoSize = false;
        _lblDot.AutoEllipsis = true;
        _lblDot.TextAlign = ContentAlignment.MiddleLeft;
        _lblDot.Margin = new Padding(0, 0, 12, 0);
        var btnXemDiem = Ui.ButtonAccent("Bài làm và câu trả lời", 230, 32);
        btnXemDiem.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        btnXemDiem.Margin = new Padding(0, 3, 10, 0);
        btnXemDiem.Click += (s, e) => XemBangDiemDot();
        var btnDongDot = Ui.ButtonOutline("Đóng lịch thi", 130, 32);
        btnDongDot.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        btnDongDot.Margin = new Padding(0, 3, 0, 0);
        btnDongDot.Click += (s, e) => DongDot();
        headerDot.Controls.Add(_lblDot, 0, 0);
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
        Controls.Add(topArea);

        Load += (s, e) => { try { _service.DongDotThiQuaHan(); } catch { } TaiLai(); };
    }

    private void TaoDe(Func<int, Form> createForm)
    {
        using var form = createForm(0);
        if (form.ShowDialog(FindForm()) == DialogResult.OK) TaiLai();
    }

    private void TaiLai()
    {
        var danhSach = _service.LayDanhSachDe();
        danhSach.Columns.Add("TrangThaiWeb", typeof(string));
        foreach (DataRow row in danhSach.Rows)
        {
            var daDuyet = string.Equals(row["Duyet"]?.ToString(), "Đã duyệt", StringComparison.OrdinalIgnoreCase);
            var soDotThi = Convert.ToInt32(row["SoDotThi"]);
            row["TrangThaiWeb"] = !daDuyet
                ? "Chưa hiển thị — chờ duyệt"
                : soDotThi == 0
                    ? "Chưa hiển thị — chưa có lịch"
                    : "Đã có lịch trên Web";
        }
        _gridDe.DataSource = danhSach;
        Ui.RenameColumns(_gridDe,
            ("DeThiID", "ID"), ("TenDe", "Tên đề"), ("TenMon", "Môn"), ("LoaiDe", "Loại đề"),
            ("SoLuongCauHoi", "Số câu"), ("HinhThucTao", "Hình thức tạo"), ("QuyenThi", "Quyền thi"),
            ("Duyet", "Kiểm duyệt"), ("TrangThaiWeb", "Hiển thị trên Web"),
            ("SoDotThi", "Số lịch thi"), ("SoMaDe", "Số mã đề"));
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
        if (_gridDe.Columns.Contains("Duyet")) _gridDe.Columns["Duyet"]!.FillWeight = 85;
        if (_gridDe.Columns.Contains("TrangThaiWeb")) _gridDe.Columns["TrangThaiWeb"]!.FillWeight = 155;
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

        if (_gridDe.Columns.Contains("TrangThaiWeb"))
        {
            foreach (DataGridViewRow row in _gridDe.Rows)
            {
                var value = row.Cells["TrangThaiWeb"].Value?.ToString() ?? "";
                var cell = row.Cells["TrangThaiWeb"];
                cell.Style.Font = new Font(Ui.Body, FontStyle.Bold);
                cell.Style.ForeColor = value.StartsWith("Đã có", StringComparison.OrdinalIgnoreCase)
                    ? Ui.Success
                    : Ui.Warning;
            }
        }

        TaiDotThi();
    }

    private int DeIdDangChon() =>
        _gridDe.CurrentRow != null && _gridDe.CurrentRow.Cells["DeThiID"].Value != null
            ? Convert.ToInt32(_gridDe.CurrentRow.Cells["DeThiID"].Value)
            : 0;

    private void MoXemCacMaDe()
    {
        var id = DeIdDangChon();
        if (id <= 0)
        {
            MessageBox.Show("Hãy chọn một đề thi để xem các mã đề 101, 102, 103...", "Chưa chọn đề",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var soMaDe = Convert.ToInt32(_gridDe.CurrentRow!.Cells["SoMaDe"].Value);
        if (soMaDe == 0)
        {
            var result = MessageBox.Show(
                "Đề này chưa có mã đề nên chưa thể xem và duyệt.\n\nBạn có muốn mở màn hình trộn mã đề ngay không?",
                "Cần trộn mã đề trước", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (result == DialogResult.Yes) MoManHinhTronDe();
            return;
        }
        using var review = new frmExamReview(id);
        review.ShowDialog(this);
        TaiLai();
    }

    private void TaiDotThi()
    {
        var id = DeIdDangChon();
        _gridDot.DataSource = id == 0 ? new DataTable() : _service.LayDanhSachDotThi(id);
        Ui.RenameColumns(_gridDot,
            ("DotThiID", "ID"), ("TenDotThi", "Tên lịch thi"), ("TenDe", "Đề"),
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
        CapNhatHuongDan();
    }

    private void CapNhatHuongDan()
    {
        var row = _gridDe.CurrentRow;
        var coDe = row?.Cells["DeThiID"].Value is not null;
        _btnTronDe.Enabled = coDe;
        _btnDuyet.Enabled = coDe;
        _btnDotThi.Enabled = coDe;
        _btnGoiDe.Enabled = coDe;
        _btnXoaDe.Enabled = coDe;

        if (!coDe)
        {
            _lblDeDangChon.Text = "Chưa có đề nào. Hãy bắt đầu bằng nút 'Tạo đề chuẩn THPT'.";
            _lblDeDangChon.ForeColor = Ui.TextMuted;
            _lblDot.Text = "Chưa có đề được chọn";
            return;
        }

        var tenDe = row!.Cells["TenDe"].Value?.ToString() ?? "";
        var daDuyet = string.Equals(row.Cells["Duyet"].Value?.ToString(), "Đã duyệt", StringComparison.OrdinalIgnoreCase);
        var soMaDe = Convert.ToInt32(row.Cells["SoMaDe"].Value);
        var soDotThi = Convert.ToInt32(row.Cells["SoDotThi"].Value);

        string next;
        Color color;
        if (soMaDe == 0)
        {
            next = "Cần làm tiếp: bấm 'Trộn mã đề'.";
            color = Ui.Warning;
        }
        else if (!daDuyet)
        {
            next = "Cần làm tiếp: bấm 'Xem và duyệt' để đề được phép lên Web.";
            color = Ui.Warning;
        }
        else if (soDotThi == 0)
        {
            next = "Cần làm tiếp: bấm 'Tạo lịch thi' để thí sinh nhìn thấy và làm đề.";
            color = Ui.Warning;
        }
        else
        {
            next = "Hoàn tất: đề đã duyệt và đã có lịch thi trên Web.";
            color = Ui.Success;
        }

        _lblDeDangChon.Text = $"Đang chọn: {tenDe}  •  {next}";
        _lblDeDangChon.ForeColor = color;
        _btnDuyet.Text = daDuyet ? "Xem nội dung đề" : "Xem và duyệt";
        _lblDot.Text = soDotThi == 0
            ? $"Lịch thi của '{tenDe}' — chưa có lịch"
            : $"Lịch thi của '{tenDe}' — {soDotThi} lịch";
    }

    private int DotIdDangChon() =>
        _gridDot.CurrentRow != null && _gridDot.CurrentRow.Cells["DotThiID"].Value != null
            ? Convert.ToInt32(_gridDot.CurrentRow.Cells["DotThiID"].Value)
            : 0;

    private void TaoDotThi()
    {
        var deId = DeIdDangChon();
        if (deId == 0) { MessageBox.Show("Hãy chọn một đề thi phía trên.", "Thông báo"); return; }
        if (Convert.ToInt32(_gridDe.CurrentRow!.Cells["SoMaDe"].Value) == 0)
        {
            var tron = MessageBox.Show(
                "Đề này chưa có mã đề. Hãy trộn mã đề trước khi tạo lịch thi.\n\nMở màn hình trộn mã đề ngay?",
                "Thiếu mã đề", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (tron == DialogResult.Yes) MoManHinhTronDe();
            return;
        }
        var daDuyet = string.Equals(_gridDe.CurrentRow!.Cells["Duyet"].Value?.ToString(), "Đã duyệt", StringComparison.OrdinalIgnoreCase);
        if (!daDuyet)
        {
            var result = MessageBox.Show(
                "Đề này chưa được duyệt nên tạo lịch lúc này cũng chưa thể hiển thị cho thí sinh.\n\nBạn có muốn mở màn hình xem nội dung và duyệt đề ngay không?",
                "Cần duyệt đề trước", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (result == DialogResult.Yes) MoXemCacMaDe();
            return;
        }
        var tenDe = _gridDe.CurrentRow!.Cells["TenDe"].Value?.ToString() ?? "";
        using var f = new frmDotThiEdit(deId, tenDe);
        if (f.ShowDialog(FindForm()) == DialogResult.OK) TaiLai();
    }

    private void XemBangDiemDot()
    {
        var id = DotIdDangChon();
        if (id == 0) { MessageBox.Show("Hãy chọn một lịch thi phía dưới để xem bài làm.", "Thông báo"); return; }
        using var f = new frmStatistics(id);
        f.ShowDialog(FindForm());
    }

    private void DongDot()
    {
        var id = DotIdDangChon();
        if (id == 0) { MessageBox.Show("Hãy chọn một lịch thi.", "Thông báo"); return; }
        if (MessageBox.Show("Đóng lịch thi này ngay? Thí sinh sẽ không thể vào làm bài.", "Xác nhận",
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
