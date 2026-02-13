using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Net.Http.Json;

namespace Autolife.Web.Components;

public partial class FolderBrowser : IDisposable
{
    [Inject] private HttpClient Http { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Parameter] public string Title { get; set; } = "Select Folder";
    [Parameter] public string InitialPath { get; set; } = "";
    [Parameter] public EventCallback<string> OnPathSelected { get; set; }
    [Parameter] public EventCallback OnCancelled { get; set; }

    private List<RootInfo> roots = new();
    private List<DirectoryInfo> directories = new();
    private string currentPath = "";
    private string parentPath = "";
    private string selectedPath = "";
    private string rootPath = "";
    private string rootPrefix = "";
    private bool isLoading = false;
    private bool showRootSelection = true;
    private bool canCreateFolder = false;
    private string newFolderName = "";
    private string errorMessage = "";
    private string errorSummary = "";
    private string errorDetails = "";
    private bool showErrorDetails = false;
    private bool createError = false;
    private string createErrorMessage = "";
    private string createErrorDetails = "";
    private string createStackTrace = "";
    private bool isWindows = false;
    private bool showCreateInline = false;
    private bool showErrorPopup = false;
    private bool showFullStackTrace = false;
    private ElementReference createInputRef;
    private System.Threading.Timer? hideErrorTimer;
    private bool _disposed = false;

    protected override async Task OnInitializedAsync()
    {
        isWindows = OperatingSystem.IsWindows();
        await LoadRoots();
        
        if (!string.IsNullOrEmpty(InitialPath) && System.IO.Directory.Exists(InitialPath))
        {
            await NavigateToPath(InitialPath);
        }
    }

    private async Task LoadRoots()
    {
        if (_disposed) return;

        isLoading = true;
        ClearError();
        try
        {
            var response = await Http.GetFromJsonAsync<List<RootInfo>>("/api/filesystem/roots");
            roots = response ?? new();
            
            if (!isWindows)
            {
                roots = roots.Where(r => r.Type != "Network").ToList();
            }
        }
        catch (Exception ex)
        {
            SetError("Failed to load drives", ex.Message);
        }
        finally
        {
            isLoading = false;
            if (!_disposed)
            {
                StateHasChanged();
            }
        }
    }

    private async Task SelectRoot(string path)
    {
        if (_disposed) return;

        if (path == "")
        {
            try
            {
                await NavigateToPath("\\\\");
            }
            catch
            {
                SetError("Network locations not accessible", "Cannot browse network. Ensure network access is enabled and you have permissions.");
            }
            return;
        }
        
        await NavigateToPath(path);
    }

    private void ShowRoots()
    {
        if (_disposed) return;

        showRootSelection = true;
        currentPath = "";
        selectedPath = "";
        rootPath = "";
        rootPrefix = "";
        directories.Clear();
        canCreateFolder = false;
        showCreateInline = false;
        ClearError();
        ClearCreateError();
        StateHasChanged();
    }

    private async Task NavigateToRoot()
    {
        if (_disposed) return;

        if (!string.IsNullOrEmpty(rootPath))
        {
            await NavigateToPath(rootPath);
        }
    }

    private async Task NavigateToPath(string path)
    {
        if (_disposed) return;

        if (path == currentPath)
        {
            return;
        }

        isLoading = true;
        ClearError();
        ClearCreateError();
        showRootSelection = false;
        showCreateInline = false;

        try
        {
            var response = await Http.GetFromJsonAsync<BrowseResponse>($"/api/filesystem/browse?path={Uri.EscapeDataString(path)}");
            if (response != null)
            {
                currentPath = response.CurrentPath;
                parentPath = response.ParentPath ?? "";
                directories = response.Directories;
                selectedPath = currentPath;
                canCreateFolder = response.CanCreateFolder;
                
                // Build ALL segments (including root)
                var segments = BuildAllSegments(currentPath);
                
                // Send to client-side JavaScript for rendering
                await UpdateClientBreadcrumb(segments);
            }
        }
        catch (Exception ex)
        {
            SetError("Cannot access directory", ex.Message);
        }
        finally
        {
            isLoading = false;
            if (!_disposed)
            {
                StateHasChanged();
            }
        }
    }

