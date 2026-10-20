using Waggo.Application.Common.Interfaces;

namespace Waggo.Application.Identity.Commands.Register;

public sealed record RegisterCommand(string Email, string Password) : ICommand<UserResponse>;
