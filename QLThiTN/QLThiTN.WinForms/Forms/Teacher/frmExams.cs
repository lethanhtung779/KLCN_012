using System.Text;
using System.Data;
using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms.Forms.Teacher;

/// <summary>Quản lý đề thi và lịch thi theo quy trình tạo → trộn → duyệt → mở lịch.</summary>
public class frmExams : Form
{
    private readonly ExamService _service = new();
    private readonly TabControl _tabsChinh = new();
    private readonly DataGridView _gridDe = new();
    private readonly DataGridView _gridDot = new();
    private readonly DataGridView _gridTatCaDot = new();
    private readonly DataGridView _gridDeTuongUng = new();
    private readonly SplitContainer _split = new();
    private readonly SplitContainer _splitTatCaDot = new();
    private readonly Label _lblDeDangChon = Ui.Label("Chọn một đề trong bảng để xem bước cần làm tiếp theo.", Ui.Small, Ui.TextMuted);
    private readonly Label _lblDot = Ui.Label("Lịch thi của đề đang chọn", Ui.H2, Ui.Navy);
    private readonly Label _lblTatCaDot = Ui.Label("Tất cả đợt thi", Ui.H2, Ui.Navy);
    private readonly Label _lblDeTuongUng = Ui.Label("Đề thi tương ứng", Ui.H2, Ui.Navy);
    private readonly Button _btnTronDe = Ui.ButtonPrimary("Trộn mã đề", 145);
    private readonly Button _btnDuyet = Ui.ButtonAccent("Xem và duyệt", 170);
    private readonly Button _btnDotThi = Ui.ButtonPrimary("Tạo lịch thi", 150);
    private readonly Button _btnGoiDe = Ui.ButtonOutline("Đổi quyền làm bài", 165);
    private readonly Button _btnXoaDe = Ui.ButtonDanger("Xóa đề", 100);
    private readonly ToolTip _tips = new();
    private int _examLoadVersion;
    private int _sessionLoadVersion;
    private int _allSessionLoadVersion;
    private int _matchingExamLoadVersion;
    private bool _bindingExams;
    private bool _bindingAllSessions;

