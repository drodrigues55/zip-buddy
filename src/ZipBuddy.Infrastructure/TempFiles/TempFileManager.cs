using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using ZipBuddy.Core.Interfaces;

namespace ZipBuddy.Infrastructure.TempFiles;

/// <summary>
/// Manages ephemeral scratch directories and secure file extraction sandboxes.
/// Each instance owns a session-level root under the system temp folder that is
/// restricted to the current user via DACL (Windows) and deleted on dispose.
/// </summary>
public sealed class TempFileManager : IDisposable
{
    private readonly string _rootTempDir;
    private readonly List<string> _createdDirectories = [];
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// Gets the root session directory path, or an empty string if initialization failed.
    /// </summary>
    public string RootTempDir => _rootTempDir;

    /// <summary>
    /// Gets whether a fallback location was used instead of the requested custom directory.
    /// </summary>
    public bool IsUsingFallback { get; }

    /// <summary>
    /// Gets any fatal error encountered during temp root initialization, or null if healthy.
    /// </summary>
    public string? InitializationError { get; }

    /// <summary>
    /// Gets whether the temporary file system is initialized and ready for operations.
    /// </summary>
    public bool IsAvailable => !string.IsNullOrEmpty(_rootTempDir) && Directory.Exists(_rootTempDir);

    public TempFileManager(string? customTempDirectory = null, ILoggingService? logger = null)
    {
        if (!string.IsNullOrWhiteSpace(customTempDirectory))
        {
            try
            {
                var candidateDir = Path.Combine(customTempDirectory, $"ZipBuddy_Temp_{Guid.NewGuid():N}");
                Directory.CreateDirectory(candidateDir);
                RestrictDirectoryToCurrentUser(candidateDir);
                _rootTempDir = candidateDir;
                return;
            }
            catch (Exception ex)
            {
                IsUsingFallback = true;
                logger?.Warn($"Could not initialize custom temporary directory '{customTempDirectory}': {ex.Message}. Falling back to system temp.");
            }
        }

        try
        {
            var systemTemp = Path.Combine(Path.GetTempPath(), "Zip Buddy", $"Session_{Guid.NewGuid():N}");
            Directory.CreateDirectory(systemTemp);
            RestrictDirectoryToCurrentUser(systemTemp);
            _rootTempDir = systemTemp;
            return;
        }
        catch (Exception ex)
        {
            logger?.Warn($"Could not initialize system temp directory: {ex.Message}. Attempting local app data fallback.");
        }

        try
        {
            var localAppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Zip Buddy", "Temp", $"Session_{Guid.NewGuid():N}");
            Directory.CreateDirectory(localAppData);
            RestrictDirectoryToCurrentUser(localAppData);
            _rootTempDir = localAppData;
            return;
        }
        catch (Exception ex)
        {
            InitializationError = ex.Message;
            logger?.Error($"Failed to initialize any temporary storage directory: {ex.Message}");
            _rootTempDir = string.Empty;
        }
    }

    /// <summary>
    /// Creates a dedicated, unique scratch subdirectory for a temporary operation
    /// (such as opening a single file). The directory is also ACL-restricted.
    /// </summary>
    public string CreateTempDirectory(string? prefix = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrEmpty(_rootTempDir) || !Directory.Exists(_rootTempDir))
        {
            throw new InvalidOperationException($"Temporary file operations are unavailable: {InitializationError ?? "Failed to initialize temp storage"}");
        }

        var name = string.IsNullOrWhiteSpace(prefix)
            ? Guid.NewGuid().ToString("N")
            : $"{prefix}_{Guid.NewGuid():N}";

        var dirPath = Path.Combine(_rootTempDir, name);
        Directory.CreateDirectory(dirPath);
        RestrictDirectoryToCurrentUser(dirPath);

        lock (_lock)
        {
            _createdDirectories.Add(dirPath);
        }

        return dirPath;
    }

    /// <summary>
    /// Cleans up a specific temporary directory and removes it from the tracking list.
    /// </summary>
    public void CleanupDirectory(string dirPath)
    {
        try
        {
            if (Directory.Exists(dirPath))
            {
                Directory.Delete(dirPath, recursive: true);
            }

            lock (_lock)
            {
                _createdDirectories.Remove(dirPath);
            }
        }
        catch
        {
            // Non-critical cleanup failure — best effort
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            if (Directory.Exists(_rootTempDir))
            {
                Directory.Delete(_rootTempDir, recursive: true);
            }
        }
        catch
        {
            // Best-effort cleanup; don't rethrow from Dispose
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Finalizer safety net: ensures the temp directory is cleaned up even if
    /// <see cref="Dispose"/> was never called.
    /// </summary>
    ~TempFileManager()
    {
        try
        {
            if (Directory.Exists(_rootTempDir))
            {
                Directory.Delete(_rootTempDir, recursive: true);
            }
        }
        catch
        {
            // Finalizer must not throw
        }
    }

    /// <summary>
    /// On Windows, sets the directory DACL to grant Full Control only to the current
    /// user and SYSTEM, removing the default inherited Everyone / Users entries.
    /// On non-Windows platforms this is a no-op (the directory is already created with
    /// process-owner permissions).
    /// </summary>
    private static void RestrictDirectoryToCurrentUser(string dirPath)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        try
        {
            var dirInfo = new DirectoryInfo(dirPath);
            var acl = dirInfo.GetAccessControl();

            // Disable inheritance and remove all inherited rules
            acl.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

            // Grant Full Control to the current user
            var currentUser = WindowsIdentity.GetCurrent().User;
            if (currentUser != null)
            {
                acl.AddAccessRule(new FileSystemAccessRule(
                    currentUser,
                    FileSystemRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow));
            }

            // Grant Full Control to SYSTEM
            var systemSid = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
            acl.AddAccessRule(new FileSystemAccessRule(
                systemSid,
                FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                AccessControlType.Allow));

            dirInfo.SetAccessControl(acl);
        }
        catch
        {
            // ACL hardening is best-effort; don't fail temp directory creation
        }
    }
}
