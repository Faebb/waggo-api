namespace Waggo.Application.Common.Interfaces;

/// <summary>User that makes the current request, taken from the access token (or the development user).</summary>
public interface ICurrentUser
{
    /// <summary>Id in the identity provider (<c>sub</c> claim).</summary>
    string Id { get; }
}