    public frmExams()
    {
        ExamService.YeuCauQuanTriVienHoacGiaoVien();
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
        var btnLamMoi = Ui.ButtonOutline("Làm mới", 105);
        _btnDuyet.Click += (_, _) => MoXemCacMaDe();
        btnLamMoi.Click += (s, e) => TaiLai();

        var btnInDe = Ui.ButtonOutline("Xuất bản in đề", 150);
        btnInDe.Click += (s, e) => XuatBanInDe();
        var btnXuatJson = Ui.ButtonOutline("Xuất file JSON", 145);
        btnXuatJson.Click += (s, e) => XuatJsonDe();

        foreach (var c in new Control[] { btnTHPTQG, btnTuDong, btnThuCong, _btnTronDe, _btnDuyet, btnInDe, btnXuatJson, _btnDotThi, btnLamMoi, _btnXoaDe })
        {
            c.Margin = new Padding(0, 3, 10, 0);
            toolbar.Controls.Add(c);
        }

        if (Session.IsGiaoVien)
        {
            _btnDuyet.Text = "Xem các mã đề";
            _btnDotThi.Visible = false;
            _btnXoaDe.Visible = false;
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
        _gridDe.SelectionChanged += (_, _) => { if (!_bindingExams) TaiDotThi(); };
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
            Dock = DockStyle.Top, Height = 52, ColumnCount = 4, RowCount = 1,
            Padding = new Padding(16, 6, 12, 6), BackColor = Color.White
        };
        headerDot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        headerDot.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        headerDot.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        headerDot.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        _lblDot.Dock = DockStyle.Fill;
        _lblDot.AutoSize = false;
        _lblDot.AutoEllipsis = true;
        _lblDot.TextAlign = ContentAlignment.MiddleLeft;
        _lblDot.Margin = new Padding(0, 0, 12, 0);
        var btnDoiGoiDot = Ui.ButtonOutline("Đổi VIP / Miễn phí", 170, 32);
        btnDoiGoiDot.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        btnDoiGoiDot.Margin = new Padding(0, 3, 10, 0);
        btnDoiGoiDot.Click += (s, e) => DoiGoiDot();
        var btnXemDiem = Ui.ButtonAccent("Bài làm và câu trả lời", 210, 32);
        btnXemDiem.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        btnXemDiem.Margin = new Padding(0, 3, 10, 0);
        btnXemDiem.Click += (s, e) => XemBangDiemDot();
        var btnDongDot = Ui.ButtonOutline("Đóng lịch thi", 130, 32);
        btnDongDot.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        btnDongDot.Margin = new Padding(0, 3, 0, 0);
        btnDongDot.Click += (s, e) => DongDot();
        headerDot.Controls.Add(_lblDot, 0, 0);
        headerDot.Controls.Add(btnDoiGoiDot, 1, 0);
        headerDot.Controls.Add(btnXemDiem, 2, 0);
        headerDot.Controls.Add(btnDongDot, 3, 0);

        Ui.StyleGrid(_gridDot);
        _gridDot.ColumnHeadersHeight = 46;
        _gridDot.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _gridDot.Dock = DockStyle.Fill;
        _gridDot.DataError += (s, e) => { e.ThrowException = false; };
        _gridDot.CellContentClick += (_, e) => DoiCongBoDiem(_gridDot, e, TaiDotThi);

        var panelDot = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        panelDot.Controls.Add(_gridDot);
        panelDot.Controls.Add(headerDot);
        _gridDot.BringToFront();
        _split.Panel2.Controls.Add(panelDot);
        _split.Panel2.BackColor = Color.White;

        // Tab 2: chọn một đợt thi phía trên để xem đề tương ứng phía dưới.
        _splitTatCaDot.Dock = DockStyle.Fill;
        _splitTatCaDot.Orientation = Orientation.Horizontal;
        _splitTatCaDot.SplitterDistance = 380;

        var headerTatCaDot = new TableLayoutPanel
        {
            Dock = DockStyle.Top, Height = 96, ColumnCount = 1, RowCount = 2,
            Padding = new Padding(16, 5, 12, 5), BackColor = Color.White
        };
        headerTatCaDot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        headerTatCaDot.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        headerTatCaDot.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        _lblTatCaDot.Dock = DockStyle.Fill;
        _lblTatCaDot.AutoSize = false;
        _lblTatCaDot.TextAlign = ContentAlignment.MiddleLeft;
        _lblTatCaDot.AutoEllipsis = true;
        var actionsTatCa = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false, Margin = Padding.Empty, BackColor = Color.White
        };
        var btnThemTatCa = Ui.ButtonAccent("Thêm đợt thi", 125, 32);
        var btnSuaTatCa = Ui.ButtonOutline("Chỉnh sửa", 110, 32);
        var btnXoaTatCa = Ui.ButtonDanger("Xóa đợt thi", 120, 32);
        var btnDoiGoiTatCa = Ui.ButtonOutline("Đổi VIP / Miễn phí", 170, 32);
        var btnXemDiemTatCa = Ui.ButtonAccent("Bài làm và câu trả lời", 210, 32);
        var btnDongTatCa = Ui.ButtonOutline("Đóng lịch thi", 130, 32);
        btnThemTatCa.Click += (_, _) => ThemTatCaDot();
        btnSuaTatCa.Click += (_, _) => SuaTatCaDot();
        btnXoaTatCa.Click += (_, _) => XoaTatCaDot();
        btnDoiGoiTatCa.Click += (_, _) => DoiGoiTatCaDot();
        btnXemDiemTatCa.Click += (_, _) => XemBangDiemTatCaDot();
        btnDongTatCa.Click += (_, _) => DongTatCaDot();
        foreach (var button in new[] { btnDongTatCa, btnXemDiemTatCa, btnDoiGoiTatCa, btnXoaTatCa, btnSuaTatCa, btnThemTatCa })
        {
            button.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            button.Margin = new Padding(0, 3, 8, 0);
            actionsTatCa.Controls.Add(button);
        }
        btnThemTatCa.Visible = btnSuaTatCa.Visible = btnXoaTatCa.Visible = Session.IsQuanTriVien;
        headerTatCaDot.Controls.Add(_lblTatCaDot, 0, 0);
        headerTatCaDot.Controls.Add(actionsTatCa, 0, 1);

