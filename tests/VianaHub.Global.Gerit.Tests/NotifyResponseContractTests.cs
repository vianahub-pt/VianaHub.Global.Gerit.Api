using Microsoft.AspNetCore.Http;
using VianaHub.Global.Gerit.Api.Helpers;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Tests.Api;

public class NotifyResponseContractTests
{
    [Fact]
    public void BoolSuccess_HasBooleanBodyAnd200Status()
    {
        var result = new Notify().CustomResponse(true, 200);
        Assert.Equal(200, Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode);
        Assert.Equal(true, Assert.IsAssignableFrom<IValueHttpResult>(result).Value);
    }

    [Fact]
    public void StatusOnlySuccess_HasNoBody()
    {
        var result = new Notify().CustomResponse(200);
        Assert.Equal(200, Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode);
        Assert.False(result is IValueHttpResult);
    }

    [Fact]
    public void NoContent_DiscardsBody()
    {
        var result = new Notify().CustomResponse(true, 204);
        Assert.Equal(204, Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode);
        Assert.False(result is IValueHttpResult);
    }
}
