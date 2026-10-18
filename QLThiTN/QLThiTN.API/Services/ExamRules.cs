using QLThiTN.API.DTOs;
using QLThiTN.API.Entities;

namespace QLThiTN.API.Services;

public static class ExamRules
{
    public static DateTime Deadline(BaiLam attempt, DotThi session) =>
        new[] { attempt.ThoiGianBatDau.AddMinutes(session.ThoiLuongLamBai > 0 ? session.ThoiLuongLamBai : 45),
            session.ThoiGianDongCong }.Min();

    // Never accept new answers after the server deadline. The saved snapshot is authoritative.
    public static List<SubmitAnswerDto> SavedAnswers(BaiLam attempt) =>
        attempt.ChiTietBaiLams.Select(ct => new SubmitAnswerDto
        {
            CauHoiID = ct.CauHoiID,
            DapAnID = ct.LuaChonBaiLams.FirstOrDefault(l => l.DapAnID.HasValue)?.DapAnID,
            NoiDungTraLoi = ct.LuaChonBaiLams.FirstOrDefault(l => !l.DapAnID.HasValue)?.NoiDungTraLoi,
            YChoices = ct.LuaChonBaiLams.Where(l => l.DapAnID.HasValue).Select(l => new SubmitYChoiceDto
            {
                DapAnID = l.DapAnID!.Value,
                LaDung = l.NoiDungTraLoi == "Dung" ? true : l.NoiDungTraLoi == "Sai" ? false : null
            }).ToList()
        }).ToList();

    public static SubmitExamResultDto ForPublication(SubmitExamResultDto result)
    {
        if (result.ScorePublished) return result;
        result.Score = 0;
        result.Correct = 0;
        result.Wrong = 0;
        // Withhold the entire breakdown, including per-question scores and explanations.
        result.Results = new();
        return result;
    }
}
