using Waggo.Domain.Common;
using Waggo.Domain.Entities.Messaging;
using Waggo.Domain.Enums.Common;

namespace Waggo.Domain.Errors.Messaging;

public static class MessageErrors
{
    public static readonly Error InvalidText = new(
        "Messages.InvalidText",
        $"Escribe un mensaje de 1 a {WalkMessage.MaxTextLength} caracteres.");

    public static readonly Error ChatClosed = new(
        "Messages.ChatClosed",
        "El chat se cierra cuando el paseo termina o se cancela.",
        ErrorType.BusinessRule);
}
