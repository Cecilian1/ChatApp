using ChatApp.Api.Data;
using ChatApp.Api.Data.Entities;
using ChatApp.Shared.Enums;
using ChatApp.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Api.Services;

public interface IFriendBusinessService
{
    Task<List<FriendDto>> GetFriendsAsync(long userId);
    Task<List<FriendRequestDto>> GetPendingRequestsAsync(long userId);
    Task<List<UserDto>> SearchUsersAsync(long userId, string keyword);
    Task<(bool Success, string? Error)> SendRequestAsync(long userId, long targetUserId);
    Task<(bool Success, string? Error)> AcceptRequestAsync(long userId, long requestId);
    Task<(bool Success, string? Error)> RejectRequestAsync(long userId, long requestId);
    Task<(bool Success, string? Error)> RemoveFriendAsync(long userId, long friendId);
}

public class FriendBusinessService(AppDbContext db) : IFriendBusinessService
{
    public async Task<List<FriendDto>> GetFriendsAsync(long userId)
    {
        return await db.Friendships
            .Where(f => f.UserId == userId)
            .Include(f => f.Friend)
            .Select(f => new FriendDto
            {
                UserId = f.FriendId.ToString(),
                Username = f.Friend.Username,
                Nickname = f.Friend.Nickname,
                AvatarSeed = f.Friend.AvatarSeed,
                OnlineStatus = f.Friend.OnlineStatus
            }).ToListAsync();
    }

    public async Task<List<FriendRequestDto>> GetPendingRequestsAsync(long userId)
    {
        return await db.FriendRequests
            .Where(r => r.ToUserId == userId && r.Status == FriendRequestStatus.Pending)
            .Include(r => r.FromUser)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new FriendRequestDto
            {
                Id = r.Id.ToString(),
                FromUserId = r.FromUserId.ToString(),
                FromUsername = r.FromUser.Username,
                FromNickname = r.FromUser.Nickname,
                ToUserId = r.ToUserId.ToString(),
                Status = r.Status,
                CreatedAt = r.CreatedAt
            }).ToListAsync();
    }

    public async Task<List<UserDto>> SearchUsersAsync(long userId, string keyword)
    {
        var friendIds = await db.Friendships.Where(f => f.UserId == userId).Select(f => f.FriendId).ToListAsync();
        var query = db.Users.Where(u => u.Id != userId && u.Status == UserStatus.Active && !friendIds.Contains(u.Id));
        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(u => u.Username.Contains(keyword) || u.Nickname.Contains(keyword));
        return await query.Take(10).Select(u => AuthService.MapUser(u)).ToListAsync();
    }

    public async Task<(bool Success, string? Error)> SendRequestAsync(long userId, long targetUserId)
    {
        if (userId == targetUserId) return (false, "不能添加自己为好友");
        if (!await db.Users.AnyAsync(u => u.Id == targetUserId && u.Status == UserStatus.Active))
            return (false, "用户不存在");
        if (await db.Friendships.AnyAsync(f => f.UserId == userId && f.FriendId == targetUserId))
            return (false, "已经是好友");
        if (await db.FriendRequests.AnyAsync(r => r.Status == FriendRequestStatus.Pending &&
            ((r.FromUserId == userId && r.ToUserId == targetUserId) || (r.FromUserId == targetUserId && r.ToUserId == userId))))
            return (false, "已有待处理的好友申请");

        db.FriendRequests.Add(new FriendRequest { FromUserId = userId, ToUserId = targetUserId, Status = FriendRequestStatus.Pending });
        await db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> AcceptRequestAsync(long userId, long requestId)
    {
        var request = await db.FriendRequests.FirstOrDefaultAsync(r =>
            r.Id == requestId && r.ToUserId == userId && r.Status == FriendRequestStatus.Pending);
        if (request is null) return (false, "申请不存在");

        request.Status = FriendRequestStatus.Accepted;
        db.Friendships.AddRange(
            new Friendship { UserId = request.FromUserId, FriendId = request.ToUserId },
            new Friendship { UserId = request.ToUserId, FriendId = request.FromUserId });

        var a = Math.Min(request.FromUserId, request.ToUserId);
        var b = Math.Max(request.FromUserId, request.ToUserId);
        if (!await db.Conversations.AnyAsync(c => c.Type == SessionType.Private && c.UserAId == a && c.UserBId == b))
        {
            db.Conversations.Add(new Conversation { Type = SessionType.Private, UserAId = a, UserBId = b });
        }
        await db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> RejectRequestAsync(long userId, long requestId)
    {
        var request = await db.FriendRequests.FirstOrDefaultAsync(r =>
            r.Id == requestId && r.ToUserId == userId && r.Status == FriendRequestStatus.Pending);
        if (request is null) return (false, "申请不存在");
        request.Status = FriendRequestStatus.Rejected;
        await db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> RemoveFriendAsync(long userId, long friendId)
    {
        var friendships = await db.Friendships.Where(f =>
            (f.UserId == userId && f.FriendId == friendId) || (f.UserId == friendId && f.FriendId == userId)).ToListAsync();
        if (friendships.Count == 0) return (false, "好友不存在");
        db.Friendships.RemoveRange(friendships);
        await db.SaveChangesAsync();
        return (true, null);
    }
}
