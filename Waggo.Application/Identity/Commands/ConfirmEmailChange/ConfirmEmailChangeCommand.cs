using Waggo.Application.Common.Interfaces;

namespace Waggo.Application.Identity.Commands.ConfirmEmailChange;

/// <summary>The refresh token of the session that confirms the change stays open; the others are closed.</summary>
public sealed record ConfirmEmailChangeCommand(string Code, string? RefreshToken) : ICommand<bool>;
