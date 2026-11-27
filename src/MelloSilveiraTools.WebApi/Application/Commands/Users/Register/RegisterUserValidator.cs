using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Validators;

namespace MelloSilveiraTools.WebApi.Application.Commands.Users.Register;

/// <summary>
/// Validates <see cref="RegisterUserRequest"/> payloads prior to user creation.
/// </summary>
public sealed class RegisterUserValidator : IValidator<RegisterUserRequest>
{
    /// <inheritdoc/>
    public ValueTask<Result> ValidateAsync(RegisterUserRequest value, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(value.Username) || value.Username.Trim().Length < 3)
            return ValueTask.FromResult(Result.CreateBadRequest("Username must contain at least 3 characters."));

        if (string.IsNullOrWhiteSpace(value.Password) || value.Password.Length < 6)
            return ValueTask.FromResult(Result.CreateBadRequest("Password must contain at least 6 characters."));

        return ValueTask.FromResult(Result.CreateSuccessOk());
    }
}
