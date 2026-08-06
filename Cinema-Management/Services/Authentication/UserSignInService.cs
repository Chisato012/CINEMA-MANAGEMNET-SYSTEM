using System.Security.Claims;
using Cinema_Management.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Cinema_Management.Services.Authentication;

public sealed class UserSignInService
{
    public async Task SignInAsync(
        HttpContext httpContext,
        User user,
        bool rememberMe = false)
    {
        httpContext.Session.SetString("UserEmail", user.Email);
        httpContext.Session.SetString("UserFullName", user.FullName);
        httpContext.Session.SetString("UserRole", user.Role);
        httpContext.Session.SetInt32("UserID", user.UserID);
        httpContext.Session.SetString("RememberMe", rememberMe.ToString());

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserID.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var properties = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = rememberMe
                ? DateTimeOffset.UtcNow.AddDays(30)
                : null
        };

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            properties);
    }
}
