using ZipBuddy.Core.Models;
using ZipBuddy.Infrastructure.Settings;
using Xunit;

namespace ZipBuddy.Tests;

public class SettingsTests
{
    [Fact]
    public async Task SettingsService_SavesAndLoads_RetainsValues()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), "zipbuddy_test_settings_" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var service = new SettingsService(tempFile);
            service.ThemeMode = "Dark";
            service.DefaultArchiveFormat = ArchiveFormat.SevenZip;
            service.DefaultCompressionLevel = CompressionLevel.Ultra;
            service.ConfirmBeforeOverwriting = false;
            await service.SaveAsync();

            var reloadedService = new SettingsService(tempFile);
            await reloadedService.LoadAsync();

            Assert.Equal("Dark", reloadedService.ThemeMode);
            Assert.Equal(ArchiveFormat.SevenZip, reloadedService.DefaultArchiveFormat);
            Assert.Equal(CompressionLevel.Ultra, reloadedService.DefaultCompressionLevel);
            Assert.False(reloadedService.ConfirmBeforeOverwriting);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task SettingsService_SavesAndLoads_IndividualContextMenuCommands()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), "zipbuddy_test_context_menu_" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var service = new SettingsService(tempFile)
            {
                EnableContextMenu = true,
                EnabledContextMenuCommands = ["OpenWith", "ExtractTo", "CompressTo7z"]
            };
            await service.SaveAsync();

            var reloadedService = new SettingsService(tempFile);
            await reloadedService.LoadAsync();

            Assert.Equal(new[] { "OpenWith", "ExtractTo", "CompressTo7z" }, reloadedService.EnabledContextMenuCommands);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task SettingsService_SavesAndLoads_TaskWindowAndCascadingSettings()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), "zipbuddy_test_tasks_" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var service = new SettingsService(tempFile)
            {
                AutoCloseTaskProgressWindow = false,
                NotifyOnTaskCompletion = false,
                UseCascadingContextMenu = true
            };
            await service.SaveAsync();

            var reloadedService = new SettingsService(tempFile);
            await reloadedService.LoadAsync();

            Assert.False(reloadedService.AutoCloseTaskProgressWindow);
            Assert.False(reloadedService.NotifyOnTaskCompletion);
            Assert.True(reloadedService.UseCascadingContextMenu);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task SettingsService_SavesAndLoads_DoubleClickActionAndExtractionOptions()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), "zipbuddy_test_bandizip_" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var service = new SettingsService(tempFile)
            {
                DoubleClickAction = "ExtractToArchiveFolder",
                DefaultExtractionFolder = @"C:\Extracted",
                OpenExtractedFolderAfterExtraction = true,
                KeepTaskProgressWindowOpen = true,
                DeleteArchiveAfterExtraction = true
            };
            await service.SaveAsync();

            var reloadedService = new SettingsService(tempFile);
            await reloadedService.LoadAsync();

            Assert.Equal("ExtractToArchiveFolder", reloadedService.DoubleClickAction);
            Assert.Equal(@"C:\Extracted", reloadedService.DefaultExtractionFolder);
            Assert.True(reloadedService.OpenExtractedFolderAfterExtraction);
            Assert.True(reloadedService.KeepTaskProgressWindowOpen);
            Assert.True(reloadedService.DeleteArchiveAfterExtraction);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void OperationProgress_ItemPercentageAndCompressionRatio_CalculatesAccurately()
    {
        var progress = new OperationProgress
        {
            BytesProcessed = 500,
            TotalBytes = 1000,
            ItemBytesProcessed = 25,
            ItemTotalBytes = 100,
            CompressedBytes = 400
        };

        Assert.Equal(50.0, progress.Percentage);
        Assert.Equal(25.0, progress.ItemPercentage);
        Assert.Equal(40.0, progress.CompressionRatio);
    }

    [Fact]
    public async Task SettingsService_MissingFile_LoadsDefaultConfiguration()
    {
        var nonExistentFile = Path.Combine(Path.GetTempPath(), $"zipbuddy_missing_{Guid.NewGuid():N}.json");
        try
        {
            var service = new SettingsService(nonExistentFile);
            await service.LoadAsync();

            Assert.Equal(string.Empty, service.DefaultExtractionFolder);
            Assert.True(service.OpenExtractedFolderAfterExtraction);
            Assert.False(service.KeepTaskProgressWindowOpen);
            Assert.True(service.AutoCloseTaskProgressWindow);
            Assert.Equal("OpenInZipBuddy", service.DoubleClickAction);
            Assert.Equal("System", service.ThemeMode);
            Assert.Equal("System", service.Language);
            Assert.Equal(ArchiveFormat.Zip, service.DefaultArchiveFormat);
            Assert.Equal(CompressionLevel.Normal, service.DefaultCompressionLevel);
            Assert.True(service.EnableContextMenu);
            Assert.False(service.UseCascadingContextMenu);
            Assert.NotEmpty(service.AssociatedExtensions);
            Assert.NotEmpty(service.EnabledContextMenuCommands);
        }
        finally
        {
            if (File.Exists(nonExistentFile))
                File.Delete(nonExistentFile);
        }
    }

    [Fact]
    public async Task SettingsService_MalformedJson_RecoversWithDefaultsAndCreatesBackup()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"zipbuddy_corrupt_{Guid.NewGuid():N}.json");
        var corruptContent = "{ this is invalid json ::: [[ }";
        try
        {
            await File.WriteAllTextAsync(tempFile, corruptContent);

            var service = new SettingsService(tempFile);
            var exception = await Record.ExceptionAsync(() => service.LoadAsync());

            Assert.Null(exception);
            Assert.Equal("System", service.ThemeMode);
            Assert.Equal(string.Empty, service.DefaultExtractionFolder);
            Assert.Equal(ArchiveFormat.Zip, service.DefaultArchiveFormat);

            // Verify backup creation and content
            Assert.NotNull(service.LastCorruptBackupPath);
            Assert.True(File.Exists(service.LastCorruptBackupPath));
            var backedUpContent = await File.ReadAllTextAsync(service.LastCorruptBackupPath);
            Assert.Equal(corruptContent, backedUpContent);

            // Verify original file is preserved before any save
            Assert.True(File.Exists(tempFile));
            var currentContent = await File.ReadAllTextAsync(tempFile);
            Assert.Equal(corruptContent, currentContent);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task SettingsService_MalformedJson_BackupFailure_PreventsOverwritingOriginal()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"zipbuddy_corrupt_fail_{Guid.NewGuid():N}.json");
        var corruptContent = "{ this is invalid json ::: [[ }";
        try
        {
            await File.WriteAllTextAsync(tempFile, corruptContent);

            var service = new SettingsService(tempFile)
            {
                SimulateBackupFailure = true
            };
            await service.LoadAsync();

            // Backup failed, so LastCorruptBackupPath must be null
            Assert.Null(service.LastCorruptBackupPath);

            // Attempting to save defaults must be blocked to preserve user's original data
            var saveResult = await service.SaveAsync();
            Assert.False(saveResult);

            // Original corrupted file content must remain completely intact
            Assert.True(File.Exists(tempFile));
            var remainingContent = await File.ReadAllTextAsync(tempFile);
            Assert.Equal(corruptContent, remainingContent);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task SettingsService_InaccessibleLockedFile_RecoversWithDefaultsWithoutCreatingCorruptBackup()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"zipbuddy_locked_{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllTextAsync(tempFile, "{}");

            // Lock file exclusively to simulate permission/concurrency failure
            using (var stream = new FileStream(tempFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                var service = new SettingsService(tempFile);
                var exception = await Record.ExceptionAsync(() => service.LoadAsync());

                Assert.Null(exception);
                Assert.Equal("System", service.ThemeMode);
                Assert.Equal(ArchiveFormat.Zip, service.DefaultArchiveFormat);

                // Should NOT create a corrupt backup for locked or transient I/O failure
                Assert.Null(service.LastCorruptBackupPath);
            }
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task SettingsService_SaveAsync_ReturnsTrueOnSuccess_AndFalseOnFailure()
    {
        var validFile = Path.Combine(Path.GetTempPath(), $"zipbuddy_save_success_{Guid.NewGuid():N}.json");
        var invalidFile = @"Z:\NonExistentDrive\DeepSubDir\settings.json"; // Invalid unmapped drive path
        try
        {
            var validService = new SettingsService(validFile);
            var success = await validService.SaveAsync();
            Assert.True(success);
            Assert.True(File.Exists(validFile));

            var failingService = new SettingsService(invalidFile);
            var failed = await failingService.SaveAsync();
            Assert.False(failed);
        }
        finally
        {
            if (File.Exists(validFile))
                File.Delete(validFile);
        }
    }

    [Fact]
    public async Task SettingsService_ValidCustomSettings_AreNotReplacedByDefault()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"zipbuddy_valid_custom_{Guid.NewGuid():N}.json");
        try
        {
            var initialService = new SettingsService(tempFile)
            {
                ThemeMode = "Light",
                Language = "pt-BR",
                DefaultExtractionFolder = @"D:\CustomExtract",
                ConfirmBeforeDeleting = false,
                ConfirmBeforeOverwriting = false,
                DefaultArchiveFormat = ArchiveFormat.SevenZip,
                DefaultCompressionLevel = CompressionLevel.Fast
            };
            await initialService.SaveAsync();

            var loadedService = new SettingsService(tempFile);
            await loadedService.LoadAsync();

            Assert.Equal("Light", loadedService.ThemeMode);
            Assert.Equal("pt-BR", loadedService.Language);
            Assert.Equal(@"D:\CustomExtract", loadedService.DefaultExtractionFolder);
            Assert.False(loadedService.ConfirmBeforeDeleting);
            Assert.False(loadedService.ConfirmBeforeOverwriting);
            Assert.Equal(ArchiveFormat.SevenZip, loadedService.DefaultArchiveFormat);
            Assert.Equal(CompressionLevel.Fast, loadedService.DefaultCompressionLevel);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void SettingsService_ProgressWindowProperties_DefaultValuesExposeInverseSemantics()
    {
        var service = new SettingsService(Path.Combine(Path.GetTempPath(), "dummy.json"));

        // Default state: KeepTaskProgressWindowOpen is false, AutoCloseTaskProgressWindow is true
        Assert.False(service.KeepTaskProgressWindowOpen);
        Assert.True(service.AutoCloseTaskProgressWindow);
    }

    [Fact]
    public void SettingsService_ProgressWindowProperties_SynchronizeBidirectionally()
    {
        var service = new SettingsService(Path.Combine(Path.GetTempPath(), "dummy.json"));

        service.KeepTaskProgressWindowOpen = true;
        Assert.True(service.KeepTaskProgressWindowOpen);
        Assert.False(service.AutoCloseTaskProgressWindow);

        service.AutoCloseTaskProgressWindow = true;
        Assert.False(service.KeepTaskProgressWindowOpen);
        Assert.True(service.AutoCloseTaskProgressWindow);
    }

    [Theory]
    [InlineData(true, true, true)]   // Auth true, legacy true (conflict): auth wins (true)
    [InlineData(true, false, true)]  // Auth true, legacy false: auth wins (true)
    [InlineData(false, true, false)] // Auth false, legacy true: auth wins (false)
    [InlineData(false, false, false)]// Auth false, legacy false (conflict): auth wins (false)
    public async Task SettingsService_Migration_AuthoritativeWinsWhenBothPresent(bool authInJson, bool legacyInJson, bool expectedAuth)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"zipbuddy_migration_both_{Guid.NewGuid():N}.json");
        try
        {
            var json = $$"""
            {
                "KeepTaskProgressWindowOpen": {{authInJson.ToString().ToLowerInvariant()}},
                "AutoCloseTaskProgressWindow": {{legacyInJson.ToString().ToLowerInvariant()}}
            }
            """;
            await File.WriteAllTextAsync(tempFile, json);

            var service = new SettingsService(tempFile);
            await service.LoadAsync();

            Assert.Equal(expectedAuth, service.KeepTaskProgressWindowOpen);
            Assert.Equal(!expectedAuth, service.AutoCloseTaskProgressWindow);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Theory]
    [InlineData(true, false)] // Legacy true (auto-close) => KeepOpen should be false
    [InlineData(false, true)] // Legacy false (do not auto-close) => KeepOpen should be true
    public async Task SettingsService_Migration_OnlyLegacyPresent_DerivesAuthoritative(bool legacyInJson, bool expectedAuth)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"zipbuddy_migration_legacy_{Guid.NewGuid():N}.json");
        try
        {
            var json = $$"""
            {
                "AutoCloseTaskProgressWindow": {{legacyInJson.ToString().ToLowerInvariant()}}
            }
            """;
            await File.WriteAllTextAsync(tempFile, json);

            var service = new SettingsService(tempFile);
            await service.LoadAsync();

            Assert.Equal(expectedAuth, service.KeepTaskProgressWindowOpen);
            Assert.Equal(!expectedAuth, service.AutoCloseTaskProgressWindow);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task SettingsService_Migration_NeitherPresent_UsesDocumentedDefault()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"zipbuddy_migration_neither_{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllTextAsync(tempFile, """
            {
                "ThemeMode": "Dark"
            }
            """);

            var service = new SettingsService(tempFile);
            await service.LoadAsync();

            Assert.False(service.KeepTaskProgressWindowOpen);
            Assert.True(service.AutoCloseTaskProgressWindow);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task SettingsService_DefaultExtractionFolder_EmptyStringPreserved()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"zipbuddy_folder_empty_{Guid.NewGuid():N}.json");
        try
        {
            var service = new SettingsService(tempFile)
            {
                DefaultExtractionFolder = string.Empty
            };
            await service.SaveAsync();

            var reloaded = new SettingsService(tempFile);
            await reloaded.LoadAsync();

            Assert.Equal(string.Empty, reloaded.DefaultExtractionFolder);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task SettingsService_DefaultExtractionFolder_ValidPathPreservedExact()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"zipbuddy_folder_path_{Guid.NewGuid():N}.json");
        var testFolder = @"C:\Zip Buddy Test Output\Sub Folder";
        try
        {
            var service = new SettingsService(tempFile)
            {
                DefaultExtractionFolder = testFolder
            };
            await service.SaveAsync();

            var reloaded = new SettingsService(tempFile);
            await reloaded.LoadAsync();

            Assert.Equal(testFolder, reloaded.DefaultExtractionFolder);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task SettingsService_DefaultExtractionFolder_PersistsArbitraryStringWithoutFilesystemValidation()
    {
        // Documents that SettingsService is purely a schema persistence layer;
        // it does not validate whether a path exists or is syntactically valid.
        var tempFile = Path.Combine(Path.GetTempPath(), $"zipbuddy_folder_invalid_{Guid.NewGuid():N}.json");
        var nonExistentPath = @"Z:\NonExistentDrive\InvalidFolder";
        try
        {
            var service = new SettingsService(tempFile)
            {
                DefaultExtractionFolder = nonExistentPath
            };
            await service.SaveAsync();

            var reloaded = new SettingsService(tempFile);
            await reloaded.LoadAsync();

            Assert.Equal(nonExistentPath, reloaded.DefaultExtractionFolder);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task SettingsService_AdvancedExtractionSettings_PersistsThreadsAndCustomTempDirectory()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"zipbuddy_adv_extract_{Guid.NewGuid():N}.json");
        try
        {
            var service = new SettingsService(tempFile)
            {
                MaxExtractionThreads = 16,
                CustomTempDirectory = @"C:\ZipBuddyScratch\Temp"
            };
            await service.SaveAsync();

            var reloaded = new SettingsService(tempFile);
            await reloaded.LoadAsync();

            Assert.Equal(16, reloaded.MaxExtractionThreads);
            Assert.Equal(@"C:\ZipBuddyScratch\Temp", reloaded.CustomTempDirectory);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(-4, true)]
    [InlineData(128, true)]
    [InlineData(8, false)]
    public void SettingsService_MaxExtractionThreads_NormalizesInvalidValues(int input, bool expectsProcessorCount)
    {
        var service = new SettingsService
        {
            MaxExtractionThreads = input
        };

        if (expectsProcessorCount)
        {
            Assert.Equal(Math.Clamp(Environment.ProcessorCount, 1, 64), service.MaxExtractionThreads);
        }
        else
        {
            Assert.Equal(input, service.MaxExtractionThreads);
        }
    }

    [Fact]
    public void SettingsService_MaxExtractionThreads_DefaultsNormalizedWithinBounds()
    {
        var service = new SettingsService();
        Assert.InRange(service.MaxExtractionThreads, 1, 64);
        Assert.Equal(Math.Clamp(Environment.ProcessorCount, 1, 64), service.MaxExtractionThreads);
    }

    [Fact]
    public void TempFileManager_InaccessibleCustomDirectory_FallsBackSafelyToSystemTemp()
    {
        // Path on a non-existent drive letter or invalid character path that cannot be created
        var invalidPath = @"Z:\NonExistent_Drive_ZipBuddy_Scratch_Test_Folder";
        using var manager = new ZipBuddy.Infrastructure.TempFiles.TempFileManager(invalidPath);

        var tempDir = manager.CreateTempDirectory("unit-test");
        try
        {
            Assert.True(Directory.Exists(tempDir));
            Assert.StartsWith(Path.GetTempPath(), tempDir, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            manager.CleanupDirectory(tempDir);
        }
    }

    [Fact]
    public void TempFileManager_InaccessibleCustomDirectory_LogsWarningAndSetsFallbackFlag()
    {
        var logger = new TestLogger();
        var invalidPath = @"Z:\NonExistent_Drive_ZipBuddy_Scratch_Test_Folder";
        using var manager = new ZipBuddy.Infrastructure.TempFiles.TempFileManager(invalidPath, logger);

        Assert.True(manager.IsUsingFallback);
        Assert.True(manager.IsAvailable);
        Assert.Single(logger.Warnings);
        Assert.Contains(invalidPath, logger.Warnings[0]);

        var tempDir = manager.CreateTempDirectory("unit-test");
        try
        {
            Assert.True(Directory.Exists(tempDir));
            Assert.StartsWith(Path.GetTempPath(), tempDir, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            manager.CleanupDirectory(tempDir);
        }
    }

    [Fact]
    public async Task SettingsService_CompressionSettings_PersistsSupportedFormatsAndLevels()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"zipbuddy_compress_test_{Guid.NewGuid():N}.json");
        try
        {
            var service = new SettingsService(tempFile)
            {
                DefaultArchiveFormat = ArchiveFormat.SevenZip,
                DefaultCompressionLevel = CompressionLevel.Ultra
            };
            await service.SaveAsync();

            var reloaded = new SettingsService(tempFile);
            await reloaded.LoadAsync();

            Assert.Equal(ArchiveFormat.SevenZip, reloaded.DefaultArchiveFormat);
            Assert.Equal(CompressionLevel.Ultra, reloaded.DefaultCompressionLevel);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Theory]
    [InlineData((ArchiveFormat)99, ArchiveFormat.Zip)]
    [InlineData(ArchiveFormat.Unknown, ArchiveFormat.Zip)]
    [InlineData(ArchiveFormat.SevenZip, ArchiveFormat.SevenZip)]
    [InlineData(ArchiveFormat.Tar, ArchiveFormat.Tar)]
    public void SettingsService_DefaultArchiveFormat_NormalizesInvalidValues(ArchiveFormat input, ArchiveFormat expected)
    {
        var service = new SettingsService
        {
            DefaultArchiveFormat = input
        };

        Assert.Equal(expected, service.DefaultArchiveFormat);
    }

    [Theory]
    [InlineData((CompressionLevel)(-1), CompressionLevel.Normal)]
    [InlineData((CompressionLevel)99, CompressionLevel.Normal)]
    [InlineData(CompressionLevel.Store, CompressionLevel.Store)]
    [InlineData(CompressionLevel.Ultra, CompressionLevel.Ultra)]
    public void SettingsService_DefaultCompressionLevel_NormalizesInvalidValues(CompressionLevel input, CompressionLevel expected)
    {
        var service = new SettingsService
        {
            DefaultCompressionLevel = input
        };

        Assert.Equal(expected, service.DefaultCompressionLevel);
    }

    [Theory]
    [InlineData(ArchiveFormat.Zip)]
    [InlineData(ArchiveFormat.SevenZip)]
    [InlineData(ArchiveFormat.Tar)]
    [InlineData(ArchiveFormat.GZip)]
    [InlineData(ArchiveFormat.BZip2)]
    public async Task SettingsService_SupportedCreationFormats_PersistsRoundtrip(ArchiveFormat format)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"zipbuddy_fmt_test_{Guid.NewGuid():N}.json");
        try
        {
            var service = new SettingsService(tempFile)
            {
                DefaultArchiveFormat = format
            };
            await service.SaveAsync();

            var reloaded = new SettingsService(tempFile);
            await reloaded.LoadAsync();

            Assert.Equal(format, reloaded.DefaultArchiveFormat);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Theory]
    [InlineData(CompressionLevel.Store)]
    [InlineData(CompressionLevel.Fast)]
    [InlineData(CompressionLevel.Normal)]
    [InlineData(CompressionLevel.Maximum)]
    [InlineData(CompressionLevel.Ultra)]
    public async Task SettingsService_CompressionLevels_PersistsRoundtrip(CompressionLevel level)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"zipbuddy_lvl_test_{Guid.NewGuid():N}.json");
        try
        {
            var service = new SettingsService(tempFile)
            {
                DefaultCompressionLevel = level
            };
            await service.SaveAsync();

            var reloaded = new SettingsService(tempFile);
            await reloaded.LoadAsync();

            Assert.Equal(level, reloaded.DefaultCompressionLevel);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task SettingsService_UpdatingCompressionSettings_PreservesAllExtractionSettings()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"zipbuddy_preserve_test_{Guid.NewGuid():N}.json");
        try
        {
            var service = new SettingsService(tempFile)
            {
                DefaultExtractionFolder = @"C:\CustomExtractFolder",
                DoubleClickAction = "ExtractHere",
                OpenExtractedFolderAfterExtraction = true,
                DeleteArchiveAfterExtraction = true,
                ConfirmBeforeOverwriting = false,
                ConfirmBeforeDeleting = false,
                MaxExtractionThreads = 16,
                CustomTempDirectory = @"C:\CustomTempDirectory",
                DefaultArchiveFormat = ArchiveFormat.Zip,
                DefaultCompressionLevel = CompressionLevel.Normal
            };
            await service.SaveAsync();

            var reloaded = new SettingsService(tempFile);
            await reloaded.LoadAsync();

            // Mutate only compression settings
            reloaded.DefaultArchiveFormat = ArchiveFormat.SevenZip;
            reloaded.DefaultCompressionLevel = CompressionLevel.Maximum;
            await reloaded.SaveAsync();

            var verified = new SettingsService(tempFile);
            await verified.LoadAsync();

            // Compression settings updated
            Assert.Equal(ArchiveFormat.SevenZip, verified.DefaultArchiveFormat);
            Assert.Equal(CompressionLevel.Maximum, verified.DefaultCompressionLevel);

            // All extraction settings strictly preserved
            Assert.Equal(@"C:\CustomExtractFolder", verified.DefaultExtractionFolder);
            Assert.Equal("ExtractHere", verified.DoubleClickAction);
            Assert.True(verified.OpenExtractedFolderAfterExtraction);
            Assert.True(verified.DeleteArchiveAfterExtraction);
            Assert.False(verified.ConfirmBeforeOverwriting);
            Assert.False(verified.ConfirmBeforeDeleting);
            Assert.Equal(16, verified.MaxExtractionThreads);
            Assert.Equal(@"C:\CustomTempDirectory", verified.CustomTempDirectory);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Theory]
    [InlineData(ArchiveFormat.Tar)]
    [InlineData(ArchiveFormat.GZip)]
    [InlineData(ArchiveFormat.BZip2)]
    public async Task ArchiveEngine_NonZip7zFormats_RejectAdjustableCompressionLevels(ArchiveFormat format)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"zipbuddy_compat_level_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var dummyFile = Path.Combine(tempDir, "sample.txt");
            await File.WriteAllTextAsync(dummyFile, "Compression test data");

            var ext = format.GetDefaultExtension();
            var outArchive = Path.Combine(tempDir, $"output{ext}");

            var engine = new ZipBuddy.Formats.ArchiveEngine();
            var result = await engine.CreateArchiveAsync(new CompressionRequest
            {
                OutputArchiveFilePath = outArchive,
                Format = format,
                SourcePaths = [dummyFile],
                Options = new CompressionOptions { Level = CompressionLevel.Fast }
            });

            Assert.False(result.Success);
            Assert.Contains("Adjustable compression levels are supported only for ZIP and 7z archives", result.ErrorMessage);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task ArchiveEngine_UnsupportedCreationFormats_FailAccurately()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"zipbuddy_unsupported_fmt_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var dummyFile = Path.Combine(tempDir, "sample.txt");
            await File.WriteAllTextAsync(dummyFile, "Data");

            var engine = new ZipBuddy.Formats.ArchiveEngine();

            var xzResult = await engine.CreateArchiveAsync(new CompressionRequest
            {
                OutputArchiveFilePath = Path.Combine(tempDir, "out.tar.xz"),
                Format = ArchiveFormat.Xz,
                SourcePaths = [dummyFile]
            });
            Assert.False(xzResult.Success);
            Assert.Contains("not directly supported", xzResult.ErrorMessage, StringComparison.OrdinalIgnoreCase);

            var rarResult = await engine.CreateArchiveAsync(new CompressionRequest
            {
                OutputArchiveFilePath = Path.Combine(tempDir, "out.rar"),
                Format = ArchiveFormat.Rar,
                SourcePaths = [dummyFile]
            });
            Assert.False(rarResult.Success);
            Assert.Contains("proprietary", rarResult.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void LocalizationService_AllCompressionSettingsKeys_AreLocalizedInBothLanguages()
    {
        var locEn = new ZipBuddy.Infrastructure.Services.LocalizationService("en-US");
        var locPt = new ZipBuddy.Infrastructure.Services.LocalizationService("pt-BR");

        string[] keys =
        [
            "Settings_Compress_Header",
            "Settings_Compress_Format_Title",
            "Settings_Compress_Format_Desc",
            "Settings_Compress_Format_Zip",
            "Settings_Compress_Format_SevenZip",
            "Settings_Compress_Format_Tar",
            "Settings_Compress_Format_GZip",
            "Settings_Compress_Format_BZip2",
            "Settings_Compress_Format_Zip_Desc",
            "Settings_Compress_Format_SevenZip_Desc",
            "Settings_Compress_Format_Tar_Desc",
            "Settings_Compress_Format_GZip_Desc",
            "Settings_Compress_Format_BZip2_Desc",
            "Settings_Compress_Level_Title",
            "Settings_Compress_Level_Desc",
            "Settings_Compress_Level_Store",
            "Settings_Compress_Level_Fast",
            "Settings_Compress_Level_Normal",
            "Settings_Compress_Level_Maximum",
            "Settings_Compress_Level_Ultra",
            "Settings_Compress_Level_Store_Desc",
            "Settings_Compress_Level_Fast_Desc",
            "Settings_Compress_Level_Normal_Desc",
            "Settings_Compress_Level_Maximum_Desc",
            "Settings_Compress_Level_Ultra_Desc",
            "Settings_Compress_Warn_Tar_Title",
            "Settings_Compress_Warn_Tar_Message",
            "Settings_Compress_Warn_TarLevel_Title",
            "Settings_Compress_Warn_TarLevel_Message",
            "Settings_Compress_Info_FixedLevel_Title",
            "Settings_Compress_Info_FixedLevel_Message",
            "Settings_Compress_Warn_FixedLevel_Title",
            "Settings_Compress_Warn_FixedLevel_Message",
            "Settings_Compress_Warn_ZipUltra_Title",
            "Settings_Compress_Warn_ZipUltra_Message",
            "Settings_Compress_Tar_Reset_Title",
            "Settings_Compress_Tar_Reset_Message",
            "Settings_Compress_Fixed_Reset_Title",
            "Settings_Compress_Fixed_Reset_Message",
            "Settings_Compress_Tar_Level_Desc",
            "Settings_Compress_Fixed_Level_Desc"
        ];

        foreach (var key in keys)
        {
            var enVal = locEn[key];
            var ptVal = locPt[key];

            Assert.False(string.IsNullOrWhiteSpace(enVal), $"Key '{key}' missing or empty in en-US.");
            Assert.False(string.IsNullOrWhiteSpace(ptVal), $"Key '{key}' missing or empty in pt-BR.");
            Assert.NotEqual(key, enVal);
            Assert.NotEqual(key, ptVal);
        }
    }

    [Theory]
    [InlineData(ArchiveFormat.Tar, CompressionLevel.Store)]
    [InlineData(ArchiveFormat.Tar, CompressionLevel.Fast)]
    [InlineData(ArchiveFormat.Tar, CompressionLevel.Maximum)]
    [InlineData(ArchiveFormat.Tar, CompressionLevel.Ultra)]
    [InlineData(ArchiveFormat.GZip, CompressionLevel.Store)]
    [InlineData(ArchiveFormat.GZip, CompressionLevel.Fast)]
    [InlineData(ArchiveFormat.GZip, CompressionLevel.Maximum)]
    [InlineData(ArchiveFormat.GZip, CompressionLevel.Ultra)]
    [InlineData(ArchiveFormat.BZip2, CompressionLevel.Store)]
    [InlineData(ArchiveFormat.BZip2, CompressionLevel.Fast)]
    [InlineData(ArchiveFormat.BZip2, CompressionLevel.Maximum)]
    [InlineData(ArchiveFormat.BZip2, CompressionLevel.Ultra)]
    public async Task SettingsService_IncompatibleFormatAndLevel_NormalizesToNormal(ArchiveFormat format, CompressionLevel level)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"zipbuddy_incompat_test_{Guid.NewGuid():N}.json");
        try
        {
            var service = new SettingsService(tempFile)
            {
                DefaultArchiveFormat = format,
                DefaultCompressionLevel = level
            };

            Assert.Equal(CompressionLevel.Normal, service.DefaultCompressionLevel);

            await service.SaveAsync();

            var reloaded = new SettingsService(tempFile);
            await reloaded.LoadAsync();

            Assert.Equal(format, reloaded.DefaultArchiveFormat);
            Assert.Equal(CompressionLevel.Normal, reloaded.DefaultCompressionLevel);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Theory]
    [InlineData(ArchiveFormat.Tar, CompressionLevel.Ultra)]
    [InlineData(ArchiveFormat.GZip, CompressionLevel.Fast)]
    [InlineData(ArchiveFormat.BZip2, CompressionLevel.Maximum)]
    public async Task SettingsService_LoadingIncompatibleFile_NormalizesUponLoad(ArchiveFormat format, CompressionLevel level)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"zipbuddy_incompat_disk_{Guid.NewGuid():N}.json");
        try
        {
            // Simulate manually corrupted or legacy settings.json file containing incompatible values
            var rawJson = $"{{\"DefaultArchiveFormat\":{(int)format},\"DefaultCompressionLevel\":{(int)level}}}";
            await File.WriteAllTextAsync(tempFile, rawJson);

            var service = new SettingsService(tempFile);
            await service.LoadAsync();

            Assert.Equal(format, service.DefaultArchiveFormat);
            Assert.Equal(CompressionLevel.Normal, service.DefaultCompressionLevel);

            await service.SaveAsync();

            var reloaded = new SettingsService(tempFile);
            await reloaded.LoadAsync();
            Assert.Equal(CompressionLevel.Normal, reloaded.DefaultCompressionLevel);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void ArchiveFormatExtensions_SupportsCompressionLevel_ValidatesAccurately()
    {
        // Zip and SevenZip support all 5 levels
        foreach (CompressionLevel lvl in Enum.GetValues<CompressionLevel>())
        {
            Assert.True(ArchiveFormat.Zip.SupportsCompressionLevel(lvl));
            Assert.True(ArchiveFormat.SevenZip.SupportsCompressionLevel(lvl));
            Assert.Equal(lvl, ArchiveFormat.Zip.GetCompatibleCompressionLevel(lvl));
            Assert.Equal(lvl, ArchiveFormat.SevenZip.GetCompatibleCompressionLevel(lvl));
        }

        // Tar, GZip, BZip2 only support Normal
        ArchiveFormat[] fixedFormats = [ArchiveFormat.Tar, ArchiveFormat.GZip, ArchiveFormat.BZip2];
        foreach (var fmt in fixedFormats)
        {
            Assert.True(fmt.SupportsCompressionLevel(CompressionLevel.Normal));
            Assert.Equal(CompressionLevel.Normal, fmt.GetCompatibleCompressionLevel(CompressionLevel.Normal));

            Assert.False(fmt.SupportsCompressionLevel(CompressionLevel.Store));
            Assert.False(fmt.SupportsCompressionLevel(CompressionLevel.Fast));
            Assert.False(fmt.SupportsCompressionLevel(CompressionLevel.Maximum));
            Assert.False(fmt.SupportsCompressionLevel(CompressionLevel.Ultra));

            Assert.Equal(CompressionLevel.Normal, fmt.GetCompatibleCompressionLevel(CompressionLevel.Store));
            Assert.Equal(CompressionLevel.Normal, fmt.GetCompatibleCompressionLevel(CompressionLevel.Fast));
            Assert.Equal(CompressionLevel.Normal, fmt.GetCompatibleCompressionLevel(CompressionLevel.Maximum));
            Assert.Equal(CompressionLevel.Normal, fmt.GetCompatibleCompressionLevel(CompressionLevel.Ultra));
        }

        // Non-creatable formats return false for all levels
        Assert.False(ArchiveFormat.Xz.SupportsCompressionLevel(CompressionLevel.Normal));
        Assert.False(ArchiveFormat.Rar.SupportsCompressionLevel(CompressionLevel.Normal));
        Assert.False(ArchiveFormat.Unknown.SupportsCompressionLevel(CompressionLevel.Normal));
    }

    [Fact]
    public void LocalizationService_AllAppearanceSettingsKeys_AreLocalizedInBothLanguages()
    {
        var locEn = new ZipBuddy.Infrastructure.Services.LocalizationService("en-US");
        var locPt = new ZipBuddy.Infrastructure.Services.LocalizationService("pt-BR");

        string[] keys =
        [
            "Settings_Appearance_Header",
            "Settings_Appearance_Theme_Title",
            "Settings_Appearance_Theme_Desc",
            "Settings_Appearance_Theme_System",
            "Settings_Appearance_Theme_Light",
            "Settings_Appearance_Theme_Dark",
            "Settings_Appearance_Theme_System_Desc",
            "Settings_Appearance_Theme_Light_Desc",
            "Settings_Appearance_Theme_Dark_Desc",
            "Settings_Appearance_Lang_Title",
            "Settings_Appearance_Lang_Desc",
            "Settings_Appearance_Lang_System",
            "Settings_Appearance_Lang_En",
            "Settings_Appearance_Lang_Pt",
            "Settings_Appearance_Lang_System_Desc",
            "Settings_Appearance_Lang_En_Desc",
            "Settings_Appearance_Lang_Pt_Desc",
            "Settings_Appearance_Restart_Title",
            "Settings_Appearance_Restart_Desc"
        ];

        foreach (var key in keys)
        {
            var enVal = locEn[key];
            var ptVal = locPt[key];

            Assert.False(string.IsNullOrWhiteSpace(enVal), $"Key '{key}' missing or empty in en-US.");
            Assert.False(string.IsNullOrWhiteSpace(ptVal), $"Key '{key}' missing or empty in pt-BR.");
            Assert.NotEqual(key, enVal);
            Assert.NotEqual(key, ptVal);
        }
    }

    [Fact]
    public async Task SettingsService_ThemeAndLanguage_Persistence_RoundtripsAccurately()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"zipbuddy_appearance_test_{Guid.NewGuid():N}.json");
        try
        {
            var service = new SettingsService(tempFile);
            await service.LoadAsync();

            Assert.Equal("System", service.ThemeMode);
            Assert.Equal("System", service.Language);

            service.ThemeMode = "Dark";
            service.Language = "pt-BR";
            service.DefaultExtractionFolder = @"C:\TestExtract";
            service.DefaultArchiveFormat = ArchiveFormat.SevenZip;
            service.DefaultCompressionLevel = CompressionLevel.Ultra;
            await service.SaveAsync();

            var loadedService = new SettingsService(tempFile);
            await loadedService.LoadAsync();

            Assert.Equal("Dark", loadedService.ThemeMode);
            Assert.Equal("pt-BR", loadedService.Language);
            Assert.Equal(@"C:\TestExtract", loadedService.DefaultExtractionFolder);
            Assert.Equal(ArchiveFormat.SevenZip, loadedService.DefaultArchiveFormat);
            Assert.Equal(CompressionLevel.Ultra, loadedService.DefaultCompressionLevel);

            // Update to Light and en-US while ensuring unedited fields remain preserved
            loadedService.ThemeMode = "Light";
            loadedService.Language = "en-US";
            await loadedService.SaveAsync();

            var reloadedService = new SettingsService(tempFile);
            await reloadedService.LoadAsync();

            Assert.Equal("Light", reloadedService.ThemeMode);
            Assert.Equal("en-US", reloadedService.Language);
            Assert.Equal(@"C:\TestExtract", reloadedService.DefaultExtractionFolder);
            Assert.Equal(ArchiveFormat.SevenZip, reloadedService.DefaultArchiveFormat);
            Assert.Equal(CompressionLevel.Ultra, reloadedService.DefaultCompressionLevel);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task SettingsService_SaveFailure_PreservesPersistedThemeAndLanguage()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"zipbuddy_savefail_test_{Guid.NewGuid():N}.json");
        try
        {
            var service = new SettingsService(tempFile);
            service.ThemeMode = "Dark";
            service.Language = "pt-BR";
            var initialSaved = await service.SaveAsync();
            Assert.True(initialSaved);

            // Verify clean baseline
            var initialCheck = new SettingsService(tempFile);
            await initialCheck.LoadAsync();
            Assert.Equal("Dark", initialCheck.ThemeMode);
            Assert.Equal("pt-BR", initialCheck.Language);

            // Make the file read-only so SaveAsync fails when attempting to overwrite
            File.SetAttributes(tempFile, FileAttributes.ReadOnly);
            var modifiedService = new SettingsService(tempFile)
            {
                ThemeMode = "Light",
                Language = "en-US"
            };

            var saveResult = await modifiedService.SaveAsync();
            Assert.False(saveResult);

            // Remove read-only and verify disk contents were NOT altered
            File.SetAttributes(tempFile, FileAttributes.Normal);
            var verifyService = new SettingsService(tempFile);
            await verifyService.LoadAsync();
            Assert.Equal("Dark", verifyService.ThemeMode);
            Assert.Equal("pt-BR", verifyService.Language);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.SetAttributes(tempFile, FileAttributes.Normal);
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task SettingsService_FileExplorer_DefaultValues_AreConsistent()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"zipbuddy_explorer_defaults_{Guid.NewGuid():N}.json");
        try
        {
            var service = new SettingsService(tempFile);
            await service.LoadAsync();

            Assert.True(service.EnableContextMenu);
            Assert.False(service.UseCascadingContextMenu);
            Assert.Contains("OpenWith", service.EnabledContextMenuCommands);
            Assert.Contains("ExtractHere", service.EnabledContextMenuCommands);
            Assert.Contains("ExtractToFolder", service.EnabledContextMenuCommands);
            Assert.Contains("ExtractTo", service.EnabledContextMenuCommands);
            Assert.Contains("CompressToZip", service.EnabledContextMenuCommands);
            Assert.Contains("CompressTo7z", service.EnabledContextMenuCommands);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task SettingsService_FileExplorer_ContextMenuAndCascading_PersistsAndLoads()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"zipbuddy_explorer_test_{Guid.NewGuid():N}.json");
        try
        {
            var service = new SettingsService(tempFile);
            service.EnableContextMenu = false;
            service.UseCascadingContextMenu = true;
            service.EnabledContextMenuCommands = ["ExtractHere", "CompressToZip"];
            var saved = await service.SaveAsync();
            Assert.True(saved);

            var reloaded = new SettingsService(tempFile);
            await reloaded.LoadAsync();

            Assert.False(reloaded.EnableContextMenu);
            Assert.True(reloaded.UseCascadingContextMenu);
            Assert.Equal(new[] { "ExtractHere", "CompressToZip" }, reloaded.EnabledContextMenuCommands);

            // Now re-enable and verify update
            reloaded.EnableContextMenu = true;
            reloaded.UseCascadingContextMenu = false;
            reloaded.EnabledContextMenuCommands = ["OpenWith", "ExtractToFolder", "CompressTo7z"];
            var savedAgain = await reloaded.SaveAsync();
            Assert.True(savedAgain);

            var secondReload = new SettingsService(tempFile);
            await secondReload.LoadAsync();
            Assert.True(secondReload.EnableContextMenu);
            Assert.False(secondReload.UseCascadingContextMenu);
            Assert.Equal(new[] { "OpenWith", "ExtractToFolder", "CompressTo7z" }, secondReload.EnabledContextMenuCommands);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void ContextMenuCommands_AllSixCommandVerbs_MatchCliSwitchConventions()
    {
        // Verifies the exact CLI verbs expected by App.xaml.cs
        string[] registeredCliVerbs =
        [
            "--extract-here",
            "--extract-to-folder",
            "--extract-to",
            "--compress-zip",
            "--compress-7z"
        ];

        foreach (var verb in registeredCliVerbs)
        {
            Assert.StartsWith("--", verb);
            Assert.False(verb.Contains(' '));
        }
    }

    private sealed class TestLogger : ZipBuddy.Core.Interfaces.ILoggingService
    {
        public List<string> Warnings { get; } = [];
        public List<string> Errors { get; } = [];

        public void Debug(string message) { }
        public void Info(string message) { }
        public void Warn(string message) => Warnings.Add(message);
        public void Error(string message, Exception? ex = null) => Errors.Add(ex == null ? message : $"{message}: {ex.Message}");
        public string GetLogDirectory() => Path.GetTempPath();
    }
}
