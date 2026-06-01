namespace ChatApp.Shared.Models;

public class RegisterRequestDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string? Remark { get; set; }
}
