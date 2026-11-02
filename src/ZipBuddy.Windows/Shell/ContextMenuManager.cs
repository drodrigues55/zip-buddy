using Microsoft.Win32;

namespace ZipBuddy.Windows.Shell;

/// <summary>
/// Registers Windows Explorer context menu items under HKCU (no admin rights needed).
/// Supports both flat verbs and grouped cascading submenus, with native brand icon integration.
/// </summary>
public static class ContextMenuManager
{
    private static readonly string[] ArchiveExtensions =
    [
        ".zip", ".zipx", ".jar", ".apk", ".7z", ".rar", ".tar", ".gz", ".bz2", ".xz", ".tgz", ".tbz2", ".txz"
    ];

    public static bool RegisterContextMenu(
        string executablePath,
        bool enableOpenWith = true,
        bool enableExtractHere = true,
        bool enableExtractToFolder = true,
        bool enableExtractTo = true,
        bool enableCompressToZip = true,
        bool enableCompressTo7z = true,
        bool useCascadingMenu = false)
    {
        if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
            return false;

        try
        {
            var iconVal = $"\"{executablePath}\",0";

            // Register archive verbs under SystemFileAssociations for each archive extension
            foreach (var ext in ArchiveExtensions)
            {
                var baseKey = $@"Software\Classes\SystemFileAssociations\{ext}\shell";

                if (useCascadingMenu)
                {
                    // Clean up flat verbs first
                    RemoveVerb(baseKey, "Zip Buddy.Open");
                    RemoveVerb(baseKey, "Zip Buddy.ExtractHere");
                    RemoveVerb(baseKey, "Zip Buddy.ExtractToFolder");
                    RemoveVerb(baseKey, "Zip Buddy.ExtractTo");

                    // Check if any archive verb is active
                    if (enableOpenWith || enableExtractHere || enableExtractToFolder || enableExtractTo)
                    {
                        SetCascadingGroup(baseKey, "Zip Buddy", "Zip Buddy", iconVal);
                        var cascadeShell = $@"{baseKey}\Zip Buddy\shell";

                        if (enableOpenWith)
                            SetVerb(cascadeShell, "Open", "Open", $"\"{executablePath}\" \"%1\"", iconVal);
                        else
                            RemoveVerb(cascadeShell, "Open");

                        if (enableExtractHere)
                            SetVerb(cascadeShell, "ExtractHere", "Extract Here", $"\"{executablePath}\" --extract-here \"%1\"", iconVal);
                        else
                            RemoveVerb(cascadeShell, "ExtractHere");

                        if (enableExtractToFolder)
                            SetVerb(cascadeShell, "ExtractToFolder", "Extract to Folder", $"\"{executablePath}\" --extract-to-folder \"%1\"", iconVal);
                        else
                            RemoveVerb(cascadeShell, "ExtractToFolder");

                        if (enableExtractTo)
                            SetVerb(cascadeShell, "ExtractTo", "Extract to...", $"\"{executablePath}\" --extract-to \"%1\"", iconVal);
                        else
                            RemoveVerb(cascadeShell, "ExtractTo");
                    }
                    else
                    {
                        RemoveVerb(baseKey, "Zip Buddy");
                    }
                }
                else
                {
                    // Remove cascading menu if previously registered
                    RemoveVerb(baseKey, "Zip Buddy");

                    // Flat verbs with clean labels and native icon
                    if (enableOpenWith)
                        SetVerb(baseKey, "Zip Buddy.Open", "Open with Zip Buddy", $"\"{executablePath}\" \"%1\"", iconVal);
                    else
                        RemoveVerb(baseKey, "Zip Buddy.Open");

                    if (enableExtractHere)
                        SetVerb(baseKey, "Zip Buddy.ExtractHere", "Extract Here", $"\"{executablePath}\" --extract-here \"%1\"", iconVal);
                    else
                        RemoveVerb(baseKey, "Zip Buddy.ExtractHere");

                    if (enableExtractToFolder)
                        SetVerb(baseKey, "Zip Buddy.ExtractToFolder", "Extract to Folder", $"\"{executablePath}\" --extract-to-folder \"%1\"", iconVal);
                    else
                        RemoveVerb(baseKey, "Zip Buddy.ExtractToFolder");

                    if (enableExtractTo)
                        SetVerb(baseKey, "Zip Buddy.ExtractTo", "Extract to...", $"\"{executablePath}\" --extract-to \"%1\"", iconVal);
                    else
                        RemoveVerb(baseKey, "Zip Buddy.ExtractTo");
                }
            }

            // Register compression verbs for all files (*) and folders (Directory)
            string[] targetKeys = [@"Software\Classes\*\shell", @"Software\Classes\Directory\shell"];
            foreach (var parentKey in targetKeys)
            {
                if (useCascadingMenu)
                {
                    RemoveVerb(parentKey, "Zip Buddy.CompressZip");
                    RemoveVerb(parentKey, "Zip Buddy.Compress7z");

                    if (enableCompressToZip || enableCompressTo7z)
                    {
                        SetCascadingGroup(parentKey, "Zip Buddy", "Zip Buddy", iconVal);
                        var cascadeShell = $@"{parentKey}\Zip Buddy\shell";

                        if (enableCompressToZip)
                            SetVerb(cascadeShell, "CompressZip", "Compress to ZIP", $"\"{executablePath}\" --compress-zip \"%1\"", iconVal);
                        else
                            RemoveVerb(cascadeShell, "CompressZip");

                        if (enableCompressTo7z)
                            SetVerb(cascadeShell, "Compress7z", "Compress to 7Z", $"\"{executablePath}\" --compress-7z \"%1\"", iconVal);
                        else
                            RemoveVerb(cascadeShell, "Compress7z");
                    }
                    else
                    {
                        RemoveVerb(parentKey, "Zip Buddy");
                    }
                }
                else
                {
                    RemoveVerb(parentKey, "Zip Buddy");

                    if (enableCompressToZip)
                        SetVerb(parentKey, "Zip Buddy.CompressZip", "Compress to ZIP", $"\"{executablePath}\" --compress-zip \"%1\"", iconVal);
                    else
                        RemoveVerb(parentKey, "Zip Buddy.CompressZip");

                    if (enableCompressTo7z)
                        SetVerb(parentKey, "Zip Buddy.Compress7z", "Compress to 7Z", $"\"{executablePath}\" --compress-7z \"%1\"", iconVal);
                    else
                        RemoveVerb(parentKey, "Zip Buddy.Compress7z");
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool UnregisterContextMenu()
    {
        try
        {
            foreach (var ext in ArchiveExtensions)
            {
                var baseKey = $@"Software\Classes\SystemFileAssociations\{ext}\shell";
                RemoveVerb(baseKey, "Zip Buddy.Open");
                RemoveVerb(baseKey, "Zip Buddy.ExtractHere");
                RemoveVerb(baseKey, "Zip Buddy.ExtractToFolder");
                RemoveVerb(baseKey, "Zip Buddy.ExtractTo");
                RemoveVerb(baseKey, "Zip Buddy");
            }

            string[] targetKeys = [@"Software\Classes\*\shell", @"Software\Classes\Directory\shell"];
            foreach (var parentKey in targetKeys)
            {
                RemoveVerb(parentKey, "Zip Buddy.CompressZip");
                RemoveVerb(parentKey, "Zip Buddy.Compress7z");
                RemoveVerb(parentKey, "Zip Buddy");
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void SetCascadingGroup(string parentKeyPath, string groupKey, string groupTitle, string iconPath)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey($@"{parentKeyPath}\{groupKey}");
            key.SetValue("MUIVerb", groupTitle);
            key.SetValue("SubCommands", string.Empty);
            if (!string.IsNullOrEmpty(iconPath))
                key.SetValue("Icon", iconPath);
        }
        catch
        {
            // Ignore registry permissions errors
        }
    }

    private static void SetVerb(string parentKeyPath, string verbKey, string menuText, string commandLine, string? iconPath = null)
    {
        try
        {
            using var verb = Registry.CurrentUser.CreateSubKey($@"{parentKeyPath}\{verbKey}");
            verb.SetValue(string.Empty, menuText);
            if (!string.IsNullOrEmpty(iconPath))
                verb.SetValue("Icon", iconPath);

            using var cmd = verb.CreateSubKey("command");
            cmd.SetValue(string.Empty, commandLine);
        }
        catch
        {
            // Ignore registry permissions errors
        }
    }

    private static void RemoveVerb(string parentKeyPath, string verbKey)
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree($@"{parentKeyPath}\{verbKey}", false);
        }
        catch
        {
            // Ignore registry deletion errors
        }
    }
}
