using QLThiTN.WinForms.Controls;
using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms.Forms.Teacher;

/// <summary>Thong ke ket qua: diem theo ky thi + chat luong cau hoi (ti le dung/sai) + bieu do.</summary>
public class frmStatistics : Form
{
    private readonly StatisticsService _service = new();
    private readonly ComboBox _cboDotThi = Ui.Combo(400);
    private readonly ComboBox _cboLopHoc = Ui.Combo(240);
    private readonly ComboBox _cboHocVien = Ui.Combo(340);
    private readonly DataGridView _gridDiem = new();
    private readonly DataGridView _gridCauHoi = new();
    private readonly DataGridView _gridHocVien = new();
    private readonly BarChartControl _chartDiem = new();
    private readonly BarChartControl _chartCauHoi = new();
    private readonly BarChartControl _chartHocVien = new();
    private readonly Label _lblTongQuan = Ui.Label("", Ui.H2, Ui.Navy);
    private readonly Label _lblHocVienTongQuan = Ui.Label("", Ui.H2, Ui.Navy);
    private readonly Label _lblCauHoiInfo = Ui.Label("", Ui.Small, Ui.TextMuted);
    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill, Font = Ui.Body };
    private bool _dangKhoiTaoLop;
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
    private int _examListLoadVersion;
    private int _classListLoadVersion;
    private int _scoreLoadVersion;
    private int _questionStatsLoadVersion;
    private int _studentListLoadVersion;
    private int _studentDetailLoadVersion;

    public frmStatistics(int? dotThiId = null)
    {
        _dotThiIdKhoiTao = dotThiId;
        Text = "Thống kê kết quả";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1100, 700);
        MinimumSize = new Size(780, 560);
        Font = Ui.Body;
        BackColor = Color.White;

        void ReviewSelectedAttempt()
        {
            if (_gridDiem.CurrentRow?.Cells["BaiLamID"].Value is not int id)
            {
                MessageBox.Show(this, "Hãy chọn một bài làm của học viên trong danh sách.", "Chưa chọn bài làm",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using var form = new frmExamReview(0, id);
            form.ShowDialog(this);
        }

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
        top.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        top.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var selector = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = Session.IsQuanTriVien ? 6 : 5, RowCount = 1 };
        selector.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        selector.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        selector.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        selector.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        selector.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        if (Session.IsQuanTriVien) selector.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var lblChon = Ui.Label("Đợt thi:", Ui.H2, Ui.Navy);
        var lblLop = Ui.Label("Lớp:", Ui.H2, Ui.Navy);
        _cboDotThi.SelectedIndexChanged += (s, e) =>
        {
            TaiDanhSachLopChoDot();
            TaiThongKeDiem();
            if (_tabs.SelectedIndex == 1) TaiThongKeCauHoi();
        };
        _cboLopHoc.SelectedIndexChanged += (s, e) =>
        {
            if (!_dangKhoiTaoLop) TaiThongKeDiem();
        };
        _tabs.SelectedIndexChanged += (s, e) =>
        {
            if (_tabs.SelectedIndex == 1) TaiThongKeCauHoi();
            else if (_tabs.SelectedIndex == 2) TaiThongKeHocVien();
            top.Visible = _tabs.SelectedIndex != 2;
        };

        var btnLamMoi = Ui.ButtonOutline("Làm mới", 100, 30);
        btnLamMoi.Click += (s, e) => { TaiDotThi(); };
        lblChon.Margin = new Padding(0, 8, 8, 0);
        _cboDotThi.Margin = new Padding(0, 4, 12, 0);
        lblLop.Margin = new Padding(0, 8, 8, 0);
        _cboLopHoc.Margin = new Padding(0, 4, 12, 0);
        btnLamMoi.Margin = new Padding(0, 4, 0, 0);
        _cboDotThi.Dock = DockStyle.Top;
        _cboDotThi.DropDownWidth = 700;
        _cboLopHoc.Dock = DockStyle.Top;
        _cboLopHoc.DropDownWidth = 350;

        selector.Controls.Add(lblChon, 0, 0);
        selector.Controls.Add(_cboDotThi, 1, 0);
        selector.Controls.Add(lblLop, 2, 0);
        selector.Controls.Add(_cboLopHoc, 3, 0);
        selector.Controls.Add(btnLamMoi, 4, 0);
        if (Session.IsQuanTriVien)
        {
            var btnAnswers = Ui.ButtonAccent("📝 Xem bài làm", 160, 30);
            btnAnswers.Margin = new Padding(10, 4, 0, 0);
            btnAnswers.Click += (_, _) => ReviewSelectedAttempt();
            selector.Controls.Add(btnAnswers, 5, 0);
        }
        _lblTongQuan.AutoSize = false;
        _lblTongQuan.Dock = DockStyle.Fill;
        _lblTongQuan.TextAlign = ContentAlignment.MiddleLeft;
        top.Controls.Add(selector, 0, 0);
        top.Controls.Add(_lblTongQuan, 0, 1);

        var tabs = _tabs;

        // ---------------------------------------------------------------- Tab 1: diem theo ky thi
        var tabDiem = new TabPage("Điểm theo kỳ thi") { BackColor = Color.White };
        Ui.StyleGrid(_gridDiem);
        if (Session.IsQuanTriVien)
        {
            _gridDiem.CellDoubleClick += (_,e) => {if(e.RowIndex>=0)ReviewSelectedAttempt();};
            var menu=new ContextMenuStrip();menu.Items.Add("Xem từng câu trả lời",null,(_,_)=>ReviewSelectedAttempt());
            _gridDiem.ContextMenuStrip=menu;
        }
        _gridDiem.Dock = DockStyle.Fill;
        var lblChart = Ui.Label("Phân bố số bài theo khoảng điểm", Ui.H2, Ui.Navy);
        lblChart.Dock = DockStyle.Top;
        lblChart.Height = 34;
        lblChart.Padding = new Padding(4, 7, 0, 0);
        _chartDiem.Dock = DockStyle.Fill;

        var lblHuongDan = Ui.Label(
            "Hướng dẫn đọc thống kê:\n\n" +
            "• Bảng: các bài thi được sắp theo điểm giảm dần.\n" +
            "• Biểu đồ: số bài theo khoảng điểm 0–10.\n" +
            "• Kết quả được đồng bộ với website.",
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
        scoreLayout.Controls.Add(ScrollableNote(lblHuongDan), 1, 1);
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
        qualityLayout.Controls.Add(ScrollableNote(lblHuongDan2), 1, 1);
        tabCauHoi.Controls.Add(qualityLayout);

        tabs.TabPages.Add(tabDiem);
        tabs.TabPages.Add(tabCauHoi);

        // ---------------------------------------------------------------- Tab 3: thong ke theo hoc vien
        var tabHocVien = new TabPage("Thống kê theo học viên") { BackColor = Color.White };
        var topHv = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 48, Padding = new Padding(12, 8, 12, 0),
            WrapContents = false, BackColor = Color.White
        };
        var lblChonHv = Ui.Label("Chọn học viên:", Ui.H2, Ui.Navy);
        lblChonHv.Margin = new Padding(0, 6, 12, 0);
        _cboHocVien.Margin = new Padding(0, 2, 12, 0);
        _cboHocVien.DropDownWidth = 400;
        _cboHocVien.SelectedIndexChanged += (s, e) => TaiChiTietHocVien();
        var btnLamMoiHv = Ui.ButtonOutline("Làm mới", 100, 30);
        btnLamMoiHv.Click += (s, e) => TaiDanhSachHocVien();
        topHv.Controls.AddRange([lblChonHv, _cboHocVien, btnLamMoiHv]);

        var pnlHvTongQuan = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = Color.FromArgb(248, 250, 254), Padding = new Padding(12, 10, 12, 8) };
        _lblHocVienTongQuan.Dock = DockStyle.Fill;
        pnlHvTongQuan.Controls.Add(_lblHocVienTongQuan);

        Ui.StyleGrid(_gridHocVien);
        _gridHocVien.Dock = DockStyle.Fill;
        if (Session.IsQuanTriVien)
        {
            _gridHocVien.CellDoubleClick += (_, e) =>
            {
                if (e.RowIndex >= 0 && _gridHocVien.Rows[e.RowIndex].Cells["BaiLamID"].Value is int blId)
                {
                    using var f = new frmExamReview(0, blId);
                    f.ShowDialog(this);
                }
            };
        }

        var chartHvPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        var lblChartHv = Ui.Label("Lịch sử điểm số các bài thi gần đây", Ui.H2, Ui.Navy);
        lblChartHv.Dock = DockStyle.Top;
        lblChartHv.Height = 34;
        lblChartHv.Padding = new Padding(4, 7, 0, 0);
        _chartHocVien.Dock = DockStyle.Fill;
        chartHvPanel.Controls.Add(_chartHocVien);
        chartHvPanel.Controls.Add(lblChartHv);

        var hvLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
            Padding = new Padding(12), BackColor = Color.White
        };
        hvLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        hvLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        hvLayout.Controls.Add(_gridHocVien, 0, 0);
        hvLayout.Controls.Add(chartHvPanel, 1, 0);

        var hvBody = new Panel { Dock = DockStyle.Fill };
        hvBody.Controls.Add(hvLayout);
        hvBody.Controls.Add(pnlHvTongQuan);
        hvBody.Controls.Add(topHv);
        tabHocVien.Controls.Add(hvBody);

        tabs.TabPages.Add(tabHocVien);

        var body = new Panel { Dock = DockStyle.Fill };
        body.Controls.Add(tabs);
        body.Controls.Add(_emptyState);
        _emptyState.BringToFront();

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 124));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(top, 0, 0);
        root.Controls.Add(body, 0, 1);
        Controls.Add(root);

        Load += (s, e) => TaiDotThi();
    }

    private static Control ScrollableNote(Label label)
    {
        var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        label.Dock = DockStyle.Top;
        label.AutoSize = true;
        label.Padding = new Padding(12);
        panel.Controls.Add(label);
        panel.SizeChanged += (_, _) => label.MaximumSize = new Size(Math.Max(100, panel.ClientSize.Width - SystemInformation.VerticalScrollBarWidth), 0);
        return panel;
    }

    private async void TaiDotThi()
    {
        var version = ++_examListLoadVersion;
        var save = ((_cboDotThi.SelectedItem as ComboItem)?.Value as int?) ?? _dotThiIdKhoiTao;
        _lblTongQuan.Text = "Đang tải danh sách đợt thi...";
        try
        {
            var exams = await Task.Run(_service.LayDanhSachDotThiChoThongKe);
            if (IsDisposed || Disposing || version != _examListLoadVersion) return;
            _cboDotThi.Items.Clear();
            foreach (System.Data.DataRow row in exams.Rows)
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
                ? "Chưa có đợt thi thuộc môn bạn được phân công.\nHãy kiểm tra phân công môn hoặc tạo đợt thi cho môn đó."
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
    private int? LopId => (_cboLopHoc.SelectedItem as ComboItem)?.Value is int id && id > 0 ? id : null;
    private int? HocVienId => (_cboHocVien.SelectedItem as ComboItem)?.Value is int id && id > 0 ? id : null;

    private async void TaiDanhSachLopChoDot()
    {
        var version = ++_classListLoadVersion;
        var dotId = DotId;
        _dangKhoiTaoLop = true;
        try
        {
            System.Data.DataTable? classes = null;
            if (dotId.HasValue)
                classes = await Task.Run(() => _service.LayDanhSachLopChoDotThi(dotId.Value));
            if (IsDisposed || Disposing || version != _classListLoadVersion) return;

            _cboLopHoc.Items.Clear();
            _cboLopHoc.Items.Add(new ComboItem(0, "Tất cả lớp"));
            if (classes != null)
            {
                foreach (System.Data.DataRow row in classes.Rows)
                    _cboLopHoc.Items.Add(new ComboItem(Convert.ToInt32(row["LopHocID"]), row["TenHienThi"].ToString() ?? ""));
            }
            _cboLopHoc.SelectedIndex = 0;
        }
        catch { }
        finally
        {
            if (version == _classListLoadVersion) _dangKhoiTaoLop = false;
        }
    }

    private async void TaiThongKeDiem()
    {
        var dotId = DotId;
        var lopId = LopId;
        if (dotId == null) return;
        var version = ++_scoreLoadVersion;
        try
        {
            Cursor = Cursors.WaitCursor;
            _lblTongQuan.Text = "Đang tải kết quả bài thi...";
            var ketQua = await Task.Run(() => _service.LayKetQuaDotThi(dotId.Value, lopId));
            if (IsDisposed || Disposing || version != _scoreLoadVersion) return;
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
        catch (Exception ex)
        {
            _lblTongQuan.Text = "Không tải được thống kê: " + ex.Message;
            _gridDiem.DataSource = null;
            _chartDiem.SetData([]);
        }
        finally
        {
            if (version == _scoreLoadVersion) Cursor = Cursors.Default;
        }
    }

    private async void TaiThongKeCauHoi()
    {
        var dotId = DotId;
        if (dotId == null) return;
        if (!_hasSubmittedAttempts)
        {
            _gridCauHoi.DataSource = null;
            _chartCauHoi.SetData([]);
            _lblCauHoiInfo.Text = "Chưa có bài nộp để đánh giá chất lượng câu hỏi.";
            return;
        }
        var version = ++_questionStatsLoadVersion;
        try
        {
            Cursor = Cursors.WaitCursor;
            _lblCauHoiInfo.Text = "Đang tải chất lượng câu hỏi...";
            var data = await Task.Run(() => _service.LayChatLuongCauHoi(dotId.Value));
            if (IsDisposed || Disposing || version != _questionStatsLoadVersion) return;
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
                    if (row.Cells["TiLeDung"].Value is null or DBNull) continue;
                    var tile = Convert.ToDouble(row.Cells["TiLeDung"].Value);
                    row.Cells["TiLeDung"].Style.BackColor = tile < 50 ? Ui.Danger
                        : tile < 75 ? Color.FromArgb(255, 227, 178)
                        : Color.FromArgb(214, 245, 222);
                }
            }
            _lblCauHoiInfo.Text = data.Count > 0 ? "Câu 1: " + data[0].NoiDung : "";
            _chartCauHoi.SetData(data.Select(d => ($"Câu {d.ThuTu}", d.TiLeDung)), "%");
        }
        catch (Exception ex)
        {
            _lblCauHoiInfo.Text = "Không tải được chất lượng câu hỏi: " + ex.Message;
            _gridCauHoi.DataSource = null;
            _chartCauHoi.SetData([]);
        }
        finally
        {
            if (version == _questionStatsLoadVersion) Cursor = Cursors.Default;
        }
    }

    private void TaiThongKeHocVien()
    {
        if (_cboHocVien.Items.Count == 0)
        {
            TaiDanhSachHocVien();
        }
        else
        {
            TaiChiTietHocVien();
        }
    }

    private async void TaiDanhSachHocVien()
    {
        var version = ++_studentListLoadVersion;
        _lblHocVienTongQuan.Text = "Đang tải danh sách học viên...";
        try
        {
            var dtHv = await Task.Run(_service.LayDanhSachHocVienChoThongKe);
            if (IsDisposed || Disposing || version != _studentListLoadVersion) return;
            _cboHocVien.Items.Clear();
            foreach (System.Data.DataRow row in dtHv.Rows)
                _cboHocVien.Items.Add(new ComboItem(Convert.ToInt32(row["HocVienID"]), row["HienThi"].ToString() ?? ""));
        }
        catch { }

        if (_cboHocVien.Items.Count > 0)
        {
            _cboHocVien.SelectedIndex = 0;
            TaiChiTietHocVien();
        }
        else
        {
            _lblHocVienTongQuan.Text = "Chưa có học viên nào trong hệ thống.";
            _gridHocVien.DataSource = null;
            _chartHocVien.SetData([]);
        }
    }

    private async void TaiChiTietHocVien()
    {
        var hocVienId = HocVienId;
        if (hocVienId == null) return;
        var version = ++_studentDetailLoadVersion;
        try
        {
            Cursor = Cursors.WaitCursor;
            _lblHocVienTongQuan.Text = "Đang tải lịch sử thi...";
            var data = await Task.Run(() => _service.LayLichSuThiCuaHocVien(hocVienId.Value));
            if (IsDisposed || Disposing || version != _studentDetailLoadVersion) return;
            _gridHocVien.DataSource = data;
            Ui.RenameColumns(_gridHocVien,
                ("BaiLamID", "ID"), ("TenDotThi", "Đợt thi"), ("TenDe", "Tên đề thi"),
                ("TenMon", "Môn"), ("LanThi", "Lần thi"), ("TongDiem", "Điểm"),
                ("PhutLamBai", "Phút làm"), ("ThoiGianNop", "Nộp bài"), ("XepLoai", "Xếp loại"));
            Ui.HideColumns(_gridHocVien, "BaiLamID", "ThoiGianBatDau", "TrangThai");

            var coDiem = data.Where(d => d.TongDiem != null).Select(d => d.TongDiem!.Value).ToList();
            if (coDiem.Count > 0)
            {
                var avg = coDiem.Average();
                var max = coDiem.Max();
                var danhGia = avg >= 8.0m ? "Học lực Giỏi" : avg >= 6.5m ? "Học lực Khá" : avg >= 5.0m ? "Trung bình" : "Cần phụ đạo thêm";
                _lblHocVienTongQuan.Text = $"Số bài thi: {coDiem.Count}   |   Điểm TB: {avg:0.00}   |   Cao nhất: {max:0.00}   |   Đánh giá: {danhGia}";

                var recent = data.Take(10).Reverse().ToList();
                _chartHocVien.SetData(recent.Select((r, idx) => (r.TenMon + $" (L{r.LanThi})", (double)(r.TongDiem ?? 0))), "đ");
            }
            else
            {
                _lblHocVienTongQuan.Text = "Học viên này chưa nộp bài thi nào.";
                _chartHocVien.SetData([]);
            }
        }
        catch (Exception ex)
        {
            _lblHocVienTongQuan.Text = "Không tải được lịch sử học viên: " + ex.Message;
            _gridHocVien.DataSource = null;
            _chartHocVien.SetData([]);
        }
        finally
        {
            if (version == _studentDetailLoadVersion) Cursor = Cursors.Default;
        }
    }

    private class ComboItem(object value, string text)
    {
        public object Value { get; } = value;
        public override string ToString() => text;
    }
}
