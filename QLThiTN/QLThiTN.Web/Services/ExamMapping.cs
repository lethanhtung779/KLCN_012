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
    /// <summary>
    /// Loai bo ma doc hai, script, cac event handler (onerror, onload, onclick...) va link nguy hiem.
    /// </summary>
    public static string SanitizeHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;

        // Xoa cac the nguy hiem bao gom ca noi dung ben trong
        var sanitized = System.Text.RegularExpressions.Regex.Replace(
            html,
            @"<(script|style|iframe|object|embed|applet|meta|link|base|form)[^>]*?>[\s\S]*?<\/\1>",
            string.Empty,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Xoa cac the tu dong/the don nguy hiem
        sanitized = System.Text.RegularExpressions.Regex.Replace(
            sanitized,
            @"<(script|style|iframe|object|embed|applet|meta|link|base|form)[^>]*?>",
            string.Empty,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Xoa tat ca thuoc tinh bat dau bang on (onerror, onload, onclick, onmouseover...)
        sanitized = System.Text.RegularExpressions.Regex.Replace(
            sanitized,
            @"\s+on[a-zA-Z]+\s*=\s*(?:""[^""]*""|'[^']*'|[^\s>]+)",
            string.Empty,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Xoa URI schemes nguy hiem (javascript:, vbscript:)
        sanitized = System.Text.RegularExpressions.Regex.Replace(
            sanitized,
            @"(?:src|href)\s*=\s*(?:""\s*(?:javascript|vbscript):[^""]*""|'\s*(?:javascript|vbscript):[^']*'|(?:javascript|vbscript):[^\s>]+)",
            string.Empty,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return sanitized;
    }

    public static string ToApiAbsoluteHtml(string? html, string apiBaseUrl)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;
        var sanitized = SanitizeHtml(html);
        var baseUrl = (apiBaseUrl ?? string.Empty).TrimEnd('/');
        if (baseUrl.Length == 0 || !sanitized.Contains("/api/", StringComparison.Ordinal)) return sanitized;

        // src="/api/..." -> src="http://host:5000/api/..."
        return System.Text.RegularExpressions.Regex.Replace(
            sanitized,
            "((?:src|href)\\s*=\\s*[\"'])/api/",
            "$1" + baseUrl + "/api/",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private static readonly System.Text.RegularExpressions.Regex ReAnyFigure =
        new(@"<figure\b[^>]*>[\s\S]*?<\/figure>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    private static readonly System.Text.RegularExpressions.Regex ReFigCaption =
        new(@"<figcaption\b[^>]*>([\s\S]*?)<\/figcaption>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    private static readonly System.Text.RegularExpressions.Regex ReAnyImg =
        new(@"<img\b[^>]*?>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    private static readonly System.Text.RegularExpressions.Regex ReSrcAttr =
        new(@"\bsrc\s*=\s*[""']([^""']+)[""']", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    private static readonly System.Text.RegularExpressions.Regex ReAltAttr =
        new(@"\balt\s*=\s*[""']([^""']*)[""']", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    /// <summary>
    /// Tách toàn bộ thẻ &lt;img&gt; hoặc &lt;figure&gt; ra khỏi nội dung chữ của câu hỏi / phương án,
    /// bảo đảm câu hỏi hiển thị liền mạch toàn bộ phần chữ ở trên, sau đó hình ảnh hiển thị ở bên dưới.
    /// </summary>
    public static (string CleanContent, List<ImageViewModel> Images) SeparateContentAndImages(
        string? html, List<ApiHinhAnh>? apiImages, string apiBaseUrl)
    {
        var absHtml = ToApiAbsoluteHtml(html, apiBaseUrl);
        var images = new List<ImageViewModel>();
        var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrEmpty(absHtml))
        {
            // 1. Trích xuất các ảnh nằm trong thẻ <figure>
            foreach (System.Text.RegularExpressions.Match figMatch in ReAnyFigure.Matches(absHtml))
            {
                var figText = figMatch.Value;
                var srcMatch = ReSrcAttr.Match(figText);
                if (srcMatch.Success)
                {
                    var src = srcMatch.Groups[1].Value;
                    var capMatch = ReFigCaption.Match(figText);
                    var cap = capMatch.Success ? SanitizeHtml(capMatch.Groups[1].Value).Trim() : string.Empty;
                    if (seenUrls.Add(src))
                    {
                        images.Add(new ImageViewModel { Url = src, Caption = cap });
                    }
                }
            }
            absHtml = ReAnyFigure.Replace(absHtml, string.Empty);

            // 2. Trích xuất các thẻ <img> độc lập
            foreach (System.Text.RegularExpressions.Match imgMatch in ReAnyImg.Matches(absHtml))
            {
                var imgTag = imgMatch.Value;
                var srcMatch = ReSrcAttr.Match(imgTag);
                if (srcMatch.Success)
                {
                    var src = srcMatch.Groups[1].Value;
                    var altMatch = ReAltAttr.Match(imgTag);
                    var alt = altMatch.Success ? altMatch.Groups[1].Value.Trim() : string.Empty;
                    if (seenUrls.Add(src))
                    {
                        images.Add(new ImageViewModel { Url = src, Caption = alt });
                    }
                }
            }
            absHtml = ReAnyImg.Replace(absHtml, string.Empty);

            // 3. Dọn dẹp thẻ <p> rỗng và khoảng trắng dư thừa do tách ảnh để lại
            absHtml = System.Text.RegularExpressions.Regex.Replace(absHtml, @"<p\b[^>]*>\s*(?:&nbsp;|\s)*<\/p>", string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            absHtml = absHtml.Trim();
        }

        // 4. Bổ sung các ảnh từ bảng HinhAnh (nếu có và chưa được thêm)
        if (apiImages != null && apiImages.Count > 0)
        {
            var baseUrl = (apiBaseUrl ?? string.Empty).TrimEnd('/');
            foreach (var ai in apiImages.OrderBy(i => i.ThuTu))
            {
                var url = ai.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? ai.Url
                    : baseUrl + ai.Url;

                var idSuffix = "/" + ai.HinhAnhID;
                if (!string.IsNullOrWhiteSpace(url) &&
                    !seenUrls.Contains(url) &&
                    !seenUrls.Any(u => u.EndsWith(idSuffix, StringComparison.OrdinalIgnoreCase)))
                {
                    seenUrls.Add(url);
                    images.Add(new ImageViewModel
                    {
                        Url = url,
                        Caption = ai.MoTa ?? string.Empty
                    });
                }
            }
        }

        return (absHtml, images);
    }

    /// <summary>
    /// Ảnh nào đã được nhúng sẵn trong NộiDung (dạng &lt;img src="/api/hinhanh/ID"&gt;)
    /// thì không nên render thêm qua partial _QuestionImages, nếu không sẽ bị trùng.
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
        SubjectName = MonHocVisual.ToDisplayName(d.TenMon),
        QuestionCount = d.SoLuongCauHoi,
        HasRegistered = false,
        CanRegister = d.TrangThai != "DaDong",
        ExamKind = ToExamKind(d.LoaiDe),
        IsAssignedToMe = d.DotThiID.HasValue,
        ScorePublished = true,
        LaMienPhi = d.LaMienPhi
    };

    public static List<QuestionViewModel> ToQuestions(List<ApiCauHoi> items, string apiBaseUrl)
    {
        return items.Select(q =>
        {
            var (content, images) = SeparateContentAndImages(q.NoiDung, q.HinhAnh, apiBaseUrl);
            return new QuestionViewModel
            {
                Id = q.CauHoiID,
                OrderIndex = q.OrderIndex,
                Content = content,
                TypeName = q.LoaiCauHoiText,
                Loai = q.LoaiCauHoi,
                Images = images,
                Options = q.Options.Select(o =>
                {
                    var (oc, oImages) = SeparateContentAndImages(o.NoiDung, o.HinhAnh, apiBaseUrl);
                    return new OptionViewModel
                    {
                        Id = o.DapAnID,
                        Content = oc,
                        Label = o.Label,
                        Images = oImages
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
            var (content, images) = SeparateContentAndImages(r.NoiDung, r.HinhAnh, apiBaseUrl);
            return new QuestionResultViewModel
            {
                OrderIndex = r.OrderIndex,
                Content = content,
                Loai = r.LoaiCauHoi,
                SelectedOptionId = r.SelectedDapAnID,
                IsCorrect = r.IsCorrect,
                Images = images,
                NoiDungTraLoi = r.NoiDungTraLoi,
                DapAnTraLoiNgan = r.DapAnTraLoiNgan,
                DiemDat = r.DiemDat,
                DiemToiDa = r.DiemToiDa,
                YResults = r.YResults.Select(y =>
                {
                    var (yc, yImages) = SeparateContentAndImages(y.NoiDung, y.HinhAnh, apiBaseUrl);
                    return new YResultViewModel
                    {
                        Label = y.Label,
                        Content = yc,
                        DapAnDung = y.DapAnDung,
                        ClientLaDung = y.ClientLaDung,
                        IsCorrect = y.IsCorrect,
                        Images = yImages
                    };
                }).ToList(),
                Options = r.Options.Select(o =>
                {
                    var (oc, oImages) = SeparateContentAndImages(o.NoiDung, o.HinhAnh, apiBaseUrl);
                    return new OptionResultViewModel
                    {
                        Id = o.DapAnID,
                        Content = oc,
                        Label = o.Label,
                        IsCorrect = o.IsCorrect,
                        IsSelected = o.IsSelected,
                        Images = oImages
                    };
                }).ToList()
            };
        }).ToList()
    };
}