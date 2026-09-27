using Planner.Domain.Users;

namespace Planner.Domain.Tests.Users;

public class EmailTests {
    [Fact]
    public void Normalizes_spaces_and_letter_case() {
        var email = new Email("  Gabriel.Abreu@Example.COM ");

        Assert.Equal("gabriel.abreu@example.com", email.Value);
    }

    [Fact]
    public void Emails_differing_only_in_case_are_equal() {
        Assert.Equal(new Email("g@example.com"), new Email("G@EXAMPLE.com"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("gabriel")]
    [InlineData("@example.com")]
    [InlineData("gabriel@")]
    [InlineData("gabriel@example")]
    [InlineData("gabriel@example.")]
    [InlineData("gabriel@@example.com")]
    [InlineData("gab riel@example.com")]
    public void Rejects_invalid_format(string value) {
        var error = Assert.Throws<DomainException>(() => new Email(value));

        Assert.Equal("user.invalid-email", error.Code);
    }

    [Fact]
    public void Accepts_254_characters_and_rejects_255() {
        const string domain = "@example.com";
        var longest = new string('a', Email.MaxLength - domain.Length) + domain;

        Assert.Equal(254, new Email(longest).Value.Length);
        var error = Assert.Throws<DomainException>(() => new Email("a" + longest));
        Assert.Equal("user.invalid-email", error.Code);
    }
}
