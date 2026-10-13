using System.Data;
using QLThiTN.WinForms.Models;
using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms.Forms.Teacher;

/// <summary>
/// Màn hình Tạo đề thi theo Ma trận Chủ đề chuẩn cấu trúc đề thi tốt nghiệp THPT Quốc gia (Bộ GD&ĐT).
/// Cho phép giáo viên phân bổ số lượng câu hỏi từng chủ đề cho Phần I, Phần II, Phần III.
/// </summary>
public class frmTaoDeTHPTQG : Form
{
    private readonly QuestionService _questionService = new();
    private readonly ExamService _examService;

    // Controls thông tin đề
    private readonly ComboBox _cboMon = Ui.Combo(240);
    private readonly TextBox _txtTenDe = Ui.Input(360);
    private readonly ComboBox _cboLoaiDe = Ui.Combo(180);
    private readonly NumericUpDown _numSoMaDe = new() { Width = 80, Minimum = 1, Maximum = 24, Value = 4, Font = Ui.Body };
    private readonly CheckBox _chkTuDongTron = new() { Text = "🔀 Tự động trộn & sinh luôn 4 mã đề (101, 102...)", AutoSize = true, Checked = true, Font = Ui.Body };

    // Grid ma trận chủ đề
    private readonly DataGridView _gridMaTran = new();

    // Thống kê tổng số câu
    private readonly Label _lblTongKet = Ui.Label("", Ui.H2, Ui.Navy);
    private readonly Label _lblGoiYChuan = Ui.Label("", Ui.Small, Ui.TextMuted);
    private readonly Label _lblError = Ui.Label("", Ui.Small, Ui.Danger);

    public int DeThiIdMoi { get; private set; }

    public frmTaoDeTHPTQG(int? giaoVienId = null)
    {
        _examService = new ExamService(giaoVienId);
        Text = "Tạo đề thi chuẩn cấu trúc THPT Quốc gia (Ma trận Chủ đề)";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(1020, 720);
        MinimumSize = new Size(920, 600);
        Font = Ui.Body;
        BackColor = Ui.BgSoft;

        // Top Panel: Tiêu đề + Hướng dẫn
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 85,
            BackColor = Color.White,
            Padding = new Padding(20, 12, 20, 8)
        };
        var lblTitle = Ui.Label("🎓 TẠO ĐỀ THI CHUẨN MA TRẬN THPT QUỐC GIA", Ui.H1, Ui.Navy);
        lblTitle.Location = new Point(20, 10);
        var lblSub = Ui.Label("Cấu trúc 3 phần theo chuẩn Bộ GD&ĐT: Phần I (Trắc nghiệm A-D), Phần II (Đúng/Sai 4 ý), Phần III (Trả lời ngắn). Phân bổ số câu linh hoạt theo từng chủ đề.",
            Ui.Small, Ui.TextMuted);
        lblSub.Location = new Point(20, 42);
        pnlHeader.Controls.AddRange([lblTitle, lblSub]);

        // Config Panel: Môn học + Tên đề + Nút nạp chuẩn
        var pnlConfig = new Panel
        {
            Dock = DockStyle.Top,
            Height = 110,
            BackColor = Color.FromArgb(248, 250, 253),
            Padding = new Padding(20, 10, 20, 10)
        };

        var lblMon = Ui.Label("Môn học *", Ui.Body, Ui.Navy);
        lblMon.Location = new Point(20, 12);
        _cboMon.Location = new Point(20, 34);
        foreach (var m in _questionService.LayMonHoc())
            _cboMon.Items.Add(new ComboItem(m.MonHocID, m.TenMon));
        if (_cboMon.Items.Count > 0) _cboMon.SelectedIndex = 0;
        _cboMon.SelectedIndexChanged += (s, e) => TaiMaTranChuDe();

        var lblTen = Ui.Label("Tên đề thi *", Ui.Body, Ui.Navy);
        lblTen.Location = new Point(280, 12);
        _txtTenDe.Location = new Point(280, 34);

        var lblLoai = Ui.Label("Loại đề", Ui.Body, Ui.Navy);
        lblLoai.Location = new Point(660, 12);
        _cboLoaiDe.Location = new Point(660, 34);
        _cboLoaiDe.Items.Add(new ComboItem("ThiThu", "Thi thử"));
        _cboLoaiDe.Items.Add(new ComboItem("ChinhThuc", "Chính thức"));
        _cboLoaiDe.Items.Add(new ComboItem("OnTap", "Bài tập về nhà"));
        _cboLoaiDe.SelectedIndex = 0;

