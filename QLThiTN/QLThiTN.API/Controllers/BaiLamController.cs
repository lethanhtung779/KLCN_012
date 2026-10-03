using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLThiTN.API.Data;
using QLThiTN.API.DTOs;
using QLThiTN.API.Entities;

namespace QLThiTN.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BaiLamController : ControllerBase
{
    private readonly QLThiTNDbContext _db;

    public BaiLamController(QLThiTNDbContext db) => _db = db;

    /// <summary>
    /// Lấy lịch sử tất cả các lần thi của một học viên.
    /// </summary>
    [HttpGet("history/{hocVienId:int}")]
    public async Task<IActionResult> GetHistory(int hocVienId)
    {
        var list = await _db.BaiLams
            .Where(b => b.HocVienID == hocVienId && b.TrangThai != "DangLam")
            .Include(b => b.DotThi)
                .ThenInclude(d => d!.DeThi)
                    .ThenInclude(dt => dt!.MonHoc)
            .OrderByDescending(b => b.ThoiGianNop ?? b.ThoiGianBatDau)
            .ToListAsync();

        var now = DateTime.Now;
        var result = list.Select(b =>
        {
            var de = b.DotThi?.DeThi;
            var mon = de?.MonHoc;
            var loaiDe = de?.LoaiDe ?? "ThiThu";
            // CongBoDiemSom = 1 hoac dot thi da ket thuc -> diem duoc cong bo
            var congBoDiem = (b.DotThi?.CongBoDiemSom ?? true)
                || (b.DotThi is not null && b.DotThi.ThoiGianDongCong <= now);

            return new BaiLamHistoryItemDto
            {
                BaiLamID = b.BaiLamID,
                DeThiID = de?.DeThiID ?? b.DotThi?.DeThiID ?? 0,
                DotThiID = b.DotThiID,
                TenDe = de?.TenDe ?? b.DotThi?.TenDotThi ?? $"Kỳ thi #{b.BaiLamID}",
                TenMon = mon?.TenMon ?? "Tổng hợp",
                LoaiDe = loaiDe,
                LoaiDeText = LoaiDeTextHelper.LoaiDeText(loaiDe),
                LanThi = b.LanThi,
                ThoiGianNop = b.ThoiGianNop ?? b.ThoiGianBatDau,
                TongDiem = b.TongDiem ?? 0m,
                ThoiLuongLamBai = b.DotThi?.ThoiLuongLamBai ?? 45,
                TrangThai = b.TrangThai,
                ScorePublished = congBoDiem
            };
        }).ToList();

        return Ok(result);
    }

    /// <summary>
    /// Thống kê kết quả học tập tổng quan của học viên.
    /// </summary>
    [HttpGet("stats/{hocVienId:int}")]
    public async Task<IActionResult> GetStudentStats(int hocVienId)
    {
        var completed = await _db.BaiLams
            .Where(b => b.HocVienID == hocVienId && b.TongDiem.HasValue)
            .ToListAsync();

        if (completed.Count == 0)
        {
            return Ok(new StudentStatsDto
            {
                TotalExams = 0,
                AverageScore = 0m,
                HighestScore = 0m
            });
        }

        var avg = Math.Round(completed.Average(b => b.TongDiem!.Value), 2, MidpointRounding.AwayFromZero);
        var max = completed.Max(b => b.TongDiem!.Value);

        return Ok(new StudentStatsDto
        {
            TotalExams = completed.Count,
            AverageScore = avg,
            HighestScore = max
        });
    }

