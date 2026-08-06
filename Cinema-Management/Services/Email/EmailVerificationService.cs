using System.Net;
using System.Security.Cryptography;
using System.Text;
using Cinema_Management.Models;
using Microsoft.AspNetCore.WebUtilities;

namespace Cinema_Management.Services.Email;

public sealed class EmailVerificationService
{
    public static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(24);
    public static readonly TimeSpan ResendCooldown = TimeSpan.FromMinutes(2);

    private const int TokenByteLength = 32;

    private readonly IEmailSender _emailSender;

    public EmailVerificationService(IEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    public string GenerateAndApplyToken(User user)
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(TokenByteLength);
        var token = WebEncoders.Base64UrlEncode(tokenBytes);
        var now = DateTime.UtcNow;

        user.EmailVerificationTokenHash = HashToken(token);
        user.EmailVerificationTokenExpiresAt = now.Add(TokenLifetime);
        user.EmailVerificationLastSentAt = now;

        return token;
    }

    public bool CanSendVerificationEmail(User user)
    {
        return user.EmailVerificationLastSentAt == null
               || DateTime.UtcNow - user.EmailVerificationLastSentAt.Value >= ResendCooldown;
    }

    public bool IsTokenValid(User user, string token)
    {
        if (string.IsNullOrWhiteSpace(token)
            || string.IsNullOrWhiteSpace(user.EmailVerificationTokenHash)
            || user.EmailVerificationTokenExpiresAt == null
            || user.EmailVerificationTokenExpiresAt < DateTime.UtcNow)
        {
            return false;
        }

        return string.Equals(
            user.EmailVerificationTokenHash,
            HashToken(token),
            StringComparison.Ordinal);
    }

    public Task SendVerificationEmailAsync(
        User user,
        string verificationLink,
        CancellationToken cancellationToken = default)
    {
        var fullName = WebUtility.HtmlEncode(user.FullName);
        var safeLink = WebUtility.HtmlEncode(verificationLink);

        var body = $"""
            <p>Xin chào {fullName},</p>
            <p>Cảm ơn bạn đã đăng ký sử dụng COSMOS Cinema. Vui lòng bấm và liên kết này để xác minh:</p>
            <p><a href="{safeLink}">Xác minh email</a></p>
            <p>Liên kết này sẽ có hiệu lực trong 24 giờ.</p>
            <p>Nếu bạn không tạo tài khoản này, vui lòng bỏ qua email này.</p>
            """;

        return _emailSender.SendEmailAsync(
            user.Email,
            "Xác minh email đăng ký COSMOS Cinema",
            body,
            cancellationToken);
    }

    public void MarkEmailConfirmed(User user)
    {
        user.EmailConfirmed = true;
        user.EmailVerificationTokenHash = null;
        user.EmailVerificationTokenExpiresAt = null;
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
