namespace ChatApp.Shared.Models;

public class MessageQueryFilter
{
    public string? SenderUsername { get; set; }
    public string? Keyword { get; set; }
    public string? TimeRange { get; set; }
    public string? UserId { get; set; }
}
