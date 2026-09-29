namespace Waggo.Domain.Enums.Common;

/// <summary>Who can see a message.</summary>
public enum MessageVisibility
{
    /// <summary>Sent to the API client and written to the log.</summary>
    Public,

    /// <summary>Only written to the log. Never leaves the server.</summary>
    Internal,
}
