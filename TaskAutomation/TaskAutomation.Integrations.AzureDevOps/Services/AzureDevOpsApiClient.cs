using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TaskAutomation.Integrations.AzureDevOps.Configuration;

namespace TaskAutomation.Integrations.AzureDevOps.Services;

public sealed class AzureDevOpsApiClient(HttpClient httpClient, IOptions<AzureDevOpsOptions> options)
{
    public async Task<JsonDocument> GetJsonAsync(string relativePath, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Get, relativePath);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    public async Task PatchAsync(string relativePath, object payload, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Patch, relativePath);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json-patch+json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string relativePath)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.OrganizationUrl) ||
            string.IsNullOrWhiteSpace(settings.Project) ||
            string.IsNullOrWhiteSpace(settings.PersonalAccessToken))
        {
            throw new InvalidOperationException("Azure DevOps configuration is incomplete. Set OrganizationUrl, Project, and PersonalAccessToken.");
        }

        var request = new HttpRequestMessage(method, $"{settings.OrganizationUrl.TrimEnd('/')}/{settings.Project}/_apis/{relativePath.TrimStart('/')}");
        var token = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{settings.PersonalAccessToken}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }
}
