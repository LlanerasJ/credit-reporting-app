using System.Text;
using CreditReporting.Api.Services;
using Microsoft.Extensions.Configuration;

namespace CreditReporting.Tests;

/// <summary>
/// Resolve() falls back to a process environment variable, so each test clears it
/// first and restores the original value afterwards.
/// </summary>
public class JwtSigningKeyTests : IDisposable
{
    private const string ValidKey = "a-test-signing-key-that-is-long-enough";

    private readonly string? _originalEnvValue;

    public JwtSigningKeyTests()
    {
        _originalEnvValue = Environment.GetEnvironmentVariable(JwtSigningKey.EnvironmentVariable);
        SetEnv(null);
    }

    public void Dispose() => SetEnv(_originalEnvValue);

    private static void SetEnv(string? value) =>
        Environment.SetEnvironmentVariable(JwtSigningKey.EnvironmentVariable, value);

    private static IConfiguration Config(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();

    [Fact]
    public void Resolve_reads_the_configured_key()
    {
        var key = JwtSigningKey.Resolve(Config(("Jwt:Key", ValidKey)));
        Assert.Equal(Encoding.UTF8.GetBytes(ValidKey), key.Key);
    }

    [Fact]
    public void Resolve_falls_back_to_the_environment_variable()
    {
        SetEnv(ValidKey);
        var key = JwtSigningKey.Resolve(Config());
        Assert.Equal(Encoding.UTF8.GetBytes(ValidKey), key.Key);
    }

    [Fact]
    public void Configuration_wins_over_the_environment_variable()
    {
        SetEnv("an-environment-key-that-is-long-enough");
        var key = JwtSigningKey.Resolve(Config(("Jwt:Key", ValidKey)));
        Assert.Equal(Encoding.UTF8.GetBytes(ValidKey), key.Key);
    }

    [Fact]
    public void Resolve_throws_when_no_key_is_configured()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => JwtSigningKey.Resolve(Config()));
        Assert.Contains("user-secrets", ex.Message);
        Assert.Contains(JwtSigningKey.EnvironmentVariable, ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_treats_a_blank_key_as_missing(string configured)
    {
        Assert.Throws<InvalidOperationException>(() => JwtSigningKey.Resolve(Config(("Jwt:Key", configured))));
    }

    [Fact]
    public void Resolve_rejects_a_key_too_short_for_HmacSha256()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => JwtSigningKey.Resolve(Config(("Jwt:Key", "short"))));
        Assert.Contains("too short", ex.Message);
    }
}
