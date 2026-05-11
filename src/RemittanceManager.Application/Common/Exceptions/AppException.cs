namespace RemittanceManager.Application.Common.Exceptions;

/// <summary>Base type for application-level exceptions translated to HTTP responses by the API middleware.</summary>
public abstract class AppException : Exception
{
    protected AppException(string message) : base(message) { }
}

public sealed class ValidationException : AppException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(string message, IReadOnlyDictionary<string, string[]>? errors = null)
        : base(message)
    {
        Errors = errors ?? new Dictionary<string, string[]>();
    }
}

public sealed class ConflictException : AppException
{
    public ConflictException(string message) : base(message) { }
}

public sealed class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message) { }
}

public sealed class UnauthorizedException : AppException
{
    public UnauthorizedException(string message) : base(message) { }
}
