using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Validators;

namespace MelloSilveiraTools.WebApi.Application.Commands.Users.Login;

/// <summary>
/// Validates <see cref="LoginRequest"/> payloads prior to authentication.
/// </summary>
public sealed class LoginValidator : IValidator<LoginRequest>
{
    /// <inheritdoc/>
    public Result Validate(LoginRequest value)
    {
        if (string.IsNullOrWhiteSpace(value.Username))
            return Result.CreateBadRequest("Username is required.");

        if (string.IsNullOrWhiteSpace(value.Password))
            return Result.CreateBadRequest("Password is required.");

        return Result.CreateSuccessOk();
    }
}
