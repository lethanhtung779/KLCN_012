namespace QLThiTN.API.DTOs;

public class DeThiListItemDto
{
    public int DeThiID { get; set; }
    public string TenDe { get; set; } = string.Empty;
    public string TenMon { get; set; } = string.Empty;
    public string LoaiDe { get; set; } = string.Empty;
    public string LoaiDeText { get; set; } = string.Empty;
    public int SoLuongCauHoi { get; set; }
    public string HinhThucTao { get; set; } = string.Empty;
    public int? DotThiID { get; set; }
    public string TenDotThi { get; set; } = string.Empty;
    public DateTime? ThoiGianMoCong { get; set; }
    public DateTime? ThoiGianDongCong { get; set; }
    public int? ThoiLuongLamBai { get; set; }
    public string TrangThai { get; set; } = string.Empty;
}

public class DeThiDetailDto
{
    public int DeThiID { get; set; }
    public string TenDe { get; set; } = string.Empty;
    public string TenMon { get; set; } = string.Empty;
    public string LoaiDe { get; set; } = string.Empty;
    public string LoaiDeText { get; set; } = string.Empty;
    public int SoLuongCauHoi { get; set; }
    public string HinhThucTao { get; set; } = string.Empty;
    public int? DotThiID { get; set; }
    public string TenDotThi { get; set; } = string.Empty;
    public DateTime? ThoiGianMoCong { get; set; }
    public DateTime? ThoiGianDongCong { get; set; }
    public int? ThoiLuongLamBai { get; set; }
    public string TrangThai { get; set; } = string.Empty;
}

public class CreateDeThiRequestDto
{
    public string TenDe { get; set; } = string.Empty;
    public int MonHocID { get; set; }
    public int GiaoVienID { get; set; } = 1;
    public string LoaiDe { get; set; } = "ThiThu";
    public int SoLuongCauHoi { get; set; } = 10;
}

public class HinhAnhDto
{
    public int HinhAnhID { get; set; }
    public int ThuTu { get; set; }
    /// <summary>Duong dan tuong doi phuc vu boi GET /api/hinhanh/{HinhAnhID}.</summary>
    public string Url { get; set; } = string.Empty;
    public string MoTa { get; set; } = string.Empty;
}

public class QuestionForDoingDto
{
    public int CauHoiID { get; set; }
    public int OrderIndex { get; set; }
    public string NoiDung { get; set; } = string.Empty;
    public string LoaiCauHoi { get; set; } = string.Empty;
    public string LoaiCauHoiText { get; set; } = string.Empty;
    public List<HinhAnhDto> HinhAnh { get; set; } = new();
    public List<OptionForDoingDto> Options { get; set; } = new();
}

public class OptionForDoingDto
{
    public int DapAnID { get; set; }
    public int ThuTu { get; set; }
    public string NoiDung { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public List<HinhAnhDto> HinhAnh { get; set; } = new();
}

public class SubmitExamRequestDto
{
    public int DeThiID { get; set; }
    public List<SubmitAnswerDto> Answers { get; set; } = new();
}

public class SubmitAnswerDto
{
    public int CauHoiID { get; set; }
    public int? DapAnID { get; set; }
    public string? NoiDungTraLoi { get; set; }

    /// <summary>Chon Dung/Sai cho tung y cua cau LoaiCauHoi = "DungSai".</summary>
    public List<SubmitYChoiceDto> YChoices { get; set; } = new();
}

public class SubmitYChoiceDto
{
    public int DapAnID { get; set; }
    /// <summary>Thi sinh chon y nay Dung (true) hay Sai (false). Null = khong tra loi.</summary>
    public bool? LaDung { get; set; }
}

public class SubmitExamResultDto
{
    public int DeThiID { get; set; }
    public decimal Score { get; set; }
    public int Total { get; set; }
    public int Correct { get; set; }
    public int Wrong { get; set; }
    public List<QuestionResultDto> Results { get; set; } = new();
}

public class QuestionResultDto
{
    public int OrderIndex { get; set; }
    public int CauHoiID { get; set; }
    public string NoiDung { get; set; } = string.Empty;
    public string LoaiCauHoi { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int? SelectedDapAnID { get; set; }
    public int? CorrectDapAnID { get; set; }
    public string GiaiThich { get; set; } = string.Empty;
    public List<HinhAnhDto> HinhAnh { get; set; } = new();
    public List<OptionResultApiDto> Options { get; set; } = new();

    /// <summary>Ket qua tung y cua cau DungSai.</summary>
    public List<YResultDto> YResults { get; set; } = new();

    /// <summary>Tra loi ngan: thi sinh da go. Null voi loai cau khac.</summary>
    public string? NoiDungTraLoi { get; set; }

    /// <summary>Tra loi ngan: dap an dung cua he thong.</summary>
    public string? DapAnTraLoiNgan { get; set; }

    /// <summary>Diem dat duoc cho cau nay (tru diem theo tung y voi DungSai).</summary>
    public decimal DiemDat { get; set; }

    /// <summary>Diem toi da cua cau (cot Diem trong bang CauHoi).</summary>
    public decimal DiemToiDa { get; set; }
}

public class YResultDto
{
    public int DapAnID { get; set; }
    public int ThuTu { get; set; }
    public string Label { get; set; } = string.Empty;
    public string NoiDung { get; set; } = string.Empty;
    /// <summary>Dap an dung cua y: true = Dung, false = Sai.</summary>
    public bool DapAnDung { get; set; }
    /// <summary>Thi sinh da chon: true = Dung, false = Sai, null = bo trong.</summary>
    public bool? ClientLaDung { get; set; }
    public bool IsCorrect { get; set; }
    public List<HinhAnhDto> HinhAnh { get; set; } = new();
}

public class OptionResultApiDto
{
    public int DapAnID { get; set; }
    public int ThuTu { get; set; }
    public string NoiDung { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public bool IsSelected { get; set; }
    public List<HinhAnhDto> HinhAnh { get; set; } = new();
}

public static class LoaiDeTextHelper
{
    public static string LoaiDeText(string loaiDe) => loaiDe switch
    {
        "OnTap" => "Bài tập về nhà",
        "ThiThu" => "Thi thử",
        "ChinhThuc" => "Thi chính thức",
        _ => "Đề thi"
    };

    public static string LoaiCauHoiText(string loai) => loai switch
    {
        "TracNghiem" => "Trắc nghiệm một đáp án",
        "DungSai" => "Đúng/Sai",
        "TraLoiNgan" => "Trả lời ngắn",
        _ => loai
    };
}