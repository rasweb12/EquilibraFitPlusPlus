using System.Reflection;
using EquilibraFitPlusPlus.Shared.Errors;
using EquilibraFitPlusPlus.Shared.Results;
using FluentValidation;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Common.Behaviors;

/// <summary>
/// Validates MediatR requests before their handlers execute.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    /// <summary>
    /// Initializes the validation behavior.
    /// </summary>
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        var errors = validationResults
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .Select(failure => Error.Validation(failure.PropertyName, failure.ErrorMessage))
            .ToArray();

        if (errors.Length == 0)
        {
            return await next(cancellationToken);
        }

        return CreateFailureResponse(errors);
    }

    private static TResponse CreateFailureResponse(IReadOnlyCollection<Error> errors)
    {
        Type responseType = typeof(TResponse);

        if (responseType == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(errors);
        }

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            Type valueType = responseType.GetGenericArguments()[0];
            MethodInfo method = typeof(Result<>)
                .MakeGenericType(valueType)
                .GetMethod(nameof(Result<object>.Failure), BindingFlags.Public | BindingFlags.Static, [typeof(IEnumerable<Error>)])
                ?? throw new InvalidOperationException("Result failure factory was not found.");

            return (TResponse)method.Invoke(null, [errors])!;
        }

        throw new ValidationException(errors.Select(error => new FluentValidation.Results.ValidationFailure(error.Field, error.Message)));
    }
}
