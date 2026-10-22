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
    private readonly CheckBox _chkLocTrung = new() { Text = "Chỉ hiện câu trùng", AutoSize = true, Font = Ui.Body, ForeColor = Ui.Danger, Margin = new Padding(6, 26, 8, 0), Cursor = Cursors.Hand };
    private readonly Label _lblCount = Ui.Label("", Ui.Small, Ui.TextMuted);
    private readonly Label _lblEmpty = Ui.Label("", Ui.Body, Ui.TextMuted);

    private bool _dangKhoiTao = true;
    private int _loadVersion;

    public frmQuestions()
    {
        Text = "Ngân hàng câu hỏi";
        Font = Ui.Body;

        var lblMon = Ui.Label("Môn:", Ui.Small, Ui.TextMuted);
        _cboMon.SelectedIndexChanged += (s, e) =>
        {
            if (_dangKhoiTao) return;
            _dangKhoiTao = true;
            TaiChuDe();
            _dangKhoiTao = false;
            TaiLai();
        };
        var lblChuDe = Ui.Label("Chủ đề:", Ui.Small, Ui.TextMuted);
        _cboChuDe.SelectedIndexChanged += (s, e) => { if (!_dangKhoiTao) TaiLai(); };
        var lblLoai = Ui.Label("Loại:", Ui.Small, Ui.TextMuted);
        _cboLoai.SelectedIndexChanged += (s, e) => { if (!_dangKhoiTao) TaiLai(); };
        var lblMuc = Ui.Label("Độ khó:", Ui.Small, Ui.TextMuted);
        _cboMuc.SelectedIndexChanged += (s, e) => { if (!_dangKhoiTao) TaiLai(); };
        _txtTuKhoa.PlaceholderText = "Tìm trong nội dung...";
        _txtTuKhoa.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter && !_dangKhoiTao) TaiLai(); };
        _chkLocTrung.CheckedChanged += (s, e) => { if (!_dangKhoiTao) TaiLai(); };

        var btnThem = Ui.ButtonAccent("Thêm câu hỏi", 160);
        btnThem.Click += (s, e) => Them();
        var btnSua = Ui.ButtonPrimary("Sửa", 100);
        btnSua.Click += (s, e) => Sua();
        var btnAn = Ui.ButtonOutline("Ẩn / Hiện", 130);
        btnAn.Click += (s, e) => AnHien();
        var btnXoa = Ui.ButtonDanger("Xóa", 100);
        btnXoa.Click += (s, e) => Xoa();
        var btnImport = Ui.ButtonOutline("Nhập Excel/TXT/ZIP", 220);
        btnImport.Click += (s, e) => ImportFile();
        var btnMau = Ui.ButtonOutline("Tạo file mẫu", 150);
        btnMau.Click += (s, e) => TaoFileMau();
        var btnLamMoi = Ui.ButtonOutline("Làm mới", 100);
        btnLamMoi.Click += (s, e) => TaiLai();

        var toolbar = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 164,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.White,
        };
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        toolbar.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        toolbar.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
        Resize += (_, _) => UpdateToolbarLayout(toolbar);

        var row1 = new FlowLayoutPanel { Padding = new Padding(16, 10, 0, 0), WrapContents = true, BackColor = Color.Transparent };
        foreach (Control c in new Control[]
                 {
                     FilterGroup(lblMon, _cboMon), FilterGroup(lblChuDe, _cboChuDe),
                     FilterGroup(lblLoai, _cboLoai), FilterGroup(lblMuc, _cboMuc), _txtTuKhoa, _chkLocTrung
                 })
        {
            c.Margin = new Padding(0, 3, 8, 0);
            row1.Controls.Add(c);
        }
        var row2 = new FlowLayoutPanel { Padding = new Padding(16, 2, 0, 0), WrapContents = true, BackColor = Color.Transparent };
        foreach (var c in new Control[] { btnThem, btnSua, btnAn, btnXoa, btnImport, btnMau, btnLamMoi })
        {
            c.Margin = new Padding(0, 3, 10, 0);
            row2.Controls.Add(c);
        }
        foreach (var row in new[] { row1, row2 })
        {
            row.Dock = DockStyle.Fill;
            row.Margin = Padding.Empty;
        }
        toolbar.Controls.Add(row1, 0, 0);
        toolbar.Controls.Add(row2, 0, 1);

        var panelBottom = new Panel { Dock = DockStyle.Bottom, Height = 32, BackColor = Color.White };
        _lblCount.Location = new Point(16, 7);
        panelBottom.Controls.Add(_lblCount);

        Ui.StyleGrid(_grid);
        _grid.Dock = DockStyle.Fill;
        _grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) Sua(); };
        _lblEmpty.AutoSize = false;
        _lblEmpty.Size = new Size(560, 68);
        _lblEmpty.TextAlign = ContentAlignment.MiddleCenter;
        _lblEmpty.BackColor = Color.White;
        _lblEmpty.Visible = false;
        _grid.Controls.Add(_lblEmpty);
        _grid.Resize += (_, _) => CenterEmptyMessage();

        Controls.Add(_grid);
        Controls.Add(panelBottom);
        Controls.Add(toolbar);
        UpdateToolbarLayout(toolbar);

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

        _dangKhoiTao = false;
        TaiChuDe();
        TaiLai();
    }

    private class ComboItem(object value, string text)
    {
        public object Value { get; } = value;
        public override string ToString() => text;
    }

    private static FlowLayoutPanel FilterGroup(Label label, ComboBox combo)
    {
        var group = new FlowLayoutPanel
        {
            Width = label.PreferredWidth + combo.Width + 12,
            Height = 32,
            WrapContents = false,
            Margin = Padding.Empty,
        };
        label.Margin = new Padding(0, 6, 6, 0);
        combo.Margin = Padding.Empty;
        group.Controls.Add(label);
        group.Controls.Add(combo);
        return group;
    }

    private void UpdateToolbarLayout(TableLayoutPanel toolbar)
    {
        var wide = ClientSize.Width >= 1100;
        toolbar.Height = wide ? 94 : 164;
        toolbar.RowStyles[0].Height = wide ? 44 : 78;
        toolbar.RowStyles[1].Height = wide ? 50 : 86;
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

    private async void TaiLai()
    {
        var version = ++_loadVersion;
        try
        {
            UseWaitCursor = true;
            _lblCount.Text = "Đang tải dữ liệu câu hỏi...";
            var monId = MonId;
            var chuDeId = ChuDeId;
            var loai = Loai;
            var muc = Muc;
            var tuKhoa = _txtTuKhoa.Text;

            var locTrung = _chkLocTrung.Checked;
            var dt = await Task.Run(() => _service.TraCuu(monId, chuDeId, loai, muc, tuKhoa, locTrung));
            if (IsDisposed || version != _loadVersion) return;

            _grid.DataSource = dt;
            Ui.RenameColumns(_grid,
                ("CauHoiID", "ID"), ("NoiDung", "Nội dung câu hỏi"), ("LoaiCauHoi", "Loại"),
                ("MucDoKho", "Độ khó"), ("Diem", "Điểm"), ("TrangThai", "Trạng thái"),
                ("TenChuDe", "Chủ đề"), ("TenMon", "Môn"), ("TenGiaoVien", "Người soạn"), ("NgayTao", "Ngày tạo"));
            Ui.HideColumns(_grid, "CauHoiID", "ChuDeID", "GiaoVienID", "NgayTao");

            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            foreach (var (column, width) in new[]
                     {
                         ("NoiDung", 300), ("LoaiCauHoi", 120), ("MucDoKho", 110),
                         ("Diem", 65), ("TrangThai", 110), ("TenChuDe", 130),
                         ("TenMon", 110), ("TenGiaoVien", 140)
                     })
                if (_grid.Columns.Contains(column)) _grid.Columns[column]!.Width = width;

            if (locTrung)
            {
                _lblCount.Text = dt.Rows.Count > 0
                    ? $"⚠ Phát hiện {dt.Rows.Count:N0} câu hỏi bị trùng lặp nội dung trong ngân hàng!"
                    : "Không phát hiện câu hỏi nào bị trùng lặp.";
                foreach (DataGridViewRow r in _grid.Rows)
                {
                    r.DefaultCellStyle.BackColor = Color.FromArgb(255, 246, 235);
                }
            }
            else
            {
                _lblCount.Text = $"Tổng cộng: {dt.Rows.Count:N0} câu hỏi";
            }

            _lblEmpty.Text = dt.Rows.Count == 0
                ? locTrung
                    ? "Không có câu hỏi nào bị trùng lặp nội dung trong bộ lọc hiện tại."
                    : Session.IsGiaoVien && _cboMon.Items.Count == 1
                        ? "Bạn chưa được phân công môn học.\nLiên hệ quản trị viên để được phân công và sử dụng ngân hàng câu hỏi."
                        : "Không tìm thấy câu hỏi phù hợp.\nHãy thử thay đổi bộ lọc hoặc thêm câu hỏi mới."
                : "";
            _lblEmpty.Visible = dt.Rows.Count == 0;
            CenterEmptyMessage();
        }
        catch (Exception ex)
        {
            if (!IsDisposed && version == _loadVersion)
            {
                _lblCount.Text = "Lỗi khi tải dữ liệu.";
                MessageBox.Show("Không thể tải danh sách câu hỏi:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        finally
        {
            if (!IsDisposed && version == _loadVersion) UseWaitCursor = false;
        }
    }

    private void CenterEmptyMessage()
    {
        _lblEmpty.Left = Math.Max(0, (_grid.ClientSize.Width - _lblEmpty.Width) / 2);
        _lblEmpty.Top = Math.Max(_grid.ColumnHeadersHeight + 24, (_grid.ClientSize.Height - _lblEmpty.Height) / 2);
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

        try
        {
            var (ok, error) = _service.Xoa(id);
            if (!ok) { MessageBox.Show(error, "Không thể xóa", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            TaiLai();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không xóa được câu hỏi:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ------------------------------------------------------------------ import
    private async void ImportFile()
    {
        using var dlg = new OpenFileDialog
        {
            Filter = "File câu hỏi (*.xlsx;*.txt;*.csv;*.zip)|*.xlsx;*.txt;*.csv;*.zip",
            Title = "Chọn file câu hỏi hoặc ZIP kèm ảnh"
        };
        if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
        try
        {
            UseWaitCursor = true; Enabled = false;
            var importer = new QuestionImportService(_service.LayChuDe(), _service.LayMonHoc());
            var rows = await Task.Run(() => importer.Read(dlg.FileName));
            if (IsDisposed) return;
            Enabled = true; UseWaitCursor = false;
            if (rows.Count == 0) { MessageBox.Show("File không có dòng câu hỏi.", "Thông báo"); return; }
            using var preview = new frmQuestionImport(rows);
            preview.ShowDialog(FindForm());
            if (preview.ImportedCount > 0) TaiLai();
        }
        catch (Exception ex)
        {
            if (!IsDisposed) MessageBox.Show("Không đọc được file:\n" + ex.Message, "Lỗi import", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { if (!IsDisposed) { Enabled = true; UseWaitCursor = false; } }
    }

    private void TaoFileMau()
    {
        using var save = new SaveFileDialog
        {
            Filter = "Gói ZIP có ảnh mẫu (*.zip)|*.zip|Excel (*.xlsx)|*.xlsx|Văn bản TAB (*.txt)|*.txt",
            FileName = "mau_import_cau_hoi", Title = "Lưu mẫu import", AddExtension = true
        };
        if (save.ShowDialog(FindForm()) != DialogResult.OK) return;
        try
        {
            var topics = _service.LayChuDe(MonId > 0 ? MonId : null);
            var topic = topics.FirstOrDefault(t => t.ChuDeID == ChuDeId) ?? topics.FirstOrDefault(t => t.TenChuDe == "Hàm số và Đạo hàm") ?? topics.FirstOrDefault();
            if (topic == null) { MessageBox.Show("Chưa có chủ đề để tạo mẫu."); return; }
            var subject = _service.LayMonHoc().First(m => m.MonHocID == topic.MonHocID);
            // Tạo ở file tạm cùng thư mục; chỉ thay file người dùng chọn sau khi tạo thành công.
            var temp = Path.Combine(Path.GetDirectoryName(save.FileName)!, Guid.NewGuid().ToString("N") + Path.GetExtension(save.FileName));
            try
            {
                QuestionImportTemplate.Create(temp, topic.TenChuDe, subject.TenMon);
                File.Move(temp, save.FileName, overwrite: true);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
            MessageBox.Show($"Đã tạo mẫu tại:\n{save.FileName}\n\nThay các câu ví dụ trước khi nhập. ZIP có ảnh mẫu; Excel có sheet HuongDan.", "Tạo file mẫu");
        }
        catch (Exception ex) { MessageBox.Show("Không tạo được mẫu: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}