        Ui.StyleGrid(_gridTatCaDot);
        _gridTatCaDot.Dock = DockStyle.Fill;
        _gridTatCaDot.ColumnHeadersHeight = 46;
        _gridTatCaDot.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _gridTatCaDot.DataError += (_, e) => { e.ThrowException = false; };
        _gridTatCaDot.CellContentClick += (_, e) => DoiCongBoDiem(_gridTatCaDot, e, TaiTatCaDotThi);
        _gridTatCaDot.SelectionChanged += (_, _) => { if (!_bindingAllSessions) TaiDeTuongUng(); };
        _gridTatCaDot.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) XemBangDiemTatCaDot(); };
        var panelTatCaDot = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        panelTatCaDot.Controls.Add(_gridTatCaDot);
        panelTatCaDot.Controls.Add(headerTatCaDot);
        _gridTatCaDot.BringToFront();
        _splitTatCaDot.Panel1.Controls.Add(panelTatCaDot);

        var headerDeTuongUng = new TableLayoutPanel
        {
            Dock = DockStyle.Top, Height = 52, ColumnCount = 2, RowCount = 1,
            Padding = new Padding(16, 7, 12, 7), BackColor = Color.White
        };
        headerDeTuongUng.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        headerDeTuongUng.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        _lblDeTuongUng.Dock = DockStyle.Fill;
        _lblDeTuongUng.AutoSize = false;
        _lblDeTuongUng.TextAlign = ContentAlignment.MiddleLeft;
        var btnXemDeTuongUng = Ui.ButtonOutline("Xem nội dung đề", 170, 32);
        btnXemDeTuongUng.Click += (_, _) => MoDeTuongUng();
        headerDeTuongUng.Controls.Add(_lblDeTuongUng, 0, 0);
        headerDeTuongUng.Controls.Add(btnXemDeTuongUng, 1, 0);

        Ui.StyleGrid(_gridDeTuongUng);
        _gridDeTuongUng.Dock = DockStyle.Fill;
        _gridDeTuongUng.ColumnHeadersHeight = 46;
        _gridDeTuongUng.DataError += (_, e) => { e.ThrowException = false; };
        _gridDeTuongUng.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) MoDeTuongUng(); };
        var panelDeTuongUng = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        panelDeTuongUng.Controls.Add(_gridDeTuongUng);
        panelDeTuongUng.Controls.Add(headerDeTuongUng);
        _gridDeTuongUng.BringToFront();
        _splitTatCaDot.Panel2.Controls.Add(panelDeTuongUng);

        var tabDeThi = new TabPage("Đề thi → Lịch thi") { BackColor = Color.White };
        var tabTatCaDot = new TabPage("Tất cả đợt thi → Đề tương ứng") { BackColor = Color.White };
        tabDeThi.Controls.Add(_split);
        tabDeThi.Controls.Add(topArea);
        tabTatCaDot.Controls.Add(_splitTatCaDot);
        _tabsChinh.Dock = DockStyle.Fill;
        _tabsChinh.SizeMode = TabSizeMode.Fixed;
        _tabsChinh.ItemSize = new Size(280, 42);
        _tabsChinh.Font = new Font(Ui.Body, FontStyle.Bold);
        _tabsChinh.TabPages.Add(tabDeThi);
        _tabsChinh.TabPages.Add(tabTatCaDot);
        _tabsChinh.SelectedIndexChanged += (_, _) =>
        {
            if (_tabsChinh.SelectedIndex == 1) TaiTatCaDotThi();
        };
        Controls.Add(_tabsChinh);

        Load += async (s, e) =>
        {
            try { await Task.Run(_service.DongDotThiQuaHan); } catch { }
            if (!IsDisposed && !Disposing) TaiLai();
        };
    }

    private void TaoDe(Func<int, Form> createForm)
    {
        using var form = createForm(0);
        if (form.ShowDialog(FindForm()) == DialogResult.OK) TaiLai();
    }

    private async void TaiLai()
    {
        var version = ++_examLoadVersion;
        _lblDeDangChon.Text = "Đang tải danh sách đề thi...";
        DataTable danhSach;
        try
        {
            danhSach = await Task.Run(_service.LayDanhSachDe);
            if (IsDisposed || Disposing || version != _examLoadVersion) return;
        }
        catch (Exception ex)
        {
            if (!IsDisposed && !Disposing && version == _examLoadVersion)
                _lblDeDangChon.Text = "Không tải được danh sách đề thi: " + ex.Message;
            return;
        }
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
        _bindingExams = true;
        try { _gridDe.DataSource = danhSach; }
        finally { _bindingExams = false; }
        Ui.RenameColumns(_gridDe,
            ("DeThiID", "ID"), ("TenDe", "Tên đề"), ("TenMon", "Môn"), ("LoaiDe", "Loại đề"),
            ("SoLuongCauHoi", "Số câu"), ("HinhThucTao", "Hình thức tạo"), ("QuyenThi", "Quyền thi"),
            ("Duyet", "Kiểm duyệt"), ("TrangThaiWeb", "Hiển thị trên Web"),
            ("SoDotThi", "Số lịch thi"), ("SoMaDe", "Số mã đề"));
        Ui.HideColumns(_gridDe, "DeThiID", "LaMienPhi", "QuyenThi");
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
        if (_tabsChinh.SelectedIndex == 1) TaiTatCaDotThi();
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
        var mappingError = _service.LayLoiMaDe(id);
        if (mappingError != null)
        {
            var result = MessageBox.Show(
                mappingError + "\n\nHãy trộn lại mã đề trước khi xem hoặc duyệt. Mở màn hình trộn mã đề ngay?",
                "Mã đề chưa đồng bộ", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes) MoManHinhTronDe();
            return;
        }
        using var review = new frmExamReview(id);
        review.ShowDialog(this);
        TaiLai();
    }

    private async void TaiDotThi()
    {
        var version = ++_sessionLoadVersion;
        var id = DeIdDangChon();
        _lblDot.Text = id == 0 ? "Lịch thi của đề đang chọn" : "Đang tải lịch thi...";
        DataTable data;
        try
        {
            data = id == 0 ? new DataTable() : await Task.Run(() => _service.LayDanhSachDotThi(id));
            if (IsDisposed || Disposing || version != _sessionLoadVersion) return;
        }
        catch (Exception ex)
        {
            if (!IsDisposed && !Disposing && version == _sessionLoadVersion)
                _lblDot.Text = "Không tải được lịch thi: " + ex.Message;
            return;
        }
        GanDuLieuDotThi(_gridDot, data, false);
        CapNhatHuongDan();
    }

    private async void TaiTatCaDotThi()
    {
        var version = ++_allSessionLoadVersion;
        _lblTatCaDot.Text = "Đang tải tất cả đợt thi...";
        try
        {
            var data = await Task.Run(() => _service.LayDanhSachDotThi(null));
            if (IsDisposed || Disposing || version != _allSessionLoadVersion) return;
            _bindingAllSessions = true;
            try { GanDuLieuDotThi(_gridTatCaDot, data, true); }
            finally { _bindingAllSessions = false; }
            _lblTatCaDot.Text = data.Rows.Count == 0
                ? "Tất cả đợt thi — chưa có dữ liệu"
                : $"Tất cả đợt thi — {data.Rows.Count} đợt";
            TaiDeTuongUng();
        }
        catch (Exception ex)
        {
            if (!IsDisposed && !Disposing && version == _allSessionLoadVersion)
                _lblTatCaDot.Text = "Không tải được danh sách đợt thi: " + ex.Message;
        }
    }

    private async void TaiDeTuongUng()
    {
        var version = ++_matchingExamLoadVersion;
        var dotThiId = DotThiId(_gridTatCaDot);
        if (dotThiId == 0)
        {
            _gridDeTuongUng.DataSource = new DataTable();
            _lblDeTuongUng.Text = "Đề thi tương ứng — chưa chọn đợt thi";
            return;
        }
        _lblDeTuongUng.Text = "Đang tải đề thi tương ứng...";
        try
        {
            var data = await Task.Run(() => _service.LayDeThiTheoDotThi(dotThiId));
            if (IsDisposed || Disposing || version != _matchingExamLoadVersion) return;
            GanDuLieuDeTuongUng(data);
            _lblDeTuongUng.Text = data.Rows.Count == 0
                ? "Không tìm thấy đề thi tương ứng"
                : "Đề thi tương ứng với đợt đang chọn";
        }
        catch (Exception ex)
        {
            if (!IsDisposed && !Disposing && version == _matchingExamLoadVersion)
                _lblDeTuongUng.Text = "Không tải được đề thi tương ứng: " + ex.Message;
        }
    }

    private static void GanDuLieuDotThi(DataGridView grid, DataTable data, bool hienTenDe)
    {
        grid.DataSource = data;
        Ui.RenameColumns(grid,
            ("DotThiID", "ID"), ("TenDotThi", "Tên lịch thi"), ("TenDe", "Đề"), ("TenMon", "Môn"),
            ("ThoiGianMoCong", "Mở cổng"), ("ThoiGianDongCong", "Đóng cổng"),
            ("ThoiLuongLamBai", "Thời lượng (phút)"), ("GioiHanSoLuong", "Giới hạn SL"),
            ("CongBoDiemSom", "Công bố điểm ngay"), ("TrangThai", "Trạng thái"),
            ("PhamVi", "Phạm vi"), ("QuyenThi", "Đối tượng thi"), ("SoLanThiToiDa", "Lần thi tối đa"),
            ("SoDangKy", "Đã đăng ký"), ("SoBaiDaNop", "Bài đã nộp"));
        Ui.HideColumns(grid, "DotThiID", "DeThiID", "LaMienPhi");
        if (!hienTenDe) Ui.HideColumns(grid, "TenDe", "TenMon");
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        grid.ScrollBars = ScrollBars.Both;
        grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
        grid.ColumnHeadersHeight = 58;

        if (grid.Columns.Contains("ThoiGianMoCong"))
            grid.Columns["ThoiGianMoCong"]!.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
        if (grid.Columns.Contains("ThoiGianDongCong"))
            grid.Columns["ThoiGianDongCong"]!.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";

        if (grid.Columns.Contains("TrangThai"))
            foreach (DataGridViewRow row in grid.Rows)
                row.Cells["TrangThai"].Value = row.Cells["TrangThai"].Value switch
                {
                    "SapMo" => "Sắp mở",
                    "DangMo" => "Đang mở",
                    "DaDong" => "Đã đóng",
                    var x => x
                };

        if (grid.Columns.Contains("PhamVi"))
            foreach (DataGridViewRow row in grid.Rows)
                row.Cells["PhamVi"].Value = row.Cells["PhamVi"].Value switch
                {
                    "ToanTruong" => "Toàn trường",
                    "TheoLop" => "Theo lớp",
                    "CongKhai" => "Công khai",
                    "NoiBo" => "Nội bộ",
                    var x => x
                };

        void DatRong(string column, int width)
        {
            if (grid.Columns.Contains(column)) grid.Columns[column]!.Width = width;
        }
        DatRong("TenDotThi", 260);
        if (hienTenDe) DatRong("TenDe", 300);
        if (hienTenDe) DatRong("TenMon", 130);
        DatRong("ThoiGianMoCong", 160);
        DatRong("ThoiGianDongCong", 160);
        DatRong("ThoiLuongLamBai", 105);
        DatRong("GioiHanSoLuong", 100);
        DatRong("CongBoDiemSom", 135);
        DatRong("TrangThai", 110);
        DatRong("PhamVi", 115);
        DatRong("QuyenThi", 130);
        DatRong("SoLanThiToiDa", 110);
        DatRong("SoDangKy", 105);
        DatRong("SoBaiDaNop", 100);

        if (grid.Columns.Contains("CongBoDiemSom"))
        {
            var publish = grid.Columns["CongBoDiemSom"]!;
            publish.ToolTipText = "Bật: xem điểm ngay sau khi nộp. Tắt: chỉ xem điểm sau khi đợt thi đóng. Bấm checkbox để thay đổi.";
            publish.DefaultCellStyle.BackColor = Color.FromArgb(235, 247, 255);
        }

        foreach (var col in new[] { "ThoiLuongLamBai", "GioiHanSoLuong", "TrangThai", "PhamVi", "SoLanThiToiDa", "SoDangKy", "SoBaiDaNop" })
        {
            if (grid.Columns.Contains(col))
            {
                grid.Columns[col]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                grid.Columns[col]!.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
        }
    }

    private void GanDuLieuDeTuongUng(DataTable data)
    {
        _gridDeTuongUng.DataSource = data;
        Ui.RenameColumns(_gridDeTuongUng,
            ("DeThiID", "ID"), ("TenDe", "Tên đề"), ("TenMon", "Môn"), ("LoaiDe", "Loại đề"),
            ("SoLuongCauHoi", "Số câu"), ("HinhThucTao", "Hình thức tạo"),
            ("QuyenThi", "Quyền thi"), ("Duyet", "Kiểm duyệt"), ("SoMaDe", "Số mã đề"));
        Ui.HideColumns(_gridDeTuongUng, "DeThiID");
        foreach (DataGridViewRow row in _gridDeTuongUng.Rows)
        {
            row.Cells["LoaiDe"].Value = row.Cells["LoaiDe"].Value switch
            {
                "OnTap" => "Bài tập về nhà",
                "ThiThu" => "Thi thử",
                "ChinhThuc" => "Chính thức",
                var value => value
            };
            row.Cells["HinhThucTao"].Value = row.Cells["HinhThucTao"].Value switch
            {
                "MaTranTHPTQG" => "Chuẩn THPTQG",
                "TuDong" => "Tự động",
                "ThuCong" => "Thủ công",
                var value => value
            };
        }
        if (_gridDeTuongUng.Columns.Contains("TenDe")) _gridDeTuongUng.Columns["TenDe"]!.FillWeight = 220;
        if (_gridDeTuongUng.Columns.Contains("TenMon")) _gridDeTuongUng.Columns["TenMon"]!.FillWeight = 90;
        if (_gridDeTuongUng.Columns.Contains("LoaiDe")) _gridDeTuongUng.Columns["LoaiDe"]!.FillWeight = 85;
        if (_gridDeTuongUng.Columns.Contains("HinhThucTao")) _gridDeTuongUng.Columns["HinhThucTao"]!.FillWeight = 105;
        foreach (var column in new[] { "SoLuongCauHoi", "SoMaDe" })
        {
            if (!_gridDeTuongUng.Columns.Contains(column)) continue;
            _gridDeTuongUng.Columns[column]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _gridDeTuongUng.Columns[column]!.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
        }
    }

    private void CapNhatHuongDan()
    {
        var row = _gridDe.CurrentRow;
        var coDe = row?.Cells["DeThiID"].Value is not null;
        _btnTronDe.Enabled = coDe;
        _btnDuyet.Enabled = coDe;
        _btnDotThi.Enabled = coDe;
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

    private static int DotThiId(DataGridView grid) =>
        grid.CurrentRow?.Cells["DotThiID"].Value is { } value ? Convert.ToInt32(value) : 0;

    private int DotIdDangChon() => DotThiId(_gridDot);

    private void DoiCongBoDiem(DataGridView grid, DataGridViewCellEventArgs e, Action refresh)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0 || grid.Columns[e.ColumnIndex].Name != "CongBoDiemSom") return;
        if (!Session.IsQuanTriVien)
        {
            MessageBox.Show("Chỉ quản trị viên được thay đổi thời điểm công bố điểm.", "Không có quyền",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var row = grid.Rows[e.RowIndex];
        var id = Convert.ToInt32(row.Cells["DotThiID"].Value);
        var current = row.Cells["CongBoDiemSom"].Value is bool value
            ? value
            : Convert.ToInt32(row.Cells["CongBoDiemSom"].Value) != 0;
        var next = !current;
        var name = row.Cells["TenDotThi"].Value?.ToString() ?? "";
        var message = next
            ? $"Cho phép xem điểm ngay sau khi nộp bài trong đợt '{name}'?"
            : $"Ẩn điểm của đợt '{name}' cho đến khi đóng cổng?";
        if (MessageBox.Show(message, "Công bố điểm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try
        {
            _service.DatCongBoDiemSom(id, next);
            refresh();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không cập nhật được thời điểm công bố điểm:\n" + ex.Message, "Lỗi",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ThemTatCaDot()
    {
        if (!Session.IsQuanTriVien) return;
        try
        {
            var exams = _service.LayDanhSachDe().Rows.Cast<DataRow>()
                .Where(row => string.Equals(row["Duyet"]?.ToString(), "Đã duyệt", StringComparison.OrdinalIgnoreCase)
                    && Convert.ToInt32(row["SoMaDe"]) > 0)
                .Select(row => new ExamOption(Convert.ToInt32(row["DeThiID"]), row["TenDe"]?.ToString() ?? ""))
                .ToList();
            if (exams.Count == 0)
            {
                MessageBox.Show("Chưa có đề nào đã duyệt và có mã đề. Hãy duyệt, trộn mã đề trước khi tạo đợt thi.",
                    "Chưa có đề phù hợp", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var picker = new Form
            {
                Text = "Chọn đề cho đợt thi", StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(650, 180), MinimumSize = new Size(560, 210),
                BackColor = Color.White, Font = Ui.Body, FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, MinimizeBox = false
            };
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3,
                Padding = new Padding(20), BackColor = Color.White
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.Controls.Add(Ui.Label("Chọn đề thi để tạo đợt mới", Ui.H2, Ui.Navy), 0, 0);
            var combo = Ui.Combo(580);
            combo.Dock = DockStyle.Fill; combo.DropDownWidth = 760;
            combo.Items.AddRange(exams.Cast<object>().ToArray());
            var currentDeId = _gridTatCaDot.CurrentRow?.Cells["DeThiID"].Value is { } currentValue
                ? Convert.ToInt32(currentValue) : 0;
            combo.SelectedIndex = Math.Max(0, exams.FindIndex(x => x.Id == currentDeId));
            layout.Controls.Add(combo, 0, 1);
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            var cancel = Ui.ButtonOutline("Hủy", 100, 36); cancel.DialogResult = DialogResult.Cancel;
            var choose = Ui.ButtonAccent("Tiếp tục", 120, 36); choose.DialogResult = DialogResult.OK;
            buttons.Controls.AddRange([cancel, choose]); layout.Controls.Add(buttons, 0, 2);
            picker.Controls.Add(layout); picker.AcceptButton = choose; picker.CancelButton = cancel;
            if (picker.ShowDialog(FindForm()) != DialogResult.OK || combo.SelectedItem is not ExamOption selected) return;

            using var form = new frmDotThiEdit(selected.Id, selected.Name);
            if (form.ShowDialog(FindForm()) == DialogResult.OK) TaiLai();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể tạo đợt thi:\n" + ex.Message, "Lỗi",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private sealed record ExamOption(int Id, string Name)
    {
        public override string ToString() => Name;
    }

    private void SuaTatCaDot()
    {
        if (!Session.IsQuanTriVien) return;
        var row = _gridTatCaDot.CurrentRow;
        var dotId = DotThiId(_gridTatCaDot);
        if (dotId == 0 || row?.Cells["DeThiID"].Value is not { } deValue)
        {
            MessageBox.Show("Hãy chọn một đợt thi cần chỉnh sửa.", "Thông báo");
            return;
        }
        try
        {
            var deId = Convert.ToInt32(deValue);
            var tenDe = row.Cells["TenDe"].Value?.ToString() ?? "";
            using var form = new frmDotThiEdit(deId, tenDe, dotId);
            if (form.ShowDialog(FindForm()) == DialogResult.OK) TaiLai();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không mở được đợt thi để chỉnh sửa:\n" + ex.Message, "Lỗi",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void XoaTatCaDot()
    {
        if (!Session.IsQuanTriVien) return;
        var row = _gridTatCaDot.CurrentRow;
        var id = DotThiId(_gridTatCaDot);
        if (id == 0 || row == null)
        {
            MessageBox.Show("Hãy chọn một đợt thi cần xóa.", "Thông báo");
            return;
        }
        var name = row.Cells["TenDotThi"].Value?.ToString() ?? "";
        var registrations = row.Cells["SoDangKy"].Value is { } value ? Convert.ToInt32(value) : 0;
        var submitted = row.Cells["SoBaiDaNop"].Value is { } submittedValue ? Convert.ToInt32(submittedValue) : 0;
        if (submitted > 0)
        {
            MessageBox.Show($"Không thể xóa '{name}' vì đã có {submitted} bài làm. Hãy đóng đợt thi để giữ lịch sử và điểm số.",
                "Không thể xóa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var registrationWarning = registrations > 0
            ? $"\n\nĐợt thi đang có {registrations} lượt đăng ký; các đăng ký này cũng sẽ bị xóa."
            : "";
        if (MessageBox.Show($"Xóa vĩnh viễn đợt thi '{name}'?{registrationWarning}\n\nHành động này không thể hoàn tác.",
                "Xác nhận xóa đợt thi", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        try
        {
            _service.XoaDotThi(id);
            MessageBox.Show("Đã xóa đợt thi.", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
            TaiLai();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể xóa đợt thi:\n" + ex.Message, "Lỗi",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            TaiTatCaDotThi();
        }
    }

    private void DoiGoiTatCaDot()
    {
        var id = DotThiId(_gridTatCaDot);
        if (id == 0) { MessageBox.Show("Hãy chọn một đợt thi.", "Thông báo"); return; }
        var row = _gridTatCaDot.CurrentRow!;
        var ten = row.Cells["TenDotThi"].Value?.ToString() ?? "";
        var dangMienPhi = LaDeMienPhi(row.Cells["LaMienPhi"].Value);
        var sangMienPhi = !dangMienPhi;
        var hoi = sangMienPhi
            ? $"Đổi đợt thi '{ten}' sang MIỄN PHÍ?"
            : $"Đổi đợt thi '{ten}' sang ĐÓNG PHÍ (VIP)?";
        if (MessageBox.Show(hoi, "Phân quyền đợt thi", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        _service.DatMienPhiDotThi(id, sangMienPhi);
        TaiTatCaDotThi();
    }

    private void XemBangDiemTatCaDot()
    {
        var id = DotThiId(_gridTatCaDot);
        if (id == 0) { MessageBox.Show("Hãy chọn một đợt thi.", "Thông báo"); return; }
        using var form = new frmStatistics(id);
        form.ShowDialog(FindForm());
    }

    private void DongTatCaDot()
    {
        var id = DotThiId(_gridTatCaDot);
        if (id == 0) { MessageBox.Show("Hãy chọn một đợt thi.", "Thông báo"); return; }
        if (MessageBox.Show("Đóng đợt thi này ngay? Thí sinh sẽ không thể vào làm bài.", "Xác nhận",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        _service.CapNhatTrangThaiDot(id, "DaDong");
        TaiTatCaDotThi();
    }

    private void MoDeTuongUng()
    {
        var id = _gridDeTuongUng.CurrentRow?.Cells["DeThiID"].Value is { } value
            ? Convert.ToInt32(value)
            : 0;
        if (id == 0) { MessageBox.Show("Chưa có đề thi tương ứng để xem.", "Thông báo"); return; }
        using var form = new frmExamReview(id);
        form.ShowDialog(FindForm());
    }

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

    private void DoiGoiDot()
    {
        var id = DotIdDangChon();
        if (id == 0) { MessageBox.Show("Hãy chọn một lịch thi phía dưới.", "Thông báo"); return; }
        var ten = _gridDot.CurrentRow!.Cells["TenDotThi"].Value?.ToString() ?? "";
        var dangMienPhi = LaDeMienPhi(_gridDot.CurrentRow.Cells["LaMienPhi"].Value);
        var sangMienPhi = !dangMienPhi;
        var hoi = sangMienPhi
            ? $"Đổi lịch thi '{ten}' sang MIỄN PHÍ?\nMọi học viên kể cả tài khoản tự do / vãng lai đều được làm bài."
            : $"Đổi lịch thi '{ten}' sang ĐÓNG PHÍ (VIP)?\nChỉ học viên VIP hoặc học viên lớp trung tâm mới được làm bài.";
        if (MessageBox.Show(hoi, "Phân quyền lịch thi", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;
        _service.DatMienPhiDotThi(id, sangMienPhi);
        TaiDotThi();
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
        try
        {
            _service.XoaDe(id);
            TaiLai();
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "Không thể xóa đề", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Xóa đề không thành công:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
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
    private void XuatJsonDe()
    {
        var id = DeIdDangChon();
        if (id <= 0)
        {
            MessageBox.Show("Hãy chọn một đề thi để xuất file JSON.", "Chưa chọn đề", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var tenDe = _gridDe.CurrentRow!.Cells["TenDe"].Value?.ToString() ?? "DeThi";
        var safeTen = string.Join("_", tenDe.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");

        using var sfd = new SaveFileDialog
        {
            Filter = "JSON file (*.json)|*.json",
            FileName = $"{safeTen}.json",
            Title = "Xuất cấu trúc đề thi thành file JSON"
        };
        if (sfd.ShowDialog(FindForm()) != DialogResult.OK) return;

        try
        {
            var json = _service.XuatDeThiJson(id);
            File.WriteAllText(sfd.FileName, json, Encoding.UTF8);
            MessageBox.Show("Đã xuất file JSON cấu trúc đề thi thành công tại:\n" + sfd.FileName, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi khi xuất file JSON:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void XuatBanInDe()
    {
        var id = DeIdDangChon();
        if (id <= 0)
        {
            MessageBox.Show("Hãy chọn một đề thi để xuất bản in.", "Chưa chọn đề", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var tenDe = _gridDe.CurrentRow!.Cells["TenDe"].Value?.ToString() ?? "DeThi";
        var safeTen = string.Join("_", tenDe.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");

        using var sfd = new SaveFileDialog
        {
            Filter = "Tài liệu Word / HTML (*.doc;*.html)|*.doc;*.html|File HTML (*.html)|*.html|Tài liệu Word (*.doc)|*.doc",
            FileName = $"BanIn_{safeTen}.doc",
            Title = "Xuất bản in đề thi (Mở được bằng Microsoft Word hoặc trình duyệt)"
        };
        if (sfd.ShowDialog(FindForm()) != DialogResult.OK) return;

        try
        {
            var html = _service.XuatBanInDeThi(id);
            File.WriteAllText(sfd.FileName, html, Encoding.UTF8);
            MessageBox.Show("Đã xuất bản in đề thi thành công tại:\n" + sfd.FileName + "\n\nBạn có thể mở trực tiếp bằng Microsoft Word để in ra giấy hoặc chuyển thành PDF.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi khi xuất bản in đề thi:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
