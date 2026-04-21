using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using WebApp.Components;
using WebApp.Data;
using WebApp.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AgenticApiOptions>(
    builder.Configuration.GetSection(AgenticApiOptions.SectionName)
);
builder.Services.AddHttpClient();
builder.Services.AddScoped<AgenticApiClient>();
builder.Services.AddScoped<Repository>();
builder.Services.AddScoped<Service>();
builder.Services.AddScoped<ProtectedLocalStorage>();
builder.Services.AddScoped<MockSessionPersistence>();

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapLangChainApi();
app
    .MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
