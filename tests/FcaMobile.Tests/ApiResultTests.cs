using Fca.Mobile.Models;

namespace FcaMobile.Tests;

public class ApiResultTests
{
    [Fact]
    public void Success_carries_value_without_error()
    {
        var result = ApiResult<string>.Success("ready");

        Assert.True(result.IsSuccess);
        Assert.Equal("ready", result.Value);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Failure_carries_message_without_value()
    {
        var result = ApiResult<int>.Failure("offline");

        Assert.False(result.IsSuccess);
        Assert.Equal("offline", result.ErrorMessage);
        Assert.Equal(default, result.Value);
    }
}
