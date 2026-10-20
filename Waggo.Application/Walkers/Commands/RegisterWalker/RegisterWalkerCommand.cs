using Waggo.Application.Common.Interfaces;
using Waggo.Domain.Enums.Walkers;

namespace Waggo.Application.Walkers.Commands.RegisterWalker;

public sealed record RegisterWalkerCommand(
    string FullName,
    DocumentType DocumentType,
    string DocumentNumber,
    string Phone,
    string? Experience) : ICommand<WalkerProfileResponse>;
