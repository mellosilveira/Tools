using MelloSilveiraTools.Core.Models;

namespace MelloSilveiraTools.Core.Validators;

public interface IValidator<T>
{
    Result Validate(T value);
}
