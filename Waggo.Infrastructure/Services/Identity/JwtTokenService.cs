using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Infrastructure.Options.Identity;

namespace Waggo.Infrastructure.Services.Identity;

/// <summary>
/// Issues the API's own JWT access tokens (ADR-016): HS256, signed and validated with the same symmetric key
/// (<see cref="JwtOptions.SigningKey"/>) since the API is both issuer and resource server.
/// </summary>
public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;
    private readonly SigningCredentials _signingCredentials;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;

        byte[] key = Convert.FromBase64String(_options.SigningKey);
        _signingCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);
    }

    public (string AccessToken, int ExpiresInSeconds) IssueAccessToken(Guid userId, IReadOnlyList<string> roles)
    {
        ArgumentNullException.ThrowIfNull(roles);
        DateTime now = DateTime.UtcNow;
        TimeSpan lifetime = TimeSpan.FromMinutes(_options.AccessTokenMinutes);

        List<Claim> claims =
        [
            new Claim("sub", userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        ];
        claims.AddRange(roles.Select(role => new Claim(_options.RoleClaimType, role)));

        JwtSecurityToken token = new(
            _options.Issuer,
            _options.Audience,
            claims,
            notBefore: now,
            expires: now.Add(lifetime),
            signingCredentials: _signingCredentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), (int)lifetime.TotalSeconds);
    }
}
