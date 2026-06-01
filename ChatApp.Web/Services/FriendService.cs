using ChatApp.Shared.Enums;
using ChatApp.Shared.Mock;
using ChatApp.Shared.Models;

namespace ChatApp.Web.Services;

public interface IFriendService
{
    List<FriendDto> GetFriends(string userId);
    List<FriendRequestDto> GetPendingRequests(string userId);
    List<FriendRequestDto> GetSentRequests(string userId);
    List<UserDto> SearchUsers(string userId, string keyword);
    (bool Success, string? Error) SendRequest(string userId, string targetUserId);
    (bool Success, string? Error) AcceptRequest(string userId, string requestId);
    (bool Success, string? Error) RejectRequest(string userId, string requestId);
    (bool Success, string? Error) RemoveFriend(string userId, string friendId);
}

public class MockFriendService : IFriendService
{
    public List<FriendDto> GetFriends(string userId)
    {
        var data = MockDataStore.Load();
        return data.Friendships
            .Where(f => f.UserId == userId)
            .Select(f => data.Users.FirstOrDefault(u => u.Id == f.FriendId))
            .Where(u => u is not null)
            .Select(u => new FriendDto
            {
                UserId = u!.Id,
                Username = u.Username,
                Nickname = u.Nickname,
                AvatarSeed = u.AvatarSeed,
                OnlineStatus = u.OnlineStatus
            })
            .ToList();
    }

    public List<FriendRequestDto> GetPendingRequests(string userId)
    {
        return MockDataStore.Load().FriendRequests
            .Where(r => r.ToUserId == userId && r.Status == FriendRequestStatus.Pending)
            .OrderByDescending(r => r.CreatedAt)
            .ToList();
    }

    public List<FriendRequestDto> GetSentRequests(string userId)
    {
        return MockDataStore.Load().FriendRequests
            .Where(r => r.FromUserId == userId && r.Status == FriendRequestStatus.Pending)
            .OrderByDescending(r => r.CreatedAt)
            .ToList();
    }

    public List<UserDto> SearchUsers(string userId, string keyword)
    {
        var data = MockDataStore.Load();
        var friendIds = data.Friendships.Where(f => f.UserId == userId).Select(f => f.FriendId).ToHashSet();

        return data.Users
            .Where(u => u.Id != userId && u.Status == UserStatus.Active)
            .Where(u => string.IsNullOrWhiteSpace(keyword) ||
                        u.Username.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                        u.Nickname.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .Where(u => !friendIds.Contains(u.Id))
            .Take(10)
            .ToList();
    }

    public (bool Success, string? Error) SendRequest(string userId, string targetUserId)
    {
        if (userId == targetUserId)
            return (false, "不能添加自己为好友");

        return MockDataStore.Mutate<(bool, string?)>(data =>
        {
            var target = data.Users.FirstOrDefault(u => u.Id == targetUserId);
            if (target is null) return (false, "用户不存在");

            if (data.Friendships.Any(f => f.UserId == userId && f.FriendId == targetUserId))
                return (false, "已经是好友");

            if (data.FriendRequests.Any(r =>
                    r.Status == FriendRequestStatus.Pending &&
                    ((r.FromUserId == userId && r.ToUserId == targetUserId) ||
                     (r.FromUserId == targetUserId && r.ToUserId == userId))))
                return (false, "已有待处理的好友申请");

            var from = data.Users.First(u => u.Id == userId);
            data.FriendRequests.Add(new FriendRequestDto
            {
                Id = Guid.NewGuid().ToString("N"),
                FromUserId = userId,
                FromUsername = from.Username,
                FromNickname = from.Nickname,
                ToUserId = targetUserId,
                Status = FriendRequestStatus.Pending,
                CreatedAt = DateTime.Now
            });

            return (true, null);
        });
    }

    public (bool Success, string? Error) AcceptRequest(string userId, string requestId)
    {
        return MockDataStore.Mutate<(bool, string?)>(data =>
        {
            var request = data.FriendRequests.FirstOrDefault(r =>
                r.Id == requestId && r.ToUserId == userId && r.Status == FriendRequestStatus.Pending);
            if (request is null) return (false, "申请不存在");

            request.Status = FriendRequestStatus.Accepted;
            AddFriendship(data, request.FromUserId, request.ToUserId);
            return (true, null);
        });
    }

    public (bool Success, string? Error) RejectRequest(string userId, string requestId)
    {
        return MockDataStore.Mutate<(bool, string?)>(data =>
        {
            var request = data.FriendRequests.FirstOrDefault(r =>
                r.Id == requestId && r.ToUserId == userId && r.Status == FriendRequestStatus.Pending);
            if (request is null) return (false, "申请不存在");

            request.Status = FriendRequestStatus.Rejected;
            return (true, null);
        });
    }

    public (bool Success, string? Error) RemoveFriend(string userId, string friendId)
    {
        return MockDataStore.Mutate<(bool, string?)>(data =>
        {
            var removed = data.Friendships.RemoveAll(f =>
                (f.UserId == userId && f.FriendId == friendId) ||
                (f.UserId == friendId && f.FriendId == userId));

            return removed > 0 ? (true, null) : (false, "好友不存在");
        });
    }

    private static void AddFriendship(MockDataSnapshot data, string userId1, string userId2)
    {
        if (!data.Friendships.Any(f => f.UserId == userId1 && f.FriendId == userId2))
            data.Friendships.Add(new FriendshipEntry { UserId = userId1, FriendId = userId2 });
        if (!data.Friendships.Any(f => f.UserId == userId2 && f.FriendId == userId1))
            data.Friendships.Add(new FriendshipEntry { UserId = userId2, FriendId = userId1 });
    }
}
