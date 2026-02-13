using Microsoft.AspNetCore.Mvc;
using System.Runtime.InteropServices;

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
            var roots = new List<DirectoryInfo>();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows: List all drives
                var drives = DriveInfo.GetDrives()
                    .Where(d => d.IsReady)
                    .Select(d => new
                    {
                        Path = d.Name,
                        Name = $"{d.Name} ({d.VolumeLabel})",
                        Type = d.DriveType.ToString()
                    });
                return Ok(drives);
            }
            else
            {
                // Linux/Mac: Start from root
                return Ok(new[] { new { Path = "/", Name = "Root", Type = "Root" } });
            }
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

            return Ok(new
            {
                CurrentPath = directory.FullName,
                ParentPath = parent,
                Directories = directories
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

            var fullPath = Path.Combine(request.ParentPath, request.Name);
            var directory = Directory.CreateDirectory(fullPath);

            return Ok(new { Path = directory.FullName, Name = directory.Name });
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

    public class CreateDirectoryRequest
    {
        public string ParentPath { get; set; } = "";
        public string Name { get; set; } = "";
    }
}
