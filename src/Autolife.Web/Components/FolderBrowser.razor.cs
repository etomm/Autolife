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
    private bool _disposed = false;

    // Double-buffering breadcrumb system
    private bool primaryVisible = true; // Which breadcrumb is currently visible
    private bool needsCalculation = false;
    private bool isCalculating = false;

    // Primary breadcrumb state
    private List<(string segment, string path)> primaryPathSegments = new();
    private List<(string segment, string path)> primaryVisibleLeadingSegments = new();
    private (string segment, string path) primaryLastSegment = ("", "");
    private bool primaryShowEllipsis = false;
    private string primaryRootPrefix = "";

    // Secondary breadcrumb state
    private List<(string segment, string path)> secondaryPathSegments = new();
    private List<(string segment, string path)> secondaryVisibleLeadingSegments = new();
    private (string segment, string path) secondaryLastSegment = ("", "");
    private bool secondaryShowEllipsis = false;
    private string secondaryRootPrefix = "";

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
        if (_disposed) return;

        if (!showRootSelection && needsCalculation)
        {
            await CalculateAndSwapBreadcrumb();
            needsCalculation = false;
            isCalculating = false;
            
            if (!_disposed)
            {
                StateHasChanged();
            }
        }
    }

    private async Task CalculateAndSwapBreadcrumb()
    {
        if (_disposed) return;

        try
        {
            Console.WriteLine("=== BREADCRUMB CALCULATION START ===");
            Console.WriteLine($"Primary visible: {primaryVisible}");
            
            await Task.Delay(10);

            if (_disposed) return;

            // Measure container width (fixed by path box)
            var containerWidth = await JS.InvokeAsync<double>("breadcrumbMeasure.getContainerWidth");
            Console.WriteLine($"Container width: {containerWidth}px");

            if (_disposed) return;

            // Measure content width from HIDDEN breadcrumb (being prepared)
            var contentWidth = await JS.InvokeAsync<double>("breadcrumbMeasure.getContentWidth");
            Console.WriteLine($"Content width: {contentWidth}px");
            
            if (_disposed) return;

            // Get references to which is which
            var hiddenSegments = primaryVisible ? secondaryPathSegments : primaryPathSegments;
            Console.WriteLine($"Hidden breadcrumb has {hiddenSegments.Count} segments");
            for (int i = 0; i < hiddenSegments.Count; i++)
            {
                Console.WriteLine($"  Segment {i}: {hiddenSegments[i].segment}");
            }

            if (containerWidth <= 0 || contentWidth <= 0)
            {
                Console.WriteLine("⚠️ Invalid measurements, showing everything");
                // No calculation possible, just show everything
                if (primaryVisible)
                {
                    secondaryShowEllipsis = false;
                    secondaryVisibleLeadingSegments = secondaryPathSegments.ToList();
                }
                else
                {
                    primaryShowEllipsis = false;
                    primaryVisibleLeadingSegments = primaryPathSegments.ToList();
                }
                SwapBreadcrumbs();
                Console.WriteLine("=== BREADCRUMB CALCULATION END (invalid measurements) ===");
                return;
            }

            var overflow = contentWidth - containerWidth;
            Console.WriteLine($"Overflow: {overflow}px (container: {containerWidth}px, content: {contentWidth}px)");

            // Small margin for safety
            if (overflow <= 10)
            {
                Console.WriteLine("✅ Everything fits, no ellipsis needed");
                // Everything fits
                if (primaryVisible)
                {
                    secondaryShowEllipsis = false;
                    secondaryVisibleLeadingSegments = secondaryPathSegments.ToList();
                }
                else
                {
                    primaryShowEllipsis = false;
                    primaryVisibleLeadingSegments = primaryPathSegments.ToList();
                }
            }
            else
            {
                Console.WriteLine($"❌ Content overflows by {overflow}px, need ellipsis");
                // Need to add ellipsis and hide segments
                if (hiddenSegments.Count <= 1)
                {
                    Console.WriteLine("Only 1 segment, showing it even if it overflows");
                    // Only one segment - show it even if it overflows
                    if (primaryVisible)
                    {
                        secondaryShowEllipsis = false;
                        secondaryVisibleLeadingSegments = secondaryPathSegments.ToList();
                    }
                    else
                    {
                        primaryShowEllipsis = false;
                        primaryVisibleLeadingSegments = primaryPathSegments.ToList();
                    }
                }
                else
                {
                    Console.WriteLine($"Multiple segments ({hiddenSegments.Count}), measuring individual widths...");
                    // Measure individual segment widths from the hidden breadcrumb
                    var segmentWidths = new List<double>();
                    
                    for (int i = 0; i < hiddenSegments.Count - 1; i++) // -1 to exclude last segment
                    {
                        if (_disposed) return;

                        var width = await JS.InvokeAsync<double>("breadcrumbMeasure.getSegmentWidth", i);
                        
                        segmentWidths.Add(width);
                        Console.WriteLine($"  Segment {i} '{hiddenSegments[i].segment}': {width}px");
                    }

                    if (_disposed) return;

                    // Calculate how many segments to remove
                    var ellipsisWidth = 50.0; // Approximate width of ellipsis button + separator
                    var targetRemoval = overflow + ellipsisWidth;
                    var accumulatedRemoval = 0.0;
                    var keepCount = 0;

                    Console.WriteLine($"Target removal: {targetRemoval}px (overflow {overflow}px + ellipsis {ellipsisWidth}px)");

                    // Remove segments from the beginning (after root) until we've removed enough
                    for (int i = 0; i < segmentWidths.Count; i++)
                    {
                        if (accumulatedRemoval < targetRemoval)
                        {
                            accumulatedRemoval += segmentWidths[i];
                            Console.WriteLine($"  Removing segment {i}, accumulated: {accumulatedRemoval}px");
                        }
                        else
                        {
                            keepCount = i;
                            Console.WriteLine($"  Stopping at segment {i}, keeping {keepCount} segments");
                            break;
                        }
                    }

                    // If we went through all segments and still need more removal, keep 0
                    if (keepCount == 0 && accumulatedRemoval < targetRemoval)
                    {
                        Console.WriteLine("  Removed all segments, keeping 0");
                    }

                    Console.WriteLine($"✂️ Final decision: show ellipsis=true, keep {keepCount} leading segments out of {hiddenSegments.Count - 1}");

                    if (primaryVisible)
                    {
                        secondaryShowEllipsis = true;
                        secondaryVisibleLeadingSegments = keepCount > 0 
                            ? secondaryPathSegments.Take(keepCount).ToList() 
                            : new List<(string, string)>();
                        Console.WriteLine($"Secondary: ellipsis={secondaryShowEllipsis}, visible segments={secondaryVisibleLeadingSegments.Count}");
                    }
                    else
                    {
                        primaryShowEllipsis = true;
                        primaryVisibleLeadingSegments = keepCount > 0 
                            ? primaryPathSegments.Take(keepCount).ToList() 
                            : new List<(string, string)>();
                        Console.WriteLine($"Primary: ellipsis={primaryShowEllipsis}, visible segments={primaryVisibleLeadingSegments.Count}");
                    }
                }
            }

            if (!_disposed)
            {
                Console.WriteLine("🔄 Swapping breadcrumbs...");
                SwapBreadcrumbs();
                Console.WriteLine($"After swap - Primary visible: {primaryVisible}");
                Console.WriteLine($"Primary: ellipsis={primaryShowEllipsis}, segments={primaryPathSegments.Count}, visible={primaryVisibleLeadingSegments.Count}");
                Console.WriteLine($"Secondary: ellipsis={secondaryShowEllipsis}, segments={secondaryPathSegments.Count}, visible={secondaryVisibleLeadingSegments.Count}");
            }
            Console.WriteLine("=== BREADCRUMB CALCULATION END ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🔴 Breadcrumb calculation error: {ex.Message}");
            Console.WriteLine($"Stack: {ex.StackTrace}");
            if (!_disposed)
            {
                // On error, just show everything in the hidden breadcrumb
                if (primaryVisible)
                {
                    secondaryShowEllipsis = false;
                    secondaryVisibleLeadingSegments = secondaryPathSegments.ToList();
                }
                else
                {
                    primaryShowEllipsis = false;
                    primaryVisibleLeadingSegments = primaryPathSegments.ToList();
                }
                SwapBreadcrumbs();
            }
        }
    }

    private void SwapBreadcrumbs()
    {
        if (_disposed) return;

        // Just swap which breadcrumb is visible
        // DON'T clear the now-hidden one - it stays for display until next navigation
        primaryVisible = !primaryVisible;
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
        directories.Clear();
        canCreateFolder = false;
        primaryPathSegments.Clear();
        primaryVisibleLeadingSegments.Clear();
        primaryLastSegment = ("", "");
        primaryShowEllipsis = false;
        primaryRootPrefix = "";
        secondaryPathSegments.Clear();
        secondaryVisibleLeadingSegments.Clear();
        secondaryLastSegment = ("", "");
        secondaryShowEllipsis = false;
        secondaryRootPrefix = "";
        primaryVisible = true;
        showCreateInline = false;
        needsCalculation = false;
        isCalculating = false;
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
                
                // Clear and populate the HIDDEN breadcrumb (for preparation)
                if (primaryVisible)
                {
                    // Primary is visible, clear and prepare secondary (hidden)
                    secondaryRootPrefix = "";
                    secondaryPathSegments.Clear();
                    secondaryVisibleLeadingSegments.Clear();
                    secondaryLastSegment = ("", "");
                    secondaryShowEllipsis = false;
                    
                    UpdateRootInfo(currentPath, out secondaryRootPrefix);
                    BuildPathSegments(currentPath, out secondaryPathSegments, out secondaryLastSegment);
                    secondaryVisibleLeadingSegments = secondaryPathSegments.ToList();
                }
                else
                {
                    // Secondary is visible, clear and prepare primary (hidden)
                    primaryRootPrefix = "";
                    primaryPathSegments.Clear();
                    primaryVisibleLeadingSegments.Clear();
                    primaryLastSegment = ("", "");
                    primaryShowEllipsis = false;
                    
                    UpdateRootInfo(currentPath, out primaryRootPrefix);
                    BuildPathSegments(currentPath, out primaryPathSegments, out primaryLastSegment);
                    primaryVisibleLeadingSegments = primaryPathSegments.ToList();
                }

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
            if (!_disposed)
            {
                StateHasChanged();
            }
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
        if (_disposed) return;

        var currentSegments = primaryVisible ? primaryPathSegments : secondaryPathSegments;
        
        if (currentSegments.Count >= 2)
        {
            var parentSegment = currentSegments[currentSegments.Count - 2];
            await NavigateToPath(parentSegment.path);
        }
        else if (currentSegments.Count == 1)
        {
            await NavigateToRoot();
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
}
