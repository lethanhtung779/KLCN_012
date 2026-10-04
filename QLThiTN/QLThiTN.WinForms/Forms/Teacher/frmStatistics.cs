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

    public frmStatistics()
    {
        Text = "Thống kê kết quả";
        Font = Ui.Body;
        BackColor = Color.White;

        // Thanh chon dot thi dung chung cho ca 2 tab
        var top = new Panel { Dock = DockStyle.Top, Height = 118, BackColor = Color.White };
        var lblChon = Ui.Label("Chọn đợt thi:", Ui.H2, Ui.Navy);
        _cboDotThi.SelectedIndexChanged += (s, e) => { TaiThongKeDiem(); TaiThongKeCauHoi(); };
        var btnLamMoi = Ui.ButtonOutline("Làm mới", 120, 30);
        btnLamMoi.Click += (s, e) => { TaiDotThi(); };
        top.Controls.AddRange([lblChon, _cboDotThi, btnLamMoi]);
        lblChon.Location = new Point(16, 14);
        _cboDotThi.Location = new Point(16, 42);
        btnLamMoi.Location = new Point(560, 42);
        _lblTongQuan.Location = new Point(16, 80);

        var tabs = new TabControl { Dock = DockStyle.Fill, Font = Ui.Body };

        // ---------------------------------------------------------------- Tab 1: diem theo ky thi
        var tabDiem = new TabPage("Điểm theo kỳ thi") { BackColor = Color.White };
        Ui.StyleGrid(_gridDiem);
        _gridDiem.Location = new Point(16, 16);
        _gridDiem.Size = new Size(640, 240);
        var lblChart = Ui.Label("Phân bố số bài theo khoảng điểm", Ui.H2, Ui.Navy);
        lblChart.Location = new Point(16, 270);
        _chartDiem.Location = new Point(16, 300);
        _chartDiem.Size = new Size(660, 300);

        var lblHuongDan = Ui.Label(
            "Hướng dẫn đọc thống kê:\n\n" +
            "• Bảng: kết quả từng bài thi của đợt đang chọn\n   (sắp theo điểm giảm dần).\n" +
            "• Biểu đồ: phân bố số bài theo khoảng điểm 0–10.\n" +
            "• Dữ liệu lấy trực tiếp từ bảng BaiLam — cùng nguồn\n   dữ liệu với website nên luôn đồng bộ.",
            Ui.Body, Color.FromArgb(43, 52, 82));
        lblHuongDan.Location = new Point(700, 300);

        tabDiem.Controls.AddRange([_gridDiem, lblChart, _chartDiem, lblHuongDan]);

        // ---------------------------------------------------------------- Tab 2: chat luong cau hoi
        var tabCauHoi = new TabPage("Chất lượng câu hỏi (Đúng/Sai)") { BackColor = Color.White };
        Ui.StyleGrid(_gridCauHoi);
        _gridCauHoi.Location = new Point(16, 16);
        _gridCauHoi.Size = new Size(660, 460);
        _gridCauHoi.SelectionChanged += (s, e) =>
        {
            if (_gridCauHoi.CurrentRow != null && _gridCauHoi.Columns.Contains("NoiDung"))
                _lblCauHoiInfo.Text = "Câu " + _gridCauHoi.CurrentRow.Cells["ThuTu"].Value + ": " +
                                      _gridCauHoi.CurrentRow.Cells["NoiDung"].Value;
        };
        _lblCauHoiInfo.Location = new Point(16, 484);
        _lblCauHoiInfo.MaximumSize = new Size(660, 0);

        var lblChart2 = Ui.Label("Tỉ lệ đúng (%) theo từng câu", Ui.H2, Ui.Navy);
        lblChart2.Location = new Point(700, 16);
        _chartCauHoi.Location = new Point(700, 48);
        _chartCauHoi.Size = new Size(320, 500);

        var lblHuongDan2 = Ui.Label(
            "Câu có tỉ lệ đúng THẤP (<50% - cột đỏ)\ncó thể quá khó, nội dung mâu thuẫn\nhoặc đáp án nhầm lẫn — cần xem lại.",
            Ui.Small, Ui.TextMuted);
        lblHuongDan2.Location = new Point(700, 556);

        tabCauHoi.Controls.AddRange([_gridCauHoi, _lblCauHoiInfo, lblChart2, _chartCauHoi, lblHuongDan2]);

        tabs.TabPages.Add(tabDiem);
        tabs.TabPages.Add(tabCauHoi);

        Controls.Add(tabs);
        Controls.Add(top);

        Load += (s, e) => TaiDotThi();
    }

    private void TaiDotThi()
    {
        var save = (_cboDotThi.SelectedItem as ComboItem)?.Value as int?;
        _cboDotThi.Items.Clear();
        foreach (System.Data.DataRow row in _service.LayDanhSachDotThiChoThongKe().Rows)
            _cboDotThi.Items.Add(new ComboItem(Convert.ToInt32(row["DotThiID"]), row["HienThi"].ToString() ?? ""));
        if (_cboDotThi.Items.Count == 0)
        {
            _lblTongQuan.Text = "Chưa có đợt thi nào để thống kê — hãy tạo đề và đợt thi trước.";
            return;
        }
        if (save != null)
            foreach (ComboItem item in _cboDotThi.Items)
                if ((int)item.Value == save) { _cboDotThi.SelectedItem = item; break; }
        if (_cboDotThi.SelectedIndex < 0) _cboDotThi.SelectedIndex = 0;
    }

    private int? DotId => (_cboDotThi.SelectedItem as ComboItem)?.Value as int?;

    private void TaiThongKeDiem()
    {
        if (DotId == null) return;
        var ketQua = _service.LayKetQuaDotThi(DotId.Value);
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

        _chartDiem.SetData(_service.PhanBoDiem(ketQua).Select(kv => (kv.Key, (double)kv.Value)), " bài");
    }

    private void TaiThongKeCauHoi()
    {
        if (DotId == null) return;
        var data = _service.LayChatLuongCauHoi(DotId.Value);
        _gridCauHoi.DataSource = data;
        Ui.RenameColumns(_gridCauHoi,
            ("CauHoiID", "ID"), ("ThuTu", "Câu"), ("NoiDung", "Nội dung câu hỏi"),
            ("SoLanTraLoi", "Số lần trả lời"), ("SoLanDung", "Số lần đúng"), ("TiLeDung", "% đúng"));
        Ui.HideColumns(_gridCauHoi, "CauHoiID", "NoiDung");
        if (_gridCauHoi.Columns.Contains("TiLeDung"))
        {
            _gridCauHoi.Columns["TiLeDung"].Width = 70;
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
