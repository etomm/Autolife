using Autolife.AI.Agents;
using Autolife.AI.Interfaces;
using Autolife.AI.Providers;
using Autolife.Core.Interfaces;
using Autolife.Storage;
using Autolife.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Configure storage path
var storagePath = builder.Configuration.GetValue<string>("StoragePath") ?? 
    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".autolife", "content");

// Register services
builder.Services.AddSingleton(new GitStorageService(storagePath));
builder.Services.AddSingleton<IAIProvider, MockAIProvider>();
builder.Services.AddSingleton<IKnowledgeService, KnowledgeService>();
builder.Services.AddSingleton<IProjectService, ProjectService>();
builder.Services.AddSingleton<IDocumentService, DocumentService>();

// Register AI agents
builder.Services.AddTransient<KnowledgeOrganizationAgent>();
builder.Services.AddTransient<DocumentSummaryAgent>();

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
