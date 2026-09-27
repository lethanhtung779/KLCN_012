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

        var body = await response.Content.ReadFromJsonAsync<ApiMessage>(JsonOptions);
        return (false, body?.Message ?? "Đăng ký thất bại.");
    }

    public async Task<List<ApiMonHoc>> GetMonHocsAsync()
    {
        return await _http.GetFromJsonAsync<List<ApiMonHoc>>("/api/monhoc", JsonOptions) ?? new();
    }
}

public class ApiMessage
{
    public string Message { get; set; } = string.Empty;
}