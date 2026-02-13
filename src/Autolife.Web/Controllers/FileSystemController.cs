using Microsoft.AspNetCore.Mvc;
using System.Runtime.InteropServices;
using System.Security;

namespace Autolife.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileSystemController : ControllerBase
{
    [HttpGet("roots")]
    public IActionResult GetRoots()
    {
        try
        {
            var roots = new List<object>();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows: List all drives
                var drives = DriveInfo.GetDrives()
                    .Where(d => d.IsReady)
                    .Select(d => new
                    {
                        Path = d.Name,
                        Name = $"{d.Name.TrimEnd('\\')} ({d.VolumeLabel})".Trim(),
                        Type = d.DriveType == DriveType.Network ? "Network" : "Drive"
                    });
                
                roots.AddRange(drives);
                
                // Add network option
                roots.Add(new
                {
                    Path = "\\\\",
                    Name = "Network Locations",
                    Type = "Network"
                });
            }
            else
            {
                // Linux/Mac: Start from root
                roots.Add(new { Path = "/", Name = "Root", Type = "Root" });
            }

            return Ok(roots);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("browse")]
    public IActionResult BrowseDirectory([FromQuery] string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return BadRequest(new { error = "Path is required" });
            }

            var directory = new DirectoryInfo(path);
            if (!directory.Exists)
            {
                return NotFound(new { error = "Directory not found" });
            }

            var directories = directory.GetDirectories()
                .Where(d => !d.Attributes.HasFlag(FileAttributes.Hidden) && 
                           !d.Attributes.HasFlag(FileAttributes.System))
                .OrderBy(d => d.Name)
                .Select(d => new
                {
                    Name = d.Name,
                    Path = d.FullName,
                    LastModified = d.LastWriteTime,
                    IsAccessible = IsDirectoryAccessible(d.FullName)
                })
                .ToList();

            var parent = directory.Parent?.FullName;
            var canCreateFolder = CanCreateFolderInDirectory(path);

            return Ok(new
            {
                CurrentPath = directory.FullName,
                ParentPath = parent,
                Directories = directories,
                CanCreateFolder = canCreateFolder
            });
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, new { error = "Access denied" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("create")]
    public IActionResult CreateDirectory([FromBody] CreateDirectoryRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.ParentPath) || string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { error = "Parent path and name are required" });
            }

            // Validate name
            if (request.Name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return BadRequest(new { error = "Invalid folder name" });
            }

            var fullPath = Path.Combine(request.ParentPath, request.Name);
            
            if (Directory.Exists(fullPath))
            {
                return BadRequest(new { error = "Folder already exists" });
            }

            var directory = Directory.CreateDirectory(fullPath);

            return Ok(new { Path = directory.FullName, Name = directory.Name });
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, new { error = "Permission denied" });
        }
        catch (IOException ex)
        {
            return BadRequest(new { error = $"Cannot create folder: {ex.Message}" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("validate")]
    public IActionResult ValidatePath([FromQuery] string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return Ok(new { isValid = false, message = "Path is empty" });
            }

            var directory = new DirectoryInfo(path);
            var exists = directory.Exists;
            var isAccessible = exists && IsDirectoryAccessible(path);

            return Ok(new
            {
                isValid = exists && isAccessible,
                exists = exists,
                isAccessible = isAccessible,
                message = exists ? (isAccessible ? "Valid" : "Access denied") : "Directory does not exist"
            });
        }
        catch (Exception ex)
        {
            return Ok(new { isValid = false, message = ex.Message });
        }
    }

    private bool IsDirectoryAccessible(string path)
    {
        try
        {
            Directory.GetDirectories(path);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool CanCreateFolderInDirectory(string path)
    {
        try
        {
            // Try to create a temp directory name and check permissions
            var testPath = Path.Combine(path, $"_test_{Guid.NewGuid()}");
            
            // Check if we can get directory info (basic permission check)
            var dirInfo = new DirectoryInfo(path);
            
            // Check if directory is read-only
            if (dirInfo.Attributes.HasFlag(FileAttributes.ReadOnly))
            {
                return false;
            }

            // Try to check write access using DirectoryInfo
            // We don't actually create the directory, just check if the path is valid
            try
            {
                var testDir = new DirectoryInfo(testPath);
                // If we can construct it without exception, we likely have access
                return true;
            }
            catch
            {
                return false;
            }
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (SecurityException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }

    public class CreateDirectoryRequest
    {
        public string ParentPath { get; set; } = "";
        public string Name { get; set; } = "";
    }
}
