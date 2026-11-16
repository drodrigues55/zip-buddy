using ZipBuddy.Core.Services;
using ZipBuddy.Infrastructure.Settings;

namespace ZipBuddy.Tests;

public class ContextMenuDefinitionsTests
{
    [Fact]
    public void BuildArchiveVerbs_AllEnabled_ReturnsTwelveOrderedVerbs()
    {
        var verbs = ContextMenuDefinitions.BuildArchiveVerbs(
            enableOpenWith: true,
            enableExtractHere: true,
            enableExtractToFolder: true,
            enableExtractTo: true,
            isPortuguese: true,
            enableCompressToZip: true,
            enableCompressTo7z: true);

        Assert.Equal(12, verbs.Count);
        Assert.Equal(
            ["01_Open", "02_ExtractTo", "03_ExtractHere", "04_ExtractToFolder", "05_TestArchive",
             "06_CompressDialog", "07_CompressEmail", "08_Compress7z", "09_CompressEmail7z",
             "10_CompressZip", "11_CompressEmailZip", "12_CrcSha"],
            verbs.Select(v => v.Key));
        Assert.Equal("Abrir arquivo compactado", verbs[0].Label);
        Assert.Equal("CRC SHA", verbs[^1].Label);
    }

    [Fact]
    public void BuildArchiveVerbs_DisabledFlags_OmitsOnlyMatchingVerbs()
    {
        var verbs = ContextMenuDefinitions.BuildArchiveVerbs(
            enableOpenWith: false,
            enableExtractHere: false,
            enableExtractToFolder: false,
            enableExtractTo: false,
            isPortuguese: false,
            enableCompressToZip: false,
            enableCompressTo7z: false);

        // Only the always-on verbs remain: test, dialog, email dialog and CRC SHA.
        Assert.Equal(["05_TestArchive", "06_CompressDialog", "07_CompressEmail", "12_CrcSha"],
                     verbs.Select(v => v.Key));
        Assert.All(verbs, v => Assert.Null(v.FlatKey));
    }

    [Fact]
    public void BuildArchiveVerbs_FlatKeys_AreLimitedToLegacyCommands()
    {
        var verbs = ContextMenuDefinitions.BuildArchiveVerbs(true, true, true, true, true, true, true);

        Assert.Equal(
            ["Zip Buddy.Open", "Zip Buddy.ExtractTo", "Zip Buddy.ExtractHere", "Zip Buddy.ExtractToFolder"],
            verbs.Where(v => v.FlatKey is not null).Select(v => v.FlatKey));
    }

    [Fact]
    public void BuildFileVerbs_EnUs_UsesEnglishLabelsAndHonorsCompressionFlags()
    {
        var verbs = ContextMenuDefinitions.BuildFileVerbs(
            enableCompressToZip: true,
            enableCompressTo7z: false,
            isPortuguese: false);

        Assert.Equal(["01_CompressDialog", "02_CompressEmail", "05_CompressZip", "06_CompressEmailZip", "07_CrcSha"],
                     verbs.Select(v => v.Key));
        Assert.Contains(verbs, v => v.Label == "Add to archive...");
        Assert.DoesNotContain(verbs, v => v.Label == "Add to 7Z");
        Assert.Equal(["Zip Buddy.CompressZip"],
                     verbs.Where(v => v.FlatKey is not null).Select(v => v.FlatKey));
    }

    [Fact]
    public void BuildFileVerbs_AllDisabled_StillKeepsAlwaysOnCommands()
    {
        var verbs = ContextMenuDefinitions.BuildFileVerbs(false, false, true);

        Assert.Equal(["01_CompressDialog", "02_CompressEmail", "07_CrcSha"],
                     verbs.Select(v => v.Key));
        Assert.All(verbs, v => Assert.Null(v.FlatKey));
    }

    [Fact]
    public void BuildArchiveVerbs_AllToggleCombinations_AreConsistent()
    {
        foreach (var open in new[] { true, false })
        foreach (var here in new[] { true, false })
        foreach (var folder in new[] { true, false })
        foreach (var to in new[] { true, false })
        foreach (var zip in new[] { true, false })
        foreach (var sevenZip in new[] { true, false })
        {
            var verbs = ContextMenuDefinitions.BuildArchiveVerbs(open, here, folder, to, true, zip, sevenZip);
            var keys = verbs.Select(v => v.Key).ToHashSet();

            var expectedCount = 4 // always-on: test, dialog, e-mail dialog, CRC SHA
                + (open ? 1 : 0) + (here ? 1 : 0) + (folder ? 1 : 0) + (to ? 1 : 0)
                + (zip ? 2 : 0) + (sevenZip ? 2 : 0);

            Assert.Equal(expectedCount, verbs.Count);
            Assert.Equal(verbs.Count, keys.Count);
            Assert.Equal(open, keys.Contains("01_Open"));
            Assert.Equal(here, keys.Contains("03_ExtractHere"));
            Assert.Equal(folder, keys.Contains("04_ExtractToFolder"));
            Assert.Equal(to, keys.Contains("02_ExtractTo"));
            Assert.Equal(zip, keys.Contains("10_CompressZip"));
            Assert.Equal(sevenZip, keys.Contains("08_Compress7z"));
            Assert.True(keys.Contains("05_TestArchive"));
            Assert.True(keys.Contains("12_CrcSha"));
        }
    }

    [Fact]
    public void Labels_FollowSelectedCulture_WithoutMixingLanguages()
    {
        var pt = ContextMenuDefinitions.BuildArchiveVerbs(true, true, true, true, true, true, true);
        var en = ContextMenuDefinitions.BuildArchiveVerbs(true, true, true, true, false, true, true);

        Assert.Equal("Extrair aqui", pt.Single(v => v.Key == "03_ExtractHere").Label);
        Assert.Equal("Extract here", en.Single(v => v.Key == "03_ExtractHere").Label);
        Assert.Equal("CRC SHA", pt.Single(v => v.Key == "12_CrcSha").Label);
        Assert.DoesNotContain(pt, v => v.Label.Contains("Extract ", StringComparison.Ordinal));
        Assert.DoesNotContain(en, v => v.Label.Contains("Extrair ", StringComparison.Ordinal));
    }

    [Fact]
    public void CascadingDefault_IsSharedAndAlignedWithSettings()
    {
        Assert.True(ContextMenuDefinitions.DefaultUseCascadingMenu);
        Assert.True(new AppSettings().UseCascadingContextMenu);
    }
}
