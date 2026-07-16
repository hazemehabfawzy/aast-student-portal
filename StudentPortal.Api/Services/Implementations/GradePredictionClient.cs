using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace StudentPortal.Api.Services.Implementations;

public class GradePredictionClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly ILogger<GradePredictionClient> _log;

    public GradePredictionClient(HttpClient http, IConfiguration config, ILogger<GradePredictionClient> log)
    {
        _http    = http;
        _baseUrl = config["PredictionService:BaseUrl"] ?? "http://prediction-service:8001";
        _log     = log;
    }

    public async Task<PredictionResult?> PredictAsync(float week7, float week12, float prefinal)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        try
        {
            var payload = JsonSerializer.Serialize(new
            {
                week7Score    = week7,
                week12Score   = week12,
                prefinalScore = prefinal
            });

            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/predict")
            {
                Content = content
            };
            // Force connection close to avoid Docker keep-alive stall on virtual networks
            request.Headers.ConnectionClose = true;

            var resp = await _http.SendAsync(request, cts.Token);

            _log.LogInformation("Prediction service responded with {Status}", resp.StatusCode);

            if (!resp.IsSuccessStatusCode) return null;

            var json = await resp.Content.ReadAsStringAsync();
            _log.LogInformation("Prediction raw JSON: {Json}", json);

            var result = JsonSerializer.Deserialize<PredictionResult>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            _log.LogInformation("Deserialized prediction: {Final}", result?.PredictedFinal);
            return result;
        }
        catch (Exception ex)
        {
            _log.LogWarning("Prediction failed: {Msg}", ex.Message);
            return null;
        }
    }
}

public class PredictionResult
{
    public float  PredictedFinal { get; set; }
    public float  PredictedTotal { get; set; }
    public ScoreRange FinalRange { get; set; } = new();
    public ScoreRange TotalRange { get; set; } = new();
    public bool   AtRisk         { get; set; }
    public string RiskLevel      { get; set; } = "LOW";
}

public class ScoreRange
{
    public float Low  { get; set; }
    public float High { get; set; }
}
