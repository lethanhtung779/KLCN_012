using System.Net.Http.Json;
using System.Text.Json;

namespace QLThiTN.Web.Services;

    public class ApiClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _http;

        public ApiClient(HttpClient http) => _http = http;

        /// <summary>Dia chi API dang dung, de ghep duong dan anh tuong doi thanh URL day du.</summary>
        public string BaseUrl => _http.BaseAddress?.ToString().TrimEnd('/') ?? string.Empty;

        /// <summary>Doc message tu body loi cua API; body co the la JSON hoac text thuan
        /// (vd trang loi 500 cua developer exception page).</summary>
        private async Task<string?> ReadErrorMessageAsync(HttpResponseMessage response)
        {
            try
            {
                var body = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(body)) return null;
                if (body.TrimStart().StartsWith('{'))
                {
                    var msg = System.Text.Json.JsonSerializer.Deserialize<ApiMessage>(body, JsonOptions);
                    if (!string.IsNullOrWhiteSpace(msg?.Message)) return msg.Message;
                }
                return null; // body la text loi he thong -> de web hien thong bao chung
            }
            catch
            {
                return null;
            }
        }

    public async Task<List<ApiDeThi>> GetDeThisAsync(int? monHocId = null, string? loaiDe = null)
    {
        var url = "/api/dethi";
        var query = new List<string>();
        if (monHocId.HasValue) query.Add($"monHocId={monHocId.Value}");
        if (!string.IsNullOrWhiteSpace(loaiDe)) query.Add($"loaiDe={Uri.EscapeDataString(loaiDe)}");
        if (query.Count > 0) url += "?" + string.Join("&", query);

        return await _http.GetFromJsonAsync<List<ApiDeThi>>(url, JsonOptions) ?? new();
    }

    public async Task<ApiDeThi?> GetDeThiAsync(int id)
    {
        return await _http.GetFromJsonAsync<ApiDeThi>($"/api/dethi/{id}", JsonOptions);
    }

    public async Task<List<ApiCauHoi>> GetCauHoiAsync(int deThiId)
    {
        return await _http.GetFromJsonAsync<List<ApiCauHoi>>($"/api/dethi/{deThiId}/cauhoi", JsonOptions) ?? new();
    }

    public async Task<ApiKetQua> NopBaiAsync(ApiNopBaiRequest request)
    {
        var response = await _http.PostAsJsonAsync($"/api/dethi/{request.DeThiID}/nopbai", request, JsonOptions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ApiKetQua>(JsonOptions) ?? new ApiKetQua();
    }

    /// <summary>Nop bai co xu loi: tra ve ket qua hoac thong bao loi tu API.</summary>
    public async Task<(ApiKetQua? Result, string? Error)> NopBaiSafeAsync(ApiNopBaiRequest request)
    {
        var response = await _http.PostAsJsonAsync($"/api/dethi/{request.DeThiID}/nopbai", request, JsonOptions);
        if (response.IsSuccessStatusCode)
            return (await response.Content.ReadFromJsonAsync<ApiKetQua>(JsonOptions), null);

        var err = await ReadErrorMessageAsync(response);
        return (null, err ?? $"Nop bai that bai ({(int)response.StatusCode}).");
    }

    /// <summary>Goi bat dau lam bai: kiem tra thoi gian/so lan thi/dang ky,
    /// tao hoac phuc hoi bai DangLam. Tra ve ket qua hoac thong bao loi tu API.</summary>
    public async Task<(ApiBatDauResult? Result, string? Error)> BatDauAsync(int deThiId, int hocVienId)
    {
        var response = await _http.PostAsJsonAsync($"/api/dethi/{deThiId}/batdau", new { HocVienID = hocVienId }, JsonOptions);
        if (response.IsSuccessStatusCode)
            return (await response.Content.ReadFromJsonAsync<ApiBatDauResult>(JsonOptions), null);

        var err = await ReadErrorMessageAsync(response);
        return (null, err ?? "Khong the bat dau bai thi.");
    }

    public async Task<(bool Success, string? Error)> LuuTamAsync(int deThiId, ApiLuuTamRequest request)
    {
        var response = await _http.PostAsJsonAsync($"/api/dethi/{deThiId}/luutam", request, JsonOptions);
        if (response.IsSuccessStatusCode) return (true, null);

        var err = await ReadErrorMessageAsync(response);
        return (false, err ?? "Luu tam that bai.");
    }

    public async Task<List<ApiDotThi>> GetDotThisAsync(int? hocVienId = null)
    {
        var url = hocVienId.HasValue ? $"/api/dotthi?hocVienId={hocVienId.Value}" : "/api/dotthi";
        return await _http.GetFromJsonAsync<List<ApiDotThi>>(url, JsonOptions) ?? new();
    }

    public async Task<(bool Success, string Message)> DangKyDotThiAsync(int dotThiId, int hocVienId)
    {
        var response = await _http.PostAsJsonAsync($"/api/dotthi/{dotThiId}/dangky", new { HocVienID = hocVienId }, JsonOptions);
        if (response.IsSuccessStatusCode)
            return (true, "Đăng ký đợt thi thành công!");

        var err = await ReadErrorMessageAsync(response);
        return (false, err ?? "Đăng ký thất bại.");
    }

    public async Task<(bool Success, string Message)> HuyDangKyDotThiAsync(int dotThiId, int hocVienId)
    {
        var response = await _http.DeleteAsync($"/api/dotthi/{dotThiId}/dangky/{hocVienId}");
        if (response.IsSuccessStatusCode)
            return (true, "Đã hủy đăng ký đợt thi.");

        var err = await ReadErrorMessageAsync(response);
        return (false, err ?? "Hủy đăng ký thất bại.");
    }

    public async Task<ApiProfile?> GetProfileAsync(int taiKhoanId)
    {
        return await _http.GetFromJsonAsync<ApiProfile>($"/api/auth/profile/{taiKhoanId}", JsonOptions);
    }

    public async Task<(bool Success, string Message)> UpdateProfileAsync(ApiUpdateProfileRequest request)
    {
        var response = await _http.PutAsJsonAsync("/api/auth/profile", request, JsonOptions);
        if (response.IsSuccessStatusCode)
            return (true, "Cập nhật thông tin thành công!");

        var err = await ReadErrorMessageAsync(response);
        return (false, err ?? "Cập nhật thất bại.");
    }

    public async Task<(bool Success, string Message)> ChangePasswordAsync(ApiChangePasswordRequest request)
    {
        var response = await _http.PutAsJsonAsync("/api/auth/password", request, JsonOptions);
        if (response.IsSuccessStatusCode)
            return (true, "Đổi mật khẩu thành công!");

        var err = await ReadErrorMessageAsync(response);
        return (false, err ?? "Đổi mật khẩu thất bại.");
    }

    public async Task<ApiLoginResult?> LoginAsync(string username, string password)
    {
        var response = await _http.PostAsJsonAsync("/api/auth/login", new ApiLoginRequest
        {
            Username = username,
            Password = password
        }, JsonOptions);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<ApiLoginResult>(JsonOptions);
    }

    public async Task<(bool Success, string Message)> RegisterAsync(ApiRegisterRequest request)
    {
        var response = await _http.PostAsJsonAsync("/api/auth/register", request, JsonOptions);
        if (response.IsSuccessStatusCode)
            return (true, "Đăng ký tài khoản thành công! Vui lòng đăng nhập.");

        var err = await ReadErrorMessageAsync(response);
        return (false, err ?? "Đăng ký thất bại.");
    }

    public async Task<List<ApiMonHoc>> GetMonHocsAsync()
    {
        return await _http.GetFromJsonAsync<List<ApiMonHoc>>("/api/monhoc", JsonOptions) ?? new();
    }

    public async Task<ApiExamStats?> GetExamStatsAsync()
    {
        return await _http.GetFromJsonAsync<ApiExamStats>("/api/dethi/stats", JsonOptions);
    }

    public async Task<List<ApiHistoryItem>> GetHistoryAsync(int hocVienId)
    {
        return await _http.GetFromJsonAsync<List<ApiHistoryItem>>($"/api/bailam/history/{hocVienId}", JsonOptions) ?? new();
    }

    public async Task<ApiKetQua?> GetBaiLamDetailAsync(int baiLamId)
    {
        return await _http.GetFromJsonAsync<ApiKetQua>($"/api/bailam/{baiLamId}", JsonOptions);
    }

    public async Task<ApiStudentStats?> GetStudentStatsAsync(int hocVienId)
    {
        return await _http.GetFromJsonAsync<ApiStudentStats>($"/api/bailam/stats/{hocVienId}", JsonOptions);
    }
}

public class ApiMessage
{
    public string Message { get; set; } = string.Empty;
}