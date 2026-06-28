using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using StudentPortal.Api.Services.Interfaces;

namespace StudentPortal.Api.Services.Implementations;

public class FaceRecognitionClient : IFaceRecognitionClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public FaceRecognitionClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _baseUrl = configuration["FaceService:BaseUrl"] ?? "http://localhost:8000";
    }

    public async Task<FaceRecognitionResponse> RecognizeAsync(string base64Image)
    {
        var response = await _httpClient.PostAsJsonAsync($"{_baseUrl.TrimEnd('/')}/recognize", new { image = base64Image });
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<FaceRecognitionResponse>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return result ?? new FaceRecognitionResponse();
    }
}
