using System.Text.Json;
using ChatApp.Shared.Enums;
using ChatApp.Shared.Models;

namespace ChatApp.Shared.Mock;

public static class MockDataStore
{
    private static readonly object Lock = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly string DataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ChatApp");

    private static readonly string DataPath = Path.Combine(DataDirectory, "mock-data.json");

    public static MockDataSnapshot Load()
    {
        lock (Lock)
        {
            if (!File.Exists(DataPath))
            {
                var seed = CreateSeedData();
                SaveInternal(seed);
                return seed;
            }

            var json = File.ReadAllText(DataPath);
            var data = JsonSerializer.Deserialize<MockDataSnapshot>(json, JsonOptions);
            return data ?? CreateSeedData();
        }
    }

    public static void Save(MockDataSnapshot data)
    {
        lock (Lock)
        {
            SaveInternal(data);
        }
    }

    public static void Mutate(Action<MockDataSnapshot> action)
    {
        lock (Lock)
        {
            var data = Load();
            action(data);
            SaveInternal(data);
        }
    }

    public static T Mutate<T>(Func<MockDataSnapshot, T> func)
    {
        lock (Lock)
        {
            var data = Load();
            var result = func(data);
            SaveInternal(data);
            return result;
        }
    }

    private static void SaveInternal(MockDataSnapshot data)
    {
        Directory.CreateDirectory(DataDirectory);
        File.WriteAllText(DataPath, JsonSerializer.Serialize(data, JsonOptions));
    }

    private static MockDataSnapshot CreateSeedData()
    {
        var now = DateTime.Now;
        var felixId = "user-felix";
        var zhangId = "user-zhang";
        var liId = "user-li";
        var groupId = "group-frontend";

        var users = new List<UserDto>
        {
            new()
            {
                Id = felixId,
                Username = "felix",
                Password = "123456",
                Nickname = "Felix (我)",
                AvatarSeed = "Felix",
                Status = UserStatus.Active,
                RegisteredAt = now.AddDays(-30),
                OnlineStatus = "在线"
            },
            new()
            {
                Id = zhangId,
                Username = "zhang_pm",
                Password = "123456",
                Nickname = "张三 (项目经理)",
                AvatarSeed = "Peter",
                Status = UserStatus.Active,
                RegisteredAt = now.AddDays(-20),
                OnlineStatus = "手机在线"
            },
            new()
            {
                Id = liId,
                Username = "li_dev",
                Password = "123456",
                Nickname = "李四",
                AvatarSeed = "Li",
                Status = UserStatus.Active,
                RegisteredAt = now.AddDays(-15),
                OnlineStatus = "在线"
            },
            new()
            {
                Id = "user-test01",
                Username = "user_test01",
                Password = "123456",
                Nickname = "研发部-小张",
                AvatarSeed = "Test01",
                Status = UserStatus.Pending,
                Remark = "日常工作沟通使用，属于研发一组成员。",
                RegisteredAt = now.AddHours(-2)
            },
            new()
            {
                Id = "user-sales",
                Username = "sales_king",
                Password = "123456",
                Nickname = "销售部-王五",
                AvatarSeed = "Sales",
                Status = UserStatus.Pending,
                Remark = "外部业务对接。",
                RegisteredAt = now.AddHours(-1)
            }
        };

        var groups = new List<GroupDto>
        {
            new()
            {
                Id = groupId,
                Name = "前端开发技术交流群",
                AvatarSeed = "Group1",
                CreatorId = felixId,
                MemberIds = [felixId, zhangId, liId],
                CreatedAt = now.AddDays(-10)
            }
        };

        var friendships = new List<FriendshipEntry>
        {
            new() { UserId = felixId, FriendId = zhangId },
            new() { UserId = zhangId, FriendId = felixId },
            new() { UserId = felixId, FriendId = liId },
            new() { UserId = liId, FriendId = felixId }
        };

        var privateSessionId = GetPrivateSessionId(felixId, zhangId);
        var groupSessionId = GetGroupSessionId(groupId);

        var messages = new List<MessageDto>
        {
            new()
            {
                Id = "msg-1",
                SessionId = privateSessionId,
                SenderId = zhangId,
                SenderName = "张三 (项目经理)",
                SenderAvatarSeed = "Peter",
                ReceiverName = "Felix (我)",
                Type = MessageType.Text,
                Content = "下午好，系统基本的原型图和功能点你都看过了吧？",
                SentAt = now.AddHours(-1),
                IsMine = false
            },
            new()
            {
                Id = "msg-2",
                SessionId = privateSessionId,
                SenderId = felixId,
                SenderName = "Felix (我)",
                SenderAvatarSeed = "Felix",
                ReceiverName = "张三 (项目经理)",
                Type = MessageType.Text,
                Content = "看过了，我正在编写 Web 端和桌面端共用的 HTML/CSS 基础布局结构。",
                SentAt = now.AddMinutes(-55),
                IsMine = true
            },
            new()
            {
                Id = "msg-3",
                SessionId = privateSessionId,
                SenderId = zhangId,
                SenderName = "张三 (项目经理)",
                SenderAvatarSeed = "Peter",
                ReceiverName = "Felix (我)",
                Type = MessageType.File,
                Content = "网上沟通系统需求文档.pdf",
                FileName = "网上沟通系统需求文档.pdf",
                FileSize = null,
                FileProgress = 100,
                SentAt = now.AddMinutes(-50),
                IsMine = false
            },
            new()
            {
                Id = "msg-4",
                SessionId = privateSessionId,
                SenderId = zhangId,
                SenderName = "张三 (项目经理)",
                SenderAvatarSeed = "Peter",
                ReceiverName = "Felix (我)",
                Type = MessageType.Text,
                Content = "那个前端布局方案写好了吗？",
                SentAt = now.AddMinutes(-30),
                IsMine = false
            },
            new()
            {
                Id = "msg-5",
                SessionId = groupSessionId,
                SenderId = liId,
                SenderName = "李四",
                SenderAvatarSeed = "Li",
                ReceiverName = "前端开发技术交流群",
                Type = MessageType.Text,
                Content = "可以使用 MAUI Blazor。",
                SentAt = now.AddMinutes(-98),
                IsMine = false
            },
            new()
            {
                Id = "msg-6",
                SessionId = groupSessionId,
                SenderId = felixId,
                SenderName = "Felix (我)",
                SenderAvatarSeed = "Felix",
                ReceiverName = "前端开发技术交流群",
                Type = MessageType.Text,
                Content = "大家把今天写的 MAUI 代码提交一下",
                SentAt = now.AddMinutes(-60),
                IsMine = true
            }
        };

        return new MockDataSnapshot
        {
            Users = users,
            FriendRequests = [],
            Groups = groups,
            Messages = messages,
            Friendships = friendships
        };
    }

    public static string GetPrivateSessionId(string userId1, string userId2)
    {
        var ids = new[] { userId1, userId2 }.OrderBy(x => x).ToArray();
        return $"session-private-{ids[0]}-{ids[1]}";
    }

    public static string GetGroupSessionId(string groupId) => $"session-group-{groupId}";
}
