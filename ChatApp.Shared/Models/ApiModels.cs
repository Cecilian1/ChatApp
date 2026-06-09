namespace ChatApp.Shared.Models;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public UserDto? User { get; set; }
    public string? AdminName { get; set; }
}

public class ApiResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }

    public static ApiResult Ok() => new() { Success = true };
    public static ApiResult Fail(string error) => new() { Success = false, Error = error };
}

public class ApiResult<T> : ApiResult
{
    public T? Data { get; set; }

    public static ApiResult<T> Ok(T data) => new() { Success = true, Data = data };
    public new static ApiResult<T> Fail(string error) => new() { Success = false, Error = error };
}

public class PendingRegistrationsResponse
{
    public int Count { get; set; }
    public List<UserDto> Items { get; set; } = [];
}
