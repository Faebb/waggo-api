using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Application.Identity;
using Waggo.Application.Identity.Commands.UpdateMyProfile;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;

namespace Waggo.Application.UnitTests.Identity.Commands.UpdateMyProfile;

public class UpdateMyProfileHandlerTests
{
    private static readonly DateTimeOffset s_now = new(2026, 10, 25, 12, 0, 0, TimeSpan.Zero);

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private UpdateMyProfileHandler Sut() => new(new UpdateMyProfileCommandValidator(), _users, _currentUser);

    private User RegisteredUser()
    {
        User user = User.Register("ana@mail.com", "hash", "token-hash", s_now).Data;
        _currentUser.Id.Returns(user.Id.ToString());
        _users.GetAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        return user;
    }

    [Fact]
    public async Task HandleAsync_FullNameAndPhone_SavesThemAndReturnsTheProfile()
    {
        User user = RegisteredUser();

        WaggoResponse<UserProfileResponse> result = await Sut().HandleAsync(
            new UpdateMyProfileCommand("Ana Pérez", "3001234567"), CancellationToken.None);

        result.IsValid.ShouldBeTrue();
        result.Data.FullName.ShouldBe("Ana Pérez");
        result.Data.Phone.ShouldBe("3001234567");
        user.FullName.ShouldBe("Ana Pérez");
        await _users.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NullFullNameAndPhone_ClearsThem()
    {
        User user = RegisteredUser();
        user.UpdateProfile("Ana Pérez", "3001234567");

        await Sut().HandleAsync(new UpdateMyProfileCommand(null, null), CancellationToken.None);

        user.FullName.ShouldBeNull();
        user.Phone.ShouldBeNull();
    }

    [Fact]
    public async Task HandleAsync_FullNameTooLong_FailsWithoutSaving()
    {
        RegisteredUser();
        string tooLong = new('a', 101);

        WaggoResponse<UserProfileResponse> result = await Sut().HandleAsync(
            new UpdateMyProfileCommand(tooLong, null), CancellationToken.None);

        result.IsValid.ShouldBeFalse();
        await _users.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
