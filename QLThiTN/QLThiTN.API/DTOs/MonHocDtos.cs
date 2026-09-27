namespace QLThiTN.API.DTOs;

public class MonHocDto
{
    public int MonHocID { get; set; }
    public string TenMon { get; set; } = string.Empty;
    public int SoChuDe { get; set; }
    public int SoCauHoi { get; set; }
}

public class ChuDeDto
{
    public int ChuDeID { get; set; }
    public int MonHocID { get; set; }
    public string TenChuDe { get; set; } = string.Empty;
    public int SoCauHoi { get; set; }
}

public class CauHoiDto
{
    public int CauHoiID { get; set; }
    public int ChuDeID { get; set; }
    public string TenChuDe { get; set; } = string.Empty;
    public string TenMon { get; set; } = string.Empty;
    public string NoiDung { get; set; } = string.Empty;
    public string LoaiCauHoi { get; set; } = string.Empty;
    public string MucDoKho { get; set; } = string.Empty;
    public decimal Diem { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public DateTime NgayTao { get; set; }
    public string? GiaiThichDapAn { get; set; }
    public List<DapAnDto> DapAns { get; set; } = new();
}

public class DapAnDto
{
    public int DapAnID { get; set; }
    public int ThuTu { get; set; }
    public string NoiDung { get; set; } = string.Empty;
    public bool LaDapAnDung { get; set; }
    public string? GiaiThich { get; set; }
}