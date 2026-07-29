using System.Security.Claims;
using Cinema_Management.Models;
using Cinema_Management.Services;
using Xunit;

namespace CinemaManagement.Tests;

public class GoogleLoginFlowTests
{
    [Fact]
    public void VerifiedEmailClaimTrueIsAccepted()
    {
        var principal = PrincipalWithVerifiedEmailClaim("true");

        Assert.True(GoogleLoginFlow.IsGoogleEmailVerified(principal));
    }

    [Fact]
    public void EmailVerifiedClaimTrueIsAcceptedAfterMappingToInternalClaim()
    {
        var principal = PrincipalWithVerifiedEmailClaim("True");

        Assert.True(GoogleLoginFlow.IsGoogleEmailVerified(principal));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("false")]
    [InlineData("not-boolean")]
    public void MissingOrFalseVerifiedEmailClaimIsRejected(string? claimValue)
    {
        var claims = claimValue == null
            ? []
            : new[] { new Claim(GoogleLoginFlow.EmailVerifiedClaimType, claimValue) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        Assert.False(GoogleLoginFlow.IsGoogleEmailVerified(principal));
    }

    [Fact]
    public void GoogleEmailMatchingLocalAccountIsRejectedWithoutMutatingUser()
    {
        var localUser = new User
        {
            UserID = 10,
            Email = "customer@example.com",
            FullName = "Customer",
            PasswordHash = "bcrypt-hash",
            Role = "KhachHang",
            Status = true,
            EmailConfirmed = true
        };

        var decision = GoogleLoginFlow.Decide(
            "google-123",
            "customer@example.com",
            true,
            googleUser: null,
            existingEmailUser: localUser);

        Assert.Equal(GoogleLoginDecisionType.ExistingLocalAccount, decision.Type);
        Assert.Null(localUser.ExternalProvider);
        Assert.Null(localUser.ExternalProviderKey);
    }

    [Fact]
    public void ExistingGoogleIdCanLoginWhenConfirmed()
    {
        var googleUser = new User
        {
            UserID = 11,
            Email = "google@example.com",
            FullName = "Google User",
            Role = "KhachHang",
            Status = true,
            EmailConfirmed = true,
            ExternalProvider = GoogleLoginFlow.GoogleProvider,
            ExternalProviderKey = "google-123"
        };

        var decision = GoogleLoginFlow.Decide(
            "google-123",
            "google@example.com",
            true,
            googleUser,
            existingEmailUser: null);

        Assert.Equal(GoogleLoginDecisionType.ExistingGoogleAccount, decision.Type);
        Assert.Same(googleUser, decision.User);
    }

    [Fact]
    public void NewGoogleEmailRequiresGoogleRegister()
    {
        var decision = GoogleLoginFlow.Decide(
            "google-123",
            "new@example.com",
            true,
            googleUser: null,
            existingEmailUser: null);

        Assert.Equal(GoogleLoginDecisionType.NewGoogleRegistrationRequired, decision.Type);
    }

    [Fact]
    public void UnconfirmedGoogleAccountIsNotLoggedIn()
    {
        var googleUser = new User
        {
            UserID = 12,
            Email = "pending@example.com",
            FullName = "Pending User",
            Role = "KhachHang",
            Status = true,
            EmailConfirmed = false,
            ExternalProvider = GoogleLoginFlow.GoogleProvider,
            ExternalProviderKey = "google-123"
        };

        var decision = GoogleLoginFlow.Decide(
            "google-123",
            "pending@example.com",
            true,
            googleUser,
            existingEmailUser: null);

        Assert.Equal(GoogleLoginDecisionType.ExistingUnconfirmedGoogleAccount, decision.Type);
    }

    private static ClaimsPrincipal PrincipalWithVerifiedEmailClaim(string claimValue)
    {
        return new ClaimsPrincipal(
            new ClaimsIdentity(
                new[] { new Claim(GoogleLoginFlow.EmailVerifiedClaimType, claimValue) }));
    }
}
