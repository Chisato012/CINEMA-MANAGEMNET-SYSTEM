using Cinema_Management.Controllers;
using Cinema_Management.Data;
using Cinema_Management.Models;
using Cinema_Management.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CinemaManagement.Tests;

public class AccountEndpointTests
{
    [Fact]
    public void ForgotPasswordGetReturnsView()
    {
        using var context = CreateDbContext();
        var controller = CreateController(context);

        var result = controller.ForgotPassword();

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<ForgotPasswordViewModel>(viewResult.Model);
    }

    [Fact]
    public void GoogleRegisterGetWithoutSessionRedirectsToLogin()
    {
        using var context = CreateDbContext();
        var controller = CreateController(context);

        var result = controller.GoogleRegister();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Login", redirect.ActionName);
        Assert.True(controller.TempData.ContainsKey("AlertError"));
    }

    [Fact]
    public void GoogleRegisterGetWithSessionReturnsView()
    {
        using var context = CreateDbContext();
        var session = new TestSession();
        session.SetString("Google_Id", "google-123");
        session.SetString("Google_Email", "  USER@Example.COM ");
        session.SetString("Google_FullName", "Google User");
        var controller = CreateController(context, session);

        var result = controller.GoogleRegister();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<AuthViewModel>(viewResult.Model);
        Assert.Equal("user@example.com", model.Email);
        Assert.Equal("Google User", model.FullName);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=CinemaManagementEndpointTests;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new ApplicationDbContext(options);
    }

    private static AccountController CreateController(
        ApplicationDbContext context,
        ISession? session = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Application:PublicBaseUrl"] = "https://cosmoscinema.id.vn"
            })
            .Build();

        var httpContext = new DefaultHttpContext
        {
            Session = session ?? new TestSession()
        };

        var controller = new AccountController(
            configuration,
            new TestWebHostEnvironment(),
            new TestHttpClientFactory(),
            context,
            new TestEmailService(),
            NullLogger<AccountController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            },
            TempData = new TempDataDictionary(httpContext, new TestTempDataProvider())
        };

        return controller;
    }

    private sealed class TestEmailService : IEmailService
    {
        public Task<bool> SendEmailAsync(
            string toEmail,
            string subject,
            string htmlBody,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }

    private sealed class TestHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            return new HttpClient();
        }
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context)
        {
            return new Dictionary<string, object>();
        }

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "CinemaManagement.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public string EnvironmentName { get; set; } = "Development";
        public string WebRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _values = new();

        public bool IsAvailable => true;
        public string Id { get; } = Guid.NewGuid().ToString("N");
        public IEnumerable<string> Keys => _values.Keys;

        public void Clear()
        {
            _values.Clear();
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task LoadAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public void Remove(string key)
        {
            _values.Remove(key);
        }

        public void Set(string key, byte[] value)
        {
            _values[key] = value;
        }

        public bool TryGetValue(string key, out byte[] value)
        {
            return _values.TryGetValue(key, out value!);
        }
    }
}
