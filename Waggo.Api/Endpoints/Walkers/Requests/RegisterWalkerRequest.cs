using Waggo.Application.Walkers.Commands.RegisterWalker;
using Waggo.Domain.Enums.Walkers;

namespace Waggo.Api.Endpoints.Walkers.Requests;

/// <summary>Body of <c>POST /api/v1/walkers/me</c>. Raw values: they are validated before use.</summary>
public sealed record RegisterWalkerRequest(
    string? FullName,
    string? DocumentType,
    string? DocumentNumber,
    string? Phone,
    string? Experience)
{
    /// <summary>Only call it after the request passed <c>RegisterWalkerRequestValidator</c>.</summary>
    public RegisterWalkerCommand ToCommand() =>
        new(
            FullName!,
            Enum.Parse<DocumentType>(DocumentType!, ignoreCase: true),
            DocumentNumber!,
            Phone!,
            Experience);
}
