using Waggo.Domain.Common;
using Waggo.Domain.Entities.Walkers;
using Waggo.Domain.Enums.Walkers;
using Waggo.Domain.Errors.Walkers;

namespace Waggo.Domain.UnitTests.Entities.Walkers;

public class WalkerProfileTests
{
    private static readonly DateTimeOffset s_now = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);

    private static WaggoResponse<WalkerProfile> Register(
        string fullName = "Andrés Gómez",
        DocumentType documentType = DocumentType.CC,
        string documentNumber = "1020304050",
        string phone = "3001234567",
        string? experience = "3 años con perros grandes") =>
        WalkerProfile.Register("walker-1", fullName, documentType, documentNumber, phone, experience, s_now);

    [Fact]
    public void Register_ValidData_IsPendingAndShowsOnlyTheLast4Digits()
    {
        WalkerProfile profile = Register().Data;

        profile.UserId.ShouldBe("walker-1");
        profile.FullName.ShouldBe("Andrés Gómez");
        profile.DocumentNumber.ShouldBe("1020304050");
        profile.DocumentLast4.ShouldBe("4050");
        profile.Status.ShouldBe(VerificationStatus.Pending);
        profile.RegisteredAt.ShouldBe(s_now);
        profile.IsVerified.ShouldBeFalse();
    }

    [Theory]
    [InlineData("Al")]
    [InlineData("   ")]
    public void Register_InvalidFullName_Fails(string fullName) =>
        Register(fullName: fullName).Errors.ShouldContain(e => e.Code == WalkerErrors.InvalidFullName.Code);

    [Theory]
    [InlineData(DocumentType.CC, "12AB45")]
    [InlineData(DocumentType.CC, "1234")]
    [InlineData(DocumentType.CE, "1234567890123456")]
    public void Register_InvalidDocument_Fails(DocumentType type, string number) =>
        Register(documentType: type, documentNumber: number)
            .Errors.ShouldContain(e => e.Code == WalkerErrors.InvalidDocument.Code);

    [Fact]
    public void Register_PassportWithLetters_IsValid() =>
        Register(documentType: DocumentType.PP, documentNumber: "AB123456").IsValid.ShouldBeTrue();

    [Theory]
    [InlineData("30012")]
    [InlineData("300123456a")]
    public void Register_InvalidPhone_Fails(string phone) =>
        Register(phone: phone).Errors.ShouldContain(e => e.Code == WalkerErrors.InvalidPhone.Code);

    [Fact]
    public void Approve_Pending_BecomesVerified()
    {
        WalkerProfile profile = Register().Data;

        WaggoResponse<WalkerProfile> result = profile.Approve("admin-1", s_now.AddHours(1));

        result.IsValid.ShouldBeTrue();
        profile.Status.ShouldBe(VerificationStatus.Approved);
        profile.IsVerified.ShouldBeTrue();
        (profile.ReviewedBy, profile.ReviewedAt).ShouldBe(("admin-1", s_now.AddHours(1)));
    }

    [Fact]
    public void Reject_Pending_KeepsTheReason()
    {
        WalkerProfile profile = Register().Data;

        profile.Reject("admin-1", "Documento ilegible", s_now).IsValid.ShouldBeTrue();

        (profile.Status, profile.RejectionReason).ShouldBe((VerificationStatus.Rejected, "Documento ilegible"));
    }

    [Fact]
    public void Reject_WithoutAReason_Fails() =>
        Register().Data.Reject("admin-1", " ", s_now)
            .Errors.Single().Code.ShouldBe(WalkerErrors.InvalidRejectionReason.Code);

    [Fact]
    public void Approve_AlreadyReviewed_FailsWithNotPending()
    {
        WalkerProfile profile = Register().Data;
        profile.Approve("admin-1", s_now);

        profile.Approve("admin-1", s_now).Errors.Single().Code.ShouldBe(WalkerErrors.NotPending.Code);
    }
}
