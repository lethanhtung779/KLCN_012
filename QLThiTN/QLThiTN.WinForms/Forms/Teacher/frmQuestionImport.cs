using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms.Forms.Teacher;

public sealed class frmQuestionImport : Form
{
    private readonly List<QuestionImportRow> _rows;
    private readonly DataGridView _grid = new();
    private readonly QuestionPreview _preview = new();
    private readonly Label _summary = new() { Dock = DockStyle.Top, Height = 56, Padding = new Padding(12, 6, 12, 0) };
    private readonly Button _import = Ui.ButtonAccent("Nhập các dòng đã chọn", 220, 40);
    private readonly CheckBox _chkBoQuaTrung = new() { Text = "Tự động loại bỏ câu trùng lặp", Checked = true, AutoSize = true, Font = Ui.Body, Margin = new Padding(12, 10, 12, 0) };
    private readonly Button _btnBoChonTrung = Ui.ButtonOutline("Bỏ chọn câu trùng", 160, 40);
    private readonly Button _btnChonTatCaMoi = Ui.ButtonOutline("Chỉ chọn câu mới", 160, 40);
    private bool _busy;
    public int ImportedCount => _rows.Count(r => r.ImportedId.HasValue);

    public frmQuestionImport(List<QuestionImportRow> rows)
    {
        _rows = rows;
        Text = "Kiểm tra file trước khi nhập câu hỏi (Có lọc trùng lặp)";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1300, 780); MinimumSize = new Size(1050, 650); Font = Ui.Body;
        var split = new SplitContainer { Dock = DockStyle.Fill, Size = new Size(1300, 650), SplitterDistance = 670, Panel1MinSize = 450, Panel2MinSize = 400 };
        Ui.StyleGrid(_grid); _grid.ReadOnly = false; _grid.Dock = DockStyle.Fill;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        _grid.AutoGenerateColumns = false; _grid.AllowUserToAddRows = false;
        _grid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Selected", HeaderText = "Nhập", Width = 55 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Line", HeaderText = "Dòng", Width = 55 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Topic", HeaderText = "Chủ đề", Width = 130 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Content", HeaderText = "Nội dung", Width = 170 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Kết quả kiểm tra & Trùng lặp", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 240 });
        foreach (DataGridViewColumn column in _grid.Columns)
        {
            column.ReadOnly = column.Name != "Selected";
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
        }
        _grid.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        _grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        _grid.CurrentCellDirtyStateChanged += (_, _) => { if (_grid.IsCurrentCellDirty) _grid.CommitEdit(DataGridViewDataErrorContexts.Commit); };
        _grid.CellValueChanged += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex != 0) return;
            var row = (QuestionImportRow?)_grid.Rows[e.RowIndex].Tag;
            if (row != null)
            {
                var userChecked = Convert.ToBoolean(_grid.Rows[e.RowIndex].Cells[0].Value);
                row.Selected = row.CanImport && (!_chkBoQuaTrung.Checked || !row.IsDuplicate) && userChecked;
            }
            UpdateSummary();
        };
        _grid.CurrentCellChanged += async (_, _) =>
        {
            if (_grid.CurrentRow?.Tag is QuestionImportRow row) await _preview.RenderAsync(row.Question);
        };
        split.Panel1.Controls.Add(_grid); split.Panel2.Controls.Add(_preview);

        _chkBoQuaTrung.CheckedChanged += (_, _) =>
        {
            foreach (DataGridViewRow r in _grid.Rows)
            {
                if (r.Tag is QuestionImportRow row && row.IsDuplicate)
                {
                    if (_chkBoQuaTrung.Checked)
                    {
                        row.Selected = false;
                        r.Cells[0].Value = false;
                        r.Cells[0].ReadOnly = true;
                    }
                    else
                    {
                        r.Cells[0].ReadOnly = !row.CanImport;
                    }
                }
            }
            UpdateSummary();
        };

        _btnBoChonTrung.Click += (_, _) =>
        {
            foreach (DataGridViewRow r in _grid.Rows)
            {
                if (r.Tag is QuestionImportRow row && row.IsDuplicate)
                {
                    row.Selected = false;
                    r.Cells[0].Value = false;
                }
            }
            UpdateSummary();
        };

        _btnChonTatCaMoi.Click += (_, _) =>
        {
            foreach (DataGridViewRow r in _grid.Rows)
            {
                if (r.Tag is QuestionImportRow row && row.CanImport && !row.IsDuplicate)
                {
                    row.Selected = true;
                    r.Cells[0].Value = true;
                }
            }
            UpdateSummary();
        };

        var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(12, 6, 0, 0), WrapContents = false, AutoScroll = true };
        var close = Ui.ButtonOutline("Đóng", 100, 40); close.Click += (_, _) => Close();
        _import.Click += async (_, _) => await ImportAsync();
        footer.Controls.AddRange([_import, _btnChonTatCaMoi, _btnBoChonTrung, _chkBoQuaTrung, close]);
        Controls.Add(split); Controls.Add(footer); Controls.Add(_summary);
        Shown += (_, _) => RefreshRows();
        FormClosing += (_, e) => { if (_busy) e.Cancel = true; };
    }

    private void RefreshRows()
    {
        _grid.Rows.Clear();
        foreach (var row in _rows)
        {
            if (_chkBoQuaTrung.Checked && row.IsDuplicate)
            {
                row.Selected = false;
            }
            else
            {
                row.Selected &= row.CanImport;
            }

            var index = _grid.Rows.Add(row.Selected, row.Line, row.Topic, row.Question.NoiDung, row.Status);
            var display = _grid.Rows[index]; display.Tag = row;

            var choPhepChon = row.CanImport && (!_chkBoQuaTrung.Checked || !row.IsDuplicate);
            display.Cells[0].ReadOnly = !choPhepChon;

            if (row.Errors.Count > 0)
            {
                display.DefaultCellStyle.ForeColor = Ui.Danger;
            }
            else if (row.IsDuplicate)
            {
                display.DefaultCellStyle.ForeColor = Color.FromArgb(190, 85, 0); // Canh bao mau cam
                display.DefaultCellStyle.BackColor = Color.FromArgb(255, 248, 235); // Nen vang am
            }
            else if (row.ImportedId.HasValue)
            {
                display.DefaultCellStyle.ForeColor = Ui.Success;
            }
        }
        UpdateSummary();
        if (_grid.Rows.Count > 0)
        {
            _grid.CurrentCell = _grid.Rows[0].Cells[1];
            _ = _preview.RenderAsync(_rows[0].Question);
        }
    }

    private void UpdateSummary()
    {
        var soHopLeMoi = _rows.Count(r => r.CanImport && !r.IsDuplicate);
        var soTrung = _rows.Count(r => r.IsDuplicate);
        var soLoi = _rows.Count(r => r.Errors.Count > 0);
        var trangThaiTrung = _chkBoQuaTrung.Checked ? "đã tự động lọc bỏ" : "chưa lọc bỏ";

        _summary.Text = $"{_rows.Count} dòng • {soHopLeMoi} câu mới hợp lệ • {soTrung} câu trùng ({trangThaiTrung}) • {soLoi} lỗi • {ImportedCount} đã nhập.\n" +
            "Các câu hỏi trùng lặp trong ngân hàng hoặc trùng trong file đã được nhận diện và tự động loại bỏ khỏi danh sách nhập.";
        _import.Enabled = !_busy && _rows.Any(r => r.Selected && r.CanImport && (!_chkBoQuaTrung.Checked || !r.IsDuplicate));
    }

    private async Task ImportAsync()
    {
        _grid.EndEdit();
        _busy = true; _grid.Enabled = false; UpdateSummary(); UseWaitCursor = true;
        try
        {
            var selected = _rows.Where(r => r.Selected && r.CanImport && (!_chkBoQuaTrung.Checked || !r.IsDuplicate)).ToArray();
            await Task.Run(() => QuestionImportService.ImportSelected(selected, new QuestionService().Them));
        }
        finally
        {
            _busy = false; _grid.Enabled = true; UseWaitCursor = false; RefreshRows();
        }
    }
}
