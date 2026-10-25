using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Application.Identity;
using Waggo.Application.Identity.Queries.GetMyProfile;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;

namespace Waggo.Application.UnitTests.Identity.Queries.GetMyProfile;

public class GetMyProfileHandlerTests
{
    private static readonly DateTimeOffset s_now = new(2026, 10, 25, 12, 0, 0, TimeSpan.Zero);

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private GetMyProfileHandler Sut() => new(_users, _currentUser);

    [Fact]
    public async Task HandleAsync_SignedInUser_ReturnsEmailFullNameAndPhone()
    {
        User user = User.Register("ana@mail.com", "hash", "token-hash", s_now).Data;
        user.UpdateProfile("Ana Pérez", "3001234567");
        _currentUser.Id.Returns(user.Id.ToString());
        _users.GetAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        WaggoResponse<UserProfileResponse> result = await Sut().HandleAsync(new GetMyProfileQuery(), CancellationToken.None);

        result.IsValid.ShouldBeTrue();
        result.Data.Email.ShouldBe("ana@mail.com");
        result.Data.FullName.ShouldBe("Ana Pérez");
        result.Data.Phone.ShouldBe("3001234567");
    }

    [Fact]
    public async Task HandleAsync_NoFullNameOrPhoneYet_ReturnsNulls()
    {
        User user = User.Register("ana@mail.com", "hash", "token-hash", s_now).Data;
        _currentUser.Id.Returns(user.Id.ToString());
        _users.GetAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        WaggoResponse<UserProfileResponse> result = await Sut().HandleAsync(new GetMyProfileQuery(), CancellationToken.None);

        result.Data.FullName.ShouldBeNull();
        result.Data.Phone.ShouldBeNull();
    }
}
