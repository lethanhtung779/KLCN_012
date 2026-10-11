using System.Data;
using System.Text;
using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms.Forms.Teacher;

/// <summary>
/// Màn hình trộn đề thi ngẫu nhiên (sinh mã đề 101, 102, 103, 104...)
/// theo chuẩn cấu trúc đề thi tốt nghiệp THPT Quốc gia (Bộ GD&ĐT).
/// </summary>
public class frmTronDe : Form
{
    private readonly ExamService _service = new();
    private readonly int _deThiId;
    private readonly string _tenDe;

    // Controls cấu hình
    private readonly NumericUpDown _numSoLuong = new() { Width = 110, Minimum = 1, Maximum = 24, Value = 4, Font = Ui.Body };
    private readonly NumericUpDown _numMaBatDau = new() { Width = 110, Minimum = 101, Maximum = 999, Value = 101, Font = Ui.Body };
    private readonly CheckBox _chkXoaCu = new() { Text = "Làm mới (xóa các mã đề cũ chưa có bài thi)", AutoSize = true, Checked = true, Font = Ui.Body };
    private readonly Button _btnTron;
    private readonly Label _lblThongKe = Ui.Label("", Ui.Body, Ui.Navy);
    private readonly Label _lblCanhBao = Ui.Label("", Ui.Small, Ui.Warning);

    // Tab 1: Danh sách mã đề & Xem trước
    private readonly DataGridView _gridMaDe = new();
    private readonly DataGridView _gridChiTiet = new();

    // Tab 2: Ma trận đáp án
    private readonly DataGridView _gridMaTran = new();

    public frmTronDe(int deThiId, string tenDe)
    {
        _deThiId = deThiId;
        _tenDe = tenDe;

        Text = $"Trộn đề thi ngẫu nhiên — {tenDe}";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(1000, 680);
        MinimumSize = new Size(880, 560);
        Font = Ui.Body;
        BackColor = Ui.BgSoft;

        // Top Panel: Tiêu đề + Thống kê chuẩn THPT QG
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 115,
            BackColor = Color.White,
            Padding = new Padding(20, 12, 20, 10)
        };

        var lblTitle = Ui.Label($"🔀 TRỘN ĐỀ THI: {tenDe}", Ui.H1, Ui.Navy);
        lblTitle.Location = new Point(20, 10);
        pnlHeader.Controls.Add(lblTitle);

        _lblThongKe.Location = new Point(20, 42);
        _lblThongKe.AutoSize = true;
        pnlHeader.Controls.Add(_lblThongKe);

        _lblCanhBao.Location = new Point(20, 85);
        _lblCanhBao.AutoSize = true;
        pnlHeader.Controls.Add(_lblCanhBao);

