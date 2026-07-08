using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CreditReporting.Shared.Dtos;

namespace CreditReporting.Wpf.Services;

/// <summary>Thrown when an API call fails. The message is suitable for display.</summary>
public class ApiException : Exception
{
    public HttpStatusCode? StatusCode { get; }

    public ApiException(string message, HttpStatusCode? statusCode = null, Exception? inner = null)
        : base(message, inner) => StatusCode = statusCode;
}

/// <summary>
/// Wraps HttpClient for all API calls. Stores the JWT after login and attaches
/// it to subsequent requests.
/// </summary>
public class ApiService
{
    private readonly HttpClient _http;

    public string? Username { get; private set; }
    public string? Role { get; private set; }
    public bool IsAuthenticated => _http.DefaultRequestHeaders.Authorization is not null;

    public ApiService(string baseUrl = "http://localhost:5006")
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public async Task LoginAsync(string username, string password)
    {
        var response = await SendAsync(() =>
            _http.PostAsJsonAsync("api/auth/login", new LoginRequest(username, password)));

        var login = await ReadAsync<LoginResponse>(response);
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
        Username = login.Username;
        Role = login.Role;
    }

    public async Task<List<CustomerSummaryDto>> SearchCustomersAsync(string? name, string? ssnLast4)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(name)) query.Add($"name={Uri.EscapeDataString(name)}");
        if (!string.IsNullOrWhiteSpace(ssnLast4)) query.Add($"ssnLast4={Uri.EscapeDataString(ssnLast4)}");

        var response = await SendAsync(() =>
            _http.GetAsync($"api/customers/search?{string.Join("&", query)}"));
        return await ReadAsync<List<CustomerSummaryDto>>(response);
    }

    public async Task<List<AccountDto>> GetCustomerAccountsAsync(int customerId)
    {
        var response = await SendAsync(() => _http.GetAsync($"api/customers/{customerId}/accounts"));
        return await ReadAsync<List<AccountDto>>(response);
    }

    public async Task<CreditReportDto> GetCreditReportAsync(int customerId, string purpose = "Account review")
    {
        var response = await SendAsync(() =>
            _http.GetAsync($"api/creditreport/{customerId}?purpose={Uri.EscapeDataString(purpose)}"));
        return await ReadAsync<CreditReportDto>(response);
    }

    public async Task<Metro2PreviewDto> Metro2PreviewAsync(Metro2GenerateRequest request)
    {
        var response = await SendAsync(() => _http.PostAsJsonAsync("api/metro2/preview", request));
        return await ReadAsync<Metro2PreviewDto>(response);
    }

    public async Task<(byte[] Content, string FileName)> Metro2GenerateAsync(Metro2GenerateRequest request)
    {
        var response = await SendAsync(() => _http.PostAsJsonAsync("api/metro2/generate", request));
        byte[] content = await response.Content.ReadAsByteArrayAsync();
        string fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                          ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                          ?? "metro2.dat";
        return (content, fileName);
    }

    public async Task<Metro2ParseResponseDto> Metro2ParseAsync(string filePath)
    {
        using var form = new MultipartFormDataContent();
        await using var stream = File.OpenRead(filePath);
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        form.Add(fileContent, "file", Path.GetFileName(filePath));

        var response = await SendAsync(() => _http.PostAsync("api/metro2/parse", form));
        return await ReadAsync<Metro2ParseResponseDto>(response);
    }

    // --- plumbing --------------------------------------------------------

    private static async Task<HttpResponseMessage> SendAsync(Func<Task<HttpResponseMessage>> send)
    {
        HttpResponseMessage response;
        try
        {
            response = await send();
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException("Cannot reach the API. Is CreditReporting.Api running?", null, ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new ApiException("The API request timed out.", null, ex);
        }

        if (response.IsSuccessStatusCode) return response;

        string detail = await ExtractProblemTitleAsync(response);
        throw new ApiException(detail, response.StatusCode);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        var result = await response.Content.ReadFromJsonAsync<T>();
        return result ?? throw new ApiException("The API returned an empty response.");
    }

    private static async Task<string> ExtractProblemTitleAsync(HttpResponseMessage response)
    {
        try
        {
            string body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("title", out var title))
                return title.GetString() ?? DefaultMessage(response.StatusCode);
        }
        catch
        {
            // fall through to the generic message
        }
        return DefaultMessage(response.StatusCode);
    }

    private static string DefaultMessage(HttpStatusCode status) => status switch
    {
        HttpStatusCode.Unauthorized => "Not authorized. Please log in again.",
        HttpStatusCode.NotFound => "The requested record was not found.",
        _ => $"The API returned {(int)status} {status}."
    };
}
