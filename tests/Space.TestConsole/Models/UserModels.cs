using Space.Abstraction.Contracts;

namespace Space.TestConsole;

public record UserCreateResponse(string Id);

public record UserCreateCommand : IRequest<UserCreateResponse>, IValidatable
{
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;

    public IEnumerable<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Name is required");

        if (string.IsNullOrWhiteSpace(Email))
            errors.Add("Email is required");
        else if (!Email.Contains("@"))
            errors.Add("Email must be a valid email address");

        return errors;
    }
}

public interface IValidatable
{
    IEnumerable<string> Validate();
}

public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}
