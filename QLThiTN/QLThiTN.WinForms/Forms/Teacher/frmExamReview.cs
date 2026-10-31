using System.Text;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using QLThiTN.Shared;
using QLThiTN.WinForms.Models;
using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms.Forms.Teacher;

public sealed class frmExamReview : Form
{
    private readonly WebView2 _web=new(){Dock=DockStyle.Fill};
    private readonly string _folder=Path.Combine(Path.GetTempPath(),"QLThiTN-review-"+Guid.NewGuid().ToString("N"));
    private ReviewDocument? _report;
    private bool _rendered;
    private string? _approvalError;
    public frmExamReview(int examId, int? attemptId=null)
    {
        Session.YeuCauQuanTriVien();
        Text=attemptId.HasValue?"Chi tiết câu trả lời của thí sinh":"Xem các mã đề và duyệt đề";
        Size=new Size(1100,800);StartPosition=FormStartPosition.CenterParent;
        _web.CreationProperties=new CoreWebView2CreationProperties{UserDataFolder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"QLThiTN","WebView2")};
        var footer=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=76,AutoScroll=true,Padding=new Padding(8,4,8,4)};
        var confirmed=new CheckBox{Text="Tôi đã xem nội dung và đáp án của tất cả mã đề",AutoSize=true,Margin=new Padding(10,14,10,0)};
        var approve=Ui.ButtonAccent("Duyệt lên web",170);approve.Enabled=false;
        var validationStatus=Ui.Label("",Ui.Small,Ui.Danger);
        validationStatus.AutoSize=true;
        validationStatus.MaximumSize=new Size(760,0);
        validationStatus.Margin=new Padding(10,8,10,0);
        confirmed.CheckedChanged+=(_,_)=>approve.Enabled=confirmed.Checked&&_report!=null&&_rendered&&_approvalError==null;
        approve.Click+=async(_,_)=>
        {
            if(_report==null)return;
            _approvalError=ExamReview.GetApprovalValidationError(_report);
            if(_approvalError!=null)
            {
                MessageBox.Show(this,_approvalError,"Đề chưa đủ điều kiện duyệt",MessageBoxButtons.OK,MessageBoxIcon.Warning);
                confirmed.Checked=false;
                return;
            }
            approve.Enabled=false;
            try
            {
                var fingerprint=_report.Fingerprint;
                await Task.Run(()=>{Session.YeuCauQuanTriVien();using var conn=Database.Open();ExamReview.Approve(conn,examId,Session.Current!.TaiKhoanID,fingerprint);});
                MessageBox.Show(this,"Đã duyệt đề. Thí sinh chỉ vào thi trong thời gian mở cổng và theo quyền được cấp.");
                DialogResult=DialogResult.OK;Close();
            }
            catch(Exception ex){MessageBox.Show(this,ex.Message,"Không duyệt được đề");confirmed.Checked=false;}
        };
        if(!attemptId.HasValue)footer.Controls.AddRange([confirmed,approve,validationStatus]);
        Controls.Add(_web);Controls.Add(footer);
        Shown+=async(_,_)=>
        {
            try
            {
                _report=await Task.Run(()=>{using var conn=Database.Open();return ExamReview.Read(conn,examId,attemptId);});
                if(IsDisposed)return;
                _approvalError=attemptId.HasValue?null:ExamReview.GetApprovalValidationError(_report);
                if(_approvalError!=null)
                {
                    confirmed.Visible=false;
                    approve.Visible=false;
                    validationStatus.Text="⚠ Đề chưa thể duyệt: "+_approvalError;
                }
                Directory.CreateDirectory(_folder);
                string ImageUrl(ReviewImage img)
                {
                    try
                    {
                        var bytes = QuestionImageStore.Read(new QuestionImage { DuongDan = img.Path });
                        var ext = Path.GetExtension(img.Path);
                        if (string.IsNullOrWhiteSpace(ext)) ext = ".png";
                        var name = img.Id + ext;
                        File.WriteAllBytes(Path.Combine(_folder, name), bytes);
                        return "https://review.local/" + name;
                    }
                    catch
                    {
                        return "";
                    }
                }
                var warning=_approvalError==null?"":$"<div style='border:1px solid #f0a020;background:#fff5dc;color:#7a4300;padding:14px 16px;border-radius:8px;margin-bottom:18px'><b>Đề chưa đủ điều kiện duyệt.</b><br>{System.Net.WebUtility.HtmlEncode(_approvalError)}</div>";
                var body=warning+ExamReview.Html(_report,ImageUrl);
                var html="""
                    <!doctype html><html lang="vi"><meta charset="utf-8">
                    <link rel="stylesheet" href="https://assets.local/katex/katex.min.css">
                    <script src="https://assets.local/katex/katex.min.js"></script>
                    <script src="https://assets.local/katex/contrib/auto-render.min.js"></script>
                    <style>html{scroll-behavior:smooth}body{font:16px 'Segoe UI';padding:24px;line-height:1.6;color:#1b2a4e}.variant-panel{scroll-margin-top:92px}.katex-display{overflow:auto}</style><body>
                    """+body+"""
                    <script>renderMathInElement(document.body,{delimiters:[{left:'$$',right:'$$',display:true},{left:'$',right:'$',display:false},{left:'\\(',right:'\\)',display:false}],throwOnError:false,trust:false});</script></body></html>
                    """;
                File.WriteAllText(Path.Combine(_folder,"review.html"),html,Encoding.UTF8);
                await _web.EnsureCoreWebView2Async();
                if(IsDisposed)return;
                _web.CoreWebView2.SetVirtualHostNameToFolderMapping("review.local",_folder,CoreWebView2HostResourceAccessKind.DenyCors);
                _web.CoreWebView2.SetVirtualHostNameToFolderMapping("assets.local",Path.Combine(AppContext.BaseDirectory,"Assets"),CoreWebView2HostResourceAccessKind.DenyCors);
                _web.CoreWebView2.NavigationStarting+=(_,e)=>{if(!e.Uri.StartsWith("https://review.local/"))e.Cancel=true;};
                _web.CoreWebView2.NewWindowRequested+=(_,e)=>e.Handled=true;
                _web.CoreWebView2.NavigationCompleted+=(_,e)=>{_rendered=e.IsSuccess;approve.Enabled=e.IsSuccess&&confirmed.Checked&&_approvalError==null;};
                _web.CoreWebView2.Navigate("https://review.local/review.html");
            }
            catch(Exception ex){_report=null;approve.Enabled=false;MessageBox.Show(this,ex.Message,"Không mở được xem trước");}
        };
    }
    protected override void Dispose(bool disposing)
    {
        if(disposing){_web.Dispose();try{if(Directory.Exists(_folder)){foreach(var file in Directory.GetFiles(_folder))File.Delete(file);Directory.Delete(_folder);}}catch(IOException){}catch(UnauthorizedAccessException){}}
        base.Dispose(disposing);
    }
}
