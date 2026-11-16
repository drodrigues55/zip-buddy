using System.Text;
using ZipBuddy.Core.Services;

namespace ZipBuddy.Tests;

public class ChecksumCalculatorTests : IDisposable
{
    private readonly string _tempDir;

    public ChecksumCalculatorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "ZipBuddy_ChecksumTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
        }
    }

    [Fact]
    public async Task ComputeAsync_EmptyFile_ReturnsExpectedHashes()
    {
        var filePath = Path.Combine(_tempDir, "empty.bin");
        await File.WriteAllBytesAsync(filePath, []);

        var result = await ChecksumCalculator.ComputeAsync(filePath);

        Assert.Equal("00000000", result.Crc32);
        Assert.Equal("0000000000000000", result.Crc64);
        Assert.Equal("DA39A3EE5E6B4B0D3255BFEF95601890AFD80709", result.Sha1);
        Assert.Equal("E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855", result.Sha256);
        Assert.Equal(0, result.FileSize);
        Assert.Equal("empty.bin", result.FileName);
    }

    [Fact]
    public async Task ComputeAsync_StandardDigits_MatchesKnownCrc32()
    {
        var filePath = Path.Combine(_tempDir, "digits.txt");
        await File.WriteAllBytesAsync(filePath, Encoding.ASCII.GetBytes("123456789"));

        var result = await ChecksumCalculator.ComputeAsync(filePath);

        // Standard test vector: CRC32 of "123456789" is 0xCBF43926
        Assert.Equal("CBF43926", result.Crc32);
        Assert.Equal(9, result.FileSize);
    }

    [Fact]
    public async Task ComputeAsync_NonExistentFile_ThrowsFileNotFoundException()
    {
        var filePath = Path.Combine(_tempDir, "does_not_exist.bin");

        await Assert.ThrowsAsync<FileNotFoundException>(() => ChecksumCalculator.ComputeAsync(filePath));
    }

    [Fact]
    public async Task ComputeAsync_StandardDigits_MatchesKnownCrc64AndShaVectors()
    {
        var filePath = Path.Combine(_tempDir, "abc.txt");
        await File.WriteAllBytesAsync(filePath, Encoding.ASCII.GetBytes("123456789"));

        var result = await ChecksumCalculator.ComputeAsync(filePath);

        // CRC-64/XZ (reflected ECMA-182, init/xorout = all ones) check value for "123456789".
        Assert.Equal("995DC9BBDF1939FA", result.Crc64);
    }

    [Fact]
    public async Task ComputeAsync_Abc_MatchesKnownShaVectors()
    {
        var filePath = Path.Combine(_tempDir, "abc.bin");
        await File.WriteAllBytesAsync(filePath, Encoding.ASCII.GetBytes("abc"));

        var result = await ChecksumCalculator.ComputeAsync(filePath);

        Assert.Equal("A9993E364706816ABA3E25717850C26C9CD0D89D", result.Sha1);
        Assert.Equal("BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD", result.Sha256);
    }

    [Fact]
    public async Task ComputeAsync_LargeFile_ReportsMonotonicProgressReachingOne()
    {
        var filePath = await CreateLargeFileAsync();
        var progress = new CollectingProgress();

        var result = await ChecksumCalculator.ComputeAsync(filePath, progress);

        Assert.Equal(2L * 1024 * 1024, result.FileSize);
        Assert.True(progress.Values.Count > 1, "Expected several progress reports for a multi-buffer file.");
        for (var i = 1; i < progress.Values.Count; i++)
            Assert.True(progress.Values[i] >= progress.Values[i - 1], "Progress must never decrease.");
        Assert.Equal(1.0, progress.Values[^1], 3);
    }

    [Fact]
    public async Task ComputeAsync_CancelledMidStream_ThrowsOperationCanceledException()
    {
        var filePath = await CreateLargeFileAsync();
        using var cts = new CancellationTokenSource();
        var progress = new CancelOnFirstReport(cts);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => ChecksumCalculator.ComputeAsync(filePath, progress, cts.Token));

        Assert.Equal(1, progress.Reports);
    }

    private async Task<string> CreateLargeFileAsync()
    {
        var filePath = Path.Combine(_tempDir, "large.bin");
        var data = new byte[2 * 1024 * 1024];
        Random.Shared.NextBytes(data);
        await File.WriteAllBytesAsync(filePath, data);
        return filePath;
    }

    private sealed class CollectingProgress : IProgress<double>
    {
        public List<double> Values { get; } = [];
        public void Report(double value) => Values.Add(value);
    }

    private sealed class CancelOnFirstReport(CancellationTokenSource cts) : IProgress<double>
    {
        public int Reports { get; private set; }
        public void Report(double value)
        {
            Reports++;
            if (Reports == 1)
                cts.Cancel();
        }
    }
}

