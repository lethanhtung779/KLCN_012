namespace QLThiTN.API.Mappings;

using QLThiTN.API.DTOs;
using QLThiTN.API.Entities;

public static class MappingExtensions
{
    public static MonHocDto ToDto(this MonHoc x) => new()
    {
        MonHocID = x.MonHocID,
        TenMon = x.TenMon,
        SoChuDe = x.ChuDes.Count,
        SoCauHoi = x.ChuDes.Sum(c => c.CauHois.Count)
    };

    public static ChuDeDto ToDto(this ChuDe x) => new()
    {
        ChuDeID = x.ChuDeID,
        MonHocID = x.MonHocID,
        TenChuDe = x.TenChuDe,
        SoCauHoi = x.CauHois.Count
    };

    public static CauHoiDto ToDto(this CauHoi x) => new()
    {
        CauHoiID = x.CauHoiID,
        ChuDeID = x.ChuDeID,
        TenChuDe = x.ChuDe?.TenChuDe ?? string.Empty,
        TenMon = x.ChuDe?.MonHoc?.TenMon ?? string.Empty,
        NoiDung = x.NoiDung,
        LoaiCauHoi = x.LoaiCauHoi,
        MucDoKho = x.MucDoKho,
        Diem = x.Diem,
        TrangThai = x.TrangThai,
        NgayTao = x.NgayTao,
        GiaiThichDapAn = x.GiaiThichDapAn,
        DapAns = x.DapAns
            .OrderBy(d => d.ThuTu)
            .Select(d => d.ToDto())
            .ToList()
    };

    public static DapAnDto ToDto(this DapAn x) => new()
    {
        DapAnID = x.DapAnID,
        ThuTu = x.ThuTu,
        NoiDung = x.NoiDung,
        LaDapAnDung = x.LaDapAnDung,
        GiaiThich = x.GiaiThich
    };
}