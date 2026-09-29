using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Waggo.Api.Infrastructure.Middleware;
using Waggo.Domain.Common;
using Waggo.Domain.Enums.Common;
using Waggo.Domain.Exceptions;

namespace Waggo.Api.UnitTests.Infrastructure.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    private static readonly Error s_walkNotFound = new("Walks.NotFound", "El paseo no existe.", ErrorType.NotFound);

    [Fact]
    public async Task InvokeAsync_NoException_LeavesTheResponseAlone()
    {
        DefaultHttpContext context = NewContext();

        await Run(context, _ => Task.CompletedTask);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
        (await ReadBody(context)).ShouldBeEmpty();
    }

    [Fact]
    public async Task InvokeAsync_WaggoException_UsesItsStatusAndPublicError()
    {
        DefaultHttpContext context = NewContext();

        await Run(context, _ => throw new NotFoundException(s_walkNotFound, "Walk 42 not found in table walks.walks"));

        context.Response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        using JsonDocument body = JsonDocument.Parse(await ReadBody(context));
        JsonElement error = body.RootElement.GetProperty("errors")[0];
        error.GetProperty("code").GetString().ShouldBe("Walks.NotFound");
        error.GetProperty("message").GetString().ShouldBe("El paseo no existe.");
        body.RootElement.GetProperty("success").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task InvokeAsync_WaggoException_NeverSendsTheTechnicalDetail()
    {
        DefaultHttpContext context = NewContext();

        await Run(context, _ => throw new ConflictException(s_walkNotFound, "UPDATE failed: row version 7"));

        (await ReadBody(context)).ShouldNotContain("row version");
    }

    [Fact]
    public async Task InvokeAsync_UnknownException_Returns500_AskingToContactTheAdministrator()
    {
        DefaultHttpContext context = NewContext();

        await Run(context, _ => throw new InvalidOperationException("Npgsql: password authentication failed"));

        context.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        string body = await ReadBody(context);
        body.ShouldContain("Server.Unexpected");
        body.ShouldContain("administrador");
        body.ShouldNotContain("Npgsql");
        body.ShouldNotContain("InvalidOperationException");
    }

    [Fact]
    public async Task InvokeAsync_BadHttpRequest_Returns400WithGenericMessage()
    {
        DefaultHttpContext context = NewContext();

        await Run(context, _ => throw new BadHttpRequestException("Failed to bind parameter \"int x\""));

        context.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        string body = await ReadBody(context);
        body.ShouldContain("Request.Invalid");
        body.ShouldNotContain("Failed to bind");
    }

    [Theory]
    [InlineData(typeof(ForbiddenException), StatusCodes.Status403Forbidden)]
    [InlineData(typeof(UnauthorizedException), StatusCodes.Status401Unauthorized)]
    [InlineData(typeof(BusinessRuleException), StatusCodes.Status422UnprocessableEntity)]
    [InlineData(typeof(ExternalServiceException), StatusCodes.Status503ServiceUnavailable)]
    public void Resolve_MapsEachCustomExceptionToItsStatus(Type exceptionType, int status)
    {
        Exception exception = (Exception)Activator.CreateInstance(exceptionType, s_walkNotFound, null, null)!;

        ExceptionHandlingMiddleware.Resolve(exception, "trace").Status.ShouldBe(status);
    }

    private static DefaultHttpContext NewContext()
    {
        DefaultHttpContext context = new();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static Task Run(HttpContext context, RequestDelegate next) =>
        new ExceptionHandlingMiddleware(next, NullLogger<ExceptionHandlingMiddleware>.Instance).InvokeAsync(context);

    private static async Task<string> ReadBody(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using StreamReader reader = new(context.Response.Body);
        return await reader.ReadToEndAsync();
    }
}
