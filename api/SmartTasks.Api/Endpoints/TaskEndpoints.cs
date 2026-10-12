using Microsoft.AspNetCore.Http.HttpResults;
using SmartTasks.Api.Application.Tasks;
using SmartTasks.Api.Domain;

namespace SmartTasks.Api.Endpoints;

public static class TaskEndpoints
{
    private const string GetTaskByIdRoute = "GetTaskById";

    public static IEndpointRouteBuilder MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tasks")
            .WithTags("Tasks")
            .WithOpenApi();

        group.MapGet("/", GetAll);

        group.MapGet("/{id:int}", GetById)
            .WithName(GetTaskByIdRoute);

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateTaskRequest>>()
            .ProducesValidationProblem();

        group.MapPut("/{id:int}", Update)
            .AddEndpointFilter<ValidationFilter<UpdateTaskRequest>>()
            .ProducesValidationProblem();

        group.MapDelete("/{id:int}", Delete);

        group.MapPatch("/{id:int}/toggle", Toggle);

        group.MapPost("/{id:int}/subtasks", AddSubTask)
            .AddEndpointFilter<ValidationFilter<CreateSubTaskRequest>>()
            .ProducesValidationProblem();

        group.MapPatch("/{id:int}/subtasks/{subTaskId:int}/toggle", ToggleSubTask);

        group.MapPost("/{id:int}/suggest-subtasks", SuggestSubTasks)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        return app;
    }

    private static async Task<Ok<IReadOnlyList<TaskResponse>>> GetAll(
        TaskStatusFilter? status, Priority? priority, ITaskService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetAllAsync(status, priority, cancellationToken));

    private static async Task<Results<Ok<TaskResponse>, NotFound>> GetById(
        int id, ITaskService service, CancellationToken cancellationToken) =>
        await service.GetByIdAsync(id, cancellationToken) is { } task
            ? TypedResults.Ok(task)
            : TypedResults.NotFound();

    private static async Task<CreatedAtRoute<TaskResponse>> Create(
        CreateTaskRequest request, ITaskService service, CancellationToken cancellationToken)
    {
        var task = await service.CreateAsync(request, cancellationToken);
        return TypedResults.CreatedAtRoute(task, GetTaskByIdRoute, new { id = task.Id });
    }

    private static async Task<Results<Ok<TaskResponse>, NotFound>> Update(
        int id, UpdateTaskRequest request, ITaskService service, CancellationToken cancellationToken) =>
        await service.UpdateAsync(id, request, cancellationToken) is { } task
            ? TypedResults.Ok(task)
            : TypedResults.NotFound();

    private static async Task<Results<NoContent, NotFound>> Delete(
        int id, ITaskService service, CancellationToken cancellationToken) =>
        await service.DeleteAsync(id, cancellationToken)
            ? TypedResults.NoContent()
            : TypedResults.NotFound();

    private static async Task<Results<Ok<TaskResponse>, NotFound>> Toggle(
        int id, ITaskService service, CancellationToken cancellationToken) =>
        await service.ToggleAsync(id, cancellationToken) is { } task
            ? TypedResults.Ok(task)
            : TypedResults.NotFound();

    private static async Task<Results<CreatedAtRoute<SubTaskResponse>, NotFound>> AddSubTask(
        int id, CreateSubTaskRequest request, ITaskService service, CancellationToken cancellationToken) =>
        await service.AddSubTaskAsync(id, request, cancellationToken) is { } subTask
            ? TypedResults.CreatedAtRoute(subTask, GetTaskByIdRoute, new { id })
            : TypedResults.NotFound();

    private static async Task<Results<Ok<SubTaskResponse>, NotFound>> ToggleSubTask(
        int id, int subTaskId, ITaskService service, CancellationToken cancellationToken) =>
        await service.ToggleSubTaskAsync(id, subTaskId, cancellationToken) is { } subTask
            ? TypedResults.Ok(subTask)
            : TypedResults.NotFound();

    private static async Task<Results<Ok<SubTaskSuggestionsResponse>, NotFound>> SuggestSubTasks(
        int id, ISubTaskSuggestionService service, CancellationToken cancellationToken) =>
        await service.SuggestAsync(id, cancellationToken) is { } suggestions
            ? TypedResults.Ok(suggestions)
            : TypedResults.NotFound();
}
