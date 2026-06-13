using ChatApp.Api.Data;
using ChatApp.Api.Data.Entities;
using ChatApp.Shared.Enums;
using ChatApp.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Api.Services;

public interface IGroupBusinessService
{
    Task<List<GroupDto>> GetGroupsAsync(long userId);
    Task<(bool Success, string? Error, GroupDto? Group)> CreateGroupAsync(long userId, string name, List<long> memberIds);
    Task<List<GroupMemberDto>> GetMembersAsync(long userId, long groupId);
}

public class GroupBusinessService(AppDbContext db) : IGroupBusinessService
{
    public async Task<List<GroupDto>> GetGroupsAsync(long userId)
    {
        return await db.GroupMembers
            .Where(m => m.UserId == userId)
            .Include(m => m.Group).ThenInclude(g => g.Members)
            .Select(m => new GroupDto
            {
                Id = m.GroupId.ToString(),
                Name = m.Group.Name,
                AvatarSeed = m.Group.AvatarSeed,
                CreatorId = m.Group.CreatorId.ToString(),
                MemberIds = m.Group.Members.Select(x => x.UserId.ToString()).ToList(),
                CreatedAt = m.Group.CreatedAt
            }).ToListAsync();
    }

    public async Task<(bool Success, string? Error, GroupDto? Group)> CreateGroupAsync(long userId, string name, List<long> memberIds)
    {
        if (string.IsNullOrWhiteSpace(name)) return (false, "群名称不能为空", null);

        var group = new ChatGroup
        {
            Name = name.Trim(),
            AvatarSeed = name.Trim(),
            CreatorId = userId
        };
        db.Groups.Add(group);
        await db.SaveChangesAsync();

        var allMembers = memberIds.Append(userId).Distinct().ToList();
        foreach (var mid in allMembers)
            db.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = mid });

        db.Conversations.Add(new Conversation { Type = SessionType.Group, GroupId = group.Id, LastMessageAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        return (true, null, new GroupDto
        {
            Id = group.Id.ToString(),
            Name = group.Name,
            AvatarSeed = group.AvatarSeed,
            CreatorId = userId.ToString(),
            MemberIds = allMembers.Select(x => x.ToString()).ToList(),
            CreatedAt = group.CreatedAt
        });
    }

    public async Task<List<GroupMemberDto>> GetMembersAsync(long userId, long groupId)
    {
        var isMember = await db.GroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == userId);
        if (!isMember) return [];

        var group = await db.Groups
            .Include(g => g.Members).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(g => g.Id == groupId);
        if (group is null) return [];

        return group.Members.Select(m => new GroupMemberDto
        {
            UserId = m.UserId.ToString(),
            Nickname = m.User.Nickname,
            AvatarSeed = m.User.AvatarSeed,
            IsCreator = m.UserId == group.CreatorId
        }).ToList();
    }
}
