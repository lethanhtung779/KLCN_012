using QLThiTN.WinForms.Models;
using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms.Forms.Teacher;

/// <summary>Soạn chữ, công thức và nhiều ảnh; chưa ghi file trước khi lưu câu hỏi.</summary>
public sealed class QuestionContentEditor : UserControl
{
    public TextBox Input { get; } = new() { Multiline = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Top, Height = 76, Font = Ui.Body };
    public List<QuestionImage> Images { get; private set; } = [];
    private readonly ListBox _list = new() { Dock = DockStyle.Left, Width = 175, Font = Ui.Small };
    private readonly PictureBox _picture = new() { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.WhiteSmoke };
    private readonly TextBox _caption = new() { Dock = DockStyle.Bottom, PlaceholderText = "Chú thích ảnh (không bắt buộc)", MaxLength = 200, Font = Ui.Small };
    private readonly Label _status = new() { Dock = DockStyle.Bottom, Height = 22, ForeColor = Ui.TextMuted, Text = "Chọn ô nhập để gõ chữ/công thức; Ctrl+V để dán ảnh.", AutoEllipsis = true };
    private bool _loading;

    public QuestionContentEditor()
    {
        Height = 270; Dock = DockStyle.Top; BackColor = Color.White; Padding = new Padding(0, 0, 0, 8);
        var formulas = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 38, WrapContents = false, AutoScroll = true };
        foreach (var (name, latex) in new[] { ("Phân số", @"\frac{a}{b}"), ("Căn", @"\sqrt{x}"), ("Số mũ", @"x^{2}"), ("Tích phân", @"\int_{a}^{b} f(x)\,dx"), ("LaTeX", @"x + y = 1") })
        {
            var button = Ui.ButtonOutline(name, name == "Tích phân" ? 100 : 85, 30);
            button.Click += (_, _) => InsertFormula(latex);
            formulas.Controls.Add(button);
        }
        var imageTools = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 37, WrapContents = false };
        AddButton(imageTools, "Chọn ảnh", ChooseImage);
        AddButton(imageTools, "Dán ảnh", PasteImage);
        AddButton(imageTools, "Xóa ảnh", () =>
        {
            if (_list.SelectedIndex < 0) return;
            Images.RemoveAt(_list.SelectedIndex); RefreshImages();
        });
        var media = new Panel { Dock = DockStyle.Fill };
        media.Controls.Add(_picture); media.Controls.Add(_list); media.Controls.Add(_caption);
        Controls.Add(media); Controls.Add(_status); Controls.Add(imageTools); Controls.Add(Input); Controls.Add(formulas);
        _list.SelectedIndexChanged += (_, _) => ShowImage();
        _caption.TextChanged += (_, _) =>
        {
            if (_loading || _list.SelectedIndex < 0) return;
            Images[_list.SelectedIndex].MoTa = _caption.Text;
        };
        Input.KeyDown += (_, e) =>
        {
            if (e.Control && e.KeyCode == Keys.V && Clipboard.ContainsImage())
            { e.SuppressKeyPress = true; PasteImage(); }
        };
    }

    private static void AddButton(FlowLayoutPanel parent, string title, Action action)
    {
        var button = Ui.ButtonOutline(title, 100, 30);
        button.Click += (_, _) =>
        {
            try { action(); }
            catch (Exception ex) { MessageBox.Show(parent.FindForm(), ex.Message, "Không xử lý được ảnh", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        };
        parent.Controls.Add(button);
    }

    public void LoadImages(List<QuestionImage>? images)
    {
        Images = images ?? []; RefreshImages();
    }

    public void AddImage(Image image, string caption = "")
    {
        Images.Add(new QuestionImage { PendingPng = QuestionImageStore.ToPng(image), MoTa = caption });
        RefreshImages(); _list.SelectedIndex = Images.Count - 1;
    }

    private void ChooseImage()
    {
        using var picker = new OpenFileDialog { Filter = "Hình ảnh|*.png;*.jpg;*.jpeg;*.bmp;*.gif", Multiselect = true };
        if (picker.ShowDialog(FindForm()) != DialogResult.OK) return;
        foreach (var file in picker.FileNames)
        {
            if (new FileInfo(file).Length > 15 * 1024 * 1024) throw new InvalidOperationException("Mỗi ảnh phải nhỏ hơn 15 MB.");
            using var image = Image.FromFile(file);
            AddImage(image);
        }
    }

    private void PasteImage()
    {
        try
        {
            using var image = Clipboard.GetImage();
            if (image == null) { _status.Text = "Clipboard chưa có ảnh. Hãy sao chép ảnh trước khi dán."; return; }
            AddImage(image);
        }
        catch (Exception ex) { _status.Text = "Không dán được ảnh: " + ex.Message; }
    }

    private void RefreshImages()
    {
        _list.Items.Clear();
        for (var i = 0; i < Images.Count; i++) _list.Items.Add($"Ảnh {i + 1}");
        if (Images.Count > 0) _list.SelectedIndex = 0;
        else ShowImage();
    }

    private void ShowImage()
    {
        _picture.Image?.Dispose(); _picture.Image = null;
        _loading = true;
        var image = _list.SelectedIndex >= 0 ? Images[_list.SelectedIndex] : null;
        _caption.Text = image?.MoTa ?? ""; _caption.Enabled = image != null;
        _loading = false;
        _status.Text = $"{Images.Count} ảnh đính kèm. Ảnh chỉ được lưu khi bấm Lưu câu hỏi.";
        if (image == null) return;
        try
        {
            using var stream = new MemoryStream(QuestionImageStore.Read(image));
            using var decoded = Image.FromStream(stream);
            _picture.Image = new Bitmap(decoded);
        }
        catch { _status.Text = "Không đọc được ảnh này. Có thể xem ảnh WebP trong tab Xem trước."; }
    }

    private void InsertFormula(string template)
    {
        using var dialog = new Form { Text = "Chèn công thức", StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(600, 190), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var hint = new Label { Text = "Thay a, b, x… bằng giá trị cần dùng. Có thể nhập mã LaTeX bất kỳ.", AutoSize = true, Location = new Point(15, 15) };
        var input = new TextBox { Text = template, Location = new Point(15, 45), Width = 560, Font = Ui.Body };
        var insert = Ui.ButtonAccent("Chèn", 100); insert.Location = new Point(15, 105); insert.DialogResult = DialogResult.OK;
        var cancel = Ui.ButtonOutline("Hủy", 100); cancel.Location = new Point(130, 105); cancel.DialogResult = DialogResult.Cancel;
        dialog.Controls.AddRange([hint, input, insert, cancel]); dialog.AcceptButton = insert; dialog.CancelButton = cancel;
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK || string.IsNullOrWhiteSpace(input.Text)) return;
        Input.SelectedText = "$" + input.Text.Trim().Trim('$') + "$";
        Input.Focus();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _picture.Image?.Dispose();
        base.Dispose(disposing);
    }
}
