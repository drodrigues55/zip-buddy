using ZipBuddy.Core.Models;
using ZipBuddy.Formats;

namespace ZipBuddy.Tests;

/// <summary>
/// Regression coverage for RAR4/RAR5 reading and extraction, including password-protected archives.
/// Sample archives come from the SharpCompress test corpus (password: "test").
/// </summary>
public class RarExtractionTests : IDisposable
{
    private readonly string _workDir = Path.Combine(Path.GetTempPath(), "ZipBuddy_RarTests_" + Guid.NewGuid().ToString("N"));
    private readonly ArchiveEngine _engine = new();

    public RarExtractionTests() => Directory.CreateDirectory(_workDir);

    public void Dispose()
    {
        try { Directory.Delete(_workDir, recursive: true); } catch { }
        GC.SuppressFinalize(this);
    }

    private static string Asset(string name) => Path.Combine(AppContext.BaseDirectory, "TestAssets", "Rar", name);

    [Theory]
    [InlineData("Rar.rar")]
    [InlineData("Rar.encrypted_filesAndHeader.rar")]
    [InlineData("Rar5.encrypted_filesAndHeader.rar")]
    public void DetectFormat_RarFiles_AreDetectedCorrectly(string name)
    {
        Assert.Equal(ArchiveFormat.Rar, _engine.DetectFormat(Asset(name)));
    }

    [Fact]
    public async Task OpenPlainRar_ExposesEntries()
    {
        var info = await _engine.OpenArchiveAsync(Asset("Rar.rar"));

        Assert.Equal(ArchiveFormat.Rar, info.Format);
        Assert.True(info.TotalEntries > 0);
        Assert.Contains(info.Entries, e => e.Name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("Rar.encrypted_filesAndHeader.rar")]
    [InlineData("Rar5.encrypted_filesAndHeader.rar")]
    public async Task OpenEncryptedRar_WithoutPassword_FailsWithPasswordMessage(string name)
    {
        var ex = await Assert.ThrowsAnyAsync<Exception>(() => _engine.OpenArchiveAsync(Asset(name)));

        Assert.True(OperationResult.IndicatesPasswordFailure(ex.Message), $"Unexpected message: {ex.Message}");
    }

    [Fact]
    public async Task ExtractEncryptedMultiVolumeRar_ReportsUnsupportedFeature_NotPasswordError()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestAssets", "Rar", "MultiVolume", "Rar.EncryptedParts.part01.rar");
        var result = await _engine.ExtractAsync(new ExtractionRequest
        {
            ArchiveFilePath = path,
            DestinationDirectory = Path.Combine(_workDir, "multivolume"),
            Password = "test",
            DefaultConflictPolicy = ConflictPolicy.Overwrite
        });

        Assert.False(result.Success);
        Assert.True(result.UnsupportedFeature);
        Assert.False(result.RequiresPassword);
        Assert.False(OperationResult.IndicatesPasswordFailure(result.ErrorMessage));
        Assert.Contains("multi-volume", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Rar.rar", null)]
    [InlineData("Rar.encrypted_filesAndHeader.rar", "test")]
    [InlineData("Rar5.encrypted_filesAndHeader.rar", "test")]
    public async Task ExtractRar_WritesAllFiles(string name, string? password)
    {
        var dest = Path.Combine(_workDir, Path.GetFileNameWithoutExtension(name));
        var result = await _engine.ExtractAsync(new ExtractionRequest
        {
            ArchiveFilePath = Asset(name),
            DestinationDirectory = dest,
            Password = password,
            DefaultConflictPolicy = ConflictPolicy.Overwrite
        });

        Assert.True(result.Success, result.ErrorMessage ?? result.DetailedError);
        Assert.Equal(3, result.FilesProcessed);
        Assert.True(Directory.Exists(dest));
        Assert.Equal(3, Directory.GetFiles(dest, "*", SearchOption.AllDirectories).Length);
    }
}