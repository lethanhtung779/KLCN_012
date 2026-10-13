using System.Net;
using System.Text;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using QLThiTN.WinForms.Models;
using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms.Forms.Teacher;

public sealed class QuestionPreview : UserControl
{
    private readonly WebView2 _browser = new() { Dock = DockStyle.Fill };
    private readonly Label _status = new() { Dock = DockStyle.Top, Height = 35, Text = "Đang chuẩn bị xem trước…" };
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "QLThiTN-preview-" + Guid.NewGuid().ToString("N"));
    private readonly List<string> _files = [];
    private bool _busy;
    private CauHoi? _pending;

    public QuestionPreview()
    {
        Dock = DockStyle.Fill;
        Controls.Add(_browser); Controls.Add(_status);
        _browser.CreationProperties = new CoreWebView2CreationProperties
        {
            UserDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QLThiTN", "WebView2")
        };
    }

    public async Task RenderAsync(CauHoi question)
    {
        if (IsDisposed) return;
        if (_busy) { _pending = question; return; }
        _busy = true;
        try
        {
            Directory.CreateDirectory(_folder);
            if (_browser.CoreWebView2 == null)
            {
                await _browser.EnsureCoreWebView2Async();
                if (IsDisposed || _browser.CoreWebView2 == null) return;
                _browser.CoreWebView2.SetVirtualHostNameToFolderMapping("question.local", _folder, CoreWebView2HostResourceAccessKind.DenyCors);
                _browser.CoreWebView2.SetVirtualHostNameToFolderMapping("assets.local", Path.Combine(AppContext.BaseDirectory, "Assets"), CoreWebView2HostResourceAccessKind.DenyCors);
                _browser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                _browser.CoreWebView2.Settings.AreHostObjectsAllowed = false;
                _browser.CoreWebView2.NewWindowRequested += (_, e) => e.Handled = true;
                _browser.CoreWebView2.NavigationStarting += (_, e) =>
                {
                    if (!e.Uri.StartsWith("https://question.local/", StringComparison.OrdinalIgnoreCase)) e.Cancel = true;
                };
            }
            var html = new StringBuilder("""
                <!doctype html><html lang="vi"><head><meta charset="utf-8">
                <meta http-equiv="Content-Security-Policy" content="default-src 'none'; img-src https://question.local; style-src 'unsafe-inline' https://assets.local; font-src https://assets.local; script-src 'unsafe-inline' https://assets.local">
                <link rel="stylesheet" href="https://assets.local/katex/katex.min.css">
                <script src="https://assets.local/katex/katex.min.js"></script>
                <script src="https://assets.local/katex/contrib/auto-render.min.js"></script>
                <style>body{font:17px 'Segoe UI',sans-serif;color:#1b2a4e;padding:20px;line-height:1.7} .text{white-space:pre-wrap;overflow-wrap:anywhere}section{border:1px solid #dbe2eb;padding:16px;margin:12px 0;border-radius:8px}img{max-width:100%;max-height:420px;object-fit:contain}figure{margin:12px 0}figcaption,.hint{color:#64748b;font-size:14px}.missing{color:#b91c1c}.katex-display{overflow-x:auto;overflow-y:hidden}</style></head><body>
                <div class="hint">Bản xem trước dành cho giáo viên, gồm đáp án và giải thích. Công thức không hợp lệ hiển thị màu đỏ.</div>
                """);
            html.Append("<h2>Câu hỏi</h2>");
            Content(html, question.NoiDung, question.HinhAnhs);
            for (var i = 0; i < question.DapAns.Count; i++)
            {
                var a = question.DapAns[i];
                var title = question.LoaiCauHoi == LoaiCauHoi.TraLoiNgan ? "Đáp số" : $"{(char)('A' + i)}";
                var answer = question.LoaiCauHoi == LoaiCauHoi.DungSai ? (a.LaDapAnDung ? " — Đúng" : " — Sai") : (a.LaDapAnDung ? " — Đáp án đúng" : "");
                html.Append("<section><strong>").Append(title).Append(answer).Append("</strong>");
                Content(html, a.NoiDung, a.HinhAnhs); html.Append("</section>");
            }
            if (!string.IsNullOrWhiteSpace(question.GiaiThichDapAn))
            {
                html.Append("<h3>Giải thích</h3>"); Content(html, question.GiaiThichDapAn, null);
            }
            html.Append("""
                <script>
                if(window.renderMathInElement){renderMathInElement(document.body,{
                  delimiters:[{left:'$$',right:'$$',display:true},{left:'\\[',right:'\\]',display:true},{left:'\\(',right:'\\)',display:false},{left:'$',right:'$',display:false}],
                  throwOnError:false,trust:false,strict:false,errorColor:'#b91c1c'
                });}else{document.body.insertAdjacentHTML('afterbegin','<p class="missing">Không tải được bộ hiển thị công thức.</p>');}
                </script></body></html>
                """);
            var path = Path.Combine(_folder, Guid.NewGuid().ToString("N") + ".html");
            File.WriteAllText(path, html.ToString(), Encoding.UTF8); _files.Add(path);
            _browser.CoreWebView2.Navigate("https://question.local/" + Path.GetFileName(path));
            _status.Text = "Xem trước chữ, ảnh và công thức — không cần kết nối Internet.";
        }
        catch (Exception ex)
        {
            if (!IsDisposed) _status.Text = "Không mở được xem trước. Cần Microsoft Edge WebView2 Runtime. " + ex.Message;
        }
        finally
        {
            _busy = false;
            if (_pending != null && !IsDisposed)
            {
                var next = _pending; _pending = null;
                await RenderAsync(next);
            }
        }
    }

    private void Content(StringBuilder html, string text, List<QuestionImage>? images)
    {
        html.Append("<div class=\"text\">").Append(WebUtility.HtmlEncode(text)).Append("</div>");
        foreach (var image in images ?? [])
        {
            try
            {
                var extension = image.PendingPng != null ? ".png" : Path.GetExtension(image.DuongDan).ToLowerInvariant();
                if (!new[] { ".png", ".jpg", ".jpeg", ".gif", ".webp", ".bmp" }.Contains(extension))
                    throw new InvalidOperationException("Định dạng ảnh không hỗ trợ.");
                var name = Guid.NewGuid().ToString("N") + extension;
                var path = Path.Combine(_folder, name);
                File.WriteAllBytes(path, QuestionImageStore.Read(image)); _files.Add(path);
                html.Append("<figure><img src=\"https://question.local/").Append(name).Append("\"><figcaption>")
                    .Append(WebUtility.HtmlEncode(image.MoTa)).Append("</figcaption></figure>");
            }
            catch { html.Append("<p class=\"missing\">Không tìm thấy hoặc không đọc được ảnh đính kèm.</p>"); }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _browser.Dispose();
            foreach (var file in _files)
                try { File.Delete(file); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            try { if (Directory.Exists(_folder)) Directory.Delete(_folder); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
        base.Dispose(disposing);
    }
}
