namespace ChatApp.Shared.Models;

public class GroupDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AvatarSeed { get; set; } = string.Empty;
    public List<string> MemberIds { get; set; } = [];
    public string CreatorId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
