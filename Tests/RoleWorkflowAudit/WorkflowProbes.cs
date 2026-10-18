using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using QLThiTN.API.Data;
using QLThiTN.API.DTOs;
using QLThiTN.API.Entities;

static class WorkflowProbes
{
    public static readonly List<string> Observations = new();
    public static void Record(string id, string result) { Observations.Add($"- **{id}**: {result}"); Console.WriteLine($"AUDIT {id}: {result}"); }
    public static async Task Run(QLThiTNDbContext db, HttpClient a, HttpClient b, HttpClient anonymous,
        HocVien student, HocVien other, DeThi original, CauHoi question)
    {
        async Task<(DeThi Exam, DotThi Session)> Fixture(string name, string state = "DangMo", string scope = "ToanTruong", int? capacity = null)
        {
            var exam = new DeThi { TenDe = name, MonHocID = original.MonHocID, GiaoVienID = original.GiaoVienID,
                LoaiDe = "ThiThu", HinhThucTao = "ThuCong", LaMienPhi = true, SoLuongCauHoi = 1,
                DeThi_CauHois = [new() { CauHoiID = question.CauHoiID, ThuTu = 1 }], MaDeThis = [new() { TenMaDe = "101" }] };
            var session = new DotThi { DeThi = exam, TenDotThi = name, TrangThai = state, PhamVi = scope,
                ThoiGianMoCong = DateTime.Now.AddHours(-1), ThoiGianDongCong = DateTime.Now.AddHours(1),
                ThoiLuongLamBai = 30, SoLanThiToiDa = 1, GioiHanSoLuong = capacity, CongBoDiemSom = true };
            db.Add(session); await db.SaveChangesAsync(); return (exam, session);
        }
        var closed = await Fixture("Admin closed", "DaDong");
        using var closedStart = await a.PostAsJsonAsync($"/api/dethi/{closed.Exam.DeThiID}/batdau", new { HocVienID = student.HocVienID });
        Record("U01", $"Start exam marked DaDong while dates are open: HTTP {(int)closedStart.StatusCode} (expected rejection).");
        var restricted = await Fixture("Class restricted", scope: "TheoLop");
        using var restrictedStart = await a.PostAsJsonAsync($"/api/dethi/{restricted.Exam.DeThiID}/batdau", new { HocVienID = student.HocVienID });
        Record("U02", $"Start TheoLop exam with no class membership: HTTP {(int)restrictedStart.StatusCode} (expected rejection).");
        var capacity = await Fixture("Capacity one", capacity: 1);
        var registrationPath = $"/api/dotthi/{capacity.Session.DotThiID}/dangky";
        (await a.PostAsJsonAsync(registrationPath, new { HocVienID = student.HocVienID })).EnsureSuccessStatusCode();
        (await a.DeleteAsync(registrationPath + $"/{student.HocVienID}")).EnsureSuccessStatusCode();
        (await b.PostAsJsonAsync(registrationPath, new { HocVienID = other.HocVienID })).EnsureSuccessStatusCode();
        using var again = await a.PostAsJsonAsync(registrationPath, new { HocVienID = student.HocVienID });
        var count = await db.DangKyDotThis.CountAsync(x => x.DotThiID == capacity.Session.DotThiID && x.TrangThai != "DaHuy");
        Record("U03", $"Re-register previously cancelled seat after another student fills capacity: HTTP {(int)again.StatusCode}; active registrations {count}/1.");
        var multi = await Fixture("Old closed session");
        multi.Session.ThoiGianDongCong = DateTime.Now.AddMinutes(-1);
        var latest = new DotThi { DeThiID = multi.Exam.DeThiID, TenDotThi = "New open session", TrangThai = "DangMo", PhamVi = "ToanTruong",
            ThoiGianMoCong = DateTime.Now.AddMinutes(-1), ThoiGianDongCong = DateTime.Now.AddHours(1), ThoiLuongLamBai = 30, SoLanThiToiDa = 1 };
        db.Add(latest); await db.SaveChangesAsync();
        using var multiStart = await a.PostAsJsonAsync($"/api/dethi/{multi.Exam.DeThiID}/batdau", new { HocVienID = student.HocVienID });
        Record("U04", $"Same exam has old closed and new open sessions; start HTTP {(int)multiStart.StatusCode}: {await multiStart.Content.ReadAsStringAsync()}");
        var vip = await Fixture("VIP only");
        vip.Exam.LaMienPhi = false; await db.SaveChangesAsync();
        using var vipStart = await a.PostAsJsonAsync($"/api/dethi/{vip.Exam.DeThiID}/batdau", new { HocVienID = student.HocVienID });
        Record("PASS-VIP", $"Free account starting paid exam: HTTP {(int)vipStart.StatusCode} (expected 403).");
        var malformed = await Fixture("Malformed answer");
        var begin = await a.PostAsJsonAsync($"/api/dethi/{malformed.Exam.DeThiID}/batdau", new { HocVienID = student.HocVienID });
        var attempt = (await begin.Content.ReadFromJsonAsync<BatDauResponseDto>())!;
        using var invalidAnswer = await a.PostAsJsonAsync($"/api/dethi/{malformed.Exam.DeThiID}/luutam", new LuuTamRequestDto {
            BaiLamID = attempt.BaiLamID, HocVienID = student.HocVienID,
            Answers = [new() { CauHoiID = question.CauHoiID, DapAnID = int.MaxValue }] });
        Record("U05", $"Autosave non-existent answer ID: HTTP {(int)invalidAnswer.StatusCode} (expected validation 400).");
        var blank = await a.PostAsJsonAsync($"/api/dethi/{malformed.Exam.DeThiID}/nopbai", new SubmitExamRequestDto {
            BaiLamID = attempt.BaiLamID, HocVienID = student.HocVienID, Answers = [] });
        var blankResult = await blank.Content.ReadFromJsonAsync<SubmitExamResultDto>();
        Record("PASS-BLANK", $"Blank submission HTTP {(int)blank.StatusCode}; score {blankResult?.Score}.");
        using var limit = await a.PostAsJsonAsync($"/api/dethi/{malformed.Exam.DeThiID}/batdau", new { HocVienID = student.HocVienID });
        Record("PASS-LIMIT", $"Second attempt after one-attempt limit: HTTP {(int)limit.StatusCode} (expected 409).");

        // Disable SMTP explicitly in the parent process; this never sends email.
        using var registration = await anonymous.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto {
            UserName = "audit-register", FullName = "Audit registration", Email = "audit-register@example.test", Password = "x" });
        var pending = await db.DangKyXacMinhEmails.AsNoTracking().SingleOrDefaultAsync(x => x.TenDangNhap == "audit-register");
        Record("U06", $"Production registration with SMTP disabled and one-character password: HTTP {(int)registration.StatusCode}; pending record exists={pending != null}.");
        if (pending != null)
        {
            // Simulated access to the email link, read only from the disposable fixture.
            var confirm = await anonymous.PostAsJsonAsync("/api/auth/confirm-email", new { pending.Token });
            var login = await anonymous.PostAsJsonAsync("/api/auth/login", new { Username = "audit-register", Password = "x" });
            Record("U07", $"Confirm one-character-password account: HTTP {(int)confirm.StatusCode}; login HTTP {(int)login.StatusCode}.");
            var replay = await anonymous.PostAsJsonAsync("/api/auth/confirm-email", new { pending.Token });
            Record("PASS-TOKEN", $"Reusing consumed confirmation token: HTTP {(int)replay.StatusCode} (expected 400).");
        }
        var mixed = await Fixture("Mixed grading");
        var trueFalse = new CauHoi { ChuDeID = question.ChuDeID, GiaoVienID = original.GiaoVienID,
            NoiDung = "True/false", LoaiCauHoi = "DungSai", MucDoKho = "NhanBiet", TrangThai = "HoatDong", Diem = 1,
            DapAns = Enumerable.Range(0,4).Select(i => new DapAn { NoiDung = "Statement " + i, ThuTu = i, LaDapAnDung = true }).ToList() };
        var shortAnswer = new CauHoi { ChuDeID = question.ChuDeID, GiaoVienID = original.GiaoVienID,
            NoiDung = "Decimal answer", LoaiCauHoi = "TraLoiNgan", MucDoKho = "NhanBiet", TrangThai = "HoatDong", Diem = 0.5m,
            DapAns = [new() { NoiDung = "2.5", LaDapAnDung = true, ThuTu = 0 }] };
        db.AddRange(new DeThi_CauHoi { DeThiID = mixed.Exam.DeThiID, CauHoi = trueFalse, ThuTu = 2 },
            new DeThi_CauHoi { DeThiID = mixed.Exam.DeThiID, CauHoi = shortAnswer, ThuTu = 3 });
        mixed.Exam.SoLuongCauHoi = 3; await db.SaveChangesAsync();
        var mixedStart = await a.PostAsJsonAsync($"/api/dethi/{mixed.Exam.DeThiID}/batdau", new { HocVienID = student.HocVienID });
        var mixedAttempt = (await mixedStart.Content.ReadFromJsonAsync<BatDauResponseDto>())!;
        var mixedSubmit = await a.PostAsJsonAsync($"/api/dethi/{mixed.Exam.DeThiID}/nopbai", new SubmitExamRequestDto {
            BaiLamID = mixedAttempt.BaiLamID, HocVienID = student.HocVienID, Answers = [
                new() { CauHoiID = question.CauHoiID, DapAnID = question.DapAns[0].DapAnID },
                new() { CauHoiID = trueFalse.CauHoiID, YChoices = trueFalse.DapAns.Select((x,i) => new SubmitYChoiceDto { DapAnID = x.DapAnID, LaDung = i < 3 }).ToList() },
                new() { CauHoiID = shortAnswer.CauHoiID, NoiDungTraLoi = "2,5" }
            ] });
        var mixedResult = await mixedSubmit.Content.ReadFromJsonAsync<SubmitExamResultDto>();
        Record("PASS-GRADING", $"Three question types: MC correct, 3/4 true-false correct, decimal comma accepted; score={mixedResult?.Score} (expected 9 under current linear partial-credit rule).");
        var teacherLogin = await anonymous.PostAsJsonAsync("/api/auth/login", new { Username = "teacher", Password = "Test-password-123!" });
        var teacherAuth = (await teacherLogin.Content.ReadFromJsonAsync<AuthResponseDto>())!;
        using var teacher = new HttpClient { BaseAddress = anonymous.BaseAddress };
        teacher.DefaultRequestHeaders.Authorization = new("Bearer", teacherAuth.AccessToken);
        var unassigned = await teacher.GetAsync($"/api/cauhoi?monHocId={original.MonHocID}");
        db.GiaoVien_MonHocs.Add(new() { GiaoVienID = original.GiaoVienID, MonHocID = original.MonHocID });
        await db.SaveChangesAsync();
        var assigned = await teacher.GetAsync($"/api/cauhoi?monHocId={original.MonHocID}");
        Record("PASS-TEACHER-API", $"Question bank unassigned HTTP {(int)unassigned.StatusCode}; after subject assignment HTTP {(int)assigned.StatusCode} (expected 403/200).");
    }
}
