using Planner.Domain.Users;

namespace Planner.Domain.Tests.Users;

public class UserTests {
    private static readonly Email SomeEmail = new("gabriel@example.com");

    [Fact]
    public void Create_trims_name() {
        var user = User.Create("  Gabriel  ", SomeEmail);

        Assert.Equal("Gabriel", user.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_empty_name(string name) {
        var error = Assert.Throws<DomainException>(() => User.Create(name, SomeEmail));

        Assert.Equal("user.invalid-name", error.Code);
    }
}
