using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLThiTN.API.Data;
using QLThiTN.API.DTOs;
using QLThiTN.API.Entities;

namespace QLThiTN.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DeThiController : ControllerBase
{
    private readonly QLThiTNDbContext _db;

    public DeThiController(QLThiTNDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? monHocId,
        [FromQuery] string? loaiDe)
    {
        var query = _db.DeThis
            .Include(d => d.MonHoc)
            .Include(d => d.DotThis)
            .OrderByDescending(d => d.DeThiID)
            .AsQueryable();

        if (monHocId.HasValue)
            query = query.Where(d => d.MonHocID == monHocId.Value);
        if (!string.IsNullOrWhiteSpace(loaiDe))
            query = query.Where(d => d.LoaiDe == loaiDe);

        var list = await query.ToListAsync();
        return Ok(list.Select(ToListItemDto));
    }

    /// <summary>Thong ke tong hop: so lieu toan he thong + theo tung de
    /// (dung cho kho de va social-proof trang chu).</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var raw = await _db.BaiLams
            .Where(b => b.TrangThai != "DangLam")
            .Join(_db.DotThis, b => b.DotThiID, d => d.DotThiID,
                (b, d) => new { d.DeThiID, Diem = b.TongDiem })
            .ToListAsync();

        var theoDe = raw.GroupBy(x => x.DeThiID).Select(g =>
        {
            var diems = g.Where(x => x.Diem != null).Select(x => x.Diem!.Value).ToList();
            return new ExamStatItemDto
            {
                DeThiID = g.Key,
                SoLuotThi = g.Count(),
                SoHoanThanh = diems.Count,
                DiemTrungBinh = diems.Count > 0
                    ? Math.Round(diems.Average(), 2, MidpointRounding.AwayFromZero)
                    : null
            };
        }).ToList();

        var stats = new ExamStatsDto
        {
            TongCauHoi = await _db.CauHois.CountAsync(c => c.TrangThai == "HoatDong"),
            TongLuotLamBai = raw.Count,
            TongHocVien = await _db.HocViens.CountAsync(),
            TheoDe = theoDe
        };
        return Ok(stats);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var de = await _db.DeThis
            .Include(d => d.MonHoc)
            .Include(d => d.DotThis)
            .FirstOrDefaultAsync(d => d.DeThiID == id);

