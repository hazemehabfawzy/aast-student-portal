using System.Text;
using System.Text.Json;

namespace StudentPortal.Api.Services.Implementations;

public class KeycloakAdminService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public KeycloakAdminService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    private string BaseUrl =>
        _config["Keycloak:AdminApi:BaseUrl"]
        ?? _config["Keycloak:Authority"]!.Replace("/realms/student-portal", "");

    private string TokenUrl =>
        _config["Keycloak:AdminApi:TokenUrl"]
        ?? $"{BaseUrl}/realms/master/protocol/openid-connect/token";

    private async Task<string> GetAdminToken()
    {
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"]  = "admin-cli",
            ["username"]   = "admin",
            ["password"]   = "admin",
        });

        var resp = await _http.PostAsync(TokenUrl, form);
        resp.EnsureSuccessStatusCode();
        var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("access_token").GetString()!;
    }

    public async Task<string> CreateUser(
        string username, string password,
        string firstName, string lastName,
        string email, string role)
    {
        const string realm = "student-portal";
        var token = await GetAdminToken();

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Create user
        var userPayload = JsonSerializer.Serialize(new
        {
            username,
            email,
            firstName,
            lastName,
            enabled     = true,
            credentials = new[] { new { type = "password", value = password, temporary = false } }
        });

        var createResp = await client.PostAsync(
            $"{BaseUrl}/admin/realms/{realm}/users",
            new StringContent(userPayload, Encoding.UTF8, "application/json"));
        createResp.EnsureSuccessStatusCode();

        var userId = createResp.Headers.Location!.ToString().Split('/').Last();

        // Get realm roles
        var rolesResp = await client.GetAsync($"{BaseUrl}/admin/realms/{realm}/roles");
        rolesResp.EnsureSuccessStatusCode();
        var rolesJson  = JsonDocument.Parse(await rolesResp.Content.ReadAsStringAsync());
        var roleObj    = rolesJson.RootElement.EnumerateArray()
            .First(r => r.GetProperty("name").GetString() == role);

        // Assign role
        var rolePayload = JsonSerializer.Serialize(new[]
        {
            new
            {
                id   = roleObj.GetProperty("id").GetString(),
                name = roleObj.GetProperty("name").GetString()
            }
        });

        await client.PostAsync(
            $"{BaseUrl}/admin/realms/{realm}/users/{userId}/role-mappings/realm",
            new StringContent(rolePayload, Encoding.UTF8, "application/json"));

        return userId;
    }
}
