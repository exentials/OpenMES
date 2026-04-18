using OpenMES.WebAdmin.Services;

namespace OpenMES.Tests;

public class LoginRedirectTests
{
    private const string BaseUri = "https://localhost:5001/";

    [Fact]
    public void SuccessfulLogin_TargetIsNeverLoginRoute()
    {
        var target = LoginRedirectResolver.ResolveTarget(null, BaseUri);

        Assert.False(string.Equals("/login", target, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidSameOriginReturnUrl_IsPreservedAfterLogin()
    {
        var target = LoginRedirectResolver.ResolveTarget(
            Uri.EscapeDataString("https://localhost:5001/users?tab=2"),
            BaseUri);

        Assert.Equal("/users?tab=2", target);
    }

    [Theory]
    [InlineData("https%3A%2F%2Fevil.example%2Fsteal")]
    [InlineData("")]
    [InlineData(null)]
    public void ExternalOrEmptyReturnUrl_FallsBackToRoot(string? returnUrl)
    {
        var target = LoginRedirectResolver.ResolveTarget(returnUrl, BaseUri);

        Assert.Equal("/", target);
    }
}
