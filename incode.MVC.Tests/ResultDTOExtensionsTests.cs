using Microsoft.AspNetCore.Mvc;

namespace incode.MVC.Tests;

public class ResultDTOExtensionsTests
{
    private sealed class TestController : ControllerBase { }

    private static int? StatusOf(ActionResult<ResultDTO<string>> result)
        => (result.Result as ObjectResult)?.StatusCode;

    [Fact]
    public void Success_is_always_200_even_when_code_is_204()
    {
        var result = new TestController().ToActionResult(ResultDTO<string>.Success("x", code: 204));

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Theory]
    [InlineData(400)]
    [InlineData(404)]
    [InlineData(409)]
    [InlineData(500)]
    public void Failure_uses_code_as_http_status(int code)
        => Assert.Equal(code, StatusOf(new TestController().ToActionResult(ResultDTO<string>.Failure("no", code))));

    [Theory]
    [InlineData(0)]
    [InlineData(200)]
    [InlineData(700)]
    public void Failure_with_non_error_code_falls_back_to_400(int code)
        => Assert.Equal(400, StatusOf(new TestController().ToActionResult(ResultDTO<string>.Failure("no", code))));
}
