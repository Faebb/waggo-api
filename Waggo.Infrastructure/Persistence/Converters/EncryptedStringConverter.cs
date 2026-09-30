using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Waggo.Infrastructure.Services.Security;

namespace Waggo.Infrastructure.Persistence.Converters;

/// <summary>
/// Stores an optional string column encrypted (RNF-003): the entity keeps plain text, the database only sees cipher.
/// EF Core never passes null to a converter, so null stays null in the column.
/// </summary>
internal sealed class EncryptedStringConverter(AesGcmFieldEncryptor encryptor)
    : ValueConverter<string?, string>(
        plain => encryptor.Encrypt(plain!),
        cipher => encryptor.Decrypt(cipher));
