namespace Fca.Mobile.Models;

public readonly record struct ApiResult<T>(bool IsSuccess, T? Value, string? ErrorMessage)
{
    public static ApiResult<T> Success(T value) => new(true, value, null);

    public static ApiResult<T> Failure(string message) => new(false, default, message);
}

public readonly record struct ApiResult(bool IsSuccess, string? ErrorMessage)
{
    public static ApiResult Success() => new(true, null);

    public static ApiResult Failure(string message) => new(false, message);
}
