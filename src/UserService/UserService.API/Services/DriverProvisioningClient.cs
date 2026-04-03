using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using UserService.API.DTOs;

namespace UserService.API.Services;

public class DriverProvisioningClient : IDriverProvisioningClient
{
    private readonly HttpClient _httpClient;

    public DriverProvisioningClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<DriverProfileResponse> CreateDriverProfileAsync(
        CreateDriverAccountRequest request,
        int userId,
        string authorizationHeader,
        CancellationToken cancellationToken = default)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "/api/drivers")
        {
            Content = JsonContent.Create(new
            {
                userId = userId.ToString(),
                fullName = request.FullName,
                licenseNumber = request.LicenseNumber,
                licenseExpiry = request.LicenseExpiry,
                phoneNumber = request.PhoneNumber,
                maxWorkingHoursPerDay = request.MaxWorkingHoursPerDay,
                currentWorkingHoursToday = 0,
                isActive = true
            })
        };

        message.Headers.TryAddWithoutValidation("Authorization", authorizationHeader);

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ReadErrorMessageAsync(response, cancellationToken);
            throw new InvalidOperationException(error);
        }

        var envelope = await response.Content.ReadFromJsonAsync<DriverEnvelope>(cancellationToken: cancellationToken);
        if (envelope?.Data is null)
        {
            throw new InvalidOperationException("DriverService returned an empty response while creating the driver profile.");
        }

        return envelope.Data;
    }

    private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var fallback = response.StatusCode == HttpStatusCode.Conflict
            ? "Driver profile already exists."
            : "Failed to create driver profile.";

        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var payload = await JsonSerializer.DeserializeAsync<ErrorEnvelope>(stream, cancellationToken: cancellationToken);
            if (!string.IsNullOrWhiteSpace(payload?.Message))
            {
                return payload.Message!;
            }

            if (payload?.Errors is { Length: > 0 })
            {
                return payload.Errors[0];
            }
        }
        catch
        {
            // Ignore malformed downstream error payloads and use the fallback message.
        }

        return fallback;
    }

    private sealed class DriverEnvelope
    {
        public DriverProfileResponse? Data { get; set; }
    }

    private sealed class ErrorEnvelope
    {
        public string? Message { get; set; }
        public string[]? Errors { get; set; }
    }
}