    private List<BreadcrumbSegment> BuildAllSegments(string fullPath)
    {
        var segments = new List<BreadcrumbSegment>();
        
        if (string.IsNullOrEmpty(fullPath))
        {
            return segments;
        }

        // Windows drive letter (C:, D:, etc.)
        if (isWindows && fullPath.Length >= 2 && fullPath[1] == ':')
        {
            var driveLetter = fullPath.Substring(0, 2).ToUpper();
            segments.Add(new BreadcrumbSegment
            {
                Label = driveLetter,
                Path = driveLetter + "\\"
            });
            
            var remaining = fullPath.Substring(2).TrimStart('\\', '/');
            if (!string.IsNullOrEmpty(remaining))
            {
                AddPathParts(segments, remaining, driveLetter + "\\");
            }
        }
        // Windows network path (\\server\share)
        else if (isWindows && fullPath.StartsWith("\\\\"))
        {
            var parts = fullPath.Substring(2).Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length >= 1)
            {
                // Server
                segments.Add(new BreadcrumbSegment
                {
                    Label = parts[0],
                    Path = $"\\\\{parts[0]}"
                });
            }
            
            if (parts.Length >= 2)
            {
                // Share
                segments.Add(new BreadcrumbSegment
                {
                    Label = parts[1],
                    Path = $"\\\\{parts[0]}\\{parts[1]}"
                });
            }
            
            if (parts.Length > 2)
            {
                // Remaining path
                var basePath = $"\\\\{parts[0]}\\{parts[1]}";
                var remaining = string.Join("\\", parts.Skip(2));
                AddPathParts(segments, remaining, basePath + "\\");
            }
        }
        // Unix/Linux path (/home/user/...)
        else if (fullPath.StartsWith("/"))
        {
            segments.Add(new BreadcrumbSegment
            {
                Label = "/",
                Path = "/"
            });
            
            var remaining = fullPath.Substring(1);
            if (!string.IsNullOrEmpty(remaining))
            {
                AddPathParts(segments, remaining, "/");
            }
        }

