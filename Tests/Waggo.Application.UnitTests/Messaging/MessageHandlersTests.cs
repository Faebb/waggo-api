using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Messaging;
using Waggo.Application.Common.Interfaces.Walks;
using Waggo.Application.Messaging;
using Waggo.Application.Messaging.Commands.SendMessage;
using Waggo.Application.Messaging.Queries.ListMessages;
using Waggo.Application.UnitTests.TestData;
using Waggo.Application.UnitTests.TestDoubles;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Messaging;
using Waggo.Domain.Entities.Walks;
using Waggo.Domain.Enums.Walks;
using Waggo.Domain.Errors.Messaging;
using Waggo.Domain.Exceptions;

namespace Waggo.Application.UnitTests.Messaging;

public class MessageHandlersTests
{
    private readonly IWalkRepository _walks = Substitute.For<IWalkRepository>();
    private readonly IWalkMessageRepository _messages = Substitute.For<IWalkMessageRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly Walk _walk = WalkMother.Requested(ownerId: "owner-1");

    public MessageHandlersTests()
    {
        _walk.Accept("walker-1", FixedTimeProvider.Default);
        _walks.GetAsync(_walk.Id, Arg.Any<CancellationToken>()).Returns(_walk);
    }

    private SendMessageHandler Send(string userId)
    {
        _currentUser.Id.Returns(userId);
        return new SendMessageHandler(_walks, _messages, _currentUser, new FixedTimeProvider());
    }

    private ListMessagesHandler List(string userId)
    {
        _currentUser.Id.Returns(userId);
        return new ListMessagesHandler(_walks, _messages, _currentUser);
    }

    [Fact]
    public async Task Send_Walker_SavesAWalkerMessage()
    {
        SendMessageCommand command = new(_walk.Id, "Estoy en la portería");

        WaggoResponse<WalkMessageResponse> result = await Send("walker-1").HandleAsync(command, CancellationToken.None);

        (result.Data.SentBy, result.Data.Text).ShouldBe(("Walker", "Estoy en la portería"));
        await _messages.Received(1).AddAsync(
            Arg.Is<WalkMessage>(m => m.SentBy == WalkParty.Walker),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Send_FinishedWalk_FailsWithChatClosed()
    {
        _walk.Start(FixedTimeProvider.Default);
        _walk.Finish(FixedTimeProvider.Default);

        WaggoResponse<WalkMessageResponse> result =
            await Send("owner-1").HandleAsync(new SendMessageCommand(_walk.Id, "¿Hola?"), CancellationToken.None);

        result.Errors.Single().Code.ShouldBe(MessageErrors.ChatClosed.Code);
        await _messages.DidNotReceive().AddAsync(Arg.Any<WalkMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Send_Stranger_ThrowsNotFound() =>
        await Should.ThrowAsync<NotFoundException>(
            () => Send("someone").HandleAsync(new SendMessageCommand(_walk.Id, "Hola"), CancellationToken.None));

    [Fact]
    public async Task List_AfterAMessage_ReturnsOnlyTheNewerOnesInOrder()
    {
        WalkMessage first = WalkMessage.Send(_walk.Id, WalkParty.Owner, "uno", FixedTimeProvider.Default).Data;
        DateTimeOffset now = FixedTimeProvider.Default;
        WalkMessage second = WalkMessage.Send(_walk.Id, WalkParty.Walker, "dos", now.AddSeconds(5)).Data;
        WalkMessage third = WalkMessage.Send(_walk.Id, WalkParty.Owner, "tres", now.AddSeconds(9)).Data;
        IReadOnlyList<WalkMessage> stored = [third, first, second];
        _messages.ListByWalkAsync(_walk.Id, Arg.Any<CancellationToken>()).Returns(stored);

        WaggoResponse<IReadOnlyList<WalkMessageResponse>> all =
            await List("owner-1").HandleAsync(new ListMessagesQuery(_walk.Id, null), CancellationToken.None);
        WaggoResponse<IReadOnlyList<WalkMessageResponse>> newer =
            await List("owner-1").HandleAsync(new ListMessagesQuery(_walk.Id, first.Id), CancellationToken.None);

        all.Data.Select(m => m.Text).ShouldBe(["uno", "dos", "tres"]);
        newer.Data.Select(m => m.Text).ShouldBe(["dos", "tres"]);
    }
}
