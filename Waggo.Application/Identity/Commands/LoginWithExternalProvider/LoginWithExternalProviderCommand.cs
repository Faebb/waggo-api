using Waggo.Application.Common.Interfaces;
using Waggo.Domain.Enums.Identity;

namespace Waggo.Application.Identity.Commands.LoginWithExternalProvider;

public sealed record LoginWithExternalProviderCommand(ExternalProvider Provider, string IdToken)
    : ICommand<SessionResponse>;
