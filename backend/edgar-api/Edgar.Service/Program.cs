using System.Net.WebSockets;
using Edgar.Service;
using Edgar.Service.Components;
using Edgar.Service.Sessions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebSockets;
using Serilog;

var builder = WebApplication.CreateBuilder(args);


builder.Host.UseSerilog((ctx, cfg) =>
    cfg
        .WriteTo.Console(
            theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code)
        .ReadFrom.Configuration(ctx.Configuration));

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
var wsSettings = builder.Configuration.GetSection("WebSockets").Get<WebSocketSettings>() ??
                 throw new Exception("WebSockets settings not found");

// builder.Services.AddOpenApi();
builder.Services.AddWebSockets(c =>
{
    c.KeepAliveInterval = TimeSpan.FromSeconds(wsSettings.KeepAliveIntervalSeconds);
    c.KeepAliveTimeout = TimeSpan.FromSeconds(wsSettings.KeepAliveTimeoutSeconds);
    foreach (var origin in wsSettings.AllowedOrigins)
        c.AllowedOrigins.Add(origin);
});
builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddScoped<ILlmService, LlmService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<ISessionRepository, InMemorySessionRepository>();
// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}


app.MapGet("/ws", async (
        [FromQuery(Name = "sessionId")] Guid sessionId,
        [FromServices] IHttpContextAccessor contextAccessor,
        [FromServices] IServiceProvider serviceProvider,
        [FromServices] ISessionService sessionService,
        CancellationToken cancellationToken = default
    ) =>
    {
        if (contextAccessor.HttpContext == null)
            throw new Exception("Context is null");

        if (!contextAccessor.HttpContext.WebSockets.IsWebSocketRequest)
            return Results.BadRequest();

        var ws = await contextAccessor.HttpContext.WebSockets.AcceptWebSocketAsync();

        var session = await sessionService.GetSessionByIdAsync(sessionId, cancellationToken);
        if (session == null)
        {
            return Results.NotFound();
        }

        using var scope = serviceProvider.CreateScope();
        
        using var manager = new SessionManager(session, scope.ServiceProvider);

        await manager.LoopAsync(ws, cancellationToken);

        return Results.Empty;
    })
    .WithName("WebSockets");

var api = app.MapGroup("api");
var sessionsGroup = api.MapGroup("sessions");
sessionsGroup.MapPost("begin",
    async ([FromServices] ISessionService sessionService, CancellationToken cancellationToken = default) =>
    {
        var session = await sessionService.CreateSessionAsync(cancellationToken);
        return Results.Created($"/api/sessions/{session.Id}", session);
    });

sessionsGroup.MapDelete("{sessionId:guid}/end",
    async ([FromRoute(Name = "sessionId")] Guid sessionId, [FromServices] ISessionService sessionService,
        CancellationToken token = default) =>
    {
        await sessionService.DeleteSessionAsync(sessionId, token);
        return Results.NoContent();
    });

sessionsGroup.MapGet("{sessionId:guid}",
        async ([FromServices] ISessionService sessionService,
            [FromRoute(Name = "sessionId")] Guid sessionId,
            CancellationToken token = default) =>
        {
            var session = await sessionService.GetSessionByIdAsync(sessionId, token);
            if (session == null)
                return Results.NotFound();


            return Results.Ok(session);
        })
    .WithName("GetSessionById");


app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();