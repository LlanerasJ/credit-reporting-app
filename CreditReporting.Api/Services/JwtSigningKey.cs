using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace CreditReporting.Api.Services;

/// <summary>
/// Resolves the JWT signing key from configuration. The key is deliberately not
/// in appsettings.json: supply it with user-secrets for local development, or with
/// the <see cref="EnvironmentVariable"/> environment variable elsewhere.
/// </summary>
public static class JwtSigningKey
{
    public const string EnvironmentVariable = "CREDITREPORTING_JWT_KEY";

    /// <summary>HMAC-SHA256 needs a key of at least 256 bits.</summary>
    private const int MinimumBytes = 32;

    /// <summary>Throws <see cref="InvalidOperationException"/> when no usable key is configured.</summary>
    public static SymmetricSecurityKey Resolve(IConfiguration config)
    {
        // Jwt:Key covers user-secrets and the Jwt__Key environment variable;
        // CREDITREPORTING_JWT_KEY is the plainer name for CI and other machines.
        var key = config["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(key))
            key = Environment.GetEnvironmentVariable(EnvironmentVariable);

        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException(
                "No JWT signing key configured. For local development run " +
                "`dotnet user-secrets set \"Jwt:Key\" \"<random value>\"` in CreditReporting.Api, " +
                $"or set the {EnvironmentVariable} environment variable. " +
                $"The key must be at least {MinimumBytes} characters.");

        if (Encoding.UTF8.GetByteCount(key) < MinimumBytes)
            throw new InvalidOperationException(
                $"The JWT signing key is too short. HMAC-SHA256 needs at least {MinimumBytes} bytes; " +
                $"the configured key is {Encoding.UTF8.GetByteCount(key)}.");

        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
    }
}
