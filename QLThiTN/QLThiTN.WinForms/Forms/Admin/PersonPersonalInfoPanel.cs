using ClosedXML.Excel;
using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms.Forms.Admin;

/// <summary>Danh sách chỉ gồm thông tin cá nhân của học viên hoặc giáo viên.</summary>
public sealed class PersonPersonalInfoPanel : UserControl
{
    private readonly string _role;
    private readonly PersonManagementService _service = new();
    private readonly TextBox _search = Ui.Input(330);
    private readonly DataGridView _grid = new();
    private readonly Label _summary = Ui.Label("", Ui.Small, Ui.TextMuted);
    private int _loadVersion;

    public PersonPersonalInfoPanel(string role)
    {
        _role = role;
        Dock = DockStyle.Fill;
        BackColor = Color.White;
        Font = Ui.Body;

        _search.PlaceholderText = "Tìm theo họ tên / điện thoại / địa chỉ / liên hệ...";
        _search.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            RefreshList();
        };
        var find = Ui.ButtonPrimary("Tìm", 80);
        find.Click += (_, _) => RefreshList();
        var refresh = Ui.ButtonOutline("Làm mới", 105);
        refresh.Click += (_, _) => { _search.Clear(); RefreshList(); };
        var export = Ui.ButtonAccent("Xuất Excel", 125);
        export.Click += (_, _) => ExportExcel();
        var note = Ui.Label("Chỉ hiển thị hồ sơ cá nhân; không có thông tin đăng nhập, quyền hoặc VIP.", Ui.Small, Ui.TextMuted);

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 60,
            WrapContents = false,
            Padding = new Padding(14, 10, 0, 8),
            BackColor = Color.White
        };
        foreach (Control control in new Control[] { _search, find, refresh, export, note })
        {
            control.Margin = new Padding(0, 3, 10, 0);
            toolbar.Controls.Add(control);
        }

        Ui.StyleGrid(_grid);
        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

        var footer = new Panel { Dock = DockStyle.Bottom, Height = 38, BackColor = Color.White };
        _summary.Location = new Point(16, 10);
        footer.Controls.Add(_summary);

        Controls.Add(_grid);
        Controls.Add(footer);
        Controls.Add(toolbar);
        RefreshList();
    }

    public async void RefreshList()
    {
        var version = ++_loadVersion;
        _summary.Text = "Đang tải thông tin cá nhân...";
        try
        {
            var data = await Task.Run(() => _service.LayThongTinCaNhan(_role, _search.Text));
            if (IsDisposed || Disposing || version != _loadVersion) return;
            _grid.DataSource = data;
            Ui.RenameColumns(_grid,
                ("HoTen", "Họ và tên"), ("SoDienThoai", "Số điện thoại"),
                ("NgaySinh", "Ngày sinh"), ("LoaiHocVienHienThi", "Loại học viên"));
            foreach (var field in ProfileFields.ForRole(_role))
                Ui.RenameColumns(_grid, (field.Key, field.Caption));
            Ui.HideColumns(_grid, "HocVienID", "GiaoVienID");
            foreach (DataGridViewColumn column in _grid.Columns)
            {
                column.Width = column.Name switch
                {
                    "HoTen" => 180,
                    "SoDienThoai" => 125,
                    "NgaySinh" => 105,
                    "LoaiHocVienHienThi" or "GioiTinh" => 115,
                    "DiaChi" or "GhiChu" or "BangCap" or "DonViCongTac" => 260,
                    _ => 175
                };
            }
            if (_grid.Columns.Contains("NgaySinh"))
                _grid.Columns["NgaySinh"]!.DefaultCellStyle.Format = "dd/MM/yyyy";
            _summary.Text = $"{_grid.Rows.Count} hồ sơ {(_role == "HocVien" ? "học viên" : "giáo viên")}";
        }
        catch (Exception ex)
        {
            if (!IsDisposed && !Disposing && version == _loadVersion)
                _summary.Text = "Không tải được thông tin cá nhân: " + ex.Message;
        }
    }

    private void ExportExcel()
    {
        var columns = _grid.Columns.Cast<DataGridViewColumn>()
            .Where(column => column.Visible)
            .OrderBy(column => column.DisplayIndex)
            .ToList();
        if (_grid.Rows.Count == 0 || columns.Count == 0)
        {
            MessageBox.Show("Không có dữ liệu để xuất.", "Xuất danh sách");
            return;
        }

        var roleName = _role == "HocVien" ? "hoc_vien" : "giao_vien";
        using var save = new SaveFileDialog
        {
            Filter = "Excel Workbook (*.xlsx)|*.xlsx",
            DefaultExt = "xlsx",
            AddExtension = true,
            FileName = $"thong_tin_ca_nhan_{roleName}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx",
            Title = "Chọn nơi lưu danh sách thông tin cá nhân"
        };
        if (save.ShowDialog(FindForm()) != DialogResult.OK) return;

        try
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add(_role == "HocVien" ? "HocVien" : "GiaoVien");
            for (var index = 0; index < columns.Count; index++)
            {
                var cell = sheet.Cell(1, index + 1);
                cell.Value = columns[index].HeaderText;
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#18335F");
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }

            var outputRow = 2;
            foreach (DataGridViewRow gridRow in _grid.Rows)
            {
                if (gridRow.IsNewRow) continue;
                for (var index = 0; index < columns.Count; index++)
                {
                    var value = gridRow.Cells[columns[index].Index].Value;
                    var cell = sheet.Cell(outputRow, index + 1);
                    if (value is DateTime date)
                    {
                        cell.Value = date;
                        cell.Style.DateFormat.Format = "dd/MM/yyyy";
                    }
                    else
                        cell.Value = value is null or DBNull ? "" : value.ToString();
                }
                outputRow++;
            }

            var used = sheet.Range(1, 1, outputRow - 1, columns.Count);
            used.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            used.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
            used.SetAutoFilter();
            sheet.SheetView.FreezeRows(1);
            sheet.Row(1).Height = 24;
            sheet.Columns().AdjustToContents();
            foreach (var column in sheet.ColumnsUsed())
            {
                if (column.Width < 12) column.Width = 12;
                if (column.Width > 45) column.Width = 45;
            }
            workbook.SaveAs(save.FileName);
            MessageBox.Show($"Đã xuất {_grid.Rows.Count} hồ sơ ra:\n{save.FileName}",
                "Xuất danh sách thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không xuất được file Excel: " + ex.Message,
                "Lỗi xuất danh sách", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
