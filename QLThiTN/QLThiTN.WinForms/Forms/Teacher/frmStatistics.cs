using QLThiTN.WinForms.Controls;
using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms.Forms.Teacher;

/// <summary>Thong ke ket qua: diem theo ky thi + chat luong cau hoi (ti le dung/sai) + bieu do.</summary>
public class frmStatistics : Form
{
    private readonly StatisticsService _service = new();
    private readonly ComboBox _cboDotThi = Ui.Combo(520);
    private readonly DataGridView _gridDiem = new();
    private readonly DataGridView _gridCauHoi = new();
    private readonly BarChartControl _chartDiem = new();
    private readonly BarChartControl _chartCauHoi = new();
    private readonly Label _lblTongQuan = Ui.Label("", Ui.H2, Ui.Navy);
    private readonly Label _lblCauHoiInfo = Ui.Label("", Ui.Small, Ui.TextMuted);
    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill, Font = Ui.Body };
    private readonly Label _emptyState = new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleCenter,
        Font = Ui.H2,
        ForeColor = Ui.TextMuted,
        BackColor = Ui.BgSoft,
        Padding = new Padding(28),
    };
    private bool _hasSubmittedAttempts;
    private readonly int? _dotThiIdKhoiTao;

    public frmStatistics(int? dotThiId = null)
    {
        _dotThiIdKhoiTao = dotThiId;
        Text = "Thống kê kết quả";
        Font = Ui.Body;
        BackColor = Color.White;

        // Thanh chon dot thi dung chung cho ca 2 tab
        var top = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.White,
            Padding = new Padding(16, 8, 16, 4),
        };
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        top.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        top.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var selector = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        var lblChon = Ui.Label("Chọn đợt thi:", Ui.H2, Ui.Navy);
        _cboDotThi.SelectedIndexChanged += (s, e) => { TaiThongKeDiem(); TaiThongKeCauHoi(); };
        var btnLamMoi = Ui.ButtonOutline("Làm mới", 120, 30);
        btnLamMoi.Click += (s, e) => { TaiDotThi(); };
        lblChon.Margin = new Padding(0, 8, 12, 0);
        _cboDotThi.Margin = new Padding(0, 4, 12, 0);
        btnLamMoi.Margin = new Padding(0, 4, 0, 0);
        selector.Controls.AddRange([lblChon, _cboDotThi, btnLamMoi]);
        _lblTongQuan.AutoSize = false;
        _lblTongQuan.Dock = DockStyle.Fill;
        _lblTongQuan.TextAlign = ContentAlignment.MiddleLeft;
        top.Controls.Add(selector, 0, 0);
        top.Controls.Add(_lblTongQuan, 0, 1);

        var tabs = _tabs;

        // ---------------------------------------------------------------- Tab 1: diem theo ky thi
        var tabDiem = new TabPage("Điểm theo kỳ thi") { BackColor = Color.White };
        Ui.StyleGrid(_gridDiem);
        _gridDiem.Dock = DockStyle.Fill;
        var lblChart = Ui.Label("Phân bố số bài theo khoảng điểm", Ui.H2, Ui.Navy);
        lblChart.Dock = DockStyle.Top;
        lblChart.Height = 34;
        lblChart.Padding = new Padding(4, 7, 0, 0);
        _chartDiem.Dock = DockStyle.Fill;

        var lblHuongDan = Ui.Label(
            "Hướng dẫn đọc thống kê:\n\n" +
            "• Bảng: kết quả từng bài thi của đợt đang chọn\n   (sắp theo điểm giảm dần).\n" +
            "• Biểu đồ: phân bố số bài theo khoảng điểm 0–10.\n" +
            "• Dữ liệu lấy trực tiếp từ bảng BaiLam — cùng nguồn\n   dữ liệu với website nên luôn đồng bộ.",
            Ui.Body, Color.FromArgb(43, 52, 82));
        lblHuongDan.AutoSize = false;
        lblHuongDan.Dock = DockStyle.Fill;
        lblHuongDan.Padding = new Padding(22, 38, 8, 0);

        var chartPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        chartPanel.Controls.Add(_chartDiem);
        chartPanel.Controls.Add(lblChart);
        var scoreLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2,
            Padding = new Padding(12), BackColor = Color.White,
        };
        scoreLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        scoreLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        scoreLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 52));
        scoreLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 48));
        scoreLayout.Controls.Add(_gridDiem, 0, 0);
        scoreLayout.SetColumnSpan(_gridDiem, 2);
        scoreLayout.Controls.Add(chartPanel, 0, 1);
        scoreLayout.Controls.Add(lblHuongDan, 1, 1);
        tabDiem.Controls.Add(scoreLayout);

        // ---------------------------------------------------------------- Tab 2: chat luong cau hoi
        var tabCauHoi = new TabPage("Chất lượng câu hỏi (Đúng/Sai)") { BackColor = Color.White };
        Ui.StyleGrid(_gridCauHoi);
        _gridCauHoi.Dock = DockStyle.Fill;
        _gridCauHoi.SelectionChanged += (s, e) =>
        {
            if (_gridCauHoi.CurrentRow != null && _gridCauHoi.Columns.Contains("NoiDung"))
                _lblCauHoiInfo.Text = "Câu " + _gridCauHoi.CurrentRow.Cells["ThuTu"].Value + ": " +
                                      _gridCauHoi.CurrentRow.Cells["NoiDung"].Value;
        };
        _lblCauHoiInfo.AutoSize = false;
        _lblCauHoiInfo.Dock = DockStyle.Fill;
        _lblCauHoiInfo.Padding = new Padding(8);

        var lblChart2 = Ui.Label("Tỉ lệ đúng (%) theo từng câu", Ui.H2, Ui.Navy);
        lblChart2.Dock = DockStyle.Top;
        lblChart2.Height = 36;
        lblChart2.Padding = new Padding(4, 8, 0, 0);
        _chartCauHoi.Dock = DockStyle.Fill;

        var lblHuongDan2 = Ui.Label(
            "Câu có tỉ lệ đúng THẤP (<50% - cột đỏ)\ncó thể quá khó, nội dung mâu thuẫn\nhoặc đáp án nhầm lẫn — cần xem lại.",
            Ui.Small, Ui.TextMuted);
        lblHuongDan2.AutoSize = false;
        lblHuongDan2.Dock = DockStyle.Fill;
        lblHuongDan2.Padding = new Padding(8);

        var qualityChartPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        qualityChartPanel.Controls.Add(_chartCauHoi);
        qualityChartPanel.Controls.Add(lblChart2);
        var qualityLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2,
            Padding = new Padding(12), BackColor = Color.White,
        };
        qualityLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        qualityLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        qualityLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        qualityLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        qualityLayout.Controls.Add(_gridCauHoi, 0, 0);
        qualityLayout.Controls.Add(qualityChartPanel, 1, 0);
        qualityLayout.Controls.Add(_lblCauHoiInfo, 0, 1);
        qualityLayout.Controls.Add(lblHuongDan2, 1, 1);
        tabCauHoi.Controls.Add(qualityLayout);

        tabs.TabPages.Add(tabDiem);
        tabs.TabPages.Add(tabCauHoi);

        var body = new Panel { Dock = DockStyle.Fill };
        body.Controls.Add(tabs);
        body.Controls.Add(_emptyState);
        _emptyState.BringToFront();

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(top, 0, 0);
        root.Controls.Add(body, 0, 1);
        Controls.Add(root);

        Load += (s, e) => TaiDotThi();
    }

    private void TaiDotThi()
    {
        var save = ((_cboDotThi.SelectedItem as ComboItem)?.Value as int?) ?? _dotThiIdKhoiTao;
        _cboDotThi.Items.Clear();
        try
        {
            foreach (System.Data.DataRow row in _service.LayDanhSachDotThiChoThongKe().Rows)
                _cboDotThi.Items.Add(new ComboItem(Convert.ToInt32(row["DotThiID"]), row["HienThi"].ToString() ?? ""));
        }
        catch (Exception ex)
        {
            ShowEmptyState("Không tải được danh sách đợt thi.\n" + ex.Message);
            return;
        }
        if (_cboDotThi.Items.Count == 0)
        {
            var message = Session.IsGiaoVien
                ? "Chưa có đợt thi thuộc môn bạn được phân công.\nQuản trị viên cần phân công môn và tạo đợt thi để thống kê xuất hiện ở đây."
                : "Chưa có đợt thi nào. Hãy tạo đề thi và đợt thi trước khi xem thống kê.";
            _cboDotThi.Items.Add(new ComboItem(0, "Chưa có đợt thi để thống kê"));
            _cboDotThi.SelectedIndex = 0;
            ShowEmptyState(message);
            return;
        }
        _tabs.Visible = true;
        _emptyState.Visible = false;
        if (save != null)
            foreach (ComboItem item in _cboDotThi.Items)
                if ((int)item.Value == save) { _cboDotThi.SelectedItem = item; break; }
        if (_cboDotThi.SelectedIndex < 0) _cboDotThi.SelectedIndex = 0;
    }

    private void ShowEmptyState(string message)
    {
        _lblTongQuan.Text = message.Split('\n')[0];
        _emptyState.Text = message;
        _tabs.Visible = false;
        _emptyState.Visible = true;
        _emptyState.BringToFront();
        _gridDiem.DataSource = null;
        _gridCauHoi.DataSource = null;
        _chartDiem.SetData([]);
        _chartCauHoi.SetData([]);
        _lblCauHoiInfo.Text = "";
    }

    private int? DotId => (_cboDotThi.SelectedItem as ComboItem)?.Value is int id && id > 0 ? id : null;

    private void TaiThongKeDiem()
    {
        if (DotId == null) return;
        var ketQua = _service.LayKetQuaDotThi(DotId.Value);
        _hasSubmittedAttempts = ketQua.Count > 0;
        _gridDiem.DataSource = ketQua;
        Ui.RenameColumns(_gridDiem,
            ("BaiLamID", "ID"), ("HoTen", "Học viên"), ("TenDangNhap", "Tài khoản"),
            ("LanThi", "Lần thi"), ("TongDiem", "Điểm"), ("ThoiGianBatDau", "Bắt đầu"),
            ("ThoiGianNop", "Nộp bài"), ("TrangThai", "Trạng thái"), ("PhutLamBai", "Phút làm bài"),
            ("XepLoai", "Xếp loại"));
        Ui.HideColumns(_gridDiem, "BaiLamID", "TenDangNhap", "ThoiGianBatDau", "ThoiGianNop", "TrangThai");

        var coDiem = ketQua.Where(k => k.TongDiem != null).Select(k => k.TongDiem!.Value).ToList();
        _lblTongQuan.Text = coDiem.Count > 0
            ? $"Số bài: {coDiem.Count}   |   Điểm TB: {coDiem.Average():0.00}   |   " +
              $"Cao nhất: {coDiem.Max():0.00}   |   Thấp nhất: {coDiem.Min():0.00}"
            : "Đợt thi này chưa có bài nào được nộp.";

        _chartDiem.SetData(coDiem.Count == 0 ? [] :
            _service.PhanBoDiem(ketQua).Select(kv => (kv.Key, (double)kv.Value)), " bài");
    }

    private void TaiThongKeCauHoi()
    {
        if (DotId == null) return;
        if (!_hasSubmittedAttempts)
        {
            _gridCauHoi.DataSource = null;
            _chartCauHoi.SetData([]);
            _lblCauHoiInfo.Text = "Chưa có bài nộp để đánh giá chất lượng câu hỏi.";
            return;
        }
        var data = _service.LayChatLuongCauHoi(DotId.Value);
        _gridCauHoi.DataSource = data;
        Ui.RenameColumns(_gridCauHoi,
            ("CauHoiID", "ID"), ("ThuTu", "Câu"), ("NoiDung", "Nội dung câu hỏi"),
            ("SoLanTraLoi", "Lượt trả lời ghi nhận"), ("SoLanDung", "Số lần đúng"), ("TiLeDung", "% đúng"));
        Ui.HideColumns(_gridCauHoi, "CauHoiID", "NoiDung");
        if (_gridCauHoi.Columns.Contains("TiLeDung"))
        {
            // Dat AutoSizeMode=None truoc Width: dat Width tren cot Fill khi grid chua co handle bi NullReferenceException.
            var colTiLe = _gridCauHoi.Columns["TiLeDung"]!;
            colTiLe.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            colTiLe.Width = 70;
            foreach (DataGridViewRow row in _gridCauHoi.Rows)
            {
                var tile = Convert.ToDouble(row.Cells["TiLeDung"].Value);
                row.Cells["TiLeDung"].Style.BackColor = tile < 50 ? Ui.Danger
                    : tile < 75 ? Color.FromArgb(255, 227, 178)
                    : Color.FromArgb(214, 245, 222);
            }
        }
        _lblCauHoiInfo.Text = data.Count > 0 ? "Câu 1: " + data[0].NoiDung : "";
        _chartCauHoi.SetData(data.Select(d => ($"Câu {d.ThuTu}", d.TiLeDung)), "%");
    }

    private class ComboItem(object value, string text)
    {
        public object Value { get; } = value;
        public override string ToString() => text;
    }
}
