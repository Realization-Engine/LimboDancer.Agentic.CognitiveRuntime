using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace LimboDancer.Host;

internal sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "LimboDancerApiKey";
    public const string HeaderName = "X-LimboDancer-Key";

    private readonly IOptionsMonitor<LimboDancerHostOptions> hostOptions;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> schemeOptions,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptionsMonitor<LimboDancerHostOptions> hostOptions)
        : base(schemeOptions, logger, encoder)
    {
        this.hostOptions = hostOptions ?? throw new ArgumentNullException(nameof(hostOptions));
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var values)
            || values.Count != 1
            || string.IsNullOrWhiteSpace(values[0]))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var suppliedKey = values[0]!;
        var credential = hostOptions.CurrentValue.ApiKeys.FirstOrDefault(candidate =>
            FixedTimeEquals(candidate.Key, suppliedKey));
        if (credential is null)
        {
            return Task.FromResult(AuthenticateResult.Fail("The API key is invalid."));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, credential.PrincipalId),
            new(LimboDancerClaimTypes.TenantId, credential.TenantId.ToString("D")),
        };
        claims.AddRange(credential.Permissions.Select(permission => new Claim(LimboDancerClaimTypes.Permission, permission)));
        var identity = new ClaimsIdentity(claims, SchemeName, ClaimTypes.NameIdentifier, ClaimTypes.Role);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static bool FixedTimeEquals(string configuredKey, string suppliedKey)
    {
        var configuredHash = SHA256.HashData(Encoding.UTF8.GetBytes(configuredKey));
        var suppliedHash = SHA256.HashData(Encoding.UTF8.GetBytes(suppliedKey));
        return CryptographicOperations.FixedTimeEquals(configuredHash, suppliedHash);
    }
}
