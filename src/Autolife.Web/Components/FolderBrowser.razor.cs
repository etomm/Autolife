using Microsoft.AspNetCore.Components;
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
    private string rootPrefix = "";
    private string rootPath = "";
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
    private ElementReference breadcrumbNavRef;
    private ElementReference breadcrumbNavSecondaryRef;
    private ElementReference breadcrumbContentSecondaryRef;
    private System.Threading.Timer? hideErrorTimer;

    // Dual breadcrumb system
    private bool showPrimaryBreadcrumb = true;
    private bool needsCalculation = false;
    private bool isCalculating = false;

    // Primary breadcrumb state
    private List<(string segment, string path)> pathSegments = new();
    private List<(string segment, string path)> visibleLeadingSegments = new();
    private (string segment, string path) lastSegment = ("", "");
    private bool showEllipsis = false;

    // Secondary breadcrumb state (for pre-calculation)
    private string secondaryRootPrefix = "";
    private List<(string segment, string path)> secondaryPathSegments = new();
    private List<(string segment, string path)> secondaryVisibleLeadingSegments = new();
    private (string segment, string path) secondaryLastSegment = ("", "");
    private bool secondaryShowEllipsis = false;

    protected override async Task OnInitializedAsync()
    {
        isWindows = OperatingSystem.IsWindows();
        await LoadRoots();
        
        if (!string.IsNullOrEmpty(InitialPath) && System.IO.Directory.Exists(InitialPath))
        {
            await NavigateToPath(InitialPath);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!showRootSelection && needsCalculation)
        {
            await CalculateAndSwapBreadcrumb();
            needsCalculation = false;
            isCalculating = false;
            StateHasChanged();
        }
    }

    private async Task CalculateAndSwapBreadcrumb()
    {
        try
        {
            await Task.Delay(10);

            // Measure container width (fixed by path box)
            var containerWidth = await JS.InvokeAsync<double>("eval", @"
                (function() {
                    var containers = document.querySelectorAll('.breadcrumb-nav');
                    for (var i = 0; i < containers.length; i++) {
                        if (containers[i].classList.contains('hidden')) {
                            return containers[i].offsetWidth;
                        }
                    }
                    return 0;
                })()
            ");

            // Measure content width (grows with content)
            var contentWidth = await JS.InvokeAsync<double>("eval", @"
                (function() {
                    var contents = document.querySelectorAll('.breadcrumb-content');
                    for (var i = 0; i < contents.length; i++) {
                        if (contents[i].parentElement.classList.contains('hidden')) {
                            return contents[i].scrollWidth;
                        }
                    }
                    return 0;
                })()
            ");
            
            if (containerWidth <= 0 || contentWidth <= 0)
            {
                secondaryShowEllipsis = false;
                secondaryVisibleLeadingSegments = secondaryPathSegments.ToList();
                SwapBreadcrumbs();
                return;
            }

            var overflow = contentWidth - containerWidth;
            Console.WriteLine($"Container: {containerWidth}px, Content: {contentWidth}px, Overflow: {overflow}px");

            if (overflow <= 10)
            {
                secondaryShowEllipsis = false;
                secondaryVisibleLeadingSegments = secondaryPathSegments.ToList();
            }
            else
            {
                if (secondaryPathSegments.Count <= 1)
                {
                    secondaryShowEllipsis = false;
                    secondaryVisibleLeadingSegments = secondaryPathSegments.ToList();
                }
                else
                {
                    // Measure individual segment widths
                    var segmentWidths = new List<double>();
                    for (int i = 0; i < secondaryPathSegments.Count - 1; i++)
                    {
                        var width = await JS.InvokeAsync<double>("eval", $@"
                            (function() {{
                                var seg = document.querySelector('[data-measure-segment=""{i}""]');
                                if (!seg) return 0;
                                var sep = seg.previousElementSibling;
                                return seg.offsetWidth + (sep && sep.hasAttribute('data-measure-sep') ? sep.offsetWidth : 0);
                            }})()
                        ");
                        segmentWidths.Add(width);
                    }

                    var ellipsisWidth = 50.0;
                    var targetRemoval = overflow + ellipsisWidth;
                    var accumulatedRemoval = 0.0;
                    var keepCount = 0;

                    for (int i = segmentWidths.Count - 1; i >= 0; i--)
                    {
                        if (accumulatedRemoval < targetRemoval)
                        {
                            accumulatedRemoval += segmentWidths[i];
                        }
                        else
                        {
                            keepCount = i + 1;
                            break;
                        }
                    }

                    Console.WriteLine($"Need to remove {targetRemoval}px, keeping {keepCount} segments");

                    secondaryShowEllipsis = true;
                    secondaryVisibleLeadingSegments = keepCount > 0 
                        ? secondaryPathSegments.Take(keepCount).ToList() 
                        : new List<(string, string)>();
                }
            }

            SwapBreadcrumbs();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Breadcrumb calculation error: {ex.Message}");
            secondaryShowEllipsis = false;
            secondaryVisibleLeadingSegments = secondaryPathSegments.ToList();
            SwapBreadcrumbs();
        }
    }

    private void SwapBreadcrumbs()
    {
        showPrimaryBreadcrumb = !showPrimaryBreadcrumb;
        rootPrefix = secondaryRootPrefix;
        pathSegments = secondaryPathSegments.ToList();
        visibleLeadingSegments = secondaryVisibleLeadingSegments.ToList();
        lastSegment = secondaryLastSegment;
        showEllipsis = secondaryShowEllipsis;
        secondaryRootPrefix = "";
        secondaryPathSegments.Clear();
        secondaryVisibleLeadingSegments.Clear();
        secondaryLastSegment = ("", "");
        secondaryShowEllipsis = false;
    }

    private async Task LoadRoots()
    {
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
            StateHasChanged();
        }
    }

    private async Task SelectRoot(string path)
    {
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
        showRootSelection = true;
        currentPath = "";
        selectedPath = "";
        rootPrefix = "";
        rootPath = "";
        directories.Clear();
        canCreateFolder = false;
        pathSegments.Clear();
        visibleLeadingSegments.Clear();
        lastSegment = ("", "");
        showEllipsis = false;
        secondaryPathSegments.Clear();
        showCreateInline = false;
        needsCalculation = false;
        isCalculating = false;
        ClearError();
        ClearCreateError();
        StateHasChanged();
    }

    private async Task NavigateToRoot()
    {
        if (!string.IsNullOrEmpty(rootPath))
        {
            await NavigateToPath(rootPath);
        }
    }

    private async Task NavigateToPath(string path)
    {
        if (path == currentPath)
        {
            return;
        }

        isLoading = true;
        isCalculating = true;
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
                
                UpdateRootInfo(currentPath, out secondaryRootPrefix);
                BuildPathSegments(currentPath, out secondaryPathSegments, out secondaryLastSegment);

                needsCalculation = true;
            }
        }
        catch (Exception ex)
        {
            SetError("Cannot access directory", ex.Message);
            needsCalculation = false;
            isCalculating = false;
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private void UpdateRootInfo(string path, out string outRootPrefix)
    {
        if (string.IsNullOrEmpty(path))
        {
            outRootPrefix = "";
            rootPath = "";
            return;
        }

        if (isWindows && path.StartsWith("\\\\"))
        {
            outRootPrefix = "Network";
            var parts = path.Substring(2).Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                rootPath = $"\\\\{parts[0]}\\{parts[1]}";
            }
            else if (parts.Length == 1)
            {
                rootPath = $"\\\\{parts[0]}";
            }
            else
            {
                rootPath = "\\\\";
            }
        }
        else if (isWindows && path.Length >= 2 && path[1] == ':')
        {
            outRootPrefix = path.Substring(0, 2).ToUpper();
            rootPath = outRootPrefix + "\\";
        }
        else if (path.StartsWith("/"))
        {
            outRootPrefix = "/";
            rootPath = "/";
        }
        else
        {
            outRootPrefix = "";
            rootPath = "";
        }
    }

    private void BuildPathSegments(string currentPathToBuild, out List<(string segment, string path)> segments, out (string segment, string path) last)
    {
        segments = new();
        last = ("", "");

        if (string.IsNullOrEmpty(currentPathToBuild))
        {
            return;
        }

        var path = currentPathToBuild;
        
        if (isWindows && path.Length >= 2 && path[1] == ':')
        {
            path = path.Substring(2).TrimStart('\\', '/');
        }
        else if (isWindows && path.StartsWith("\\\\"))
        {
            var parts = path.Substring(2).Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 2)
            {
                path = string.Join("\\", parts.Skip(2));
            }
            else
            {
                path = "";
            }
        }
        else if (path.StartsWith("/"))
        {
            path = path.Substring(1);
        }

        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        var pathParts = path.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
        var accumulated = currentPathToBuild.Substring(0, currentPathToBuild.Length - path.Length);

        foreach (var part in pathParts)
        {
            accumulated += part + System.IO.Path.DirectorySeparatorChar;
            segments.Add((part, accumulated.TrimEnd(System.IO.Path.DirectorySeparatorChar)));
        }

        if (segments.Count > 0)
        {
            last = segments[^1];
        }
    }

    private async Task NavigateUpOneFolder()
    {
        if (pathSegments.Count >= 2)
        {
            var parentSegment = pathSegments[pathSegments.Count - 2];
            await NavigateToPath(parentSegment.path);
        }
        else if (pathSegments.Count == 1)
        {
            await NavigateToRoot();
        }
    }

    private async Task SelectDirectory(string path)
    {
        selectedPath = path;
        await NavigateToPath(path);
    }

    private async Task ShowInlineCreate()
    {
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
        showCreateInline = false;
        newFolderName = "";
        ClearCreateError();
        StateHasChanged();
    }

    private async Task HandleCreateFolderKeyUp(KeyboardEventArgs e)
    {
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
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            SetCreateError("Creation failed", ex.Message, ex.StackTrace ?? "No stack trace available");
            StateHasChanged();
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
        hideErrorTimer = new System.Threading.Timer(_ =>
        {
            InvokeAsync(() =>
            {
                showErrorPopup = false;
                showFullStackTrace = false;
                StateHasChanged();
            });
        }, null, 150, System.Threading.Timeout.Infinite);
    }

    private void HideErrorDetails()
    {
        showErrorPopup = false;
        showFullStackTrace = false;
        hideErrorTimer?.Dispose();
        hideErrorTimer = null;
        StateHasChanged();
    }

    private void ToggleStackTrace()
    {
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
        showErrorDetails = !showErrorDetails;
    }

    private async Task ConfirmSelection()
    {
        if (!string.IsNullOrEmpty(selectedPath))
        {
            await OnPathSelected.InvokeAsync(selectedPath);
        }
    }

    private async Task HandleCancel()
    {
        await OnCancelled.InvokeAsync();
    }

    public void Dispose()
    {
        hideErrorTimer?.Dispose();
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
}
