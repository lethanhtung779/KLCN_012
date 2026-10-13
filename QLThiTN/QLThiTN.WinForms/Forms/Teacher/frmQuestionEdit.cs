using QLThiTN.WinForms.Models;
using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms.Forms.Teacher;

public class frmQuestionEdit : Form
{
    private readonly QuestionService _service = new();
    private readonly int _id;
    private readonly ComboBox _cboMon = Ui.Combo(240);
    private readonly ComboBox _cboChuDe = Ui.Combo(300);
    private readonly ComboBox _cboLoai = Ui.Combo(240);
    private readonly ComboBox _cboMuc = Ui.Combo(180);
    private readonly NumericUpDown _numDiem = new() { Width = 80, DecimalPlaces = 2, Increment = 0.25m, Minimum = 0.01m, Maximum = 100m, Value = 0.25m };
    private readonly QuestionContentEditor _question = new() { Height = 350 };
    private readonly QuestionContentEditor[] _answers = Enumerable.Range(0, 4).Select(_ => new QuestionContentEditor()).ToArray();
    private readonly TextBox _explanation = new() { Multiline = true, Height = 90, Dock = DockStyle.Top, Font = Ui.Body, ScrollBars = ScrollBars.Vertical };
    private readonly ComboBox _correct = Ui.Combo(110);
    private readonly ComboBox[] _trueFalse = Enumerable.Range(0, 4).Select(_ => Ui.Combo(100)).ToArray();
    private readonly Panel[] _answerRows = new Panel[4];
    private readonly Label[] _answerLabels = new Label[4];
    private readonly Label _answerHint = new() { AutoSize = true, Font = Ui.Small };
    private readonly TextBox _shortAnswer = Ui.Input(300);
    private readonly Panel _shortPanel = new() { Dock = DockStyle.Top, Height = 105 };
    private readonly FlowLayoutPanel _choiceTools = new() { Dock = DockStyle.Top, Height = 42, WrapContents = false };
    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill, Font = Ui.Body };
    private readonly QuestionPreview _preview = new();
    private readonly Label _error = new() { Dock = DockStyle.Fill, ForeColor = Ui.Danger, AutoEllipsis = true, Font = Ui.Small };
    private readonly Button _save = Ui.ButtonAccent("Lưu câu hỏi", 160, 40);
    private string _previousType = LoaiCauHoi.TracNghiem;
    private bool _loading;
    private string QuestionType => (_cboLoai.SelectedItem as ComboItem)?.Value as string ?? LoaiCauHoi.TracNghiem;

    public frmQuestionEdit(int id, int macDinhMon = 0)
    {
        _id = id;
        Text = id == 0 ? "Thêm câu hỏi mới" : $"Sửa câu hỏi #{id}";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1050, 800); MinimumSize = new Size(860, 650);
        BackColor = Color.White; Font = Ui.Body;
        var metadata = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true, Padding = new Padding(12) };
        AddField(metadata, "Môn học *", _cboMon);
        AddField(metadata, "Chủ đề *", _cboChuDe);
        AddField(metadata, "Loại câu hỏi *", _cboLoai);
        AddField(metadata, "Độ khó", _cboMuc);
        AddField(metadata, "Điểm", _numDiem);
        var contentPage = new TabPage("Nội dung câu hỏi") { AutoScroll = true, Padding = new Padding(12), BackColor = Color.White };
        var explanationTitle = new Label { Text = "Giải thích đáp án (có thể nhập công thức $...$)", Dock = DockStyle.Top, Height = 30 };
        contentPage.Controls.Add(_explanation); contentPage.Controls.Add(explanationTitle); contentPage.Controls.Add(_question);
        var answerPage = new TabPage("Đáp án") { AutoScroll = true, Padding = new Padding(12), BackColor = Color.White };
        for (var i = 3; i >= 0; i--)
        {
            var row = new Panel { Dock = DockStyle.Top, Height = 315 };
            var heading = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 38, WrapContents = false };
            _answerLabels[i] = new Label { Text = $"Đáp án {(char)('A' + i)}", AutoSize = true, Font = Ui.H2, Padding = new Padding(0, 5, 12, 0) };
            _trueFalse[i].Items.AddRange(["Đúng", "Sai"]); _trueFalse[i].SelectedIndex = 1;
            heading.Controls.Add(_answerLabels[i]); heading.Controls.Add(_trueFalse[i]);
            row.Controls.Add(_answers[i]); row.Controls.Add(heading);
            _answerRows[i] = row; answerPage.Controls.Add(row);
        }
        var shortLabel = new Label { Text = "Đáp số dùng để chấm bài (bắt buộc nhập chữ hoặc số)", AutoSize = true, Location = new Point(0, 8) };
        _shortAnswer.Location = new Point(0, 40);
        _shortPanel.Controls.AddRange([shortLabel, _shortAnswer]);
        _correct.Items.AddRange(["A", "B", "C", "D"]);
        _choiceTools.Controls.Add(new Label { Text = "Đáp án đúng:", AutoSize = true, Padding = new Padding(0, 5, 8, 0) });
        _choiceTools.Controls.Add(_correct); _choiceTools.Controls.Add(_answerHint);
        answerPage.Controls.Add(_shortPanel); answerPage.Controls.Add(_choiceTools);
        var previewPage = new TabPage("Xem trước") { BackColor = Color.White };
        previewPage.Controls.Add(_preview);
        _tabs.TabPages.AddRange([contentPage, answerPage, previewPage]);
        _tabs.SelectedIndexChanged += async (_, _) => { if (_tabs.SelectedIndex == 2) await _preview.RenderAsync(ReadQuestion()); };
        var footer = new Panel { Dock = DockStyle.Bottom, Height = 90, Padding = new Padding(12) };
        var actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 45 };
        var cancel = Ui.ButtonOutline("Hủy", 100, 40); cancel.DialogResult = DialogResult.Cancel; CancelButton = cancel;
        _save.Click += (_, _) => SaveQuestion();
        actions.Controls.AddRange([_save, cancel]); footer.Controls.Add(_error); footer.Controls.Add(actions);
        Controls.Add(_tabs); Controls.Add(footer); Controls.Add(metadata);

        foreach (var m in _service.LayMonHoc()) _cboMon.Items.Add(new ComboItem(m.MonHocID, m.TenMon));
        _cboMon.SelectedIndexChanged += (_, _) => LoadTopics();
        _cboLoai.Items.AddRange([new ComboItem(LoaiCauHoi.TracNghiem, "Trắc nghiệm"), new ComboItem(LoaiCauHoi.DungSai, "Đúng/Sai"), new ComboItem(LoaiCauHoi.TraLoiNgan, "Trả lời ngắn")]);
        foreach (var (code, display) in MucDoKho.All) _cboMuc.Items.Add(new ComboItem(code, display));
        _cboMuc.SelectedIndex = 0;
        _cboLoai.SelectedIndexChanged += (_, _) => ChangeType();
        if (_cboMon.Items.Count > 0) _cboMon.SelectedIndex = 0;
        Select(_cboMon, macDinhMon); _cboLoai.SelectedIndex = 0;
        if (id > 0) LoadQuestion(id);
    }

    private sealed record ComboItem(object Value, string Text)
    {
        public override string ToString() => Text;
    }

    private static void AddField(FlowLayoutPanel parent, string title, Control input)
    {
        var field = new Panel { Width = input.Width + 15, Height = 62 };
        field.Controls.Add(new Label { Text = title, AutoSize = true, Location = new Point(0, 0), Font = Ui.Small });
        input.Location = new Point(0, 25); field.Controls.Add(input); parent.Controls.Add(field);
    }

    private static void Select(ComboBox combo, object value)
    {
        foreach (ComboItem item in combo.Items)
            if (Equals(item.Value, value)) { combo.SelectedItem = item; return; }
    }

    private void LoadTopics()
    {
        _cboChuDe.Items.Clear();
        if (_cboMon.SelectedItem is not ComboItem mon) return;
        foreach (var cd in _service.LayChuDe((int)mon.Value)) _cboChuDe.Items.Add(new ComboItem(cd.ChuDeID, cd.TenChuDe));
        if (_cboChuDe.Items.Count > 0) _cboChuDe.SelectedIndex = 0;
    }

    private void ChangeType()
    {
        var shortType = QuestionType == LoaiCauHoi.TraLoiNgan;
        if (!_loading && shortType && _answers.Any(a => a.Images.Count > 0))
        {
            Select(_cboLoai, _previousType);
            MessageBox.Show(this, "Hãy xóa các ảnh đáp án trước khi chuyển sang trả lời ngắn. Ảnh câu hỏi vẫn được giữ lại.", "Đổi loại câu hỏi");
            return;
        }
        _previousType = QuestionType;
        for (var i = 0; i < 4; i++)
        {
            _answerRows[i].Visible = !shortType;
            _trueFalse[i].Visible = QuestionType == LoaiCauHoi.DungSai;
            _answerLabels[i].Text = QuestionType == LoaiCauHoi.DungSai ? $"Ý {(char)('a' + i)}" : $"Đáp án {(char)('A' + i)}";
        }
        _choiceTools.Visible = QuestionType == LoaiCauHoi.TracNghiem;
        _shortPanel.Visible = shortType;
        _answerHint.Text = "Chọn một đáp án đúng; mỗi đáp án có thể là chữ, công thức hoặc ảnh.";
    }

    private void LoadQuestion(int id)
    {
        var q = _service.LayChiTiet(id) ?? throw new InvalidOperationException("Không tìm thấy câu hỏi.");
        var topic = _service.LayChuDe().First(c => c.ChuDeID == q.ChuDeID);
        _loading = true;
        Select(_cboMon, topic.MonHocID); Select(_cboChuDe, q.ChuDeID);
        Select(_cboLoai, q.LoaiCauHoi); Select(_cboMuc, q.MucDoKho);
        _numDiem.Maximum = Math.Max(_numDiem.Maximum, q.Diem); _numDiem.Value = Math.Max(_numDiem.Minimum, q.Diem);
        _question.Input.Text = q.NoiDung; _question.LoadImages(q.HinhAnhs);
        _explanation.Text = q.GiaiThichDapAn ?? "";
        for (var i = 0; i < Math.Min(4, q.DapAns.Count); i++)
        {
            _answers[i].Input.Text = q.DapAns[i].NoiDung; _answers[i].LoadImages(q.DapAns[i].HinhAnhs);
            _trueFalse[i].SelectedIndex = q.DapAns[i].LaDapAnDung ? 0 : 1;
            if (q.DapAns[i].LaDapAnDung) _correct.SelectedIndex = i;
        }
        _shortAnswer.Text = q.DapAns.FirstOrDefault()?.NoiDung ?? "";
        _loading = false;
    }

    private CauHoi ReadQuestion()
    {
        var q = new CauHoi
        {
            CauHoiID = _id, ChuDeID = (_cboChuDe.SelectedItem as ComboItem)?.Value as int? ?? 0,
            NoiDung = _question.Input.Text.Trim(), HinhAnhs = _question.Images,
            LoaiCauHoi = QuestionType, MucDoKho = (_cboMuc.SelectedItem as ComboItem)?.Value as string ?? MucDoKho.ChuaXacDinh,
            Diem = _numDiem.Value, GiaiThichDapAn = _explanation.Text.Trim()
        };
        if (QuestionType == LoaiCauHoi.TraLoiNgan)
            q.DapAns.Add(new DapAn { NoiDung = _shortAnswer.Text.Trim(), LaDapAnDung = true, HinhAnhs = _answers[0].Images });
        else
            for (var i = 0; i < 4; i++)
                q.DapAns.Add(new DapAn { ThuTu = i, NoiDung = _answers[i].Input.Text.Trim(), HinhAnhs = _answers[i].Images,
                    LaDapAnDung = QuestionType == LoaiCauHoi.TracNghiem ? _correct.SelectedIndex == i : _trueFalse[i].SelectedIndex == 0 });
        return q;
    }

    private void SaveQuestion()
    {
        _error.Text = ""; _save.Enabled = false;
        try
        {
            var q = ReadQuestion();
            if (_id == 0) _service.Them(q); else _service.Sua(q);
            DialogResult = DialogResult.OK; Close();
        }
        catch (Exception ex) { _error.Text = ex.Message; }
        finally { if (!IsDisposed) _save.Enabled = true; }
    }
}
