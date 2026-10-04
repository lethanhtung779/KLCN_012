using System.Data;
using QLThiTN.WinForms.Models;
using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms.Forms.Teacher;

/// <summary>Quan ly ngan hang cau hoi: loc, CRUD, an/hien, import Excel/.txt.</summary>
public class frmQuestions : Form
{
    private readonly QuestionService _service = new();
    private readonly DataGridView _grid = new();

    private readonly ComboBox _cboMon = Ui.Combo(150);
    private readonly ComboBox _cboChuDe = Ui.Combo(180);
    private readonly ComboBox _cboLoai = Ui.Combo(150);
    private readonly ComboBox _cboMuc = Ui.Combo(150);
    private readonly TextBox _txtTuKhoa = Ui.Input(170);
    private readonly Label _lblCount = Ui.Label("", Ui.Small, Ui.TextMuted);

    public frmQuestions()
    {
        Text = "Ngân hàng câu hỏi";
        Font = Ui.Body;

        var lblMon = Ui.Label("Môn:", Ui.Small, Ui.TextMuted);
        _cboMon.SelectedIndexChanged += (s, e) => { TaiChuDe(); TaiLai(); };
        var lblChuDe = Ui.Label("Chủ đề:", Ui.Small, Ui.TextMuted);
        _cboChuDe.SelectedIndexChanged += (s, e) => TaiLai();
        var lblLoai = Ui.Label("Loại:", Ui.Small, Ui.TextMuted);
        _cboLoai.SelectedIndexChanged += (s, e) => TaiLai();
        var lblMuc = Ui.Label("Độ khó:", Ui.Small, Ui.TextMuted);
        _cboMuc.SelectedIndexChanged += (s, e) => TaiLai();
        _txtTuKhoa.PlaceholderText = "Tìm trong nội dung...";
        _txtTuKhoa.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) TaiLai(); };

        var btnThem = Ui.ButtonAccent("＋ Thêm câu hỏi", 160);
        btnThem.Click += (s, e) => Them();
        var btnSua = Ui.ButtonPrimary("✎ Sửa", 100);
        btnSua.Click += (s, e) => Sua();
        var btnAn = Ui.ButtonOutline("👁 Ẩn / Hiện", 130);
        btnAn.Click += (s, e) => AnHien();
        var btnXoa = Ui.ButtonDanger("🗑 Xóa", 100);
        btnXoa.Click += (s, e) => Xoa();
        var btnImport = Ui.ButtonOutline("📥 Import (Excel/.txt)", 190);
        btnImport.Click += (s, e) => ImportFile();
        var btnMau = Ui.ButtonOutline("📄 Tạo file mẫu", 150);
        btnMau.Click += (s, e) => TaoFileMau();

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 96,
            BackColor = Color.White,
            WrapContents = false
        };

        var row1 = new FlowLayoutPanel { Width = 2000, Height = 44, Padding = new Padding(16, 10, 0, 0), WrapContents = false, BackColor = Color.Transparent };
        foreach (Control c in new Control[] { lblMon, _cboMon, lblChuDe, _cboChuDe, lblLoai, _cboLoai, lblMuc, _cboMuc, _txtTuKhoa })
        {
            c.Margin = new Padding(0, 4, 8, 0);
            row1.Controls.Add(c);
        }
        var row2 = new FlowLayoutPanel { Width = 2000, Height = 50, Padding = new Padding(16, 2, 0, 0), WrapContents = false, BackColor = Color.Transparent };
        foreach (var c in new Control[] { btnThem, btnSua, btnAn, btnXoa, btnImport, btnMau })
        {
            c.Margin = new Padding(0, 3, 10, 0);
            row2.Controls.Add(c);
        }
        toolbar.Controls.Add(row1);
        toolbar.Controls.Add(row2);

        var panelBottom = new Panel { Dock = DockStyle.Bottom, Height = 32, BackColor = Color.White };
        _lblCount.Location = new Point(16, 7);
        panelBottom.Controls.Add(_lblCount);

        Ui.StyleGrid(_grid);
        _grid.Dock = DockStyle.Fill;
        _grid.CellDoubleClick += (s, e) => Sua();

        Controls.Add(_grid);
        Controls.Add(panelBottom);
        Controls.Add(toolbar);

        // Danh muc
        _cboMon.Items.Add(new ComboItem(0, "Tất cả môn"));
        foreach (var m in _service.LayMonHoc()) _cboMon.Items.Add(new ComboItem(m.MonHocID, m.TenMon));
        _cboMon.SelectedIndex = 0;

        _cboLoai.Items.Add(new ComboItem("", "Tất cả loại"));
        _cboLoai.Items.Add(new ComboItem(LoaiCauHoi.TracNghiem, "Trắc nghiệm"));
        _cboLoai.Items.Add(new ComboItem(LoaiCauHoi.DungSai, "Đúng/Sai"));
        _cboLoai.Items.Add(new ComboItem(LoaiCauHoi.TraLoiNgan, "Trả lời ngắn"));
        _cboLoai.SelectedIndex = 0;

        _cboMuc.Items.Add(new ComboItem("", "Tất cả độ khó"));
        foreach (var (code, display) in MucDoKho.All) _cboMuc.Items.Add(new ComboItem(code, display));
        _cboMuc.SelectedIndex = 0;

        TaiLai();
    }

    private class ComboItem(object value, string text)
    {
        public object Value { get; } = value;
        public override string ToString() => text;
    }

    private int MonId => (_cboMon.SelectedItem as ComboItem)?.Value as int? ?? 0;
    private int ChuDeId => (_cboChuDe.SelectedItem as ComboItem)?.Value as int? ?? 0;
    private string Loai => (_cboLoai.SelectedItem as ComboItem)?.Value as string ?? "";
    private string Muc => (_cboMuc.SelectedItem as ComboItem)?.Value as string ?? "";

    private void TaiChuDe()
    {
        _cboChuDe.Items.Clear();
        _cboChuDe.Items.Add(new ComboItem(0, "Tất cả chủ đề"));
        foreach (var cd in _service.LayChuDe(MonId > 0 ? MonId : null))
            _cboChuDe.Items.Add(new ComboItem(cd.ChuDeID, cd.TenChuDe));
        _cboChuDe.SelectedIndex = 0;
    }

    private void TaiLai()
    {
        var dt = _service.TraCuu(MonId, ChuDeId, Loai, Muc, _txtTuKhoa.Text);
        _grid.DataSource = dt;
        Ui.RenameColumns(_grid,
            ("CauHoiID", "ID"), ("NoiDung", "Nội dung câu hỏi"), ("LoaiCauHoi", "Loại"),
            ("MucDoKho", "Độ khó"), ("Diem", "Điểm"), ("TrangThai", "Trạng thái"),
            ("TenChuDe", "Chủ đề"), ("TenMon", "Môn"), ("TenGiaoVien", "Người soạn"), ("NgayTao", "Ngày tạo"));
        Ui.HideColumns(_grid, "CauHoiID", "ChuDeID", "GiaoVienID", "NgayTao");

        // Hien thi loai / do kho / trang thai bang tieng Viet
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (_grid.Columns.Contains("LoaiCauHoi"))
                row.Cells["LoaiCauHoi"].Value = LoaiCauHoi.Display(row.Cells["LoaiCauHoi"].Value?.ToString());
            if (_grid.Columns.Contains("MucDoKho"))
                row.Cells["MucDoKho"].Value = MucDoKho.Display(row.Cells["MucDoKho"].Value?.ToString());
            if (_grid.Columns.Contains("TrangThai"))
                row.Cells["TrangThai"].Value = row.Cells["TrangThai"].Value?.ToString() == "HoatDong" ? "Hoạt động" : "Đã ẩn";
        }
        _grid.Columns["NoiDung"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        _lblCount.Text = $"Tổng cộng: {dt.Rows.Count} câu hỏi";
    }

    private int IdDangChon() =>
        _grid.CurrentRow != null && _grid.CurrentRow.Cells["CauHoiID"].Value != null
            ? Convert.ToInt32(_grid.CurrentRow.Cells["CauHoiID"].Value)
            : 0;

    private void Them()
    {
        using var f = new frmQuestionEdit(0, MonId);
        if (f.ShowDialog(FindForm()) == DialogResult.OK) TaiLai();
    }

    private void Sua()
    {
        var id = IdDangChon();
        if (id == 0) { MessageBox.Show("Hãy chọn một câu hỏi.", "Thông báo"); return; }
        using var f = new frmQuestionEdit(id);
        if (f.ShowDialog(FindForm()) == DialogResult.OK) TaiLai();
    }

    private void AnHien()
    {
        var id = IdDangChon();
        if (id == 0) { MessageBox.Show("Hãy chọn một câu hỏi.", "Thông báo"); return; }
        var trangThai = _grid.CurrentRow!.Cells["TrangThai"].Value?.ToString();
        _service.DoiTrangThai(id, trangThai == "Hoạt động");
        TaiLai();
    }

    private void Xoa()
    {
        var id = IdDangChon();
        if (id == 0) { MessageBox.Show("Hãy chọn một câu hỏi.", "Thông báo"); return; }
        if (MessageBox.Show("Bạn chắc chắn muốn XÓA câu hỏi này khỏi ngân hàng?",
                "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        var (ok, error) = _service.Xoa(id);
        if (!ok) { MessageBox.Show(error, "Không thể xóa", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        TaiLai();
    }

    // ------------------------------------------------------------------ import
    private void ImportFile()
    {
        using var dlg = new OpenFileDialog
        {
            Filter = "File câu hỏi (*.xlsx;*.csv;*.txt)|*.xlsx;*.csv;*.txt",
            Title = "Chọn file câu hỏi import"
        };
        if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;

        try
        {
            var rows = File.Exists(dlg.FileName) && dlg.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
                ? DocExcel(dlg.FileName)
                : DocText(dlg.FileName);

            if (rows.Count == 0) { MessageBox.Show("File không có dữ liệu (hoặc thiếu dòng header).", "Thông báo"); return; }

            var (them, loi, chiTiet) = _service.Import(rows);
            var msg = $"Import hoàn tất: thêm {them} câu hỏi, lỗi {loi} dòng.";
            if (chiTiet.Count > 0)
                msg += "\n\nChi tiết lỗi:\n" + string.Join("\n", chiTiet.Take(15)) + (chiTiet.Count > 15 ? "\n..." : "");
            MessageBox.Show(msg, "Kết quả import", MessageBoxButtons.OK,
                chiTiet.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
            TaiLai();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không đọc được file:\n" + ex.Message, "Lỗi import", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static List<(string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string)> DocExcel(string file)
    {
        var list = new List<(string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string)>();
        using var wb = new ClosedXML.Excel.XLWorkbook(file);
        var ws = wb.Worksheets.First();
        var used = ws.RangeUsed();
        if (used == null) return list;

        for (var r = 2; r <= used.LastRow().RowNumber(); r++) // bo qua header
        {
            string Get(int col) => used.Cell(r, col).GetString().Trim();
            list.Add((Get(1), Get(2), Get(3), Get(4), Get(5), Get(6), Get(7), Get(8), Get(9),
                Get(10), Get(11), Get(12), Get(13), Get(14), Get(15), Get(16)));
        }
        return list;
    }

    private static List<(string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string)> DocText(string file)
    {
        var list = new List<(string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string)>();
        foreach (var line in File.ReadAllLines(file, System.Text.Encoding.UTF8))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
            var parts = line.Split('|');
            if (parts.Length < 16 || parts[1].Trim() == "NoiDung") continue; // bo header
            var g = (int i) => i < parts.Length ? parts[i].Trim() : "";
            list.Add((g(0), g(1), g(2), g(3), g(4), g(5), g(6), g(7), g(8), g(9), g(10), g(11), g(12), g(13), g(14), g(15)));
        }
        return list;
    }

    private void TaoFileMau()
    {
        try
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "mau_import_cau_hoi.xlsx");
            using var wb = new ClosedXML.Excel.XLWorkbook();
            var ws = wb.Worksheets.Add("CauHoi");
            for (var i = 0; i < QuestionService.HeaderImport.Length; i++)
                ws.Cell(1, i + 1).Value = QuestionService.HeaderImport[i];
            ws.Row(1).Style.Font.Bold = true;
            ws.Row(1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#1B2A4E");
            ws.Row(1).Style.Font.FontColor = ClosedXML.Excel.XLColor.White;

            // Vi du minh hoa
            ws.Cell(2, 1).Value = "Ham so va Dao ham";
            ws.Cell(2, 2).Value = "Tiệm cận ngang của hàm số y = (2x+1)/(x-3) là?";
            ws.Cell(2, 3).Value = "TracNghiem";
            ws.Cell(2, 4).Value = "NhanBiet";
            ws.Cell(2, 5).Value = "0.25";
            ws.Cell(2, 6).Value = "y = 2";
            ws.Cell(2, 7).Value = "y = 3";
            ws.Cell(2, 8).Value = "y = -2";
            ws.Cell(2, 9).Value = "y = -3";
            ws.Cell(2, 10).Value = "A";
            ws.Cell(2, 16).Value = "Ví dụ trắc nghiệm";
            ws.Cell(3, 1).Value = "Ham so va Dao ham";
            ws.Cell(3, 2).Value = "Nhận định sau đúng hay sai: Hàm số y = x^3 đồng biến trên R.";
            ws.Cell(3, 3).Value = "DungSai";
            ws.Cell(3, 6).Value = "Hàm đồng biến trên R";
            ws.Cell(3, 11).Value = "D";
            ws.Cell(4, 1).Value = "Ham so va Dao ham";
            ws.Cell(4, 2).Value = "Cho hàm f(x) = x^2. Tính f(3).";
            ws.Cell(4, 3).Value = "TraLoiNgan";
            ws.Cell(4, 15).Value = "9";

            ws.Columns().AdjustToContents();
            wb.SaveAs(path);
            MessageBox.Show($"Đã tạo file mẫu tại:\n{path}\n\nChủ đề phải trùng tên chủ đề đã có trong hệ thống (tab Câu hỏi -> cột Chủ đề).",
                "Tạo file mẫu", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không tạo được file mẫu: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
