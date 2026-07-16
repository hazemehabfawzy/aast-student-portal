using System.Text;
using System.Text.Json;

namespace StudentPortal.Api.Services.Implementations;

public class FaceRecognitionClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public FaceRecognitionClient(HttpClient http, IConfiguration config)
    {
        _http    = http;
        _baseUrl = config["FaceService:BaseUrl"] ?? "http://face-service:8000";
    }

    public async Task<FaceRecognitionResponse?> RecognizeAsync(string imageBase64)
    {
        try
        {
            var payload = JsonSerializer.Serialize(new { image = imageBase64 });
            var resp = await _http.PostAsync(
                $"{_baseUrl}/recognize",
                new StringContent(payload, Encoding.UTF8, "application/json"));

            if (!resp.IsSuccessStatusCode) return null;

            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<FaceRecognitionResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }
}

public class FaceRecognitionResponse
{
    public List<FaceMatch> Matches { get; set; } = new();
}

public class FaceMatch
{
    public string StudentKey { get; set; } = string.Empty;
    public float  Confidence { get; set; }
}