        if (de is null) return NotFound();
        return Ok(ToListItemDto(de));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDeThiRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.TenDe))
            return BadRequest(new { message = "TenDe khong duoc de trong" });
        if (request.SoLuongCauHoi < 1)
            return BadRequest(new { message = "SoLuongCauHoi phai >= 1" });

        var chuDes = await _db.ChuDes
            .Where(c => c.MonHocID == request.MonHocID)
            .Select(c => c.ChuDeID)
            .ToListAsync();

        if (chuDes.Count == 0)
            return BadRequest(new { message = "Mon hoc chua co chu de" });

        var pool = await _db.CauHois
            .Where(c => chuDes.Contains(c.ChuDeID) && c.TrangThai == "HoatDong")
            .Select(c => c.CauHoiID)
            .ToListAsync();

        if (pool.Count < request.SoLuongCauHoi)
            return BadRequest(new { message = $"Khong du cau hoi (co {pool.Count}, can {request.SoLuongCauHoi})" });

        var random = Random.Shared;
        var chosen = pool.OrderBy(_ => random.Next()).Take(request.SoLuongCauHoi).ToList();
        chosen.Sort();

        var de = new DeThi
        {
            TenDe = request.TenDe,
            MonHocID = request.MonHocID,
            GiaoVienID = request.GiaoVienID,
            LoaiDe = request.LoaiDe,
            SoLuongCauHoi = chosen.Count,
            HinhThucTao = "TuDong"
        };
        _db.DeThis.Add(de);
        for (int i = 0; i < chosen.Count; i++)
        {
            _db.DeThi_CauHois.Add(new DeThi_CauHoi
            {
                DeThi = de,
                CauHoiID = chosen[i],
                ThuTu = i + 1
            });
        }
        await _db.SaveChangesAsync();

        var created = await _db.DeThis
            .Include(d => d.MonHoc)
            .Include(d => d.DotThis)
            .FirstAsync(d => d.DeThiID == de.DeThiID);
        return CreatedAtAction(nameof(GetById), new { id = de.DeThiID }, ToListItemDto(created));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var de = await _db.DeThis.FindAsync(id);
        if (de is null) return NotFound();

        _db.DeThis.Remove(de);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id:int}/cauhoi")]
    public async Task<IActionResult> GetQuestions(int id)
    {
        var qs = await _db.DeThi_CauHois
            .Where(x => x.DeThiID == id)
            .OrderBy(x => x.ThuTu)
            .Include(x => x.CauHoi)
                .ThenInclude(c => c!.DapAns)
                    .ThenInclude(d => d.HinhAnhs)
            .Include(x => x.CauHoi)
                .ThenInclude(c => c!.HinhAnhs)
            .ToListAsync();

        if (qs.Count == 0) return NotFound();

        return Ok(qs.Select(q => new QuestionForDoingDto
        {
            CauHoiID = q.CauHoiID,
            OrderIndex = q.ThuTu,
            NoiDung = q.CauHoi!.NoiDung,
            LoaiCauHoi = q.CauHoi.LoaiCauHoi,
            LoaiCauHoiText = LoaiDeTextHelper.LoaiCauHoiText(q.CauHoi.LoaiCauHoi),
            HinhAnh = ToHinhAnhDto(q.CauHoi.HinhAnhs),
            Options = q.CauHoi.LoaiCauHoi == "TraLoiNgan"
                ? new List<OptionForDoingDto>()
                : q.CauHoi.DapAns
                    .OrderBy(d => d.ThuTu)
                    .Select(d => new OptionForDoingDto
                    {
                        DapAnID = d.DapAnID,
                        ThuTu = d.ThuTu,
                        NoiDung = d.NoiDung,
                        // Cau Dung/Sai: y gan nhan a b c d (chu thuong), khac voi A B C D
                        Label = q.CauHoi.LoaiCauHoi == "DungSai"
                            ? ((char)('a' + d.ThuTu)).ToString()
                            : LabelOf(d.ThuTu),
                        HinhAnh = ToHinhAnhDto(d.HinhAnhs)
                    }).ToList()
        }));
    }

    /// <summary>Bat dau (hoac phuc hoi) bai thi: kiem tra cua so thoi gian, so lan thi,
    /// dang ky dot thi; tao BaiLam trang thai DangLam va tra ve so giay con lai
    /// tinh theo dong ho server (F5 khong duoc them thoi gian).</summary>
    [HttpPost("{id:int}/batdau")]
    public async Task<IActionResult> BatDau(int id, [FromBody] BatDauRequestDto request)
    {
        var de = await _db.DeThis
            .Include(d => d.DotThis)
            .FirstOrDefaultAsync(d => d.DeThiID == id);
        if (de is null) return NotFound(new { message = "Khong tim thay de thi" });

        var hv = await _db.HocViens.FindAsync(request.HocVienID);
        if (hv is null) return Unauthorized(new { message = "Ban can dang nhap de lam bai thi" });

        var now = DateTime.Now;
        var dot = de.DotThis.FirstOrDefault();
        if (dot is null)
        {
            dot = TaoDotThiFallback(de);
            await _db.SaveChangesAsync();
        }

        if (dot.ThoiGianMoCong > now)
            return BadRequest(new { message = "De thi chua mo cong, vui long quay lai dung han" });
        if (dot.ThoiGianDongCong < now)
            return BadRequest(new { message = "De thi da dong cong, het han lam bai" });

        // SoLanThiToiDa <= 0 = khong gioi han (du lieu cu co the de 0)
        if (dot.SoLanThiToiDa > 0)
        {
            var soLanDa = await _db.BaiLams
                .CountAsync(b => b.HocVienID == request.HocVienID
                    && b.DotThiID == dot.DotThiID && b.TrangThai != "DangLam");
            if (soLanDa >= dot.SoLanThiToiDa)
                return Conflict(new { message = $"Ban da het luot thi toi da ({dot.SoLanThiToiDa} lan)" });
        }

        // Dot thi co gioi han so luong = thi theo lop/hoc vien duoc phan cong,
        // bat buoc dang ky truoc. Thi thu cong khai (khong gioi han) thi vao tu do.
        if (dot.GioiHanSoLuong.HasValue)
        {
            var daDangKy = await _db.DangKyDotThis
                .AnyAsync(k => k.DotThiID == dot.DotThiID
                    && k.HocVienID == request.HocVienID && k.TrangThai != "DaHuy");
            if (!daDangKy)
                return StatusCode(403, new { message = "Ban chua dang ky dot thi nay. Hay dang ky truoc khi vao thi." });
        }

        var thoiLuong = dot.ThoiLuongLamBai > 0 ? dot.ThoiLuongLamBai : 45;

        var dangLam = await _db.BaiLams
            .Include(b => b.ChiTietBaiLams)
                .ThenInclude(ct => ct.LuaChonBaiLams)
            .FirstOrDefaultAsync(b => b.HocVienID == request.HocVienID
                && b.DotThiID == dot.DotThiID && b.TrangThai == "DangLam");

        BaiLam baiLam;
        bool isResume;
        if (dangLam is not null)
        {
            baiLam = dangLam;
            isResume = true;
        }
        else
        {
            var maDe = await _db.MaDeThis.FirstOrDefaultAsync(m => m.DeThiID == id);
            if (maDe == null)
            {
                maDe = new MaDeThi { DeThiID = id, TenMaDe = "101" };
                _db.MaDeThis.Add(maDe);
                await _db.SaveChangesAsync();
            }

            var maxLanThi = await _db.BaiLams
                .Where(b => b.HocVienID == request.HocVienID && b.DotThiID == dot.DotThiID)
                .Select(b => (int?)b.LanThi)
                .MaxAsync() ?? 0;

            baiLam = new BaiLam
            {
                HocVienID = request.HocVienID,
                DotThiID = dot.DotThiID,
                MaDeID = maDe.MaDeID,
                LanThi = maxLanThi + 1,
                ThoiGianBatDau = now,
                TrangThai = "DangLam"
            };
            _db.BaiLams.Add(baiLam);
            await _db.SaveChangesAsync();
            isResume = false;
        }

        var remainingSeconds = Math.Max(0, thoiLuong * 60 - (int)(now - baiLam.ThoiGianBatDau).TotalSeconds);

        // Phuc hoi dap an da luu (neu co) de web khoi phuc form
        var savedAnswers = new List<SavedAnswerDto>();
        if (baiLam.ChiTietBaiLams.Count > 0)
        {
            var loaiMap = await _db.DeThi_CauHois
                .Where(x => x.DeThiID == id)
                .Include(x => x.CauHoi)
                .ToDictionaryAsync(x => x.CauHoiID, x => x.CauHoi!.LoaiCauHoi);

            foreach (var ct in baiLam.ChiTietBaiLams)
            {
                if (!loaiMap.TryGetValue(ct.CauHoiID, out var loai)) continue;
                var saved = new SavedAnswerDto { CauHoiID = ct.CauHoiID };
                foreach (var lc in ct.LuaChonBaiLams)
                {
                    if (loai == "DungSai")
                    {
                        saved.YChoices.Add(new SubmitYChoiceDto
                        {
                            DapAnID = lc.DapAnID ?? 0,
                            LaDung = lc.NoiDungTraLoi == "Dung" ? true
                                : lc.NoiDungTraLoi == "Sai" ? false : null
                        });
                    }
                    else if (loai == "TraLoiNgan")
                    {
                        saved.NoiDungTraLoi = lc.NoiDungTraLoi;
                    }
                    else
                    {
                        saved.DapAnID = lc.DapAnID;
                    }
                }
                if (saved.DapAnID != null || saved.NoiDungTraLoi != null || saved.YChoices.Count > 0)
                    savedAnswers.Add(saved);
            }
        }

        return Ok(new BatDauResponseDto
        {
            BaiLamID = baiLam.BaiLamID,
            DotThiID = dot.DotThiID,
            LanThi = baiLam.LanThi,
            IsResume = isResume,
            RemainingSeconds = remainingSeconds,
            ThoiGianBatDau = baiLam.ThoiGianBatDau,
            ThoiGianDongCong = dot.ThoiGianDongCong,
            ThoiLuongLamBai = thoiLuong,
            SavedAnswers = savedAnswers
        });
    }

    /// <summary>Luu tam dap an vao DB cho bai thi DangLam (goi dinh ky tu web).</summary>
    [HttpPost("{id:int}/luutam")]
    public async Task<IActionResult> LuuTam(int id, [FromBody] LuuTamRequestDto request)
    {
        var baiLam = await _db.BaiLams
            .Include(b => b.ChiTietBaiLams)
                .ThenInclude(ct => ct.LuaChonBaiLams)
            .FirstOrDefaultAsync(b => b.BaiLamID == request.BaiLamID);
        if (baiLam is null) return NotFound(new { message = "Khong tim thay bai lam" });
        if (baiLam.HocVienID != request.HocVienID)
            return Unauthorized(new { message = "Bai lam khong thuoc ve hoc vien nay" });
        if (baiLam.TrangThai != "DangLam")
            return BadRequest(new { message = "Bai thi da duoc nop, khong the luu tiep" });

        var items = await LoadExamItemsAsync(id);
        var soCau = await LuuDapAnTamAsync(baiLam, items, request.Answers);

        return Ok(new LuuTamResponseDto { LuuLuc = DateTime.Now, SoCauDaLuu = soCau });
    }

    [HttpPost("{id:int}/nopbai")]
    public async Task<IActionResult> Submit(int id, [FromBody] SubmitExamRequestDto request)
    {
        var de = await _db.DeThis
            .Include(d => d.MonHoc)
            .Include(d => d.DotThis)
            .FirstOrDefaultAsync(d => d.DeThiID == id);
        if (de is null) return NotFound(new { message = "Khong tim thay de thi" });

        var now = DateTime.Now;
        var dotThi = de.DotThis.FirstOrDefault();
        if (dotThi is null)
        {
            dotThi = TaoDotThiFallback(de);
            await _db.SaveChangesAsync();
        }

        // Chi chan nop thu cong ngoai cua so thoi gian; tu dong nop khi het gio
        // (HetGio) van duoc chap nhan de chot bai thi sinh dang lam truoc do.
        if (!request.HetGio)
        {
            if (dotThi.ThoiGianMoCong > now)
                return BadRequest(new { message = "De thi chua mo cong, khong the nop bai" });
            if (dotThi.ThoiGianDongCong < now)
                return BadRequest(new { message = "De thi da dong cong, khong the nop bai" });
        }

        var items = await LoadExamItemsAsync(id);
        if (items.Count == 0) return NotFound();

        var answerMap = request.Answers.ToDictionary(a => a.CauHoiID);

        var results = new List<QuestionResultDto>();
        decimal tongDiemDat = 0m, tongDiemToiDa = 0m;
        int correct = 0;

        foreach (var item in items)
        {
            var cau = item.CauHoi!;
            var answer = answerMap.GetValueOrDefault(cau.CauHoiID);
            var diemToiDa = cau.Diem > 0 ? cau.Diem : DiemMacDinh(cau.LoaiCauHoi);
            var diemDat = 0m;
            bool isCorrect;

            if (cau.LoaiCauHoi == "TraLoiNgan")
            {
                var correctDa = cau.DapAns.FirstOrDefault(d => d.LaDapAnDung);
                isCorrect = TraLoiNganDung(answer?.NoiDungTraLoi, correctDa?.NoiDung);
                if (isCorrect) diemDat = diemToiDa;

                results.Add(new QuestionResultDto
                {
                    OrderIndex = item.ThuTu,
                    CauHoiID = cau.CauHoiID,
                    NoiDung = cau.NoiDung,
                    LoaiCauHoi = cau.LoaiCauHoi,
                    IsCorrect = isCorrect,
                    GiaiThich = cau.GiaiThichDapAn ?? string.Empty,
                    HinhAnh = ToHinhAnhDto(cau.HinhAnhs),
                    NoiDungTraLoi = answer?.NoiDungTraLoi,
                    DapAnTraLoiNgan = correctDa?.NoiDung,
                    DiemDat = diemDat,
                    DiemToiDa = diemToiDa
                });
            }
            else if (cau.LoaiCauHoi == "DungSai")
            {
                // Cham theo tung y a-d: 0.25 diem moi y (diemToiDa / so y).
                var yRows = cau.DapAns.OrderBy(d => d.ThuTu).ToList();
                var chonMap = (answer?.YChoices ?? new List<SubmitYChoiceDto>())
                    .Where(y => y.DapAnID != 0)
                    .GroupBy(y => y.DapAnID)
                    .ToDictionary(g => g.Key, g => g.Last().LaDung);
                var diemMoiY = yRows.Count > 0 ? diemToiDa / yRows.Count : 0m;
                var yResults = new List<YResultDto>();
                int soYDung = 0;

                foreach (var y in yRows)
                {
                    var clientChon = chonMap.GetValueOrDefault(y.DapAnID);
                    var yDung = clientChon.HasValue && clientChon.Value == y.LaDapAnDung;
                    if (yDung) { soYDung++; diemDat += diemMoiY; }

                    yResults.Add(new YResultDto
                    {
                        DapAnID = y.DapAnID,
                        ThuTu = y.ThuTu,
                        Label = ((char)('a' + y.ThuTu)).ToString(),
                        NoiDung = y.NoiDung,
                        DapAnDung = y.LaDapAnDung,
                        ClientLaDung = clientChon,
                        IsCorrect = yDung,
                        HinhAnh = ToHinhAnhDto(y.HinhAnhs)
                    });
                }

                isCorrect = yRows.Count > 0 && soYDung == yRows.Count;

                results.Add(new QuestionResultDto
                {
                    OrderIndex = item.ThuTu,
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
                var correctDa = cau.DapAns.FirstOrDefault(d => d.LaDapAnDung);
                isCorrect = answer?.DapAnID != null && correctDa != null
                    && answer.DapAnID == correctDa.DapAnID;
                if (isCorrect) diemDat = diemToiDa;

                results.Add(new QuestionResultDto
                {
                    OrderIndex = item.ThuTu,
                    CauHoiID = cau.CauHoiID,
                    NoiDung = cau.NoiDung,
                    LoaiCauHoi = cau.LoaiCauHoi,
                    IsCorrect = isCorrect,
                    SelectedDapAnID = answer?.DapAnID,
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
                            IsSelected = answer?.DapAnID == d.DapAnID,
                            HinhAnh = ToHinhAnhDto(d.HinhAnhs)
                        }).ToList()
                });
            }

            tongDiemDat += diemDat;
            tongDiemToiDa += diemToiDa;
            if (isCorrect) correct++;
        }

        var total = results.Count;
        var score = tongDiemToiDa > 0
            ? Math.Round(tongDiemDat / tongDiemToiDa * 10, 2, MidpointRounding.AwayFromZero)
            : 0m;

        // Lưu bài làm vào Database
        int hocVienId = request.HocVienID ?? 0;
        if (hocVienId <= 0 || !await _db.HocViens.AnyAsync(h => h.HocVienID == hocVienId))
        {
            var guest = await _db.HocViens.Include(h => h.TaiKhoan)
                .FirstOrDefaultAsync(h => h.TaiKhoan!.TenDangNhap == "khach")
                ?? await _db.HocViens.OrderBy(h => h.HocVienID).FirstOrDefaultAsync();
            hocVienId = guest?.HocVienID ?? 1;
        }

        var maDe = await _db.MaDeThis.FirstOrDefaultAsync(m => m.DeThiID == id);
        if (maDe == null)
        {
            maDe = new MaDeThi { DeThiID = id, TenMaDe = "101" };
            _db.MaDeThis.Add(maDe);
            await _db.SaveChangesAsync();
        }

        BaiLam baiLam;
        if (request.BaiLamID is int baiLamId && baiLamId > 0)
        {
            // Luong chuan: web da goi batdau truoc khi lam bai, cap nhat bai DangLam
            var existing = await _db.BaiLams
                .Include(b => b.ChiTietBaiLams)
                    .ThenInclude(ct => ct.LuaChonBaiLams)
                .FirstOrDefaultAsync(b => b.BaiLamID == baiLamId);
            if (existing is null)
                return BadRequest(new { message = "Bai lam khong ton tai hoac da het han" });
            if (existing.HocVienID != hocVienId || existing.DotThiID != dotThi.DotThiID)
                return BadRequest(new { message = "Bai lam khong hop le voi dot thi nay" });
            if (existing.TrangThai != "DangLam")
                return BadRequest(new { message = "Bai thi da duoc nop truoc do" });

            baiLam = existing;
            baiLam.ThoiGianNop = now;
            baiLam.TongDiem = score;
            baiLam.TrangThai = request.HetGio ? "HetGio" : "DaNop";
        }
        else
        {
            // Luong du phong: goi nopbai truc tiep ma khong qua batdau
            var maxLanThi = await _db.BaiLams
                .Where(b => b.HocVienID == hocVienId && b.DotThiID == dotThi.DotThiID)
                .Select(b => (int?)b.LanThi)
                .MaxAsync() ?? 0;

            baiLam = new BaiLam
            {
                HocVienID = hocVienId,
                DotThiID = dotThi.DotThiID,
                MaDeID = maDe.MaDeID,
                LanThi = maxLanThi + 1,
                ThoiGianBatDau = request.ThoiGianBatDau
                    ?? now.AddMinutes(-(dotThi.ThoiLuongLamBai > 0 ? dotThi.ThoiLuongLamBai : 45)),
                ThoiGianNop = now,
                TongDiem = score,
                TrangThai = request.HetGio ? "HetGio" : "DaNop"
            };
            _db.BaiLams.Add(baiLam);
            await _db.SaveChangesAsync();
        }

        var startTime = baiLam.ThoiGianBatDau;
        var durationUsed = (int)Math.Max(1, Math.Round((now - startTime).TotalMinutes));

        var resultMap = results.ToDictionary(r => r.CauHoiID);
        var ctMap = baiLam.ChiTietBaiLams.ToDictionary(ct => ct.CauHoiID);
        foreach (var item in items)
        {
            var res = resultMap.GetValueOrDefault(item.CauHoiID);
            if (ctMap.TryGetValue(item.CauHoiID, out var ct))
            {
                ct.DiemDatDuoc = res?.DiemDat ?? 0m;
            }
            else
            {
                ct = new ChiTietBaiLam
                {
                    BaiLamID = baiLam.BaiLamID,
                    CauHoiID = item.CauHoiID,
                    DiemDatDuoc = res?.DiemDat ?? 0m
                };
                _db.ChiTietBaiLams.Add(ct);
                ctMap[item.CauHoiID] = ct;
            }
        }
        await _db.SaveChangesAsync();

        // Ghi lua chon cua thi sinh (thay the toan bo lua chon cu neu co)
        await LuuDapAnTamAsync(baiLam, items, request.Answers);

        return Ok(new SubmitExamResultDto
        {
            BaiLamID = baiLam.BaiLamID,
            DeThiID = id,
            TenDe = de?.TenDe ?? $"Kỳ thi #{id}",
            TenMon = de?.MonHoc?.TenMon ?? string.Empty,
            Score = score,
            Total = total,
            Correct = correct,
            Wrong = total - correct,
            ThoiGianBatDau = startTime,
            ThoiGianNop = now,
            DurationUsed = durationUsed,
            ScorePublished = dotThi.CongBoDiemSom || dotThi.ThoiGianDongCong <= now,
            Results = results
        });
    }

    private async Task<List<DeThi_CauHoi>> LoadExamItemsAsync(int id) =>
        await _db.DeThi_CauHois
            .Where(x => x.DeThiID == id)
            .OrderBy(x => x.ThuTu)
            .Include(x => x.CauHoi)
                .ThenInclude(c => c!.DapAns)
                    .ThenInclude(d => d.HinhAnhs)
            .Include(x => x.CauHoi)
                .ThenInclude(c => c!.HinhAnhs)
            .ToListAsync();

    /// <summary>Dot thi du phong khi de chua duoc lap lich (mo 30 ngay, khong gioi han).</summary>
    private DotThi TaoDotThiFallback(DeThi de)
    {
        var dot = new DotThi
        {
            DeThiID = de.DeThiID,
            TenDotThi = "Đợt thi - " + (de.TenDe ?? $"Đề #{de.DeThiID}"),
            ThoiGianMoCong = DateTime.Now.AddDays(-1),
            ThoiGianDongCong = DateTime.Now.AddDays(30),
            ThoiLuongLamBai = 45,
            TrangThai = "DangMo",
            PhamVi = "ToanTruong",
            SoLanThiToiDa = 0
        };
        _db.DotThis.Add(dot);
        de.DotThis.Add(dot);
        return dot;
    }

    /// <summary>Ghi (thay the) lua chon cua thi sinh cho tung cau — dung chung cho
    /// luutam (autosave) va nopbai. Chi tao ChiTietBaiLam cho cau co noi dung;
    /// cau bo trong thi xoa du lieu cu neu co.</summary>
    private async Task<int> LuuDapAnTamAsync(BaiLam baiLam, List<DeThi_CauHoi> items, List<SubmitAnswerDto> answers)
    {
        var answerMap = answers.ToDictionary(a => a.CauHoiID);

        // Doc lai chi tiet hien co tu DB thay vi dung navigation da Include:
        // them dong chao ca hai duong (navigation + FK) deu bi EF fixup la
        // nguyen nhan trung CauHoiID trong collection.
        var ctMap = await _db.ChiTietBaiLams
            .Where(x => x.BaiLamID == baiLam.BaiLamID)
            .Include(x => x.LuaChonBaiLams)
            .ToDictionaryAsync(x => x.CauHoiID);
        var changed = false;

        foreach (var item in items)
        {
            var cau = item.CauHoi!;
            var answer = answerMap.GetValueOrDefault(item.CauHoiID);
            var hasContent = HasAnyContent(answer);
            var hasOld = ctMap.TryGetValue(item.CauHoiID, out var ct);

            if (!hasContent)
            {
                if (hasOld && ct!.LuaChonBaiLams.Count > 0)
                {
                    _db.LuaChonBaiLams.RemoveRange(ct.LuaChonBaiLams);
                    ct.LuaChonBaiLams.Clear();
                    changed = true;
                }
                continue;
            }

            if (!hasOld)
            {
                ct = new ChiTietBaiLam
                {
                    BaiLamID = baiLam.BaiLamID,
                    CauHoiID = item.CauHoiID,
                    DiemDatDuoc = 0
                };
                _db.ChiTietBaiLams.Add(ct);
                ctMap[item.CauHoiID] = ct;
                changed = true;
            }

            if (ct!.LuaChonBaiLams.Count > 0)
            {
                _db.LuaChonBaiLams.RemoveRange(ct.LuaChonBaiLams);
                ct.LuaChonBaiLams.Clear();
                changed = true;
            }

            foreach (var lc in BuildLuaChon(cau, answer, ct.ChiTietID))
            {
                // Gan navigation thay vi FK de EF tu sinh ChiTietID cho chi tiet moi
                lc.ChiTietBaiLam = ct;
                _db.LuaChonBaiLams.Add(lc);
                changed = true;
            }
        }

        if (changed) await _db.SaveChangesAsync();
        return ctMap.Values.Count(ct => ct.LuaChonBaiLams.Count > 0);
    }

    private static bool HasAnyContent(SubmitAnswerDto? a) =>
        a is not null && ((a.DapAnID is int id && id > 0)
            || !string.IsNullOrWhiteSpace(a.NoiDungTraLoi)
            || a.YChoices.Any(y => y.DapAnID > 0));

    private static List<LuaChonBaiLam> BuildLuaChon(CauHoi cau, SubmitAnswerDto? answer, int chiTietID)
    {
        var rows = new List<LuaChonBaiLam>();
        if (cau.LoaiCauHoi == "TraLoiNgan")
        {
            if (!string.IsNullOrWhiteSpace(answer?.NoiDungTraLoi))
                rows.Add(new LuaChonBaiLam { ChiTietID = chiTietID, DapAnID = null, NoiDungTraLoi = answer.NoiDungTraLoi.Trim() });
        }
        else if (cau.LoaiCauHoi == "DungSai")
        {
            foreach (var y in answer?.YChoices ?? new List<SubmitYChoiceDto>())
            {
                // Chi ghi nhung y thi sinh thuc su chon Dung/Sai
                if (y.DapAnID > 0 && y.LaDung != null)
                    rows.Add(new LuaChonBaiLam
                    {
                        ChiTietID = chiTietID,
                        DapAnID = y.DapAnID,
                        NoiDungTraLoi = y.LaDung == true ? "Dung" : "Sai"
                    });
            }
        }
        else // TracNghiem
        {
            if (answer?.DapAnID is int da && da > 0)
                rows.Add(new LuaChonBaiLam { ChiTietID = chiTietID, DapAnID = da, NoiDungTraLoi = null });
        }
        return rows;
    }

    private static decimal DiemMacDinh(string loai) => loai switch
    {
        "DungSai" => 1.00m,
        "TraLoiNgan" => 0.50m,
        _ => 0.25m
    };

    /// <summary>So sanh tra loi ngan: dung ca chuoi (khong phan biet hoa/thuong)
    /// va so (663 = 663,0 = 663.0).</summary>
    private static bool TraLoiNganDung(string? client, string? correct)
    {
        if (string.IsNullOrWhiteSpace(client) || string.IsNullOrWhiteSpace(correct))
            return false;
        var c = client.Trim();
        var k = correct.Trim();
        if (string.Equals(c, k, StringComparison.OrdinalIgnoreCase)) return true;

        if (decimal.TryParse(k.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var kd) &&
            decimal.TryParse(c.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var cd))
            return cd == kd;
        return false;
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

    private DeThiListItemDto ToListItemDto(DeThi d)
    {
        var dot = d.DotThis.FirstOrDefault();
        var trangThai = dot is null ? "DaDong"
            : dot.ThoiGianMoCong > DateTime.Now ? "SapMo"
            : dot.ThoiGianDongCong < DateTime.Now ? "DaDong"
            : "DangMo";

        return new DeThiListItemDto
        {
            DeThiID = d.DeThiID,
            TenDe = d.TenDe,
            TenMon = d.MonHoc?.TenMon ?? string.Empty,
            LoaiDe = d.LoaiDe,
            LoaiDeText = LoaiDeTextHelper.LoaiDeText(d.LoaiDe),
            SoLuongCauHoi = d.SoLuongCauHoi,
            HinhThucTao = d.HinhThucTao,
            DotThiID = dot?.DotThiID,
            TenDotThi = dot?.TenDotThi ?? string.Empty,
            ThoiGianMoCong = dot?.ThoiGianMoCong,
            ThoiGianDongCong = dot?.ThoiGianDongCong,
            ThoiLuongLamBai = dot?.ThoiLuongLamBai,
            TrangThai = trangThai
        };
    }
}