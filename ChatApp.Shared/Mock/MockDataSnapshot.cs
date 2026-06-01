using ChatApp.Shared.Enums;
using ChatApp.Shared.Models;

namespace ChatApp.Shared.Mock;

public class MockDataSnapshot
{
    public List<UserDto> Users { get; set; } = [];
    public List<FriendRequestDto> FriendRequests { get; set; } = [];
    public List<GroupDto> Groups { get; set; } = [];
    public List<MessageDto> Messages { get; set; } = [];
    public List<FriendshipEntry> Friendships { get; set; } = [];
}

public class FriendshipEntry
{
    public string UserId { get; set; } = string.Empty;
    public string FriendId { get; set; } = string.Empty;
}
