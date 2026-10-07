using EquilibraFitPlusPlus.Shared.Errors;

namespace EquilibraFitPlusPlus.Shared.Results;

/// <summary>
/// Represents the result of an operation without a return value.
/// </summary>
public class Result
{
    private readonly List<Error> _errors;

    /// <summary>
    /// Initializes a result instance.
    /// </summary>
    protected Result(bool isSuccess, IEnumerable<Error>? errors = null)
    {
        IsSuccess = isSuccess;
        _errors = errors?.ToList() ?? [];
    }

    /// <summary>
    /// Indicates whether the operation completed successfully.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Indicates whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Errors collected during the operation.
    /// </summary>
    public IReadOnlyCollection<Error> Errors => _errors;

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Result Success() => new(true);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static Result Failure(IEnumerable<Error> errors) => new(false, errors);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static Result Failure(Error error) => new(false, [error]);
}

/// <summary>
/// Represents the result of an operation with a return value.
/// </summary>
public sealed class Result<T> : Result
{
    private Result(bool isSuccess, T? value, IEnumerable<Error>? errors = null)
        : base(isSuccess, errors)
    {
        Value = value;
    }

    /// <summary>
    /// Returned value when the operation succeeds.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Result<T> Success(T value) => new(true, value);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static new Result<T> Failure(IEnumerable<Error> errors) => new(false, default, errors);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static new Result<T> Failure(Error error) => new(false, default, [error]);
}
