namespace ChatApp.Admin.Services;

public interface IAdminAuthService
{
    bool IsLoggedIn { get; }
    bool Login(string username, string password);
    void Logout();
    event Action? OnChange;
}

public interface IAdminService
{
    int GetPendingCount();
    List<ChatApp.Shared.Models.UserDto> GetPendingRegistrations();
    List<ChatApp.Shared.Models.UserDto> GetAllUsers(string? keyword, ChatApp.Shared.Enums.UserStatus? status);
    List<ChatApp.Shared.Models.MessageDto> QueryMessages(ChatApp.Shared.Models.MessageQueryFilter filter);
    bool ApproveRegistration(string userId);
    bool RejectRegistration(string userId);
    bool BanUser(string userId);
    bool UnbanUser(string userId);
    bool DeleteMessage(string messageId);
    bool DeleteMessages(IEnumerable<string> messageIds);
}
