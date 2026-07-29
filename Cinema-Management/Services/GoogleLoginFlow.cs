using System.Security.Claims;
using Cinema_Management.Models;

namespace Cinema_Management.Services;

public enum GoogleLoginDecisionType
{
    InvalidGoogleProfile,
    UnverifiedGoogleEmail,
    ExistingGoogleAccount,
    ExistingUnconfirmedGoogleAccount,
    NewGoogleRegistrationRequired,
    ExistingLocalAccount,
    DifferentExternalProvider,
    InactiveAccount
}

public sealed record GoogleLoginDecision(GoogleLoginDecisionType Type, User? User = null);

public static class GoogleLoginFlow
{
    public const string EmailVerifiedClaimType = "urn:google:email_verified";
    public const string GoogleProvider = "Google";
    public const string ExistingLocalAccountMessage =
        "Email này đã có tài khoản. Vui lòng đăng nhập bằng email và mật khẩu.";
    public const string DifferentExternalProviderMessage =
        "Email này đã được liên kết với phương thức đăng nhập khác.";

    public static bool IsGoogleEmailVerified(ClaimsPrincipal principal)
    {
        return principal
            .FindAll(EmailVerifiedClaimType)
            .Any(claim => bool.TryParse(claim.Value, out var verified) && verified);
    }

    public static GoogleLoginDecision Decide(
        string? googleId,
        string normalizedEmail,
        bool emailVerified,
        User? googleUser,
        User? existingEmailUser)
    {
        if (string.IsNullOrWhiteSpace(googleId) || string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return new GoogleLoginDecision(GoogleLoginDecisionType.InvalidGoogleProfile);
        }

        if (!emailVerified)
        {
            return new GoogleLoginDecision(GoogleLoginDecisionType.UnverifiedGoogleEmail);
        }

        if (googleUser != null)
        {
            if (!googleUser.Status)
            {
                return new GoogleLoginDecision(GoogleLoginDecisionType.InactiveAccount, googleUser);
            }

            return googleUser.EmailConfirmed
                ? new GoogleLoginDecision(GoogleLoginDecisionType.ExistingGoogleAccount, googleUser)
                : new GoogleLoginDecision(GoogleLoginDecisionType.ExistingUnconfirmedGoogleAccount, googleUser);
        }

        if (existingEmailUser != null)
        {
            if (IsLocalAccount(existingEmailUser))
            {
                return new GoogleLoginDecision(GoogleLoginDecisionType.ExistingLocalAccount, existingEmailUser);
            }

            return new GoogleLoginDecision(GoogleLoginDecisionType.DifferentExternalProvider, existingEmailUser);
        }

        return new GoogleLoginDecision(GoogleLoginDecisionType.NewGoogleRegistrationRequired);
    }

    public static bool IsLocalAccount(User user)
    {
        return !string.IsNullOrWhiteSpace(user.PasswordHash)
               || string.IsNullOrWhiteSpace(user.ExternalProvider);
    }
}
