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

public class GroupMemberDto
{
    public string UserId { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string AvatarSeed { get; set; } = string.Empty;
    public bool IsCreator { get; set; }
}
