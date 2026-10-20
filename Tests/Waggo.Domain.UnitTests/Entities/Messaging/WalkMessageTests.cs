using Waggo.Domain.Entities.Messaging;
using Waggo.Domain.Enums.Walks;
using Waggo.Domain.Errors.Messaging;

namespace Waggo.Domain.UnitTests.Entities.Messaging;

public class WalkMessageTests
{
    private static readonly DateTimeOffset s_now = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Send_Text_IsTrimmedAndKeepsWhoSentIt()
    {
        Guid walkId = Guid.NewGuid();

        WalkMessage message = WalkMessage.Send(walkId, WalkParty.Owner, "  ¿Ya llegaste? ", s_now).Data;

        (message.WalkId, message.SentBy, message.Text, message.SentAt)
            .ShouldBe((walkId, WalkParty.Owner, "¿Ya llegaste?", s_now));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Send_EmptyText_FailsWithInvalidText(string text) =>
        WalkMessage.Send(Guid.NewGuid(), WalkParty.Walker, text, s_now)
            .Errors.Single().Code.ShouldBe(MessageErrors.InvalidText.Code);

    [Fact]
    public void Send_TextLongerThan1000_FailsWithInvalidText() =>
        WalkMessage.Send(Guid.NewGuid(), WalkParty.Walker, new string('a', 1001), s_now)
            .Errors.Single().Code.ShouldBe(MessageErrors.InvalidText.Code);
}