        var btnNapChuan = Ui.ButtonAccent("⚡ Nạp Ma trận chuẩn Bộ GD&ĐT", 250, 36);
        btnNapChuan.Location = new Point(20, 68);
        btnNapChuan.Click += (s, e) => NapMaTranChuanBo();

        _lblGoiYChuan.Location = new Point(290, 76);
        _lblGoiYChuan.AutoSize = true;

        pnlConfig.Controls.AddRange([lblMon, _cboMon, lblTen, _txtTenDe, lblLoai, _cboLoaiDe, btnNapChuan, _lblGoiYChuan]);

        // Center Panel: Bảng Ma trận
        var pnlGridWrap = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(20, 10, 20, 10)
        };

        var lblGridTitle = Ui.Label("Bảng phân bổ số lượng câu hỏi theo Chủ đề:", Ui.H2, Ui.Navy);
        lblGridTitle.Dock = DockStyle.Top;
        lblGridTitle.Height = 28;

        Ui.StyleGrid(_gridMaTran);
        _gridMaTran.Dock = DockStyle.Fill;
        _gridMaTran.ReadOnly = false;
        _gridMaTran.CellValueChanged += (s, e) => CapNhatTongKet();
        _gridMaTran.CurrentCellDirtyStateChanged += (s, e) =>
        {
            if (_gridMaTran.IsCurrentCellDirty)
                _gridMaTran.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };
        _gridMaTran.DataError += (s, e) => { e.Cancel = false; };

        pnlGridWrap.Controls.Add(_gridMaTran);
        pnlGridWrap.Controls.Add(lblGridTitle);

        // Bottom Panel: Tổng kết + Nút Tạo
        var pnlBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 115,
            BackColor = Color.White,
            Padding = new Padding(20, 10, 20, 10)
        };

        _lblTongKet.Location = new Point(20, 10);
        _lblTongKet.AutoSize = true;

        _lblError.Location = new Point(20, 42);
        _lblError.AutoSize = true;

        _chkTuDongTron.Location = new Point(20, 70);
        var lblSoMa = Ui.Label("Số mã đề:", Ui.Body, Ui.Navy);
        lblSoMa.Location = new Point(410, 72);
        _numSoMaDe.Location = new Point(490, 70);

        var btnTaoDe = Ui.ButtonAccent("⚡ Tạo đề thi & Sinh mã đề", 220, 42);
        Ui.GradientButton(btnTaoDe, Ui.Accent, Ui.Accent2);
        btnTaoDe.Location = new Point(620, 62);
        btnTaoDe.Click += (s, e) => ThucHienTaoDe();

        var btnHuy = Ui.ButtonOutline("Hủy", 100, 42);
        btnHuy.Location = new Point(860, 62);
        btnHuy.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        pnlBottom.Controls.AddRange([_lblTongKet, _lblError, _chkTuDongTron, lblSoMa, _numSoMaDe, btnTaoDe, btnHuy]);

        Controls.Add(pnlGridWrap);
        Controls.Add(pnlBottom);
        Controls.Add(pnlConfig);
        Controls.Add(pnlHeader);

        Load += (s, e) => TaiMaTranChuDe();
    }

    private class ComboItem(object value, string text)
    {
        public object Value { get; } = value;
        public override string ToString() => text;
    }

    private void TaiMaTranChuDe()
    {
        if (_cboMon.SelectedItem == null) return;
        var monId = (int)(_cboMon.SelectedItem as ComboItem)!.Value!;
        var tenMon = (_cboMon.SelectedItem as ComboItem)!.ToString() ?? "";

        _txtTenDe.Text = $"Đề thi thử THPT Quốc gia - Môn {tenMon} ({DateTime.Now:dd/MM/yyyy})";

        var dtPool = _examService.LayMaTranChuDeTheoMon(monId);

        // Tạo bảng giao diện ma trận
        var table = new DataTable();
        table.Columns.Add("ChuDeID", typeof(int));
        table.Columns.Add("TenChuDe", typeof(string));
        table.Columns.Add("SoTN", typeof(int));
        table.Columns.Add("KhoTN", typeof(string));
        table.Columns.Add("SoDS", typeof(int));
        table.Columns.Add("KhoDS", typeof(string));
        table.Columns.Add("SoTLN", typeof(int));
        table.Columns.Add("KhoTLN", typeof(string));
        table.Columns.Add("TongCau", typeof(int));

        foreach (DataRow r in dtPool.Rows)
        {
            var row = table.NewRow();
            row["ChuDeID"] = r["ChuDeID"];
            row["TenChuDe"] = r["TenChuDe"];
            row["SoTN"] = 0;
            row["KhoTN"] = $"/{r["SoTN"]}";
            row["SoDS"] = 0;
            row["KhoDS"] = $"/{r["SoDS"]}";
            row["SoTLN"] = 0;
            row["KhoTLN"] = $"/{r["SoTLN"]}";
            row["TongCau"] = 0;
            table.Rows.Add(row);
        }

        _gridMaTran.DataSource = table;

        Ui.RenameColumns(_gridMaTran,
            ("TenChuDe", "Tên Chủ đề / Chuyên đề"),
            ("SoTN", "Phần I: TN (A-D)"),
            ("KhoTN", "Kho TN"),
            ("SoDS", "Phần II: Đúng/Sai"),
            ("KhoDS", "Kho Đ/S"),
            ("SoTLN", "Phần III: Trả lời ngắn"),
            ("KhoTLN", "Kho TLN"),
            ("TongCau", "Tổng câu"));
        Ui.HideColumns(_gridMaTran, "ChuDeID");

        // Cấu hình cột nào được sửa, cột nào chỉ xem
        foreach (DataGridViewColumn col in _gridMaTran.Columns)
        {
            if (col.Name is "SoTN" or "SoDS" or "SoTLN")
            {
                col.ReadOnly = false;
                col.DefaultCellStyle.BackColor = Color.FromArgb(255, 253, 235);
                col.DefaultCellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            else
            {
                col.ReadOnly = true;
                if (col.Name is "KhoTN" or "KhoDS" or "KhoTLN")
                {
                    col.DefaultCellStyle.ForeColor = Ui.TextMuted;
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                }
            }
        }

        // Tự động nạp cấu trúc chuẩn luôn khi vừa chọn môn
        NapMaTranChuanBo();
    }

    private void NapMaTranChuanBo()
    {
        if (_gridMaTran.Rows.Count == 0 || _cboMon.SelectedItem == null) return;
        var tenMon = (_cboMon.SelectedItem as ComboItem)!.ToString() ?? "";

        int chuanTN = 18;
        int chuanDS = 4;
        int chuanTLN = 6;

        if (tenMon.Contains("Toan", StringComparison.OrdinalIgnoreCase) || tenMon.Contains("Toán", StringComparison.OrdinalIgnoreCase))
        {
            chuanTN = 12;
            chuanDS = 4;
            chuanTLN = 6;
            _lblGoiYChuan.Text = "Chuẩn Bộ GD&ĐT môn Toán: 12 câu Phần I, 4 câu Phần II, 6 câu Phần III (Tổng 22 câu / 90 phút).";
        }
        else if (tenMon.Contains("Tieng Anh", StringComparison.OrdinalIgnoreCase) || tenMon.Contains("Tiếng Anh", StringComparison.OrdinalIgnoreCase))
        {
            chuanTN = 40;
            chuanDS = 0;
            chuanTLN = 0;
            _lblGoiYChuan.Text = "Chuẩn Bộ GD&ĐT môn Tiếng Anh: 40 câu trắc nghiệm 4 lựa chọn (Tổng 40 câu / 50 phút).";
        }
        else if (tenMon.Contains("Lich Su", StringComparison.OrdinalIgnoreCase) || tenMon.Contains("Lịch sử", StringComparison.OrdinalIgnoreCase))
        {
            chuanTN = 18;
            chuanDS = 4;
            chuanTLN = 0;
            _lblGoiYChuan.Text = "Chuẩn Bộ GD&ĐT môn Lịch sử: 18 câu Phần I, 4 câu Phần II (Tổng 22 câu / 50 phút).";
        }
        else
        {
            // Vật lý, Hóa học, Sinh học, Địa lí, Tin học, Công nghệ
            _lblGoiYChuan.Text = $"Chuẩn Bộ GD&ĐT môn {tenMon}: 18 câu Phần I, 4 câu Phần II, 6 câu Phần III (Tổng 28 câu / 50 phút).";
        }

        // Phân bổ đều và thông minh theo số câu có sẵn trong kho
        var rows = _gridMaTran.Rows.Cast<DataGridViewRow>().ToList();
        var n = rows.Count;
        if (n == 0) return;

        // Reset về 0
        foreach (var r in rows)
        {
            r.Cells["SoTN"].Value = 0;
            r.Cells["SoDS"].Value = 0;
            r.Cells["SoTLN"].Value = 0;
            r.Cells["TongCau"].Value = 0;
        }

        // Phân bổ Phần I (TN)
        PhanBoCau(rows, "SoTN", "KhoTN", chuanTN);

        // Phân bổ Phần II (Đ/S)
        PhanBoCau(rows, "SoDS", "KhoDS", chuanDS);

        // Phân bổ Phần III (TLN)
        PhanBoCau(rows, "SoTLN", "KhoTLN", chuanTLN);

        CapNhatTongKet();
    }

    private void PhanBoCau(List<DataGridViewRow> rows, string colSo, string colKho, int canPhanBo)
    {
        var conLai = canPhanBo;
        var rIndex = 0;
        int maxLoop = 100;

        while (conLai > 0 && maxLoop-- > 0)
        {
            bool coThem = false;
            for (int i = 0; i < rows.Count && conLai > 0; i++)
            {
                var row = rows[(rIndex + i) % rows.Count];
                var hienTai = Convert.ToInt32(row.Cells[colSo].Value ?? 0);
                var khoStr = row.Cells[colKho].Value?.ToString()?.TrimStart('/') ?? "0";
                int.TryParse(khoStr, out var khoMax);

                if (hienTai < khoMax)
                {
                    row.Cells[colSo].Value = hienTai + 1;
                    conLai--;
                    coThem = true;
                }
            }
            if (!coThem) break; // Đã hết câu trong ngân hàng
            rIndex++;
        }
    }

    private void CapNhatTongKet()
    {
        int sumTN = 0, sumDS = 0, sumTLN = 0;

        foreach (DataGridViewRow r in _gridMaTran.Rows)
        {
            var tn = Convert.ToInt32(r.Cells["SoTN"].Value ?? 0);
            var ds = Convert.ToInt32(r.Cells["SoDS"].Value ?? 0);
            var tln = Convert.ToInt32(r.Cells["SoTLN"].Value ?? 0);
            r.Cells["TongCau"].Value = tn + ds + tln;

            sumTN += tn;
            sumDS += ds;
            sumTLN += tln;
        }

        var total = sumTN + sumDS + sumTLN;
        _lblTongKet.Text = $"📊 Đang chọn: {total} câu  |  Phần I (TN): {sumTN} câu  |  Phần II (Đ/S): {sumDS} câu  |  Phần III (TLN): {sumTLN} câu";
    }

    private void ThucHienTaoDe()
    {
        _lblError.Text = "";
        if (string.IsNullOrWhiteSpace(_txtTenDe.Text))
        {
            _lblError.Text = "⚠ Vui lòng nhập tên đề thi.";
            _txtTenDe.Focus();
            return;
        }

        var monId = (int)(_cboMon.SelectedItem as ComboItem)!.Value!;
        var loaiDe = (_cboLoaiDe.SelectedItem as ComboItem)?.Value?.ToString() ?? "ThiThu";

        var maTran = new List<MaTranChuDeItem>();
        foreach (DataGridViewRow r in _gridMaTran.Rows)
        {
            var chId = Convert.ToInt32(r.Cells["ChuDeID"].Value);
            var tenCd = r.Cells["TenChuDe"].Value?.ToString() ?? "";
            var tn = Convert.ToInt32(r.Cells["SoTN"].Value ?? 0);
            var ds = Convert.ToInt32(r.Cells["SoDS"].Value ?? 0);
            var tln = Convert.ToInt32(r.Cells["SoTLN"].Value ?? 0);

            if (tn > 0 || ds > 0 || tln > 0)
            {
                maTran.Add(new MaTranChuDeItem
                {
                    ChuDeID = chId,
                    TenChuDe = tenCd,
                    SoCauPhan1 = tn,
                    SoCauPhan2 = ds,
                    SoCauPhan3 = tln
                });
            }
        }

        if (maTran.Count == 0)
        {
            _lblError.Text = "⚠ Chưa chọn câu hỏi nào cho các chủ đề trong ma trận.";
            return;
        }

        Cursor = Cursors.WaitCursor;
        try
        {
            var soLuongMa = (int)_numSoMaDe.Value;
            var tuDongTron = _chkTuDongTron.Checked;

            var (id, err) = _examService.TaoDeTheoMaTranTHPTQG(
                _txtTenDe.Text.Trim(),
                monId,
                loaiDe,
                maTran,
                tuDongTron,
                soLuongMa);

            if (id == 0)
            {
                _lblError.Text = "⚠ " + err;
                return;
            }

            DeThiIdMoi = id;
            var thongBao = $"Tạo đề thi chuẩn THPT Quốc gia thành công! (ID Đề: {id})\nTổng số câu: {maTran.Sum(x => x.SoCauPhan1 + x.SoCauPhan2 + x.SoCauPhan3)} câu.";
            if (tuDongTron)
                thongBao += $"\nĐã tự động trộn và sinh {soLuongMa} mã đề (101, 102...) kèm ma trận đáp án!";

            MessageBox.Show(thongBao, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // Mở luôn màn hình xem mã đề và ma trận đáp án
            using var fTron = new frmTronDe(id, _txtTenDe.Text.Trim());
            fTron.ShowDialog(this);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _lblError.Text = "⚠ Lỗi: " + ex.Message;
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }
}
