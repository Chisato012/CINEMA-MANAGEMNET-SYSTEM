namespace Cinema_Management.Services.Email;

public sealed class SmtpEmailOptions
{
    public string? Host { get; set; }

    public int Port { get; set; } = 587;

    public bool EnableSsl { get; set; } = true;

    public string? UserName { get; set; }

    public string? Password { get; set; }

    public string? FromEmail { get; set; }

    public string? FromName { get; set; }
}
