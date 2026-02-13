using Autolife.AI.Services;
using Autolife.Core.Interfaces;
using Autolife.Core.Services;
using Autolife.Storage.Repositories;
using Autolife.Storage.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add Controllers for API endpoints
builder.Services.AddControllers();

// Configure HttpClient for Blazor Server
builder.Services.AddScoped(sp =>
{
    var navigationManager = sp.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
    return new HttpClient
    {
        BaseAddress = new Uri(navigationManager.BaseUri)
    };
});

// Register repositories (in-memory for now)
builder.Services.AddSingleton<IKnowledgeRepository, InMemoryKnowledgeRepository>();
builder.Services.AddSingleton<IDocumentRepository, InMemoryDocumentRepository>();
builder.Services.AddSingleton<IProjectRepository, InMemoryProjectRepository>();

// Register AI Provider Manager and Fallback AI Service
builder.Services.AddSingleton<IAIProviderManager, InMemoryAIProviderRepository>();
builder.Services.AddSingleton<IAiService, FallbackAiService>();

// Register Settings Manager
builder.Services.AddSingleton<ISettingsManager, InMemorySettingsManager>();

// Register business services
builder.Services.AddScoped<IKnowledgeService, KnowledgeService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IProjectService, ProjectService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Map API controllers
app.MapControllers();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
