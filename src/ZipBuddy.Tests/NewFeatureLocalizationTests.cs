using ZipBuddy.Infrastructure.Services;

namespace ZipBuddy.Tests;

public class NewFeatureLocalizationTests
{
    public static readonly string[] NewKeys =
    [
        "Task_TestingTitle", "Task_TestingSubtitle", "Task_TestCompleteTitle",
        "Task_TestCompleteMessage", "Task_TestFailed", "Task_TestError",
        "Task_TestPasswordAttempts", "Task_Processed", "Task_Speed", "Task_Calculating", "Task_Complete",
        "Checksum_Title", "Checksum_Calculating", "Checksum_SizeCalculating", "Checksum_FileSize",
        "Checksum_Copy", "Checksum_CopyAll", "Checksum_StatusCalculating", "Checksum_StatusDone",
        "Checksum_StatusCanceled", "Checksum_FileNotFound", "Checksum_Error", "Checksum_Copied",
        "Checksum_CopyFailed", "Checksum_SummaryFile", "Checksum_SummarySize",
        "CompressDlg_Title", "CompressDlg_Source", "CompressDlg_Destination",
        "CompressDlg_DestinationPlaceholder", "CompressDlg_Format", "CompressDlg_FormatZip",
        "CompressDlg_Format7z", "CompressDlg_FormatTar", "CompressDlg_FormatGZip",
        "CompressDlg_Level", "CompressDlg_LevelStore", "CompressDlg_LevelFast",
        "CompressDlg_LevelNormal", "CompressDlg_LevelMaximum", "CompressDlg_LevelUltra",
        "CompressDlg_Password", "CompressDlg_PasswordPlaceholder", "CompressDlg_Email",
        "CompressDlg_Compress", "CompressDlg_FileTypeChoice", "CompressDlg_ErrorTitle",
        "CompressDlg_ErrorEmpty", "CompressDlg_ErrorInvalidPath", "CompressDlg_ErrorSameAsSource",
        "CompressDlg_ErrorPassword", "CompressDlg_OverwriteTitle", "CompressDlg_OverwriteMessage"
    ];

    [Fact]
    public void AllNewKeys_ExistInBothCatalogsAndAreDistinct()
    {
        var en = new LocalizationService("en-US");
        var pt = new LocalizationService("pt-BR");

        foreach (var key in NewKeys)
        {
            Assert.NotEqual(key, en[key]);
            Assert.False(string.IsNullOrWhiteSpace(en[key]), $"en-US key '{key}' is empty");
            Assert.NotEqual(key, pt[key]);
            Assert.False(string.IsNullOrWhiteSpace(pt[key]), $"pt-BR key '{key}' is empty");
        }
    }

    [Fact]
    public void NewDialogKeys_RespectSelectedCulture()
    {
        var loc = new LocalizationService("System") { CurrentCulture = "pt-BR" };

        Assert.Equal("Compactar", loc["CompressDlg_Compress"]);
        Assert.Equal("Copiar Tudo", loc["Checksum_CopyAll"]);
        Assert.Equal("Teste concluído", loc["Task_TestCompleteTitle"]);

        loc.CurrentCulture = "en-US";

        Assert.Equal("Compress", loc["CompressDlg_Compress"]);
        Assert.Equal("Copy All", loc["Checksum_CopyAll"]);
        Assert.Equal("Test complete", loc["Task_TestCompleteTitle"]);
    }

    [Fact]
    public void FormattedNewKeys_ApplyArguments()
    {
        var loc = new LocalizationService("en-US");

        Assert.Equal("2 of 5 items", loc.GetString("Task_Processed", 2, 5));
        Assert.Contains("archive.zip", loc.GetString("Task_TestingTitle", "archive.zip"));
        Assert.Contains("7", loc.GetString("Task_TestCompleteMessage", 7));
    }

    [Fact]
    public void UnsupportedCulture_FallsBackToEnglishForNewKeys()
    {
        var loc = new LocalizationService("fr-FR");

        Assert.Equal("Compress", loc["CompressDlg_Compress"]);
        Assert.Equal("Test complete", loc["Task_TestCompleteTitle"]);
        Assert.Equal("Copy All", loc["Checksum_CopyAll"]);
    }
}
