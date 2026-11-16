namespace ZipBuddy.Core.Services;

/// <summary>
/// Pure definition of a Windows Explorer verb. <see cref="Arguments"/> is the command-line
/// suffix appended to the application path; the registry manager composes the final command.
/// </summary>
public sealed record ContextMenuVerb(string Key, string Label, string Arguments, string? FlatKey = null);

/// <summary>
/// Builds the ordered list of context-menu verbs from the user's settings.
/// Kept free of registry/UI dependencies so it can be unit-tested in isolation.
/// </summary>
public static class ContextMenuDefinitions
{
    public const string GroupName = "Zip Buddy";

    /// <summary>Single source of truth for the cascading-menu default (cascading menu by default).</summary>
    public const bool DefaultUseCascadingMenu = true;

    public static IReadOnlyList<ContextMenuVerb> BuildArchiveVerbs(
        bool enableOpenWith,
        bool enableExtractHere,
        bool enableExtractToFolder,
        bool enableExtractTo,
        bool isPortuguese,
        bool enableCompressToZip = true,
        bool enableCompressTo7z = true)
    {
        var verbs = new List<ContextMenuVerb>();

        if (enableOpenWith)
            verbs.Add(new("01_Open", Loc(isPortuguese, "Abrir arquivo compactado", "Open archive"), "\"%1\"", "Zip Buddy.Open"));

        if (enableExtractTo)
            verbs.Add(new("02_ExtractTo", Loc(isPortuguese, "Extrair arquivos...", "Extract files..."), "--extract-to \"%1\"", "Zip Buddy.ExtractTo"));

        if (enableExtractHere)
            verbs.Add(new("03_ExtractHere", Loc(isPortuguese, "Extrair aqui", "Extract here"), "--extract-here \"%1\"", "Zip Buddy.ExtractHere"));

        if (enableExtractToFolder)
            verbs.Add(new("04_ExtractToFolder", Loc(isPortuguese, "Extrair em pasta separada", "Extract to folder"), "--extract-to-folder \"%1\"", "Zip Buddy.ExtractToFolder"));

        verbs.Add(new("05_TestArchive", Loc(isPortuguese, "Testar arquivo compactado", "Test archive"), "--test-archive \"%1\""));
        verbs.Add(new("06_CompressDialog", Loc(isPortuguese, "Adicionar ao arquivo compactado...", "Add to archive..."), "--compress-dialog \"%1\""));
        verbs.Add(new("07_CompressEmail", Loc(isPortuguese, "Comprimir e enviar por email...", "Compress and email..."), "--compress-email-dialog \"%1\""));

        if (enableCompressTo7z)
            verbs.Add(new("08_Compress7z", Loc(isPortuguese, "Adicionar como 7Z", "Add to 7Z"), "--compress-7z \"%1\""));

        if (enableCompressTo7z)
            verbs.Add(new("09_CompressEmail7z", Loc(isPortuguese, "Comprimir como 7Z e enviar por email", "Compress to 7Z and email"), "--compress-email-7z \"%1\""));

        if (enableCompressToZip)
            verbs.Add(new("10_CompressZip", Loc(isPortuguese, "Adicionar como ZIP", "Add to ZIP"), "--compress-zip \"%1\""));

        if (enableCompressToZip)
            verbs.Add(new("11_CompressEmailZip", Loc(isPortuguese, "Comprimir como ZIP e enviar por email", "Compress to ZIP and email"), "--compress-email-zip \"%1\""));

        verbs.Add(new("12_CrcSha", "CRC SHA", "--crc-sha \"%1\""));

        return verbs;
    }

    public static IReadOnlyList<ContextMenuVerb> BuildFileVerbs(
        bool enableCompressToZip,
        bool enableCompressTo7z,
        bool isPortuguese)
    {
        var verbs = new List<ContextMenuVerb>
        {
            new("01_CompressDialog", Loc(isPortuguese, "Adicionar ao arquivo compactado...", "Add to archive..."), "--compress-dialog \"%1\""),
            new("02_CompressEmail", Loc(isPortuguese, "Comprimir e enviar por email...", "Compress and email..."), "--compress-email-dialog \"%1\"")
        };

        if (enableCompressTo7z)
        {
            verbs.Add(new("03_Compress7z", Loc(isPortuguese, "Adicionar como 7Z", "Add to 7Z"), "--compress-7z \"%1\"", "Zip Buddy.Compress7z"));
            verbs.Add(new("04_CompressEmail7z", Loc(isPortuguese, "Comprimir como 7Z e enviar por email", "Compress to 7Z and email"), "--compress-email-7z \"%1\""));
        }

        if (enableCompressToZip)
        {
            verbs.Add(new("05_CompressZip", Loc(isPortuguese, "Adicionar como ZIP", "Add to ZIP"), "--compress-zip \"%1\"", "Zip Buddy.CompressZip"));
            verbs.Add(new("06_CompressEmailZip", Loc(isPortuguese, "Comprimir como ZIP e enviar por email", "Compress to ZIP and email"), "--compress-email-zip \"%1\""));
        }

        verbs.Add(new("07_CrcSha", "CRC SHA", "--crc-sha \"%1\""));

        return verbs;
    }

    private static string Loc(bool isPortuguese, string pt, string en) => isPortuguese ? pt : en;
}
