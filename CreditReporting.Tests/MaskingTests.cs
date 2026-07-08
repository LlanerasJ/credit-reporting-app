using CreditReporting.Api.Services;

namespace CreditReporting.Tests;

public class MaskingTests
{
    [Fact]
    public void MaskSsn_shows_only_last4() =>
        Assert.Equal("***-**-1234", Masking.MaskSsn("1234"));

    [Theory]
    [InlineData("4001123456789", "****6789")]
    [InlineData("123", "***")]
    public void MaskAccountNumber_hides_all_but_last4(string input, string expected) =>
        Assert.Equal(expected, Masking.MaskAccountNumber(input));

    [Fact]
    public void HashSsn_is_deterministic_and_not_reversible_text()
    {
        string hash = Masking.HashSsn("900001234");
        Assert.Equal(Masking.HashSsn("900001234"), hash);
        Assert.DoesNotContain("900001234", hash);
        Assert.Equal(64, hash.Length); // SHA-256 hex
    }

    [Fact]
    public void Password_hash_verifies_and_rejects()
    {
        string stored = Masking.HashPassword("Demo123!");
        Assert.True(Masking.VerifyPassword("Demo123!", stored));
        Assert.False(Masking.VerifyPassword("wrong", stored));
    }

    [Fact]
    public void Password_hashes_are_salted()
    {
        Assert.NotEqual(Masking.HashPassword("Demo123!"), Masking.HashPassword("Demo123!"));
    }
}
