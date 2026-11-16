using Microsoft.Win32;
using ZipBuddy.Core.Services;

namespace ZipBuddy.Windows.Shell;

/// <summary>
/// Registers Windows Explorer context menu items under HKCU (no admin rights needed).
/// Supports flat verbs and rich grouped cascading submenus (7-Zip / WinRAR style),
/// with native brand icon integration.
/// </summary>
public static class ContextMenuManager
{
    private static readonly string[] ArchiveExtensions =
    [
        ".zip", ".zipx", ".jar", ".apk", ".7z", ".rar", ".tar", ".gz", ".bz2", ".xz", ".tgz", ".tbz2", ".txz"
    ];

    private static readonly string[] AllFlatVerbs =
    [
        "Zip Buddy.Open",
        "Zip Buddy.ExtractTo",
        "Zip Buddy.ExtractHere",
        "Zip Buddy.ExtractToFolder",
        "Zip Buddy.CompressZip",
        "Zip Buddy.Compress7z"
    ];

    public static bool RegisterContextMenu(
        string executablePath,
        bool enableOpenWith = true,
        bool enableExtractHere = true,
        bool enableExtractToFolder = true,
        bool enableExtractTo = true,
        bool enableCompressToZip = true,
        bool enableCompressTo7z = true,
        bool useCascadingMenu = ContextMenuDefinitions.DefaultUseCascadingMenu,
        bool isPortuguese = true)
    {
        if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
            return false;

        try
        {
            var iconVal = $"\"{executablePath}\",0";

            var archiveVerbs = ContextMenuDefinitions.BuildArchiveVerbs(
                enableOpenWith,
                enableExtractHere,
                enableExtractToFolder,
                enableExtractTo,
                isPortuguese,
                enableCompressToZip,
                enableCompressTo7z);

            var fileVerbs = ContextMenuDefinitions.BuildFileVerbs(
                enableCompressToZip,
                enableCompressTo7z,
                isPortuguese);

            // 1. Archive verbs under SystemFileAssociations for each archive extension
            foreach (var ext in ArchiveExtensions)
            {
                var baseKey = $@"Software\Classes\SystemFileAssociations\{ext}\shell";
                ApplyVerbs(baseKey, archiveVerbs, executablePath, iconVal, useCascadingMenu);
            }

            // 2. Compression verbs for all files (*) and folders (Directory)
            string[] targetKeys = [@"Software\Classes\*\shell", @"Software\Classes\Directory\shell"];
            foreach (var parentKey in targetKeys)
            {
                ApplyVerbs(parentKey, fileVerbs, executablePath, iconVal, useCascadingMenu);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void ApplyVerbs(
        string baseKey,
        IReadOnlyList<ContextMenuVerb> verbs,
        string executablePath,
        string iconVal,
        bool useCascadingMenu)
    {
        if (useCascadingMenu)
        {
            // Rebuild the group from scratch so disabled verbs never leave stale numbered keys.
            foreach (var flatKey in AllFlatVerbs)
                RemoveVerb(baseKey, flatKey);
            RemoveVerb(baseKey, ContextMenuDefinitions.GroupName);

            if (verbs.Count == 0)
                return;

            SetCascadingGroup(baseKey, ContextMenuDefinitions.GroupName, ContextMenuDefinitions.GroupName, iconVal);
            var cascadeShell = $@"{baseKey}\{ContextMenuDefinitions.GroupName}\shell";

            foreach (var verb in verbs)
                SetVerb(cascadeShell, verb.Key, verb.Label, $"\"{executablePath}\" {verb.Arguments}", iconVal);
        }
        else
        {
            RemoveVerb(baseKey, ContextMenuDefinitions.GroupName);

            var enabledFlatKeys = verbs.Where(v => v.FlatKey is not null)
                                       .Select(v => v.FlatKey!)
                                       .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var flatKey in AllFlatVerbs)
            {
                if (!enabledFlatKeys.Contains(flatKey))
                    RemoveVerb(baseKey, flatKey);
            }

            foreach (var verb in verbs.Where(v => v.FlatKey is not null))
                SetVerb(baseKey, verb.FlatKey!, verb.Label, $"\"{executablePath}\" {verb.Arguments}", iconVal);
        }
    }

    public static bool UnregisterContextMenu()
    {
        try
        {
            foreach (var ext in ArchiveExtensions)
            {
                var baseKey = $@"Software\Classes\SystemFileAssociations\{ext}\shell";
                foreach (var flatKey in AllFlatVerbs)
                    RemoveVerb(baseKey, flatKey);
                RemoveVerb(baseKey, ContextMenuDefinitions.GroupName);
            }

            string[] targetKeys = [@"Software\Classes\*\shell", @"Software\Classes\Directory\shell"];
            foreach (var parentKey in targetKeys)
            {
                foreach (var flatKey in AllFlatVerbs)
                    RemoveVerb(parentKey, flatKey);
                RemoveVerb(parentKey, ContextMenuDefinitions.GroupName);
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
            key.SetValue(string.Empty, groupTitle);
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
            verb.SetValue("MUIVerb", menuText);
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
