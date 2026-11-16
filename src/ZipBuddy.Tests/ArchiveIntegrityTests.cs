using ZipBuddy.Core.Models;
using ZipBuddy.Formats;

namespace ZipBuddy.Tests;

public class ArchiveIntegrityTests : IDisposable
{
    private readonly string _workDir = Path.Combine(Path.GetTempPath(), "ZipBuddy_Integrity_" + Guid.NewGuid().ToString("N"));
    private readonly ArchiveEngine _engine = new();

    public ArchiveIntegrityTests() => Directory.CreateDirectory(_workDir);

    public void Dispose()
    {
        try { Directory.Delete(_workDir, recursive: true); } catch { }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task TestArchive_CorruptedEntry_ReportsCorruptedEntries()
    {
        var source = Path.Combine(_workDir, "data.bin");
        var data = new byte[4096];
        Random.Shared.NextBytes(data);
        await File.WriteAllBytesAsync(source, data);

        var archive = Path.Combine(_workDir, "corrupt.zip");
        var created = await _engine.CreateArchiveAsync(new CompressionRequest
        {
            OutputArchiveFilePath = archive,
            Format = ArchiveFormat.Zip,
            SourcePaths = [source],
            Options = new CompressionOptions { Level = CompressionLevel.Store }
        });
        Assert.True(created.Success, created.ErrorMessage);

        var bytes = await File.ReadAllBytesAsync(archive);
        var header = FindLocalFileHeader(bytes);
        Assert.True(header >= 0, "ZIP local file header not found.");

        var nameLength = BitConverter.ToUInt16(bytes, header + 26);
        var extraLength = BitConverter.ToUInt16(bytes, header + 28);
        var dataOffset = header + 30 + nameLength + extraLength;
        Assert.True(dataOffset < bytes.Length);

        bytes[dataOffset] ^= 0xFF; // flip one stored byte so the CRC check fails
        await File.WriteAllBytesAsync(archive, bytes);

        var result = await _engine.TestArchiveAsync(new TestArchiveRequest { ArchiveFilePath = archive });

        Assert.False(result.Success);
        Assert.NotEmpty(result.CorruptedEntries);
        Assert.Contains("failed integrity verification", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TestArchive_EncryptedWithoutPassword_SetsRequiresPassword()
    {
        var source = Path.Combine(_workDir, "secret.txt");
        await File.WriteAllTextAsync(source, "confidential payload");

        var archive = Path.Combine(_workDir, "secret.zip");
        var created = await _engine.CreateArchiveAsync(new CompressionRequest
        {
            OutputArchiveFilePath = archive,
            Format = ArchiveFormat.Zip,
            SourcePaths = [source],
            Options = new CompressionOptions { Password = "s3cret" }
        });
        Assert.True(created.Success, created.ErrorMessage);

        var result = await _engine.TestArchiveAsync(new TestArchiveRequest { ArchiveFilePath = archive });

        Assert.False(result.Success);
        Assert.True(result.RequiresPassword, $"Expected a password-related failure, got: {result.ErrorMessage}");
    }

    [Fact]
    public async Task TestArchive_EncryptedWithCorrectPassword_Succeeds()
    {
        var source = Path.Combine(_workDir, "secret2.txt");
        await File.WriteAllTextAsync(source, "confidential payload");

        var archive = Path.Combine(_workDir, "secret2.zip");
        var created = await _engine.CreateArchiveAsync(new CompressionRequest
        {
            OutputArchiveFilePath = archive,
            Format = ArchiveFormat.Zip,
            SourcePaths = [source],
            Options = new CompressionOptions { Password = "s3cret" }
        });
        Assert.True(created.Success, created.ErrorMessage);

        var result = await _engine.TestArchiveAsync(new TestArchiveRequest
        {
            ArchiveFilePath = archive,
            Password = "s3cret"
        });

        Assert.True(result.Success, result.ErrorMessage ?? result.DetailedError);
        Assert.False(result.RequiresPassword);
    }

    private static int FindLocalFileHeader(byte[] bytes)
    {
        for (var i = 0; i < bytes.Length - 4; i++)
        {
            if (bytes[i] == 0x50 && bytes[i + 1] == 0x4B && bytes[i + 2] == 0x03 && bytes[i + 3] == 0x04)
                return i;
        }
        return -1;
    }
}
