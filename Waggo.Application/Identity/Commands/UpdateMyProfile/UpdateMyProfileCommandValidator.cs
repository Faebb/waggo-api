using FluentValidation;
using Waggo.Application.Common.Extensions;
using Waggo.Domain.Entities.Identity;
using Waggo.Domain.Errors.Identity;

namespace Waggo.Application.Identity.Commands.UpdateMyProfile;

/// <summary>Field rules of <see cref="UpdateMyProfileCommand"/>. Both fields are optional; a null clears them.</summary>
internal sealed class UpdateMyProfileCommandValidator : AbstractValidator<UpdateMyProfileCommand>
{
    public UpdateMyProfileCommandValidator()
    {
        RuleFor(command => command.FullName)
            .Must(fullName => fullName is null || fullName.Trim().Length <= User.MaxFullNameLength)
            .WithError(IdentityErrors.InvalidFullName);

        RuleFor(command => command.Phone)
            .Must(phone => phone is null || phone.Trim().Length <= User.MaxPhoneLength)
            .WithError(IdentityErrors.InvalidPhone);
    }
}