        return segments;
    }

    private void AddPathParts(List<BreadcrumbSegment> segments, string remaining, string basePath)
    {
        var parts = remaining.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
        var accumulated = basePath;
        
        foreach (var part in parts)
        {
            accumulated += part;
            segments.Add(new BreadcrumbSegment
            {
                Label = part,
                Path = accumulated
            });
            accumulated += System.IO.Path.DirectorySeparatorChar;
        }
    }

    private async Task UpdateClientBreadcrumb(List<BreadcrumbSegment> segments)
    {
        try
        {
            var pathData = new
            {
                segments = segments.Select(s => new { label = s.Label, path = s.Path }).ToArray()
            };
            
            await JS.InvokeVoidAsync("breadcrumbManager.updateBreadcrumb", pathData);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating breadcrumb: {ex.Message}");
        }
    }

    private async Task SelectDirectory(string path)
    {
        if (_disposed) return;

        selectedPath = path;
        await NavigateToPath(path);
    }

    private async Task ShowInlineCreate()
    {
        if (_disposed) return;

        showCreateInline = true;
        newFolderName = "";
        ClearCreateError();
        StateHasChanged();
        
        try
        {
            await JS.InvokeAsync<object>("eval", "setTimeout(function() { var input = document.querySelector('.inline-create-input'); if(input) input.focus(); }, 100)");
        }
        catch { }
    }

    private void CancelInlineCreate()
    {
        if (_disposed) return;

        showCreateInline = false;
        newFolderName = "";
        ClearCreateError();
        StateHasChanged();
    }

    private async Task HandleCreateFolderKeyUp(KeyboardEventArgs e)
    {
        if (_disposed) return;

        if (e.Key == "Enter" && !string.IsNullOrWhiteSpace(newFolderName))
        {
            await CreateFolder();
        }
        else if (e.Key == "Escape")
        {
            CancelInlineCreate();
        }
    }

    private async Task CreateFolder()
    {
        if (_disposed) return;
        if (string.IsNullOrWhiteSpace(newFolderName)) return;

        ClearCreateError();

        try
        {
            var response = await Http.PostAsJsonAsync("/api/filesystem/create", new
            {
                ParentPath = currentPath,
                Name = newFolderName
            });

            if (response.IsSuccessStatusCode)
            {
                showCreateInline = false;
                newFolderName = "";
                await NavigateToPath(currentPath);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                SetCreateError("Cannot create folder", errorContent, "HTTP " + response.StatusCode);
                if (!_disposed)
                {
                    StateHasChanged();
                }
            }
        }
        catch (Exception ex)
        {
            SetCreateError("Creation failed", ex.Message, ex.StackTrace ?? "No stack trace available");
            if (!_disposed)
            {
                StateHasChanged();
            }
        }
    }

    private void SetCreateError(string message, string details, string stackTrace)
    {
        createError = true;
        createErrorMessage = message;
        createErrorDetails = details;
        createStackTrace = stackTrace;
    }

    private void ClearCreateError()
    {
        createError = false;
        createErrorMessage = "";
        createErrorDetails = "";
        createStackTrace = "";
        showErrorPopup = false;
        showFullStackTrace = false;
        hideErrorTimer?.Dispose();
        hideErrorTimer = null;
    }

    private void ShowErrorDetails()
    {
        if (_disposed) return;

        if (createError)
        {
            hideErrorTimer?.Dispose();
            hideErrorTimer = null;
            showErrorPopup = true;
            StateHasChanged();
        }
    }

    private void KeepErrorDetailsVisible()
    {
        hideErrorTimer?.Dispose();
        hideErrorTimer = null;
    }

    private void StartHideErrorTimer()
    {
        if (_disposed) return;

        hideErrorTimer = new System.Threading.Timer(_ =>
        {
            InvokeAsync(() =>
            {
                if (!_disposed)
                {
                    showErrorPopup = false;
                    showFullStackTrace = false;
                    StateHasChanged();
                }
            });
        }, null, 150, System.Threading.Timeout.Infinite);
    }

    private void HideErrorDetails()
    {
        if (_disposed) return;

        showErrorPopup = false;
        showFullStackTrace = false;
        hideErrorTimer?.Dispose();
        hideErrorTimer = null;
        StateHasChanged();
    }

    private void ToggleStackTrace()
    {
        if (_disposed) return;

        showFullStackTrace = !showFullStackTrace;
        StateHasChanged();
    }

    private void SetError(string summary, string details)
    {
        errorSummary = summary;
        errorDetails = details;
        errorMessage = summary;
        showErrorDetails = false;
    }

    private void ClearError()
    {
        errorMessage = "";
        errorSummary = "";
        errorDetails = "";
        showErrorDetails = false;
    }

    private void ToggleErrorDetails()
    {
        if (_disposed) return;

        showErrorDetails = !showErrorDetails;
    }

    private async Task ConfirmSelection()
    {
        if (_disposed) return;

        if (!string.IsNullOrEmpty(selectedPath))
        {
            await OnPathSelected.InvokeAsync(selectedPath);
        }
    }

    private async Task HandleCancel()
    {
        if (_disposed) return;

        await OnCancelled.InvokeAsync();
    }

    public void Dispose()
    {
        _disposed = true;
        hideErrorTimer?.Dispose();
        hideErrorTimer = null;
    }

    private class RootInfo
    {
        public string Path { get; set; } = "";
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
    }

    private class DirectoryInfo
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public DateTime LastModified { get; set; }
        public bool IsAccessible { get; set; }
    }

    private class BrowseResponse
    {
        public string CurrentPath { get; set; } = "";
        public string? ParentPath { get; set; }
        public List<DirectoryInfo> Directories { get; set; } = new();
        public bool CanCreateFolder { get; set; }
    }

    private class BreadcrumbSegment
    {
        public string Label { get; set; } = "";
        public string Path { get; set; } = "";
    }
}
