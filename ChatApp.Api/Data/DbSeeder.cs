using BCrypt.Net;
using ChatApp.Api.Data;
using ChatApp.Api.Data.Entities;
using ChatApp.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (!await db.Admins.AnyAsync())
        {
            db.Admins.Add(new Admin
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                DisplayName = "超级管理员"
            });
        }

        if (!await db.Users.AnyAsync())
        {
            var felix = new User
            {
                Username = "felix",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                Nickname = "Felix (我)",
                AvatarSeed = "Felix",
                Status = UserStatus.Active,
                OnlineStatus = "在线",
                RegisteredAt = DateTime.UtcNow.AddDays(-30)
            };
            var zhang = new User
            {
                Username = "zhang_pm",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                Nickname = "张三 (项目经理)",
                AvatarSeed = "Peter",
                Status = UserStatus.Active,
                OnlineStatus = "手机在线",
                RegisteredAt = DateTime.UtcNow.AddDays(-20)
            };
            var li = new User
            {
                Username = "li_dev",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                Nickname = "李四",
                AvatarSeed = "Li",
                Status = UserStatus.Active,
                OnlineStatus = "在线",
                RegisteredAt = DateTime.UtcNow.AddDays(-15)
            };
            db.Users.AddRange(felix, zhang, li);
            await db.SaveChangesAsync();

            db.Friendships.AddRange(
                new Friendship { UserId = felix.Id, FriendId = zhang.Id },
                new Friendship { UserId = zhang.Id, FriendId = felix.Id },
                new Friendship { UserId = felix.Id, FriendId = li.Id },
                new Friendship { UserId = li.Id, FriendId = felix.Id });

            var privateConv = new Conversation
            {
                Type = SessionType.Private,
                UserAId = Math.Min(felix.Id, zhang.Id),
                UserBId = Math.Max(felix.Id, zhang.Id),
                LastMessageAt = DateTime.UtcNow
            };
            db.Conversations.Add(privateConv);

            var group = new ChatGroup
            {
                Name = "前端开发技术交流群",
                AvatarSeed = "Group1",
                CreatorId = felix.Id
            };
            db.Groups.Add(group);
            await db.SaveChangesAsync();

            db.GroupMembers.AddRange(
                new GroupMember { GroupId = group.Id, UserId = felix.Id },
                new GroupMember { GroupId = group.Id, UserId = zhang.Id },
                new GroupMember { GroupId = group.Id, UserId = li.Id });

            var groupConv = new Conversation
            {
                Type = SessionType.Group,
                GroupId = group.Id,
                LastMessageAt = DateTime.UtcNow.AddMinutes(-60)
            };
            db.Conversations.Add(groupConv);
            await db.SaveChangesAsync();

            db.Messages.AddRange(
                new Message
                {
                    ConversationId = privateConv.Id,
                    SenderId = zhang.Id,
                    Type = MessageType.Text,
                    Content = "那个前端布局方案写好了吗？",
                    SentAt = DateTime.UtcNow.AddMinutes(-30)
                },
                new Message
                {
                    ConversationId = groupConv.Id,
                    SenderId = li.Id,
                    Type = MessageType.Text,
                    Content = "可以使用 MAUI Blazor。",
                    SentAt = DateTime.UtcNow.AddMinutes(-98)
                });
            await db.SaveChangesAsync();
        }
    }
}
