using System.Globalization;
using ZipBuddy.Core.Interfaces;
using ZipBuddy.Infrastructure.Services;
using ZipBuddy.Infrastructure.Settings;
using Xunit;

namespace ZipBuddy.Tests;

public class LocalizationTests
{
    [Fact]
    public void LocalizationService_InitialCulture_DefaultsCorrectly()
    {
        var loc = new LocalizationService("en-US");
        Assert.Equal("en-US", loc.CurrentCulture);
        Assert.Equal("en-US", loc.EffectiveCulture);

        var locPt = new LocalizationService("pt-BR");
        Assert.Equal("pt-BR", locPt.CurrentCulture);
        Assert.Equal("pt-BR", locPt.EffectiveCulture);
    }

    [Fact]
    public void LocalizationService_CultureSwitch_TriggersCultureChangedEvent()
    {
        var loc = new LocalizationService("en-US");
        var eventFired = false;
        loc.CultureChanged += (s, e) => eventFired = true;

        loc.CurrentCulture = "pt-BR";

        Assert.True(eventFired);
        Assert.Equal("pt-BR", loc.CurrentCulture);
        Assert.Equal("pt-BR", loc.EffectiveCulture);
    }

    [Fact]
    public void LocalizationService_EnglishStrings_ReturnCorrectValues()
    {
        var loc = new LocalizationService("en-US");

        Assert.Equal("Zip Buddy", loc["AppName"]);
        Assert.Equal("Open", loc["Action_Open"]);
        Assert.Equal("Extract", loc["Action_Extract"]);
        Assert.Equal("Settings", loc["Action_Settings"]);
        Assert.Equal("Apply", loc["Action_Apply"]);
        Assert.Equal("Close", loc["Action_Exit"]);
        Assert.Equal("Settings applied successfully.", loc["Settings_Applied_Success"]);
        Assert.Equal("Ready", loc["Status_Ready"]);
        Assert.Equal("Password Required", loc["Password_Title"]);
    }

    [Fact]
    public void LocalizationService_PortugueseStrings_ReturnCorrectValues()
    {
        var loc = new LocalizationService("pt-BR");

        Assert.Equal("Zip Buddy", loc["AppName"]);
        Assert.Equal("Abrir", loc["Action_Open"]);
        Assert.Equal("Extrair", loc["Action_Extract"]);
        Assert.Equal("Configurações", loc["Action_Settings"]);
        Assert.Equal("Aplicar", loc["Action_Apply"]);
        Assert.Equal("Sair", loc["Action_Exit"]);
        Assert.Equal("Configurações aplicadas com sucesso.", loc["Settings_Applied_Success"]);
        Assert.Equal("Pronto", loc["Status_Ready"]);
        Assert.Equal("Senha Necessária", loc["Password_Title"]);
    }

    [Fact]
    public void LocalizationService_MissingKey_FallsBackGracefully()
    {
        var loc = new LocalizationService("en-US");

        var result = loc.GetString("NonExistent_Test_Key_12345");
        Assert.Equal("NonExistent_Test_Key_12345", result);

        var emptyResult = loc.GetString(string.Empty);
        Assert.Equal(string.Empty, emptyResult);
    }

    [Fact]
    public void LocalizationService_Formatting_AppliesArgumentsCorrectly()
    {
        var locEn = new LocalizationService("en-US");
        Assert.Equal("15 items", locEn.GetString("Status_ItemsCount", 15));

        var locPt = new LocalizationService("pt-BR");
        Assert.Equal("15 itens", locPt.GetString("Status_ItemsCount", 15));
    }

    [Fact]
    public async Task SettingsService_LanguageProperty_PersistsAndLoads()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"zipbuddy_settings_test_{Guid.NewGuid():N}.json");
        try
        {
            var settings = new SettingsService(tempFile);
            await settings.LoadAsync();
            Assert.Equal("System", settings.Language);

            settings.Language = "pt-BR";
            await settings.SaveAsync();

            var loadedSettings = new SettingsService(tempFile);
            await loadedSettings.LoadAsync();
            Assert.Equal("pt-BR", loadedSettings.Language);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void LocalizationService_SystemLanguage_ResolvesToActualSystemCultureWithFallback()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            // When system UI culture is Portuguese
            CultureInfo.CurrentUICulture = new CultureInfo("pt-BR");
            var locPt = new LocalizationService("System");
            Assert.Equal("System", locPt.CurrentCulture);
            Assert.Equal("pt-BR", locPt.EffectiveCulture);

            // When system UI culture is another language (e.g. French, German)
            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
            var locFr = new LocalizationService("System");
            Assert.Equal("System", locFr.CurrentCulture);
            Assert.Equal("en-US", locFr.EffectiveCulture);

            // Empty/whitespace culture should also behave as System
            var locEmpty = new LocalizationService(string.Empty);
            Assert.Equal("en-US", locEmpty.EffectiveCulture);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public void AssemblyVersion_MatchesGeneralAvailability_Version_1_0_X()
    {
        var version = typeof(LocalizationService).Assembly.GetName().Version;
        Assert.NotNull(version);
        Assert.Equal(1, version.Major);
        Assert.Equal(0, version.Minor);
        Assert.True(version.Build >= 0);
    }

    [Fact]
    public void LocalizationService_FileExplorerKeys_ExistInBothEnglishAndPortuguese()
    {
        var keys = new[]
        {
            "Settings_Explorer_Header",
            "Settings_Explorer_ContextMenu_Title",
            "Settings_Explorer_ContextMenu_Desc",
            "Settings_Explorer_ContextMenu_Enable",
            "Settings_Explorer_ContextMenu_Cascading",
            "Settings_Explorer_Commands_Title",
            "Settings_Explorer_Cmd_OpenWith",
            "Settings_Explorer_Cmd_ExtractHere",
            "Settings_Explorer_Cmd_ExtractToFolder",
            "Settings_Explorer_Cmd_ExtractTo",
            "Settings_Explorer_Cmd_CompressZip",
            "Settings_Explorer_Cmd_Compress7z",
            "Settings_Explorer_Assoc_Title",
            "Settings_Explorer_Assoc_Desc",
            "Settings_Explorer_Assoc_Status_Title",
            "Settings_Explorer_Assoc_Status_Desc",
            "Settings_Explorer_Assoc_Status_Registered",
            "Settings_Explorer_Assoc_Status_NotRegistered",
            "Settings_Explorer_Assoc_Btn_Register",
            "Settings_Explorer_Assoc_Btn_DefaultApps",
            "Settings_Explorer_Assoc_Btn_Unregister",
            "Settings_Explorer_DefaultApps_Note_Title",
            "Settings_Explorer_DefaultApps_Note_Desc",
            "Settings_Explorer_Assoc_Registered_Success",
            "Settings_Explorer_Assoc_Registered_Failed",
            "Settings_Explorer_Assoc_Unregistered_Success",
            "Settings_Explorer_Assoc_Unregistered_Failed",
            "Settings_Explorer_DefaultApps_Launched"
        };

        var locEn = new LocalizationService("en-US");
        var locPt = new LocalizationService("pt-BR");

        foreach (var key in keys)
        {
            var valEn = locEn[key];
            var valPt = locPt[key];

            Assert.NotEqual(key, valEn);
            Assert.False(string.IsNullOrWhiteSpace(valEn), $"English key '{key}' should not be empty");

            Assert.NotEqual(key, valPt);
            Assert.False(string.IsNullOrWhiteSpace(valPt), $"Portuguese key '{key}' should not be empty");
        }
    }
}
