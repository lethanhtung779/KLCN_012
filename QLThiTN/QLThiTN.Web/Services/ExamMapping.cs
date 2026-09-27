using QLThiTN.Web.ViewModels;

namespace QLThiTN.Web.Services;

public static class ExamMapping
{
    /// <summary>
    /// Noi dung cau hoi / dap an trong DB chua <c>&lt;img src="/api/hinhanh/123"&gt;</c>.
    /// Day la duong dan TUONG DOI, nen trinh duyet quyet no ve host cua Web (vd :5264)
    /// thay vi host cua API (:5000) =&gt; 404 =&gt; browser chi hien alt text.
    /// O day ta ghi de thanh URL TUYET DOI tro thang sang API.
    /// </summary>
    public static string ToApiAbsoluteHtml(string? html, string apiBaseUrl)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;
        var baseUrl = (apiBaseUrl ?? string.Empty).TrimEnd('/');
        if (baseUrl.Length == 0) return html;
        if (!html.Contains("/api/", StringComparison.Ordinal)) return html;

        // src="/api/..." -> src="http://host:5000/api/..."
        return System.Text.RegularExpressions.Regex.Replace(
            html,
            "((?:src|href)\\s*=\\s*[\"'])/api/",
            "$1" + baseUrl + "/api/",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Anh nao da duoc nhung san trong NoiDung (dang &lt;img src="/api/hinhanh/ID"&gt;)
    /// thi khong nen render them qua partial _QuestionImages, neu khong se bi trung.
    /// </summary>
    public static List<ImageViewModel> ToImagesExcluding(
        List<ApiHinhAnh>? items, string apiBaseUrl, string? contentHtml)
    {
        var used = ExtractImageIds(contentHtml);
        if (used.Count > 0 && items is not null)
            items = items.Where(i => !used.Contains(i.HinhAnhID)).ToList();
        return ToImages(items, apiBaseUrl);
    }

    /// <summary>Lay ra danh sach HinhAnhID da xuat hien trong chuoi HTML noi dung.</summary>
    private static readonly System.Text.RegularExpressions.Regex ReImgId =
        new(@"/api/hinhanh/(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    private static HashSet<int> ExtractImageIds(string? html)
    {
        var set = new HashSet<int>();
        if (string.IsNullOrEmpty(html)) return set;
        foreach (System.Text.RegularExpressions.Match m in ReImgId.Matches(html))
            if (int.TryParse(m.Groups[1].Value, out int id)) set.Add(id);
        return set;
    }

    /// <summary>
    /// Ghep duong dan tuong doi do API tra ve ("/api/hinhanh/123") thanh URL day du.
    /// Browser tai anh truc tiep tu API vi anh nam o project tach cau hoi, khong
    /// phai trong wwwroot cua Web.
    /// </summary>
    public static List<ImageViewModel> ToImages(List<ApiHinhAnh>? items, string apiBaseUrl)
    {
        if (items is null || items.Count == 0) return new();

        var baseUrl = (apiBaseUrl ?? string.Empty).TrimEnd('/');
        return items
            .OrderBy(i => i.ThuTu)
            .Select(i => new ImageViewModel
            {
                Url = i.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? i.Url
                    : baseUrl + i.Url,
                Caption = i.MoTa
            })
            .Where(i => !string.IsNullOrWhiteSpace(i.Url))
            .ToList();
    }

    public static ExamKind ToExamKind(string? loaiDe) => loaiDe switch
    {
        "OnTap" => ExamKind.Homework,
        "ThiThu" => ExamKind.MonthlyMock,
        "ChinhThuc" => ExamKind.PublicMock,
        _ => ExamKind.MonthlyMock
    };

    public static ExamScheduleViewModel ToSchedule(ApiDeThi d) => new()
    {
        Id = d.DeThiID,
        Title = d.TenDe,
        Description = string.IsNullOrWhiteSpace(d.TenDotThi) ? d.LoaiDeText : d.TenDotThi,
        Duration = d.ThoiLuongLamBai ?? 45,
        StartTime = d.ThoiGianMoCong,
        EndTime = d.ThoiGianDongCong,
        MaxStudents = null,
        SubjectName = d.TenMon,
        QuestionCount = d.SoLuongCauHoi,
        HasRegistered = false,
        CanRegister = d.TrangThai != "DaDong",
        ExamKind = ToExamKind(d.LoaiDe),
        IsAssignedToMe = d.DotThiID.HasValue,
        ScorePublished = true
    };

    public static List<QuestionViewModel> ToQuestions(List<ApiCauHoi> items, string apiBaseUrl)
    {
        return items.Select(q =>
        {
            var content = ToApiAbsoluteHtml(q.NoiDung, apiBaseUrl);
            return new QuestionViewModel
            {
                Id = q.CauHoiID,
                OrderIndex = q.OrderIndex,
                Content = content,
                TypeName = q.LoaiCauHoiText,
                Images = ToImagesExcluding(q.HinhAnh, apiBaseUrl, content),
                Options = q.Options.Select(o =>
                {
                    var oc = ToApiAbsoluteHtml(o.NoiDung, apiBaseUrl);
                    return new OptionViewModel
                    {
                        Id = o.DapAnID,
                        Content = oc,
                        Label = o.Label,
                        Images = ToImagesExcluding(o.HinhAnh, apiBaseUrl, oc)
                    };
                }).ToList()
            };
        }).ToList();
    }

    public static ExamResultViewModel ToResult(ApiKetQua k, string examTitle, int duration, DateTime startTime, string apiBaseUrl) => new()
    {
        Id = k.DeThiID,
        ExamId = k.DeThiID,
        ExamTitle = examTitle,
        Score = k.Score,
        TotalQuestions = k.Total,
        CorrectAnswers = k.Correct,
        WrongAnswers = k.Wrong,
        StartTime = startTime,
        EndTime = DateTime.Now,
        DurationUsed = duration,
        QuestionResults = k.Results.Select(r =>
        {
            var content = ToApiAbsoluteHtml(r.NoiDung, apiBaseUrl);
            return new QuestionResultViewModel
            {
                OrderIndex = r.OrderIndex,
                Content = content,
                SelectedOptionId = r.SelectedDapAnID,
                IsCorrect = r.IsCorrect,
                Images = ToImagesExcluding(r.HinhAnh, apiBaseUrl, content),
                Options = r.Options.Select(o =>
                {
                    var oc = ToApiAbsoluteHtml(o.NoiDung, apiBaseUrl);
                    return new OptionResultViewModel
                    {
                        Id = o.DapAnID,
                        Content = oc,
                        Label = o.Label,
                        IsCorrect = o.IsCorrect,
                        IsSelected = o.IsSelected,
                        Images = ToImagesExcluding(o.HinhAnh, apiBaseUrl, oc)
                    };
                }).ToList()
            };
        }).ToList()
    };
}