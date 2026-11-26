using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Validators;

namespace MelloSilveiraTools.WebApi.Application.Commands.Users.Login;

/// <summary>
/// Validates <see cref="LoginRequest"/> payloads prior to authentication.
/// </summary>
public sealed class LoginValidator : IValidator<LoginRequest>
{
    /// <inheritdoc/>
    public ValueTask<Result> ValidateAsync(LoginRequest value, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(value.Username))
            return ValueTask.FromResult(Result.CreateBadRequest("Username is required."));

        if (string.IsNullOrWhiteSpace(value.Password))
            return ValueTask.FromResult(Result.CreateBadRequest("Password is required."));

        return ValueTask.FromResult(Result.CreateSuccessOk());
    }
}
