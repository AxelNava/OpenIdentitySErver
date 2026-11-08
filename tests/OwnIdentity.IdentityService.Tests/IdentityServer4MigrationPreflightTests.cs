using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OwnIdentity.IdentityService.Controllers.Admin;
using OwnIdentity.IdentityService.Security;

namespace OwnIdentity.IdentityService.Tests;

public sealed class IdentityServer4MigrationPreflightTests
{
    [Fact]
    public async Task IdentityServer4MigrationPreflight_complete_sample_returns_exact_redacted_summary()
    {
        var controller = CreateController(Encoding.UTF8.GetBytes(LegacyInventoryValidatorTests.CompleteManifest));

        var action = await controller.Preflight(CancellationToken.None);
        var response = Assert.IsType<IdentityServer4MigrationPreflightController.PreflightResponse>(action.Value);

        Assert.True(response.IsValid);
        Assert.True(response.CutoverBlocked);
        Assert.Equal(
            new IdentityServer4MigrationPreflightController.PreflightSummary(1, 1, 1, 1, 2, 1, 1, 1),
            response.Summary);
        Assert.Empty(response.Diagnostics);
        Assert.False(response.DiagnosticsTruncated);
        Assert.Equal("no-store", controller.Response.Headers.CacheControl);
    }

    [Fact]
    public async Task IdentityServer4MigrationPreflight_ropc_missing_remediation_returns_safe_blockers()
    {
        const string manifest = """
            {
              "schemaVersion": 1,
              "legacyTables": [],
              "customExtensions": [],
              "credentials": [],
              "keys": [],
              "clients": [{
                "clientId": "must-not-be-returned",
                "migrationClassification": "ropc-blocker",
                "grantTypes": ["password"],
                "migrationTarget": "ropc"
              }],
              "customGrants": [],
              "adrs": []
            }
            """;
        var controller = CreateController(Encoding.UTF8.GetBytes(manifest));

        var action = await controller.Preflight(CancellationToken.None);
        var response = Assert.IsType<IdentityServer4MigrationPreflightController.PreflightResponse>(action.Value);
        var serialized = JsonSerializer.Serialize(response);

        Assert.False(response.IsValid);
        Assert.True(response.CutoverBlocked);
        Assert.Collection(
            response.Diagnostics,
            diagnostic => Assert.Equal("LIV122 $.clients[0].owner", $"{diagnostic.Code} {diagnostic.Path}"),
            diagnostic => Assert.Equal("LIV123 $.clients[0].migrationTarget", $"{diagnostic.Code} {diagnostic.Path}"));
        Assert.DoesNotContain("must-not-be-returned", serialized, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(InvalidJsonBodies))]
    public async Task IdentityServer4MigrationPreflight_invalid_json_or_utf8_returns_only_generic_json_error(byte[] body)
    {
        var controller = CreateController(body);

        var action = await controller.Preflight(CancellationToken.None);
        var response = Assert.IsType<IdentityServer4MigrationPreflightController.PreflightResponse>(action.Value);

        Assert.False(response.IsValid);
        Assert.False(response.CutoverBlocked);
        Assert.Equal(
            new IdentityServer4MigrationPreflightController.PreflightSummary(0, 0, 0, 0, 0, 0, 0, 0),
            response.Summary);
        var diagnostic = Assert.Single(response.Diagnostics);
        Assert.Equal("JSON001", diagnostic.Code);
        Assert.Equal("$", diagnostic.Path);
        Assert.Equal("Manifest is not valid UTF-8 JSON.", diagnostic.Message);
    }

    [Fact]
    public async Task IdentityServer4MigrationPreflight_never_serializes_secret_values_or_arbitrary_paths()
    {
        var sentinel = $"secret-sentinel-{Guid.NewGuid():N}";
        var manifest = LegacyInventoryValidatorTests.CompleteManifest.Replace(
            "\"kind\": \"shared-secret-hash-metadata\"",
            $"\"kind\": \"shared-secret-hash-metadata\", \"clientSecret{sentinel}\": \"{sentinel}\"",
            StringComparison.Ordinal);
        var controller = CreateController(Encoding.UTF8.GetBytes(manifest));

        var action = await controller.Preflight(CancellationToken.None);
        var response = Assert.IsType<IdentityServer4MigrationPreflightController.PreflightResponse>(action.Value);
        var serialized = JsonSerializer.Serialize(response);

        Assert.Contains(response.Diagnostics, diagnostic => diagnostic.Code == "LIV001" && diagnostic.Path == "$");
        Assert.DoesNotContain(sentinel, serialized, StringComparison.Ordinal);
    }

