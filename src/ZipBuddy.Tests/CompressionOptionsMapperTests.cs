using ZipBuddy.Core.Models;
using ZipBuddy.Core.Services;

namespace ZipBuddy.Tests;

public class CompressionOptionsMapperTests
{
    [Theory]
    [InlineData(0, ArchiveFormat.Zip)]
    [InlineData(1, ArchiveFormat.SevenZip)]
    [InlineData(2, ArchiveFormat.Tar)]
    [InlineData(3, ArchiveFormat.GZip)]
    [InlineData(99, ArchiveFormat.Zip)]
    public void MapFormat_MapsSelectedIndex(int index, ArchiveFormat expected)
    {
        Assert.Equal(expected, CompressionOptionsMapper.MapFormat(index));
    }

    [Theory]
    [InlineData(0, CompressionLevel.Store)]
    [InlineData(1, CompressionLevel.Fast)]
    [InlineData(2, CompressionLevel.Normal)]
    [InlineData(3, CompressionLevel.Maximum)]
    [InlineData(4, CompressionLevel.Ultra)]
    [InlineData(-1, CompressionLevel.Normal)]
    public void MapLevel_MapsSelectedIndex(int index, CompressionLevel expected)
    {
        Assert.Equal(expected, CompressionOptionsMapper.MapLevel(index));
    }

    [Fact]
    public void SupportsPassword_OnlyZipAndSevenZip()
    {
        Assert.True(CompressionOptionsMapper.SupportsPassword(ArchiveFormat.Zip));
        Assert.True(CompressionOptionsMapper.SupportsPassword(ArchiveFormat.SevenZip));
        Assert.False(CompressionOptionsMapper.SupportsPassword(ArchiveFormat.Tar));
        Assert.False(CompressionOptionsMapper.SupportsPassword(ArchiveFormat.GZip));
    }

    [Fact]
    public void ReplaceExtension_HandlesDoubleExtensionAndSingleExtension()
    {
        Assert.Equal(@"C:\data\archive.7z", CompressionOptionsMapper.ReplaceExtension(@"C:\data\archive.tar.gz", ArchiveFormat.SevenZip));
        Assert.Equal(@"C:\data\backup.tar.gz", CompressionOptionsMapper.ReplaceExtension(@"C:\data\backup.zip", ArchiveFormat.GZip));
    }

    [Fact]
    public void ValidateDestination_RejectsEmptySameSourceAndInvalidPaths()
    {
        const string source = @"C:\data\report.txt";

        Assert.Equal(DestinationValidation.Empty, CompressionOptionsMapper.ValidateDestination("   ", source));
        Assert.Equal(DestinationValidation.SameAsSource, CompressionOptionsMapper.ValidateDestination(@"C:\data\report.txt", source));
        Assert.Equal(DestinationValidation.SameAsSource, CompressionOptionsMapper.ValidateDestination(@"c:\DATA\REPORT.TXT", source));
        Assert.Equal(DestinationValidation.InvalidPath, CompressionOptionsMapper.ValidateDestination(@"C:\data\", source));
        Assert.Equal(DestinationValidation.InvalidPath, CompressionOptionsMapper.ValidateDestination(@"C:\data<>|bad.zip", source));
        Assert.Equal(DestinationValidation.Valid, CompressionOptionsMapper.ValidateDestination(@"C:\data\report.zip", source));
    }

    [Fact]
    public void FilePickerExtension_MatchesFormatPickerContract()
    {
        Assert.Equal(".zip", CompressionOptionsMapper.GetFilePickerExtension(ArchiveFormat.Zip));
        Assert.Equal(".7z", CompressionOptionsMapper.GetFilePickerExtension(ArchiveFormat.SevenZip));
        Assert.Equal(".tar", CompressionOptionsMapper.GetFilePickerExtension(ArchiveFormat.Tar));
        Assert.Equal(".gz", CompressionOptionsMapper.GetFilePickerExtension(ArchiveFormat.GZip));
    }

    [Fact]
    public void ReplaceExtension_IsIdempotentForSameFormat()
    {
        const string path = @"C:\data\archive.tar.gz";

        Assert.Equal(path, CompressionOptionsMapper.ReplaceExtension(path, ArchiveFormat.GZip));
    }

    [Fact]
    public void SupportsCompressionLevel_MatchesEngineContract()
    {
        Assert.True(CompressionOptionsMapper.SupportsCompressionLevel(ArchiveFormat.Zip));
        Assert.True(CompressionOptionsMapper.SupportsCompressionLevel(ArchiveFormat.SevenZip));
        Assert.False(CompressionOptionsMapper.SupportsCompressionLevel(ArchiveFormat.Tar));
        Assert.False(CompressionOptionsMapper.SupportsCompressionLevel(ArchiveFormat.GZip));
    }

    [Fact]
    public void BuildDefaultDestination_UsesSourceNameAndFormatExtension()
    {
        const string sourceFile = @"C:\data\report.txt";
        var fromFile = CompressionOptionsMapper.BuildDefaultDestination(sourceFile, ArchiveFormat.GZip);
        Assert.Equal(Path.Combine(@"C:\data", "report.tar.gz"), fromFile);

        var tempDir = Path.GetTempPath().TrimEnd('\\', '/');
        var fromDirectory = CompressionOptionsMapper.BuildDefaultDestination(tempDir, ArchiveFormat.SevenZip);
        Assert.Equal(Path.GetFileName(tempDir) + ".7z", Path.GetFileName(fromDirectory));
    }
}
