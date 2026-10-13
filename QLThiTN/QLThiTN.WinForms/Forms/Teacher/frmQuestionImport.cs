using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms.Forms.Teacher;

public sealed class frmQuestionImport : Form
{
    private readonly List<QuestionImportRow> _rows;
    private readonly DataGridView _grid = new();
    private readonly QuestionPreview _preview = new();
    private readonly Label _summary = new() { Dock = DockStyle.Top, Height = 48, Padding = new Padding(12, 6, 12, 0) };
    private readonly Button _import = Ui.ButtonAccent("Nhập các dòng đã chọn", 220, 40);
    private bool _busy;
    public int ImportedCount => _rows.Count(r => r.ImportedId.HasValue);

    public frmQuestionImport(List<QuestionImportRow> rows)
    {
        _rows = rows;
        Text = "Kiểm tra file trước khi nhập câu hỏi";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1250, 780); MinimumSize = new Size(1050, 650); Font = Ui.Body;
        var split = new SplitContainer { Dock = DockStyle.Fill, Size = new Size(1250, 650), SplitterDistance = 620, Panel1MinSize = 450, Panel2MinSize = 400 };
        Ui.StyleGrid(_grid); _grid.ReadOnly = false; _grid.Dock = DockStyle.Fill;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        _grid.AutoGenerateColumns = false; _grid.AllowUserToAddRows = false;
        _grid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Selected", HeaderText = "Nhập", Width = 55 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Line", HeaderText = "Dòng", Width = 55 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Topic", HeaderText = "Chủ đề", Width = 130 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Content", HeaderText = "Nội dung", Width = 160 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Kết quả kiểm tra", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 200 });
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
            if (row != null) row.Selected = row.CanImport && Convert.ToBoolean(_grid.Rows[e.RowIndex].Cells[0].Value);
            UpdateSummary();
        };
        _grid.CurrentCellChanged += async (_, _) =>
        {
            if (_grid.CurrentRow?.Tag is QuestionImportRow row) await _preview.RenderAsync(row.Question);
        };
        split.Panel1.Controls.Add(_grid); split.Panel2.Controls.Add(_preview);
        var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(12, 6, 0, 0) };
        var close = Ui.ButtonOutline("Đóng", 100, 40); close.Click += (_, _) => Close();
        _import.Click += async (_, _) => await ImportAsync();
        footer.Controls.AddRange([_import, close]);
        Controls.Add(split); Controls.Add(footer); Controls.Add(_summary);
        Shown += (_, _) => RefreshRows();
        FormClosing += (_, e) => { if (_busy) e.Cancel = true; };
    }

    private void RefreshRows()
    {
        _grid.Rows.Clear();
        foreach (var row in _rows)
        {
            row.Selected &= row.CanImport;
            var index = _grid.Rows.Add(row.Selected, row.Line, row.Topic, row.Question.NoiDung, row.Status);
            var display = _grid.Rows[index]; display.Tag = row;
            display.Cells[0].ReadOnly = !row.CanImport;
            if (row.Errors.Count > 0) display.DefaultCellStyle.ForeColor = Ui.Danger;
            else if (row.ImportedId.HasValue) display.DefaultCellStyle.ForeColor = Ui.Success;
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
        _summary.Text = $"{_rows.Count} dòng • {_rows.Count(r => r.CanImport)} hợp lệ • {_rows.Count(r => r.Errors.Count > 0)} lỗi • {ImportedCount} đã nhập.\n" +
            "Chọn dòng để xem chữ, ảnh và công thức. Chỉ các dòng được đánh dấu mới được nhập; dòng lỗi luôn bị bỏ qua.";
        _import.Enabled = !_busy && _rows.Any(r => r.Selected && r.CanImport);
    }

    private async Task ImportAsync()
    {
        _grid.EndEdit();
        _busy = true; _grid.Enabled = false; UpdateSummary(); UseWaitCursor = true;
        try
        {
            var selected = _rows.Where(r => r.Selected && r.CanImport).ToArray();
            await Task.Run(() => QuestionImportService.ImportSelected(selected, new QuestionService().Them));
        }
        finally
        {
            _busy = false; _grid.Enabled = true; UseWaitCursor = false; RefreshRows();
        }
    }
}
