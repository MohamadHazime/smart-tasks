using System.ComponentModel.DataAnnotations;

namespace SmartTasks.Api.Endpoints;

public sealed class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<T>().FirstOrDefault();
        if (request is null)
            return TypedResults.Problem("Request body is required.", statusCode: StatusCodes.Status400BadRequest);

        var results = new List<ValidationResult>();
        if (Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true))
            return await next(context);

        var errors = results
            .SelectMany(r => r.MemberNames.DefaultIfEmpty(string.Empty),
                (result, member) => (Member: member, Message: result.ErrorMessage ?? "Invalid value."))
            .GroupBy(e => e.Member)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Message).ToArray());

        return TypedResults.ValidationProblem(errors);
    }
}
