using Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Api.Tests.Middleware;

public class GlobalExceptionHandlerTests
{
    private readonly Mock<IProblemDetailsService> _problemDetailsService = new();
    private readonly Mock<IHostEnvironment> _environment = new();
    private ProblemDetailsContext? _writtenResponse;

    public GlobalExceptionHandlerTests()
    {
        // Capture what the handler sends back, so we can check it
        _problemDetailsService
            .Setup(s => s.TryWriteAsync(It.IsAny<ProblemDetailsContext>()))
            .Callback<ProblemDetailsContext>(context => _writtenResponse = context)
            .ReturnsAsync(true);
    }

    private GlobalExceptionHandler CreateHandler(string environmentName)
    {
        _environment.Setup(e => e.EnvironmentName).Returns(environmentName);

        return new GlobalExceptionHandler(
            NullLogger<GlobalExceptionHandler>.Instance,
            _problemDetailsService.Object,
            _environment.Object);
    }

    public static TheoryData<Exception, int> ExceptionCases => new()
    {
        { new BadRequestException("Invalid data"), StatusCodes.Status400BadRequest },
        { new UnauthorizedAccessException("Not allowed"), StatusCodes.Status403Forbidden },
        { new NotFoundException("Task not found"), StatusCodes.Status404NotFound },
        { new ConflictException("Email already exists"), StatusCodes.Status409Conflict },
        { new InvalidOperationException("Something broke"), StatusCodes.Status500InternalServerError }
    };

    [Theory]
    [MemberData(nameof(ExceptionCases))]
    public async Task TryHandleAsync_MapsExceptionToCorrectStatusCode(Exception exception, int expectedStatusCode)
    {
        // Arrange
        var handler = CreateHandler(Environments.Development);
        var httpContext = new DefaultHttpContext();

        // Act
        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        Assert.True(handled);
        Assert.Equal(expectedStatusCode, httpContext.Response.StatusCode);
        Assert.NotNull(_writtenResponse);
        Assert.Equal(expectedStatusCode, _writtenResponse.ProblemDetails.Status);
    }

    [Fact]
    public async Task TryHandleAsync_ConflictException_ShowsMessageToUser()
    {
        // Arrange
        var handler = CreateHandler(Environments.Production);
        var exception = new ConflictException("Email already exists");

        // Act
        await handler.TryHandleAsync(new DefaultHttpContext(), exception, CancellationToken.None);

        // Assert
        Assert.NotNull(_writtenResponse);
        Assert.Equal("Email already exists", _writtenResponse.ProblemDetails.Detail);
    }

    [Fact]
    public async Task TryHandleAsync_ServerErrorInProduction_HidesInternalDetails()
    {
        // Arrange
        var handler = CreateHandler(Environments.Production);
        var exception = new InvalidOperationException("Database password is wrong");

        // Act
        await handler.TryHandleAsync(new DefaultHttpContext(), exception, CancellationToken.None);

        // Assert
        Assert.NotNull(_writtenResponse);
        Assert.DoesNotContain("password", _writtenResponse.ProblemDetails.Detail);
        Assert.Equal("An unexpected error occurred. Please try again later.", _writtenResponse.ProblemDetails.Detail);
    }
}