    /// <summary>
    /// Lấy chi tiết một bài làm đã nộp (để xem lại bài thi).
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetDetail(int id)
    {
        var baiLam = await _db.BaiLams
            .Include(b => b.DotThi)
                .ThenInclude(d => d!.DeThi)
                    .ThenInclude(dt => dt!.MonHoc)
            .Include(b => b.ChiTietBaiLams)
                .ThenInclude(ct => ct.CauHoi)
                    .ThenInclude(c => c!.DapAns)
                        .ThenInclude(d => d.HinhAnhs)
            .Include(b => b.ChiTietBaiLams)
                .ThenInclude(ct => ct.CauHoi)
                    .ThenInclude(c => c!.HinhAnhs)
            .Include(b => b.ChiTietBaiLams)
                .ThenInclude(ct => ct.LuaChonBaiLams)
            .FirstOrDefaultAsync(b => b.BaiLamID == id);

        if (baiLam is null) return NotFound();

        var de = baiLam.DotThi?.DeThi;
        var deThiId = de?.DeThiID ?? baiLam.DotThi?.DeThiID ?? 0;

        // Lấy thứ tự câu hỏi trong đề
        var dtchMap = await _db.DeThi_CauHois
            .Where(x => x.DeThiID == deThiId)
            .ToDictionaryAsync(x => x.CauHoiID, x => x.ThuTu);

        var results = new List<QuestionResultDto>();
        int correctCount = 0;

        var sortedChiTiets = baiLam.ChiTietBaiLams
            .OrderBy(ct => dtchMap.GetValueOrDefault(ct.CauHoiID, ct.ChiTietID))
            .ToList();

        int order = 1;
        foreach (var ct in sortedChiTiets)
        {
            var cau = ct.CauHoi!;
            var orderIndex = dtchMap.GetValueOrDefault(ct.CauHoiID, order++);
            var diemToiDa = cau.Diem > 0 ? cau.Diem : (cau.LoaiCauHoi == "DungSai" ? 1.0m : (cau.LoaiCauHoi == "TraLoiNgan" ? 0.5m : 0.25m));
            var diemDat = ct.DiemDatDuoc ?? 0m;

            if (cau.LoaiCauHoi == "TraLoiNgan")
            {
                var correctDa = cau.DapAns.FirstOrDefault(d => d.LaDapAnDung);
                var clientChoice = ct.LuaChonBaiLams.FirstOrDefault(l => l.DapAnID == null);
                bool isCorrect = diemDat >= diemToiDa && diemToiDa > 0;
                if (isCorrect) correctCount++;

                results.Add(new QuestionResultDto
                {
                    OrderIndex = orderIndex,
                    CauHoiID = cau.CauHoiID,
                    NoiDung = cau.NoiDung,
                    LoaiCauHoi = cau.LoaiCauHoi,
                    IsCorrect = isCorrect,
                    GiaiThich = cau.GiaiThichDapAn ?? string.Empty,
                    HinhAnh = ToHinhAnhDto(cau.HinhAnhs),
                    NoiDungTraLoi = clientChoice?.NoiDungTraLoi,
                    DapAnTraLoiNgan = correctDa?.NoiDung,
                    DiemDat = diemDat,
                    DiemToiDa = diemToiDa
                });
            }
            else if (cau.LoaiCauHoi == "DungSai")
            {
                var yRows = cau.DapAns.OrderBy(d => d.ThuTu).ToList();
                var chonMap = ct.LuaChonBaiLams
                    .Where(l => l.DapAnID.HasValue)
                    .ToDictionary(l => l.DapAnID!.Value, l => l.NoiDungTraLoi == "Dung");

                var yResults = new List<YResultDto>();
                int soYDung = 0;

                foreach (var y in yRows)
                {
                    var hasChoice = chonMap.ContainsKey(y.DapAnID);
                    bool? clientLaDung = hasChoice ? chonMap[y.DapAnID] : null;
                    var yDung = clientLaDung.HasValue && clientLaDung.Value == y.LaDapAnDung;
                    if (yDung) soYDung++;

                    yResults.Add(new YResultDto
                    {
                        DapAnID = y.DapAnID,
                        ThuTu = y.ThuTu,
                        Label = ((char)('a' + y.ThuTu)).ToString(),
                        NoiDung = y.NoiDung,
                        DapAnDung = y.LaDapAnDung,
                        ClientLaDung = clientLaDung,
                        IsCorrect = yDung,
                        HinhAnh = ToHinhAnhDto(y.HinhAnhs)
                    });
                }

                bool isCorrect = yRows.Count > 0 && soYDung == yRows.Count;
                if (isCorrect) correctCount++;

                results.Add(new QuestionResultDto
                {
                    OrderIndex = orderIndex,
                    CauHoiID = cau.CauHoiID,
                    NoiDung = cau.NoiDung,
                    LoaiCauHoi = cau.LoaiCauHoi,
                    IsCorrect = isCorrect,
                    GiaiThich = cau.GiaiThichDapAn ?? string.Empty,
                    HinhAnh = ToHinhAnhDto(cau.HinhAnhs),
                    YResults = yResults,
                    DiemDat = diemDat,
                    DiemToiDa = diemToiDa
                });
            }
            else // TracNghiem
            {
                var selectedChoice = ct.LuaChonBaiLams.FirstOrDefault(l => l.DapAnID.HasValue);
                var selectedDapAnId = selectedChoice?.DapAnID;
                var correctDa = cau.DapAns.FirstOrDefault(d => d.LaDapAnDung);
                bool isCorrect = selectedDapAnId.HasValue && correctDa != null && selectedDapAnId.Value == correctDa.DapAnID;
                if (isCorrect) correctCount++;

                results.Add(new QuestionResultDto
                {
                    OrderIndex = orderIndex,
                    CauHoiID = cau.CauHoiID,
                    NoiDung = cau.NoiDung,
                    LoaiCauHoi = cau.LoaiCauHoi,
                    IsCorrect = isCorrect,
                    SelectedDapAnID = selectedDapAnId,
                    GiaiThich = correctDa?.GiaiThich ?? cau.GiaiThichDapAn ?? string.Empty,
                    HinhAnh = ToHinhAnhDto(cau.HinhAnhs),
                    DiemDat = diemDat,
                    DiemToiDa = diemToiDa,
                    Options = cau.DapAns
                        .OrderBy(d => d.ThuTu)
                        .Select(d => new OptionResultApiDto
                        {
                            DapAnID = d.DapAnID,
                            ThuTu = d.ThuTu,
                            NoiDung = d.NoiDung,
                            Label = LabelOf(d.ThuTu),
                            IsCorrect = d.LaDapAnDung,
                            IsSelected = selectedDapAnId.HasValue && selectedDapAnId.Value == d.DapAnID,
                            HinhAnh = ToHinhAnhDto(d.HinhAnhs)
                        }).ToList()
                });
            }
        }

        var total = results.Count;
        var durationUsed = (int)Math.Max(1, Math.Round(((baiLam.ThoiGianNop ?? DateTime.Now) - baiLam.ThoiGianBatDau).TotalMinutes));

        var dotThi = baiLam.DotThi;
        var scorePublished = (dotThi?.CongBoDiemSom ?? true)
            || (dotThi is not null && dotThi.ThoiGianDongCong <= DateTime.Now);

        // Chua cong bo diem -> khong tra ve dap an dung / giai thich
        if (!scorePublished)
        {
            foreach (var r in results)
            {
                r.IsCorrect = false;
                r.GiaiThich = string.Empty;
                r.DapAnTraLoiNgan = null;
                foreach (var y in r.YResults) { y.DapAnDung = false; y.IsCorrect = false; }
                foreach (var o in r.Options) { o.IsCorrect = false; }
            }
        }

        return Ok(new SubmitExamResultDto
        {
            BaiLamID = baiLam.BaiLamID,
            DeThiID = deThiId,
            TenDe = de?.TenDe ?? $"Kỳ thi #{deThiId}",
            TenMon = de?.MonHoc?.TenMon ?? string.Empty,
            Score = baiLam.TongDiem ?? 0m,
            Total = total,
            Correct = correctCount,
            Wrong = total - correctCount,
            ThoiGianBatDau = baiLam.ThoiGianBatDau,
            ThoiGianNop = baiLam.ThoiGianNop ?? DateTime.Now,
            DurationUsed = durationUsed,
            ScorePublished = scorePublished,
            Results = results
        });
    }

    private static List<HinhAnhDto> ToHinhAnhDto(IEnumerable<HinhAnh> list) =>
        list.OrderBy(h => h.ThuTu).Select(h => new HinhAnhDto
        {
            HinhAnhID = h.HinhAnhID,
            ThuTu = h.ThuTu,
            Url = $"/api/hinhanh/{h.HinhAnhID}",
            MoTa = h.MoTa ?? string.Empty
        }).ToList();

    private static string LabelOf(int thuTu) => thuTu switch
    {
        0 => "A",
        1 => "B",
        2 => "C",
        3 => "D",
        _ => ((char)('A' + thuTu)).ToString()
    };
}
