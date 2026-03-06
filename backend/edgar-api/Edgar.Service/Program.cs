using System.Net.WebSockets;
using Edgar.Service;
using Edgar.Service.Components;
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


// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}



app.MapGet("/ws", async ([FromServices] IHttpContextAccessor contextAccessor) =>
    {
        if (contextAccessor.HttpContext == null)
            throw new Exception("Context is null");

        if (!contextAccessor.HttpContext.WebSockets.IsWebSocketRequest)
            return Results.BadRequest();

        var ws = await contextAccessor.HttpContext.WebSockets.AcceptWebSocketAsync();

        while (ws.State == WebSocketState.Open)
        {
            await Task.Delay(1000);
        }

        return Results.Empty;
    })
    .WithName("WebSockets");

var api = app.MapGroup("api");

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();