using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartTasks.Api.Application;
using SmartTasks.Api.Application.Ai;
using SmartTasks.Api.Application.Tasks;
using SmartTasks.Api.Endpoints;
using SmartTasks.Api.Infrastructure;
using SmartTasks.Api.Infrastructure.Ai;

const string DevCorsPolicy = "DevFrontend";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<ISubTaskSuggestionService, SubTaskSuggestionService>();

builder.Services.AddOptions<OpenRouterOptions>()
    .Bind(builder.Configuration.GetSection(OpenRouterOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddHttpClient<IAiService, OpenRouterService>((sp, client) =>
{
    var openRouter = sp.GetRequiredService<IOptions<OpenRouterOptions>>().Value;
    client.BaseAddress = new Uri(openRouter.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(openRouter.TimeoutSeconds);
    client.DefaultRequestHeaders.Add("X-Title", "SmartTasks");
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddCors(options =>
    options.AddPolicy(DevCorsPolicy, policy => policy
        .WithOrigins("http://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors(DevCorsPolicy);
}

app.UseHttpsRedirection();

app.MapTaskEndpoints();

await app.RunAsync();