        // Config Panel: Các tùy chọn trộn đề
        var pnlConfig = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            BackColor = Color.FromArgb(248, 250, 253),
            Padding = new Padding(20, 12, 20, 10)
        };

        var lblSl = Ui.Label("Số mã đề:", Ui.Body, Ui.Navy);
        lblSl.Location = new Point(20, 20);
        _numSoLuong.Location = new Point(100, 18);

        var lblStart = Ui.Label("Mã bắt đầu:", Ui.Body, Ui.Navy);
        lblStart.Location = new Point(230, 20);
        _numMaBatDau.Location = new Point(320, 18);

        _chkXoaCu.Location = new Point(450, 20);

        _btnTron = Ui.ButtonAccent("⚡ Tiến hành trộn đề", 180, 36);
        _btnTron.Location = new Point(780, 14);
        _btnTron.Click += (s, e) => ThucHienTron();

        pnlConfig.Controls.AddRange([lblSl, _numSoLuong, lblStart, _numMaBatDau, _chkXoaCu, _btnTron]);

        // TabControl chính
        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Padding = new Point(16, 6)
        };

        // --- TAB 1: Danh sách & Xem trước
        var tabXem = new TabPage("  📋 Danh sách mã đề & Xem trước  ") { BackColor = Color.White };
        var splitXem = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 320,
            BackColor = Ui.BorderSoft
        };

        // Bên trái Tab 1: Grid các mã đề
        var pnlLeft = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12) };
        var lblDsMaDe = Ui.Label("Các mã đề hiện có:", Ui.H2, Ui.Navy);
        lblDsMaDe.Dock = DockStyle.Top;
        lblDsMaDe.Height = 30;
        Ui.StyleGrid(_gridMaDe);
        _gridMaDe.Dock = DockStyle.Fill;
        _gridMaDe.SelectionChanged += (s, e) => TaiChiTietMaDeDangChon();
        pnlLeft.Controls.Add(_gridMaDe);
        pnlLeft.Controls.Add(lblDsMaDe);
        splitXem.Panel1.Controls.Add(pnlLeft);

        // Bên phải Tab 1: Chi tiết câu hỏi của mã đề
        var pnlRight = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12) };
        var lblCtTitle = Ui.Label("Nội dung câu hỏi & phương án theo mã đề (đã hoán vị):", Ui.H2, Ui.Navy);
        lblCtTitle.Dock = DockStyle.Top;
        lblCtTitle.Height = 30;
        Ui.StyleGrid(_gridChiTiet);
        _gridChiTiet.Dock = DockStyle.Fill;
        pnlRight.Controls.Add(_gridChiTiet);
        pnlRight.Controls.Add(lblCtTitle);
        splitXem.Panel2.Controls.Add(pnlRight);

        tabXem.Controls.Add(splitXem);

        // --- TAB 2: Ma trận đáp án
        var tabMaTran = new TabPage("  📊 Ma trận đáp án (Answer Key)  ") { BackColor = Color.White };
        var pnlMaTranWrap = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16), BackColor = Color.White };
        var pnlMaTranToolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 44,
            BackColor = Color.White,
            WrapContents = false
        };

        var btnCopyMatrix = Ui.ButtonPrimary("📋 Sao chép ma trận (Excel)", 210, 34);
        btnCopyMatrix.Click += (s, e) => SaoChepMaTran();

        var btnXuatText = Ui.ButtonOutline("📄 Xuất file Text (.txt)", 180, 34);
        btnXuatText.Click += (s, e) => XuatFileText();

        pnlMaTranToolbar.Controls.Add(btnCopyMatrix);
        pnlMaTranToolbar.Controls.Add(btnXuatText);

        Ui.StyleGrid(_gridMaTran);
        _gridMaTran.Dock = DockStyle.Fill;

        pnlMaTranWrap.Controls.Add(_gridMaTran);
        pnlMaTranWrap.Controls.Add(pnlMaTranToolbar);
        tabMaTran.Controls.Add(pnlMaTranWrap);

        tabs.TabPages.Add(tabXem);
        tabs.TabPages.Add(tabMaTran);

        // Bottom Panel: Đóng
        var pnlBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 50,
            BackColor = Color.White,
            Padding = new Padding(0, 8, 20, 8)
        };
        var btnDong = Ui.ButtonOutline("Đóng", 100, 34);
        btnDong.Dock = DockStyle.Right;
        btnDong.Click += (s, e) => Close();
        pnlBottom.Controls.Add(btnDong);

        Controls.Add(tabs);
        Controls.Add(pnlConfig);
        Controls.Add(pnlHeader);
        Controls.Add(pnlBottom);

        Load += (s, e) => KhoiTaoDuLieu();
    }

    private void KhoiTaoDuLieu()
    {
        var (tong, tn, ds, tln) = _service.LayThongKePhanCauHoi(_deThiId);
        _lblThongKe.Text = $"Cấu trúc đề: {tong} câu hỏi  |  Phần I (Trắc nghiệm A-D): {tn} câu  |  Phần II (Đúng/Sai 4 ý): {ds} câu  |  Phần III (Trả lời ngắn): {tln} câu";

        var (coBai, soBai) = _service.KiemTraBaiLamCuaDe(_deThiId);
        if (coBai)
        {
            _lblCanhBao.Text = $"⚠ Lưu ý: Đề thi này đã có {soBai} bài làm của thí sinh. Bạn không nên xóa mã đề cũ để đảm bảo toàn vẹn kết quả thi.";
            _chkXoaCu.Checked = false;
            _chkXoaCu.Enabled = false;
        }
        else
        {
            _lblCanhBao.Text = "✔ Chuẩn THPT Quốc gia: Thứ tự 3 phần được bảo toàn; câu hỏi và phương án được hoán vị ngẫu nhiên độc lập trong từng phần.";
        }

        TaiDanhSachMaDe();
        TaiMaTranDapAn();
    }

    private void TaiDanhSachMaDe()
    {
        var dt = _service.LayDanhSachMaDe(_deThiId);
        _gridMaDe.DataSource = dt;
        Ui.RenameColumns(_gridMaDe,
            ("MaDeID", "ID"),
            ("TenMaDe", "Mã đề"),
            ("SoCauHoi", "Số câu"),
            ("SoBaiLam", "Bài thi"));
        Ui.HideColumns(_gridMaDe, "MaDeID");

        if (_gridMaDe.Rows.Count > 0)
        {
            _gridMaDe.ClearSelection();
            _gridMaDe.Rows[0].Selected = true;
            TaiChiTietMaDeDangChon();
        }
        else
        {
            _gridChiTiet.DataSource = null;
        }
    }

    private void TaiChiTietMaDeDangChon()
    {
        if (_gridMaDe.CurrentRow == null || _gridMaDe.CurrentRow.Cells["MaDeID"].Value == null)
        {
            _gridChiTiet.DataSource = null;
            return;
        }

        var maDeId = Convert.ToInt32(_gridMaDe.CurrentRow.Cells["MaDeID"].Value);
        var dt = _service.LayChiTietMaDe(maDeId);

        // Tạo bảng hiển thị đẹp: gom nhóm theo câu hỏi
        var viewTable = new DataTable();
        viewTable.Columns.Add("CauSo", typeof(int));
        viewTable.Columns.Add("LoaiCauHoi", typeof(string));
        viewTable.Columns.Add("NoiDungCau", typeof(string));
        viewTable.Columns.Add("PhuongAn", typeof(string));
        viewTable.Columns.Add("DapAnDung", typeof(string));

        var grouped = dt.AsEnumerable().GroupBy(r => new
        {
            CauSo = Convert.ToInt32(r["CauSo"]),
            Loai = r["LoaiCauHoi"]?.ToString() ?? "",
            NoiDung = r["NoiDungCau"]?.ToString() ?? ""
        });

        foreach (var g in grouped)
        {
            var row = viewTable.NewRow();
            row["CauSo"] = g.Key.CauSo;
            row["LoaiCauHoi"] = g.Key.Loai switch
            {
                "TracNghiem" => "Phần I: TN",
                "DungSai" => "Phần II: Đ/S",
                "TraLoiNgan" => "Phần III: TLN",
                _ => g.Key.Loai
            };

            // Rút gọn nội dung câu hỏi
            var nd = g.Key.NoiDung;
            row["NoiDungCau"] = nd.Length > 90 ? nd[..90] + "…" : nd;

            // Xử lý các phương án và đáp án đúng
            if (g.Key.Loai == "TracNghiem")
            {
                var options = new List<string>();
                string correctLabel = "";
                foreach (var opt in g.OrderBy(x => Convert.ToInt32(x["ThuTuDapAn"])))
                {
                    var tt = Convert.ToInt32(opt["ThuTuDapAn"]);
                    var label = ((char)('A' + tt)).ToString();
                    var txt = opt["NoiDungDapAn"]?.ToString() ?? "";
                    if (txt.Length > 20) txt = txt[..20] + "…";
                    options.Add($"{label}. {txt}");

                    if (opt["LaDapAnDung"] != DBNull.Value && Convert.ToBoolean(opt["LaDapAnDung"]))
                        correctLabel = label;
                }
                row["PhuongAn"] = string.Join("  |  ", options);
                row["DapAnDung"] = correctLabel;
            }
            else if (g.Key.Loai == "DungSai")
            {
                var options = new List<string>();
                var corrects = new List<string>();
                foreach (var opt in g.OrderBy(x => Convert.ToInt32(x["ThuTuDapAn"])))
                {
                    var tt = Convert.ToInt32(opt["ThuTuDapAn"]);
                    var label = ((char)('a' + tt)).ToString();
                    var isRight = opt["LaDapAnDung"] != DBNull.Value && Convert.ToBoolean(opt["LaDapAnDung"]);
                    var txt = opt["NoiDungDapAn"]?.ToString() ?? "";
                    if (txt.Length > 15) txt = txt[..15] + "…";
                    options.Add($"{label}) {txt}");
                    corrects.Add($"{label}: {(isRight ? "Đ" : "S")}");
                }
                row["PhuongAn"] = string.Join("  |  ", options);
                row["DapAnDung"] = string.Join(", ", corrects);
            }
            else // TraLoiNgan
            {
                var first = g.FirstOrDefault();
                row["PhuongAn"] = "(Thí sinh tự điền số / đáp án ngắn)";
                row["DapAnDung"] = first?["NoiDungDapAn"]?.ToString() ?? "";
            }

            viewTable.Rows.Add(row);
        }

        _gridChiTiet.DataSource = viewTable;
        Ui.RenameColumns(_gridChiTiet,
            ("CauSo", "Câu"),
            ("LoaiCauHoi", "Phần thi"),
            ("NoiDungCau", "Nội dung câu hỏi"),
            ("PhuongAn", "Các phương án đã đảo"),
            ("DapAnDung", "Đáp án đúng"));

        if (_gridChiTiet.Columns.Contains("CauSo"))
            _gridChiTiet.Columns["CauSo"]!.Width = 60;
        if (_gridChiTiet.Columns.Contains("LoaiCauHoi"))
            _gridChiTiet.Columns["LoaiCauHoi"]!.Width = 110;
        if (_gridChiTiet.Columns.Contains("DapAnDung"))
            _gridChiTiet.Columns["DapAnDung"]!.Width = 130;
    }

    private void TaiMaTranDapAn()
    {
        var dt = _service.LayMaTranDapAn(_deThiId);
        _gridMaTran.DataSource = dt;
        Ui.RenameColumns(_gridMaTran,
            ("ThuTu", "Câu"),
            ("LoaiCauHoi", "Phần thi"));

        if (_gridMaTran.Columns.Contains("ThuTu"))
            _gridMaTran.Columns["ThuTu"]!.Width = 60;
        if (_gridMaTran.Columns.Contains("LoaiCauHoi"))
            _gridMaTran.Columns["LoaiCauHoi"]!.Width = 120;
    }

    private void ThucHienTron()
    {
        var soLuong = (int)_numSoLuong.Value;
        var maBatDau = (int)_numMaBatDau.Value;
        var xoaCu = _chkXoaCu.Checked;

        if (xoaCu)
        {
            var confirm = MessageBox.Show(
                $"Bạn có chắc chắn muốn làm mới và sinh {soLuong} mã đề (từ {maBatDau} đến {maBatDau + soLuong - 1}) theo chuẩn THPT Quốc gia không?",
                "Xác nhận trộn đề",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;
        }

        Cursor = Cursors.WaitCursor;
        _btnTron.Enabled = false;

        try
        {
            var (success, msg, _) = _service.TronDeThi(_deThiId, soLuong, maBatDau, xoaCu);
            if (success)
            {
                MessageBox.Show(msg, "Trộn đề thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                TaiDanhSachMaDe();
                TaiMaTranDapAn();
            }
            else
            {
                MessageBox.Show(msg, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi: " + ex.Message, "Lỗi thực thi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnTron.Enabled = true;
            Cursor = Cursors.Default;
        }
    }

    private void SaoChepMaTran()
    {
        if (_gridMaTran.Rows.Count == 0)
        {
            MessageBox.Show("Chưa có dữ liệu ma trận đáp án để sao chép.", "Thông báo");
            return;
        }

        var sb = new StringBuilder();

        // Tiêu đề cột (TSV)
        var headers = new List<string>();
        foreach (DataGridViewColumn col in _gridMaTran.Columns)
            headers.Add(col.HeaderText);
        sb.AppendLine(string.Join("\t", headers));

        // Dữ liệu từng dòng
        foreach (DataGridViewRow row in _gridMaTran.Rows)
        {
            var cells = new List<string>();
            foreach (DataGridViewCell cell in row.Cells)
                cells.Add(cell.Value?.ToString() ?? "");
            sb.AppendLine(string.Join("\t", cells));
        }

        try
        {
            Clipboard.SetText(sb.ToString());
            MessageBox.Show("Đã sao chép ma trận đáp án vào Clipboard!\nBạn có thể nhấn Ctrl+V trực tiếp vào Microsoft Excel hoặc Word.",
                "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể ghi vào Clipboard: " + ex.Message, "Lỗi");
        }
    }

    private void XuatFileText()
    {
        if (_gridMaTran.Rows.Count == 0)
        {
            MessageBox.Show("Chưa có dữ liệu ma trận đáp án để xuất.", "Thông báo");
            return;
        }

        using var sfd = new SaveFileDialog
        {
            Filter = "Text file (*.txt)|*.txt",
            FileName = $"DapAn_{_tenDe.Replace(" ", "_")}.txt"
        };

        if (sfd.ShowDialog() != DialogResult.OK) return;

        var sb = new StringBuilder();
        sb.AppendLine("==========================================================================");
        sb.AppendLine($"BẢNG MA TRẬN ĐÁP ÁN ĐỀ THI: {_tenDe.ToUpper()}");
        sb.AppendLine($"Ngày tạo: {DateTime.Now:dd/MM/yyyy HH:mm:ss} — Chuẩn đề thi tốt nghiệp THPT Quốc gia");
        sb.AppendLine("==========================================================================");
        sb.AppendLine();

        // Tiêu đề bảng
        var cols = _gridMaTran.Columns.Cast<DataGridViewColumn>().Select(c => c.HeaderText).ToList();
        sb.AppendLine(string.Join("  |  ", cols));
        sb.AppendLine(new string('-', 74));

        foreach (DataGridViewRow row in _gridMaTran.Rows)
        {
            var vals = row.Cells.Cast<DataGridViewCell>().Select(c => c.Value?.ToString() ?? "").ToList();
            sb.AppendLine(string.Join("  |  ", vals));
        }

        sb.AppendLine();
        sb.AppendLine("--- HẾT ---");

        try
        {
            File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
            MessageBox.Show("Đã xuất file thành công tại:\n" + sfd.FileName, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi khi lưu file: " + ex.Message, "Lỗi");
        }
    }
}
