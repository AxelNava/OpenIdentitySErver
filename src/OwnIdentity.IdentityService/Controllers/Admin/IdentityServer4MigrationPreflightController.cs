using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OwnIdentity.IdentityService.Security;
using OwnIdentity.LegacyInventory;

namespace OwnIdentity.IdentityService.Controllers.Admin;

[ApiController]
[Route("api/admin/migrations/identityserver4/preflight")]
[Authorize(Policy = IdentityConfiguration.AdminPolicy)]
public sealed class IdentityServer4MigrationPreflightController : ControllerBase
{
    public const int MaximumBodyBytes = 1_048_576;
    public const int MaximumDiagnostics = 100;

    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private static readonly Regex SafeDiagnosticPath = new(
        @"^\$(?:\.(?:schemaVersion|legacyTables|customExtensions|credentials|keys|clients|customGrants|adrs)(?:\[\d+\])?(?:\.(?:name|disposition|id|kind|purpose|clientId|migrationClassification|grantTypes|owner|migrationTarget|status)(?:\[\d+\])?)?)?$",
        RegexOptions.CultureInvariant);

    [HttpPost]
    [Consumes("application/json")]
    public async Task<ActionResult<PreflightResponse>> Preflight(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";

        if (!IsUtf8Json(Request.ContentType))
        {
            return StatusCode(
                StatusCodes.Status415UnsupportedMediaType,
                new ProblemDetails
                {
                    Status = StatusCodes.Status415UnsupportedMediaType,
                    Title = "Only an application/json UTF-8 body is accepted."
                });
        }

        if (Request.ContentLength > MaximumBodyBytes)
        {
            return PayloadTooLarge();
        }

        try
        {
            var body = await ReadBodyAsync(Request.Body, cancellationToken);
            string json;
            try
            {
                json = StrictUtf8.GetString(body);
            }
            catch (DecoderFallbackException)
            {
                return JsonFailure();
            }

            LegacyInventoryValidationResult result;
            try
            {
                result = LegacyInventoryValidator.Validate(json);
            }
            catch (JsonException)
            {
                return JsonFailure();
            }

            var diagnostics = result.Diagnostics
                .Take(MaximumDiagnostics)
                .Select(diagnostic => new PreflightDiagnostic(
                    diagnostic.Code,
                    SafeDiagnosticPath.IsMatch(diagnostic.Path) ? diagnostic.Path : "$",
                    SafeMessage(diagnostic.Code)))
                .ToArray();

            return new PreflightResponse(
                result.IsValid,
                result.Summary.RopcBlockerCount > 0,
                new PreflightSummary(
                    result.Summary.LegacyTableCount,
                    result.Summary.CustomExtensionCount,
                    result.Summary.CredentialCount,
                    result.Summary.KeyCount,
                    result.Summary.ClientCount,
                    result.Summary.CustomGrantCount,
                    result.Summary.AdrCount,
                    result.Summary.RopcBlockerCount),
                diagnostics,
                result.Diagnostics.Count > MaximumDiagnostics);
        }
        catch (BodyTooLargeException)
        {
            return PayloadTooLarge();
        }
        catch (Exception)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Preflight could not be completed."
                });
        }
    }

    private static bool IsUtf8Json(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        var parts = contentType.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length is 1 or 2
            && string.Equals(parts[0], "application/json", StringComparison.OrdinalIgnoreCase)
            && (parts.Length == 1 || string.Equals(parts[1], "charset=utf-8", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<byte[]> ReadBodyAsync(Stream body, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[64 * 1024];
        while (true)
        {
            var bytesRead = await body.ReadAsync(chunk, cancellationToken);
            if (bytesRead == 0)
            {
                return buffer.ToArray();
            }

            if (buffer.Length + bytesRead > MaximumBodyBytes)
            {
                throw new BodyTooLargeException();
            }

            await buffer.WriteAsync(chunk.AsMemory(0, bytesRead), cancellationToken);
        }
    }

    private ActionResult<PreflightResponse> PayloadTooLarge() => StatusCode(
        StatusCodes.Status413PayloadTooLarge,
        new ProblemDetails
        {
            Status = StatusCodes.Status413PayloadTooLarge,
            Title = "Request body exceeds the 1 MiB limit."
        });

    private static PreflightResponse JsonFailure() => new(
        false,
        false,
        new PreflightSummary(0, 0, 0, 0, 0, 0, 0, 0),
        [new PreflightDiagnostic("JSON001", "$", "Manifest is not valid UTF-8 JSON.")],
        false);

    private static string SafeMessage(string code) => code switch
    {
        "LIV001" => "Secret-bearing fields are prohibited; retain metadata and disposition only.",
        "LIV002" => "Manifest contains an unknown field or an invalid field type.",
        "LIV003" => "Duplicate JSON fields are not allowed.",
        "LIV100" => "Schema version must be 1.",
        "LIV101" => "Required inventory section is missing.",
        "LIV102" => "Inventory entry must be an object.",
        "LIV110" => "Required inventory metadata is missing.",
        "LIV120" => "Grant types must be present.",
        "LIV121" => "ROPC clients must be classified as non-executable blocker records.",
        "LIV122" => "ROPC blocker owner is required.",
        "LIV123" => "ROPC blocker requires a non-ROPC migration target.",
        "LIV130" => "Custom grants must have the quarantine disposition.",
        "LIV140" => "Unresolved ADR owner is required.",
        _ => "Manifest validation failed."
    };

    private sealed class BodyTooLargeException : Exception;

    public sealed record PreflightResponse(
        bool IsValid,
        bool CutoverBlocked,
        PreflightSummary Summary,
        IReadOnlyList<PreflightDiagnostic> Diagnostics,
        bool DiagnosticsTruncated);

    public sealed record PreflightSummary(
        int Tables,
        int Extensions,
        int Credentials,
        int Keys,
        int Clients,
        int CustomGrants,
        int Adrs,
        int RopcBlockers);

    public sealed record PreflightDiagnostic(string Code, string Path, string Message);
}