    [Fact]
    public async Task IdentityServer4MigrationPreflight_caps_diagnostics_at_one_hundred()
    {
        var entries = string.Join(',', Enumerable.Repeat("null", 101));
        var manifest = $$"""
            {
              "schemaVersion": 1,
              "legacyTables": [{{entries}}],
              "customExtensions": [],
              "credentials": [],
              "keys": [],
              "clients": [],
              "customGrants": [],
              "adrs": []
            }
            """;
        var controller = CreateController(Encoding.UTF8.GetBytes(manifest));

        var action = await controller.Preflight(CancellationToken.None);
        var response = Assert.IsType<IdentityServer4MigrationPreflightController.PreflightResponse>(action.Value);

        Assert.Equal(100, response.Diagnostics.Count);
        Assert.True(response.DiagnosticsTruncated);
    }

    [Fact]
    public async Task IdentityServer4MigrationPreflight_rejects_streamed_oversize_body_without_echo()
    {
        var body = new byte[IdentityServer4MigrationPreflightController.MaximumBodyBytes + 1];
        Encoding.UTF8.GetBytes("oversize-secret-sentinel").CopyTo(body, body.Length - 24);
        var controller = CreateController(body, contentLength: null);

        var action = await controller.Preflight(CancellationToken.None);
        var result = Assert.IsType<ObjectResult>(action.Result);
        var serialized = JsonSerializer.Serialize(result.Value);

        Assert.Equal(StatusCodes.Status413PayloadTooLarge, result.StatusCode);
        Assert.DoesNotContain("oversize-secret-sentinel", serialized, StringComparison.Ordinal);
        Assert.Equal("no-store", controller.Response.Headers.CacheControl);
    }

    [Fact]
    public async Task IdentityServer4MigrationPreflight_rejects_wrong_media_type()
    {
        var controller = CreateController(Encoding.UTF8.GetBytes("{}"), "text/plain");

        var action = await controller.Preflight(CancellationToken.None);
        var result = Assert.IsType<ObjectResult>(action.Result);

        Assert.Equal(StatusCodes.Status415UnsupportedMediaType, result.StatusCode);
        Assert.Equal("no-store", controller.Response.Headers.CacheControl);
    }

    [Fact]
    public void IdentityServer4MigrationPreflight_requires_admin_policy_and_json_post_contract()
    {
        var controllerType = typeof(IdentityServer4MigrationPreflightController);
        var authorize = Assert.Single(controllerType.GetCustomAttributes<AuthorizeAttribute>());
        var route = Assert.Single(controllerType.GetCustomAttributes<RouteAttribute>());
        var method = Assert.IsAssignableFrom<MethodInfo>(controllerType.GetMethod(nameof(IdentityServer4MigrationPreflightController.Preflight)));
        var consumes = Assert.Single(method.GetCustomAttributes<ConsumesAttribute>());

        Assert.Equal(IdentityConfiguration.AdminPolicy, authorize.Policy);
        Assert.Equal("api/admin/migrations/identityserver4/preflight", route.Template);
        Assert.Single(method.GetCustomAttributes<HttpPostAttribute>());
        Assert.Equal(["application/json"], consumes.ContentTypes);
    }

    [Fact]
    public async Task IdentityServer4MigrationPreflight_independent_requests_do_not_reuse_prior_result()
    {
        var validController = CreateController(Encoding.UTF8.GetBytes(LegacyInventoryValidatorTests.CompleteManifest));
        var validAction = await validController.Preflight(CancellationToken.None);
        var validResponse = Assert.IsType<IdentityServer4MigrationPreflightController.PreflightResponse>(validAction.Value);

        var malformedController = CreateController(Encoding.UTF8.GetBytes("{"));
        var malformedAction = await malformedController.Preflight(CancellationToken.None);
        var malformedResponse = Assert.IsType<IdentityServer4MigrationPreflightController.PreflightResponse>(malformedAction.Value);

        Assert.True(validResponse.IsValid);
        Assert.False(malformedResponse.IsValid);
        Assert.False(malformedResponse.CutoverBlocked);
        Assert.Equal(
            new IdentityServer4MigrationPreflightController.PreflightSummary(0, 0, 0, 0, 0, 0, 0, 0),
            malformedResponse.Summary);
        var diagnostic = Assert.Single(malformedResponse.Diagnostics);
        Assert.Equal("JSON001", diagnostic.Code);
        Assert.Equal("$", diagnostic.Path);
    }

    public static TheoryData<byte[]> InvalidJsonBodies => new()
    {
        Encoding.UTF8.GetBytes("{"),
        new byte[] { 0xc3, 0x28 }
    };

    private static IdentityServer4MigrationPreflightController CreateController(
        byte[] body,
        string contentType = "application/json",
        long? contentLength = 0)
    {
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(body);
        context.Request.ContentType = contentType;
        context.Request.ContentLength = contentLength == 0 ? body.Length : contentLength;

        return new IdentityServer4MigrationPreflightController
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
    }
}
