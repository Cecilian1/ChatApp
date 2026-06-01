using ChatApp.Shared.Mock;
using ChatApp.Shared.Models;

namespace ChatApp.Web.Services;

public interface IGroupService
{
    (bool Success, string? Error, GroupDto? Group) CreateGroup(string userId, string name, List<string> memberIds);
    List<GroupDto> GetGroups(string userId);
}

public class MockGroupService : IGroupService
{
    public (bool Success, string? Error, GroupDto? Group) CreateGroup(string userId, string name, List<string> memberIds)
    {
        if (string.IsNullOrWhiteSpace(name))
            return (false, "群名称不能为空", null);

        return MockDataStore.Mutate<(bool, string?, GroupDto?)>(data =>
        {
            var allMembers = memberIds.Append(userId).Distinct().ToList();
            var group = new GroupDto
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = name.Trim(),
                AvatarSeed = name.Trim(),
                CreatorId = userId,
                MemberIds = allMembers,
                CreatedAt = DateTime.Now
            };
            data.Groups.Add(group);
            return (true, null, group);
        });
    }

    public List<GroupDto> GetGroups(string userId)
    {
        return MockDataStore.Load().Groups.Where(g => g.MemberIds.Contains(userId)).ToList();
    }
}
