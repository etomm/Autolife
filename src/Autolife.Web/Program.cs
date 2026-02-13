using Autolife.AI.Services;
using Autolife.Core.Interfaces;
using Autolife.Core.Services;
using Autolife.Storage.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Register repositories (in-memory for now)
builder.Services.AddSingleton<IKnowledgeRepository, InMemoryKnowledgeRepository>();
builder.Services.AddSingleton<IDocumentRepository, InMemoryDocumentRepository>();
builder.Services.AddSingleton<IProjectRepository, InMemoryProjectRepository>();

// Register AI service
builder.Services.AddSingleton<IAiService, MockAiService>();

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

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
