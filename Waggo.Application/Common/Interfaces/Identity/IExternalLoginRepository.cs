using Waggo.Domain.Entities.Identity;
using Waggo.Domain.Enums.Identity;

namespace Waggo.Application.Common.Interfaces.Identity;

/// <summary>Storage of the Google/Microsoft accounts linked to a <see cref="User"/> (ADR-017).</summary>
public interface IExternalLoginRepository
{
    Task<ExternalLogin?> GetAsync(
        ExternalProvider provider,
        string providerUserId,
        CancellationToken cancellationToken);

    Task AddAsync(ExternalLogin externalLogin, CancellationToken cancellationToken);
}
