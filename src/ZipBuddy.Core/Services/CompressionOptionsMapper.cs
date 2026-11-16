using ZipBuddy.Core.Models;

namespace ZipBuddy.Core.Services;

public enum DestinationValidation
{
    Valid,
    Empty,
    InvalidPath,
    SameAsSource
}

/// <summary>
/// Pure mapping between the compression dialog's control indices and domain options.
/// Kept in Core so it can be unit-tested without WinUI dependencies.
/// </summary>
public static class CompressionOptionsMapper
{
    public static ArchiveFormat MapFormat(int selectedIndex) => selectedIndex switch
    {
        1 => ArchiveFormat.SevenZip,
        2 => ArchiveFormat.Tar,
        3 => ArchiveFormat.GZip,
        _ => ArchiveFormat.Zip
    };

    public static CompressionLevel MapLevel(int selectedIndex) => selectedIndex switch
    {
        0 => CompressionLevel.Store,
        1 => CompressionLevel.Fast,
        3 => CompressionLevel.Maximum,
        4 => CompressionLevel.Ultra,
        _ => CompressionLevel.Normal
    };

    public static bool SupportsPassword(ArchiveFormat format) => format is ArchiveFormat.Zip or ArchiveFormat.SevenZip;

    public static bool SupportsCompressionLevel(ArchiveFormat format) => format is ArchiveFormat.Zip or ArchiveFormat.SevenZip;

    public static string GetFilePickerExtension(ArchiveFormat format) => format == ArchiveFormat.GZip ? ".gz" : format.GetDefaultExtension();

    public static string ReplaceExtension(string path, ArchiveFormat format)
    {
        var extension = format.GetDefaultExtension();
        var current = path ?? string.Empty;

        if (current.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase))
            return current[..^7] + extension;

        return Path.ChangeExtension(current, extension);
    }

    public static string BuildDefaultDestination(string sourcePath, ArchiveFormat format)
    {
        var isDirectory = Directory.Exists(sourcePath);
        var name = isDirectory
            ? Path.GetFileName(sourcePath.TrimEnd('\\', '/'))
            : Path.GetFileNameWithoutExtension(sourcePath);

        var parent = (isDirectory
            ? Path.GetDirectoryName(sourcePath.TrimEnd('\\', '/'))
            : Path.GetDirectoryName(sourcePath))
            ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        return Path.Combine(parent, name + format.GetDefaultExtension());
    }

    public static DestinationValidation ValidateDestination(string? destination, string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(destination))
            return DestinationValidation.Empty;

        var candidate = destination.Trim();
        if (candidate.EndsWith('\\') || candidate.EndsWith('/'))
            return DestinationValidation.InvalidPath;

        try
        {
            var fullDestination = Path.GetFullPath(candidate);
            var fileName = Path.GetFileName(fullDestination);
            if (string.IsNullOrWhiteSpace(Path.GetFileNameWithoutExtension(fullDestination)) ||
                fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return DestinationValidation.InvalidPath;

            var fullSource = Path.GetFullPath(sourcePath);
            return string.Equals(fullDestination, fullSource, StringComparison.OrdinalIgnoreCase)
                ? DestinationValidation.SameAsSource
                : DestinationValidation.Valid;
        }
        catch
        {
            return DestinationValidation.InvalidPath;
        }
    }
}
