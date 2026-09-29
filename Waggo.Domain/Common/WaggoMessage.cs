using Waggo.Domain.Enums.Common;

namespace Waggo.Domain.Common;

/// <summary>One entry of the errors, warnings or infos stack of a <see cref="WaggoResponse{T}"/>.</summary>
/// <param name="Code">Stable identifier, e.g. <c>Pricing.InvalidDuration</c>.</param>
/// <param name="Message">Text; public messages are in Spanish because the end user reads them.</param>
/// <param name="Visibility">Internal messages only go to the log and never leave the server.</param>
public sealed record WaggoMessage(string Code, string Message, MessageVisibility Visibility = MessageVisibility.Public);
