namespace Autolife.Core.Models;

public class UserSettings
{
    public Theme Theme { get; set; } = Theme.Light;
    public string Language { get; set; } = "English";
    public DateFormat DateFormat { get; set; } = DateFormat.MMDDYYYY;
    public int ItemsPerPage { get; set; } = 20;
}

public enum Theme
{
    Light,
    Dark,
    Auto
}

public enum DateFormat
{
    MMDDYYYY,
    DDMMYYYY,
    YYYYMMDD
}
