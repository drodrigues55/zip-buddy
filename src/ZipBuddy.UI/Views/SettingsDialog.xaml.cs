using ZipBuddy.Core.Interfaces;
using ZipBuddy.Core.Models;
using ZipBuddy.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace ZipBuddy.UI.Views;

public sealed partial class SettingsDialog : ContentDialog
{
    private readonly SettingsViewModel _viewModel;
    private readonly ILocalizationService _loc;

    private sealed record SettingsSnapshot(
        string DefaultExtractionFolder,
        int DoubleClickActionIndex,
        bool OpenFolderAfterExtraction,
        bool DeleteArchiveAfterExtraction,
        bool ConfirmBeforeOverwriting,
        bool ConfirmBeforeDeleting,
        int MaxExtractionThreads,
        string CustomTempDirectory,
        int DefaultFormatIndex,
        int DefaultLevelIndex,
        int ThemeIndex,
        int LanguageIndex,
        bool EnableContextMenu,
        bool UseCascadingContextMenu,
        bool EnableOpenWith,
        bool EnableExtractHere,
        bool EnableExtractToFolder,
        bool EnableExtractTo,
        bool EnableCompressToZip,
        bool EnableCompressTo7z);

    private SettingsSnapshot? _baselineSnapshot;
    private bool _isInitializing;
    private bool _isDirty;

    public SettingsDialog()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<SettingsViewModel>();
        _loc = App.Services.GetRequiredService<ILocalizationService>();

        _isInitializing = true;
        try
        {
            ApplyLocalization();
            LoadViewModelData();
            NavView.SelectedItem = NavExtraction;
        }
        finally
        {
            _isInitializing = false;
        }

        CaptureBaseline();
        RegisterChangeHandlers();

        PrimaryButtonClick += async (s, e) =>
        {
            var deferral = e.GetDeferral();
            try
            {
                if (_isDirty)
                {
                    var success = await SaveSettingsFromUIAsync();
                    if (!success)
                    {
                        // Prevent automatic dialog close so the user can review the failure message
                        e.Cancel = true;
                        StatusInfoBar.Severity = InfoBarSeverity.Error;
                        StatusInfoBar.Message = _viewModel.StatusMessage;
                        StatusInfoBar.IsOpen = true;
                    }
                }
            }
            finally
            {
                deferral.Complete();
            }
        };

        SecondaryButtonClick += async (s, e) =>
        {
            var deferral = e.GetDeferral();
            // Never close dialog on Apply
            e.Cancel = true;
            try
            {
                if (!_isDirty) return;

                var success = await SaveSettingsFromUIAsync();
                if (success)
                {
                    CaptureBaseline();
                    StatusInfoBar.Severity = InfoBarSeverity.Success;
                    StatusInfoBar.Message = _loc["Settings_Applied_Success"];
                    StatusInfoBar.IsOpen = true;
                }
                else
                {
                    StatusInfoBar.Severity = InfoBarSeverity.Error;
                    StatusInfoBar.Message = _viewModel.StatusMessage;
                    StatusInfoBar.IsOpen = true;
                }
            }
            finally
            {
                deferral.Complete();
            }
        };
    }

    private async Task<bool> SaveSettingsFromUIAsync()
    {
        // Extraction
        _viewModel.DefaultExtractionFolder = DefaultExtractionFolderBox.Text.Trim();
        _viewModel.DoubleClickAction = DoubleClickActionBox.SelectedIndex switch
        {
            1 => "ExtractToArchiveFolder",
            2 => "ExtractToPredefinedFolder",
            3 => "PromptDestinationFolder",
            4 => "ExtractHere",
            _ => "OpenInZipBuddy"
        };

        _viewModel.OpenFolderAfterExtraction = OpenFolderToggle.IsOn;
        _viewModel.DeleteArchiveAfterExtraction = DeleteArchiveToggle.IsOn;
        _viewModel.ConfirmBeforeOverwriting = ConfirmOverwriteCheckBox.IsChecked ?? true;
        _viewModel.ConfirmBeforeDeleting = ConfirmDeleteCheckBox.IsChecked ?? true;
        _viewModel.MaxExtractionThreads = (int)Math.Clamp(MaxThreadsSlider.Value, 1, 64);
        _viewModel.CustomTempDirectory = CustomTempDirBox.Text.Trim();

        // Compression
        var format = (DefaultFormatComboBox.SelectedItem as ComboBoxItem)?.Tag switch
        {
            "SevenZip" => ArchiveFormat.SevenZip,
            "Tar" => ArchiveFormat.Tar,
            "GZip" => ArchiveFormat.GZip,
            "BZip2" => ArchiveFormat.BZip2,
            _ => ArchiveFormat.Zip
        };

        var level = (DefaultLevelComboBox.SelectedItem as ComboBoxItem)?.Tag switch
        {
            "Store" => CompressionLevel.Store,
            "Fast" => CompressionLevel.Fast,
            "Maximum" => CompressionLevel.Maximum,
            "Ultra" => CompressionLevel.Ultra,
            _ => CompressionLevel.Normal
        };

        // Validate compatibility again at the save boundary
        level = format.GetCompatibleCompressionLevel(level);

        _viewModel.DefaultArchiveFormat = format;
        _viewModel.DefaultCompressionLevel = level;

        // Appearance
        _viewModel.ThemeMode = (ThemeComboBox.SelectedItem as ComboBoxItem)?.Tag as string ?? "System";
        _viewModel.Language = (LanguageComboBox.SelectedItem as ComboBoxItem)?.Tag as string ?? "System";

        // File Explorer
        _viewModel.EnableContextMenu = ContextMenuMasterToggle.IsOn;
        _viewModel.UseCascadingContextMenu = CascadingMenuCheckBox.IsChecked ?? true;
        _viewModel.EnableOpenWith = CmdOpenWithCheckBox.IsChecked ?? true;
        _viewModel.EnableExtractHere = CmdExtractHereCheckBox.IsChecked ?? true;
        _viewModel.EnableExtractToFolder = CmdExtractToFolderCheckBox.IsChecked ?? true;
        _viewModel.EnableExtractTo = CmdExtractToCheckBox.IsChecked ?? true;
        _viewModel.EnableCompressToZip = CmdCompressToZipCheckBox.IsChecked ?? true;
        _viewModel.EnableCompressTo7z = CmdCompressTo7zCheckBox.IsChecked ?? true;

        return await _viewModel.SaveSettingsAsync();
    }

    private SettingsSnapshot CaptureCurrentSnapshot()
    {
        return new SettingsSnapshot(
            DefaultExtractionFolderBox?.Text?.Trim() ?? string.Empty,
            DoubleClickActionBox?.SelectedIndex ?? 0,
            OpenFolderToggle?.IsOn ?? false,
            DeleteArchiveToggle?.IsOn ?? false,
            ConfirmOverwriteCheckBox?.IsChecked ?? true,
            ConfirmDeleteCheckBox?.IsChecked ?? true,
            (int)(MaxThreadsSlider?.Value ?? 1),
            CustomTempDirBox?.Text?.Trim() ?? string.Empty,
            DefaultFormatComboBox?.SelectedIndex ?? 0,
            DefaultLevelComboBox?.SelectedIndex ?? 2,
            ThemeComboBox?.SelectedIndex ?? 0,
            LanguageComboBox?.SelectedIndex ?? 0,
            ContextMenuMasterToggle?.IsOn ?? false,
            CascadingMenuCheckBox?.IsChecked ?? true,
            CmdOpenWithCheckBox?.IsChecked ?? true,
            CmdExtractHereCheckBox?.IsChecked ?? true,
            CmdExtractToFolderCheckBox?.IsChecked ?? true,
            CmdExtractToCheckBox?.IsChecked ?? true,
            CmdCompressToZipCheckBox?.IsChecked ?? true,
            CmdCompressTo7zCheckBox?.IsChecked ?? true);
    }

    private void CaptureBaseline()
    {
        _baselineSnapshot = CaptureCurrentSnapshot();
        _isDirty = false;
        UpdateActionButtonsState();
    }

    private void UpdateActionButtonsState()
    {
        IsSecondaryButtonEnabled = _isDirty;
        if (_loc != null)
        {
            CloseButtonText = _isDirty ? _loc["Action_Cancel"] : _loc["Action_Exit"];
        }
    }

    private void EvaluateDirtyState()
    {
        if (_isInitializing || _baselineSnapshot == null) return;

        var current = CaptureCurrentSnapshot();
        var dirty = current != _baselineSnapshot;
        if (dirty != _isDirty)
        {
            _isDirty = dirty;
            UpdateActionButtonsState();
        }
    }

    private void OnSettingInputChanged(object? sender, object? e)
    {
        EvaluateDirtyState();
    }

    private void RegisterChangeHandlers()
    {
        DefaultExtractionFolderBox.TextChanged += (s, e) => EvaluateDirtyState();
        DoubleClickActionBox.SelectionChanged += (s, e) => EvaluateDirtyState();
        OpenFolderToggle.Toggled += (s, e) => EvaluateDirtyState();
        ConfirmOverwriteCheckBox.Checked += (s, e) => EvaluateDirtyState();
        ConfirmOverwriteCheckBox.Unchecked += (s, e) => EvaluateDirtyState();
        ConfirmDeleteCheckBox.Checked += (s, e) => EvaluateDirtyState();
        ConfirmDeleteCheckBox.Unchecked += (s, e) => EvaluateDirtyState();
        CustomTempDirBox.TextChanged += (s, e) => EvaluateDirtyState();
        CascadingMenuCheckBox.Checked += (s, e) => EvaluateDirtyState();
        CascadingMenuCheckBox.Unchecked += (s, e) => EvaluateDirtyState();
        CmdOpenWithCheckBox.Checked += (s, e) => EvaluateDirtyState();
        CmdOpenWithCheckBox.Unchecked += (s, e) => EvaluateDirtyState();
        CmdExtractHereCheckBox.Checked += (s, e) => EvaluateDirtyState();
        CmdExtractHereCheckBox.Unchecked += (s, e) => EvaluateDirtyState();
        CmdExtractToFolderCheckBox.Checked += (s, e) => EvaluateDirtyState();
        CmdExtractToFolderCheckBox.Unchecked += (s, e) => EvaluateDirtyState();
        CmdExtractToCheckBox.Checked += (s, e) => EvaluateDirtyState();
        CmdExtractToCheckBox.Unchecked += (s, e) => EvaluateDirtyState();
        CmdCompressToZipCheckBox.Checked += (s, e) => EvaluateDirtyState();
        CmdCompressToZipCheckBox.Unchecked += (s, e) => EvaluateDirtyState();
        CmdCompressTo7zCheckBox.Checked += (s, e) => EvaluateDirtyState();
        CmdCompressTo7zCheckBox.Unchecked += (s, e) => EvaluateDirtyState();
    }

    private void LoadViewModelData()
    {
        // Extraction
        DefaultExtractionFolderBox.Text = _viewModel.DefaultExtractionFolder ?? string.Empty;

        DoubleClickActionBox.SelectedIndex = _viewModel.DoubleClickAction switch
        {
            "ExtractToArchiveFolder" => 1,
            "ExtractToPredefinedFolder" => 2,
            "PromptDestinationFolder" => 3,
            "ExtractHere" => 4,
            _ => 0
        };

        OpenFolderToggle.IsOn = _viewModel.OpenFolderAfterExtraction;
        DeleteArchiveToggle.IsOn = _viewModel.DeleteArchiveAfterExtraction;
        DeleteArchiveWarning.IsOpen = _viewModel.DeleteArchiveAfterExtraction;

        ConfirmOverwriteCheckBox.IsChecked = _viewModel.ConfirmBeforeOverwriting;
        ConfirmDeleteCheckBox.IsChecked = _viewModel.ConfirmBeforeDeleting;

        var threads = _viewModel.MaxExtractionThreads > 0 ? _viewModel.MaxExtractionThreads : Environment.ProcessorCount;
        MaxThreadsSlider.Value = Math.Clamp(threads, 1, 64);
        MaxThreadsValueText.Text = _loc != null
            ? _loc.GetString("Settings_Extract_Adv_Threads_Format", (int)MaxThreadsSlider.Value)
            : $"{MaxThreadsSlider.Value} threads";

        CustomTempDirBox.Text = _viewModel.CustomTempDirectory ?? string.Empty;

        // Compression
        DefaultFormatComboBox.SelectedIndex = _viewModel.DefaultArchiveFormat switch
        {
            ArchiveFormat.SevenZip => 1,
            ArchiveFormat.Tar => 2,
            ArchiveFormat.GZip => 3,
            ArchiveFormat.BZip2 => 4,
            _ => 0
        };

        var format = _viewModel.DefaultArchiveFormat;
        var level = format.GetCompatibleCompressionLevel(_viewModel.DefaultCompressionLevel);

        DefaultLevelComboBox.SelectedIndex = level switch
        {
            CompressionLevel.Store => 0,
            CompressionLevel.Fast => 1,
            CompressionLevel.Maximum => 3,
            CompressionLevel.Ultra => 4,
            _ => 2
        };

        _lastSelectedFormat = format;
        UpdateCompressionControlsAndGuidance(isFormatChange: false);

        // Appearance
        ThemeComboBox.SelectedIndex = _viewModel.ThemeMode switch
        {
            "Light" => 1,
            "Dark" => 2,
            _ => 0
        };

        LanguageComboBox.SelectedIndex = _viewModel.Language switch
        {
            "en-US" or "en" => 1,
            "pt-BR" or "pt" => 2,
            _ => 0
        };

        UpdateAppearanceExplanations();

        // File Explorer
        ContextMenuMasterToggle.IsOn = _viewModel.EnableContextMenu;
        CascadingMenuCheckBox.IsChecked = _viewModel.UseCascadingContextMenu;
        CmdOpenWithCheckBox.IsChecked = _viewModel.EnableOpenWith;
        CmdExtractHereCheckBox.IsChecked = _viewModel.EnableExtractHere;
        CmdExtractToFolderCheckBox.IsChecked = _viewModel.EnableExtractToFolder;
        CmdExtractToCheckBox.IsChecked = _viewModel.EnableExtractTo;
        CmdCompressToZipCheckBox.IsChecked = _viewModel.EnableCompressToZip;
        CmdCompressTo7zCheckBox.IsChecked = _viewModel.EnableCompressTo7z;

        UpdateContextMenuControlsEnabled();
        UpdateFileAssociationStatusBadge();
    }

    private void ApplyLocalization()
    {
        Title = _loc["Action_Settings"];
        PrimaryButtonText = _loc["Action_Save"];
        SecondaryButtonText = _loc["Action_Apply"];
        CloseButtonText = _isDirty ? _loc["Action_Cancel"] : _loc["Action_Exit"];
        IsSecondaryButtonEnabled = _isDirty;

        // Navigation categories
        NavExtraction.Content = _loc["Nav_Extraction"];
        NavCompression.Content = _loc["Nav_Compression"];
        NavAppearance.Content = _loc["Nav_Appearance"];
        NavFileExplorer.Content = _loc["Nav_FileExplorer"];
        NavNotifications.Content = _loc["Nav_Notifications"];
        NavUpdates.Content = _loc["Nav_Updates"];
        NavAdvanced.Content = _loc["Nav_Advanced"];

        // Extraction page
        ExtractionPageHeader.Text = _loc["Settings_Extract_Header"];

        DestGroupTitle.Text = _loc["Settings_Extract_Dest_Title"];
        DestGroupDesc.Text = _loc["Settings_Extract_Dest_Desc"];
        DefaultExtractionFolderBox.PlaceholderText = _loc["Settings_Extract_Dest_Placeholder"];
        BrowseExtractionFolderButton.Content = _loc["Action_Browse"];
        DestExplanationText.Text = _loc["Settings_Extract_Dest_Explanation"];

        OpenGroupTitle.Text = _loc["Settings_Extract_Open_Title"];
        OpenGroupDesc.Text = _loc["Settings_Extract_Open_Desc"];
        OptOpenInZipBuddy.Content = _loc["Settings_Extract_DoubleClick_Open"];
        OptExtractToArchiveFolder.Content = _loc["Settings_Extract_DoubleClick_ArchiveFolder"];
        OptExtractToPredefinedFolder.Content = _loc["Settings_Extract_DoubleClick_Predefined"];
        OptPromptDestinationFolder.Content = _loc["Settings_Extract_DoubleClick_Prompt"];
        OptExtractHere.Content = _loc["Settings_Extract_DoubleClick_Here"];

        AfterGroupTitle.Text = _loc["Settings_Extract_After_Title"];
        OpenFolderToggle.Header = _loc["Settings_Extract_After_OpenFolder"];
        DeleteArchiveToggle.Header = _loc["Settings_Extract_After_DeleteArchive"];
        DeleteArchiveWarning.Title = _loc["Settings_Extract_After_DeleteArchive_Title"];
        DeleteArchiveWarning.Message = _loc["Settings_Extract_After_DeleteArchive_Warn"];

        SafetyGroupTitle.Text = _loc["Settings_Extract_Safety_Title"];
        ConfirmOverwriteCheckBox.Content = _loc["Settings_Extract_Safety_ConfirmOverwrite"];
        ConfirmDeleteCheckBox.Content = _loc["Settings_Extract_Safety_ConfirmDelete"];

        AdvGroupTitle.Text = _loc["Settings_Extract_Adv_Title"];
        AdvGroupSubtitle.Text = _loc["Settings_Extract_Adv_Subtitle"];
        MaxThreadsTitle.Text = _loc["Settings_Extract_Adv_Threads"];
        MaxThreadsDesc.Text = _loc["Settings_Extract_Adv_Threads_Desc"];
        TempDirTitle.Text = _loc["Settings_Extract_Adv_TempDir"];
        TempDirDesc.Text = _loc["Settings_Extract_Adv_TempDir_Desc"];
        CustomTempDirBox.PlaceholderText = _loc["Settings_Extract_Adv_TempDir_Placeholder"];
        BrowseTempDirButton.Content = _loc["Action_Browse"];

        // Compression page
        CompressionPageHeader.Text = _loc["Settings_Compress_Header"];

        FormatGroupTitle.Text = _loc["Settings_Compress_Format_Title"];
        FormatGroupDesc.Text = _loc["Settings_Compress_Format_Desc"];
        OptFormatZip.Content = _loc["Settings_Compress_Format_Zip"];
        OptFormatSevenZip.Content = _loc["Settings_Compress_Format_SevenZip"];
        OptFormatTar.Content = _loc["Settings_Compress_Format_Tar"];
        OptFormatGZip.Content = _loc["Settings_Compress_Format_GZip"];
        OptFormatBZip2.Content = _loc["Settings_Compress_Format_BZip2"];

        LevelGroupTitle.Text = _loc["Settings_Compress_Level_Title"];
        LevelGroupDesc.Text = _loc["Settings_Compress_Level_Desc"];
        OptLevelStore.Content = _loc["Settings_Compress_Level_Store"];
        OptLevelFast.Content = _loc["Settings_Compress_Level_Fast"];
        OptLevelNormal.Content = _loc["Settings_Compress_Level_Normal"];
        OptLevelMaximum.Content = _loc["Settings_Compress_Level_Maximum"];
        OptLevelUltra.Content = _loc["Settings_Compress_Level_Ultra"];

        UpdateCompressionControlsAndGuidance(isFormatChange: false);

        // Appearance page
        AppearancePageHeader.Text = _loc["Settings_Appearance_Header"];

        ThemeGroupTitle.Text = _loc["Settings_Appearance_Theme_Title"];
        ThemeGroupDesc.Text = _loc["Settings_Appearance_Theme_Desc"];
        OptThemeSystem.Content = _loc["Settings_Appearance_Theme_System"];
        OptThemeLight.Content = _loc["Settings_Appearance_Theme_Light"];
        OptThemeDark.Content = _loc["Settings_Appearance_Theme_Dark"];

        LanguageGroupTitle.Text = _loc["Settings_Appearance_Lang_Title"];
        LanguageGroupDesc.Text = _loc["Settings_Appearance_Lang_Desc"];
        OptLanguageSystem.Content = _loc["Settings_Appearance_Lang_System"];
        OptLanguageEn.Content = _loc["Settings_Appearance_Lang_En"];
        OptLanguagePt.Content = _loc["Settings_Appearance_Lang_Pt"];

        AppearanceRestartInfoBar.Title = _loc["Settings_Appearance_Restart_Title"];
        AppearanceRestartInfoBar.Message = _loc["Settings_Appearance_Restart_Desc"];

        UpdateAppearanceExplanations();

        // File Explorer page
        FileExplorerPageHeader.Text = _loc["Settings_Explorer_Header"];
        ContextMenuGroupTitle.Text = _loc["Settings_Explorer_ContextMenu_Title"];
        ContextMenuGroupDesc.Text = _loc["Settings_Explorer_ContextMenu_Desc"];
        ContextMenuMasterToggle.Header = _loc["Settings_Explorer_ContextMenu_Enable"];
        CascadingMenuCheckBox.Content = _loc["Settings_Explorer_ContextMenu_Cascading"];
        CommandsListHeader.Text = _loc["Settings_Explorer_Commands_Title"];
        CmdOpenWithCheckBox.Content = _loc["Settings_Explorer_Cmd_OpenWith"];
        CmdExtractHereCheckBox.Content = _loc["Settings_Explorer_Cmd_ExtractHere"];
        CmdExtractToFolderCheckBox.Content = _loc["Settings_Explorer_Cmd_ExtractToFolder"];
        CmdExtractToCheckBox.Content = _loc["Settings_Explorer_Cmd_ExtractTo"];
        CmdCompressToZipCheckBox.Content = _loc["Settings_Explorer_Cmd_CompressZip"];
        CmdCompressTo7zCheckBox.Content = _loc["Settings_Explorer_Cmd_Compress7z"];

        AssocGroupTitle.Text = _loc["Settings_Explorer_Assoc_Title"];
        AssocGroupDesc.Text = _loc["Settings_Explorer_Assoc_Desc"];
        AssocStatusTitle.Text = _loc["Settings_Explorer_Assoc_Status_Title"];
        AssocStatusDesc.Text = _loc["Settings_Explorer_Assoc_Status_Desc"];
        RegisterAssocButton.Content = _loc["Settings_Explorer_Assoc_Btn_Register"];
        OpenDefaultAppsButton.Content = _loc["Settings_Explorer_Assoc_Btn_DefaultApps"];
        UnregisterAssocButton.Content = _loc["Settings_Explorer_Assoc_Btn_Unregister"];

        DefaultAppsInfoBar.Title = _loc["Settings_Explorer_DefaultApps_Note_Title"];
        DefaultAppsInfoBar.Message = _loc["Settings_Explorer_DefaultApps_Note_Desc"];

        UpdateFileAssociationStatusBadge();
    }

    private void OnNavSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem selectedItem && selectedItem.Tag is string tag)
        {
            if (string.Equals(tag, "Extraction", StringComparison.OrdinalIgnoreCase))
            {
                ExtractionPageScrollViewer.Visibility = Visibility.Visible;
                CompressionPageScrollViewer.Visibility = Visibility.Collapsed;
                AppearancePageScrollViewer.Visibility = Visibility.Collapsed;
                FileExplorerPageScrollViewer.Visibility = Visibility.Collapsed;
                PlaceholderPageScrollViewer.Visibility = Visibility.Collapsed;
            }
            else if (string.Equals(tag, "Compression", StringComparison.OrdinalIgnoreCase))
            {
                ExtractionPageScrollViewer.Visibility = Visibility.Collapsed;
                CompressionPageScrollViewer.Visibility = Visibility.Visible;
                AppearancePageScrollViewer.Visibility = Visibility.Collapsed;
                FileExplorerPageScrollViewer.Visibility = Visibility.Collapsed;
                PlaceholderPageScrollViewer.Visibility = Visibility.Collapsed;

                UpdateCompressionControlsAndGuidance(isFormatChange: false);
            }
            else if (string.Equals(tag, "Appearance", StringComparison.OrdinalIgnoreCase))
            {
                ExtractionPageScrollViewer.Visibility = Visibility.Collapsed;
                CompressionPageScrollViewer.Visibility = Visibility.Collapsed;
                AppearancePageScrollViewer.Visibility = Visibility.Visible;
                FileExplorerPageScrollViewer.Visibility = Visibility.Collapsed;
                PlaceholderPageScrollViewer.Visibility = Visibility.Collapsed;

                UpdateAppearanceExplanations();
            }
            else if (string.Equals(tag, "FileExplorer", StringComparison.OrdinalIgnoreCase))
            {
                ExtractionPageScrollViewer.Visibility = Visibility.Collapsed;
                CompressionPageScrollViewer.Visibility = Visibility.Collapsed;
                AppearancePageScrollViewer.Visibility = Visibility.Collapsed;
                FileExplorerPageScrollViewer.Visibility = Visibility.Visible;
                PlaceholderPageScrollViewer.Visibility = Visibility.Collapsed;

                UpdateFileAssociationStatusBadge();
            }
            else
            {
                ExtractionPageScrollViewer.Visibility = Visibility.Collapsed;
                CompressionPageScrollViewer.Visibility = Visibility.Collapsed;
                AppearancePageScrollViewer.Visibility = Visibility.Collapsed;
                FileExplorerPageScrollViewer.Visibility = Visibility.Collapsed;
                PlaceholderPageScrollViewer.Visibility = Visibility.Visible;

                ConfigurePlaceholder(tag);
            }
        }
    }

    private ArchiveFormat _lastSelectedFormat = ArchiveFormat.Zip;

    private void OnDefaultFormatSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateCompressionControlsAndGuidance(isFormatChange: true);
        EvaluateDirtyState();
    }

    private void OnDefaultLevelSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateCompressionControlsAndGuidance(isFormatChange: false);
        EvaluateDirtyState();
    }

    private void UpdateCompressionControlsAndGuidance(bool isFormatChange = false)
    {
        if (_loc == null || DefaultFormatComboBox == null || DefaultLevelComboBox == null ||
            FormatExplanationText == null || LevelExplanationText == null || FormatLimitationInfoBar == null)
        {
            return;
        }

        var selectedFormat = (DefaultFormatComboBox.SelectedItem as ComboBoxItem)?.Tag switch
        {
            "SevenZip" => ArchiveFormat.SevenZip,
            "Tar" => ArchiveFormat.Tar,
            "GZip" => ArchiveFormat.GZip,
            "BZip2" => ArchiveFormat.BZip2,
            _ => ArchiveFormat.Zip
        };

        var selectedLevel = (DefaultLevelComboBox.SelectedItem as ComboBoxItem)?.Tag switch
        {
            "Store" => CompressionLevel.Store,
            "Fast" => CompressionLevel.Fast,
            "Maximum" => CompressionLevel.Maximum,
            "Ultra" => CompressionLevel.Ultra,
            _ => CompressionLevel.Normal
        };

        bool levelResetOccurred = false;

        // If format does not support configurable levels, enforce and update level selection
        var supportsConfigurableLevels = selectedFormat is ArchiveFormat.Zip or ArchiveFormat.SevenZip;
        if (!supportsConfigurableLevels)
        {
            if (selectedLevel != CompressionLevel.Normal)
            {
                // Must adjust selection to compatible default (Normal)
                DefaultLevelComboBox.SelectedIndex = 2; // Normal
                selectedLevel = CompressionLevel.Normal;
                levelResetOccurred = true;
            }

            // Lock ComboBox and disable incompatible items
            DefaultLevelComboBox.IsEnabled = false;
            if (OptLevelStore != null) OptLevelStore.IsEnabled = false;
            if (OptLevelFast != null) OptLevelFast.IsEnabled = false;
            if (OptLevelNormal != null) OptLevelNormal.IsEnabled = true;
            if (OptLevelMaximum != null) OptLevelMaximum.IsEnabled = false;
            if (OptLevelUltra != null) OptLevelUltra.IsEnabled = false;
        }
        else
        {
            DefaultLevelComboBox.IsEnabled = true;
            if (OptLevelStore != null) OptLevelStore.IsEnabled = true;
            if (OptLevelFast != null) OptLevelFast.IsEnabled = true;
            if (OptLevelNormal != null) OptLevelNormal.IsEnabled = true;
            if (OptLevelMaximum != null) OptLevelMaximum.IsEnabled = true;
            if (OptLevelUltra != null) OptLevelUltra.IsEnabled = true;
        }

        // Format explanation
        FormatExplanationText.Text = selectedFormat switch
        {
            ArchiveFormat.SevenZip => _loc["Settings_Compress_Format_SevenZip_Desc"],
            ArchiveFormat.Tar => _loc["Settings_Compress_Format_Tar_Desc"],
            ArchiveFormat.GZip => _loc["Settings_Compress_Format_GZip_Desc"],
            ArchiveFormat.BZip2 => _loc["Settings_Compress_Format_BZip2_Desc"],
            _ => _loc["Settings_Compress_Format_Zip_Desc"]
        };

        // Level explanation
        LevelExplanationText.Text = selectedFormat switch
        {
            ArchiveFormat.Tar => _loc["Settings_Compress_Tar_Level_Desc"],
            ArchiveFormat.GZip or ArchiveFormat.BZip2 => _loc["Settings_Compress_Fixed_Level_Desc"],
            _ => selectedLevel switch
            {
                CompressionLevel.Store => _loc["Settings_Compress_Level_Store_Desc"],
                CompressionLevel.Fast => _loc["Settings_Compress_Level_Fast_Desc"],
                CompressionLevel.Maximum => _loc["Settings_Compress_Level_Maximum_Desc"],
                CompressionLevel.Ultra => _loc["Settings_Compress_Level_Ultra_Desc"],
                _ => _loc["Settings_Compress_Level_Normal_Desc"]
            }
        };

        // Contextual guidance & notification of reset
        if (selectedFormat == ArchiveFormat.Tar)
        {
            if (levelResetOccurred && isFormatChange)
            {
                FormatLimitationInfoBar.Title = _loc["Settings_Compress_Tar_Reset_Title"];
                FormatLimitationInfoBar.Message = _loc["Settings_Compress_Tar_Reset_Message"];
            }
            else
            {
                FormatLimitationInfoBar.Title = _loc["Settings_Compress_Warn_Tar_Title"];
                FormatLimitationInfoBar.Message = _loc["Settings_Compress_Warn_Tar_Message"];
            }
            FormatLimitationInfoBar.Severity = InfoBarSeverity.Informational;
            FormatLimitationInfoBar.IsOpen = true;
        }
        else if (selectedFormat is ArchiveFormat.GZip or ArchiveFormat.BZip2)
        {
            if (levelResetOccurred && isFormatChange)
            {
                FormatLimitationInfoBar.Title = _loc["Settings_Compress_Fixed_Reset_Title"];
                FormatLimitationInfoBar.Message = _loc["Settings_Compress_Fixed_Reset_Message"];
            }
            else
            {
                FormatLimitationInfoBar.Title = _loc["Settings_Compress_Info_FixedLevel_Title"];
                FormatLimitationInfoBar.Message = _loc["Settings_Compress_Info_FixedLevel_Message"];
            }
            FormatLimitationInfoBar.Severity = InfoBarSeverity.Informational;
            FormatLimitationInfoBar.IsOpen = true;
        }
        else if (selectedFormat == ArchiveFormat.Zip && selectedLevel == CompressionLevel.Ultra)
        {
            FormatLimitationInfoBar.Title = _loc["Settings_Compress_Warn_ZipUltra_Title"];
            FormatLimitationInfoBar.Message = _loc["Settings_Compress_Warn_ZipUltra_Message"];
            FormatLimitationInfoBar.Severity = InfoBarSeverity.Informational;
            FormatLimitationInfoBar.IsOpen = true;
        }
        else
        {
            FormatLimitationInfoBar.IsOpen = false;
        }

        _lastSelectedFormat = selectedFormat;
    }

    private void ConfigurePlaceholder(string tag)
    {
        PlaceholderInfoBar.Title = _loc["Nav_ComingSoon_Title"];
        PlaceholderInfoBar.Message = _loc["Nav_ComingSoon_Desc"];

        switch (tag)
        {
            case "Notifications":
                PlaceholderIcon.Glyph = "\uEA8F";
                PlaceholderTitle.Text = _loc["Nav_Notifications"];
                PlaceholderDescTitle.Text = _loc["Nav_ComingSoon_Notifications_Title"];
                PlaceholderDescText.Text = _loc["Nav_ComingSoon_Notifications_Desc"];
                break;

            case "Updates":
                PlaceholderIcon.Glyph = "\uE895";
                PlaceholderTitle.Text = _loc["Nav_Updates"];
                PlaceholderDescTitle.Text = _loc["Nav_ComingSoon_Updates_Title"];
                PlaceholderDescText.Text = _loc["Nav_ComingSoon_Updates_Desc"];
                break;

            case "Advanced":
                PlaceholderIcon.Glyph = "\uE713";
                PlaceholderTitle.Text = _loc["Nav_Advanced"];
                PlaceholderDescTitle.Text = _loc["Nav_ComingSoon_Advanced_Title"];
                PlaceholderDescText.Text = _loc["Nav_ComingSoon_Advanced_Desc"];
                break;
        }
    }

    private void OnDeleteArchiveToggled(object sender, RoutedEventArgs e)
    {
        DeleteArchiveWarning.IsOpen = DeleteArchiveToggle.IsOn;
        EvaluateDirtyState();
    }

    private void OnMaxThreadsSliderValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (MaxThreadsValueText != null)
        {
            MaxThreadsValueText.Text = _loc != null
                ? _loc.GetString("Settings_Extract_Adv_Threads_Format", (int)e.NewValue)
                : $"{(int)e.NewValue} threads";
        }
        EvaluateDirtyState();
    }

    private async void OnBrowseExtractionFolderClick(object sender, RoutedEventArgs e)
    {
        var folderPicker = new FolderPicker();
        folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
        folderPicker.FileTypeFilter.Add("*");

        var hwnd = WindowNative.GetWindowHandle(MainWindow.Current);
        InitializeWithWindow.Initialize(folderPicker, hwnd);

        var folder = await folderPicker.PickSingleFolderAsync();
        if (folder != null)
        {
            DefaultExtractionFolderBox.Text = folder.Path;
        }
    }

    private async void OnBrowseTempDirClick(object sender, RoutedEventArgs e)
    {
        var folderPicker = new FolderPicker();
        folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
        folderPicker.FileTypeFilter.Add("*");

        var hwnd = WindowNative.GetWindowHandle(MainWindow.Current);
        InitializeWithWindow.Initialize(folderPicker, hwnd);

        var folder = await folderPicker.PickSingleFolderAsync();
        if (folder != null)
        {
            CustomTempDirBox.Text = folder.Path;
        }
    }

    private void OnThemeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateAppearanceExplanations();
        EvaluateDirtyState();
    }

    private void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateAppearanceExplanations();
        EvaluateDirtyState();
    }

    private void UpdateAppearanceExplanations()
    {
        var themeTag = (ThemeComboBox?.SelectedItem as ComboBoxItem)?.Tag as string ?? "System";
        if (ThemeExplanationText != null && _loc != null)
        {
            ThemeExplanationText.Text = themeTag switch
            {
                "Light" => _loc["Settings_Appearance_Theme_Light_Desc"],
                "Dark" => _loc["Settings_Appearance_Theme_Dark_Desc"],
                _ => _loc["Settings_Appearance_Theme_System_Desc"]
            };
        }

        var langTag = (LanguageComboBox?.SelectedItem as ComboBoxItem)?.Tag as string ?? "System";
        if (LanguageExplanationText != null && _loc != null)
        {
            LanguageExplanationText.Text = langTag switch
            {
                "en-US" => _loc["Settings_Appearance_Lang_En_Desc"],
                "pt-BR" => _loc["Settings_Appearance_Lang_Pt_Desc"],
                _ => _loc["Settings_Appearance_Lang_System_Desc"]
            };
        }
    }

    private void OnContextMenuMasterToggled(object sender, RoutedEventArgs e)
    {
        UpdateContextMenuControlsEnabled();
        EvaluateDirtyState();
    }

    private void UpdateContextMenuControlsEnabled()
    {
        var isEnabled = ContextMenuMasterToggle?.IsOn ?? false;
        if (CascadingMenuCheckBox != null) CascadingMenuCheckBox.IsEnabled = isEnabled;
        if (CmdOpenWithCheckBox != null) CmdOpenWithCheckBox.IsEnabled = isEnabled;
        if (CmdExtractHereCheckBox != null) CmdExtractHereCheckBox.IsEnabled = isEnabled;
        if (CmdExtractToFolderCheckBox != null) CmdExtractToFolderCheckBox.IsEnabled = isEnabled;
        if (CmdExtractToCheckBox != null) CmdExtractToCheckBox.IsEnabled = isEnabled;
        if (CmdCompressToZipCheckBox != null) CmdCompressToZipCheckBox.IsEnabled = isEnabled;
        if (CmdCompressTo7zCheckBox != null) CmdCompressTo7zCheckBox.IsEnabled = isEnabled;
    }

    private void UpdateFileAssociationStatusBadge()
    {
        if (AssocStatusBadge == null || _loc == null) return;

        bool isAssociated = ZipBuddy.Windows.Shell.FileAssociationManager.IsAssociated(".zip");
        if (isAssociated)
        {
            AssocStatusBadge.Text = _loc["Settings_Explorer_Assoc_Status_Registered"];
            AssocStatusBadge.Foreground = Application.Current.Resources["SystemFillColorSuccessBrush"] as Microsoft.UI.Xaml.Media.Brush;
        }
        else
        {
            AssocStatusBadge.Text = _loc["Settings_Explorer_Assoc_Status_NotRegistered"];
            AssocStatusBadge.Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"] as Microsoft.UI.Xaml.Media.Brush;
        }
    }

    private void OnRegisterAssociationsClick(object sender, RoutedEventArgs e)
    {
        var ok = _viewModel.RegisterFileAssociations();
        UpdateFileAssociationStatusBadge();

        ExplorerActionInfoBar.Severity = ok ? InfoBarSeverity.Success : InfoBarSeverity.Error;
        ExplorerActionInfoBar.Message = _viewModel.StatusMessage;
        ExplorerActionInfoBar.IsOpen = true;
    }

    private void OnOpenDefaultAppsClick(object sender, RoutedEventArgs e)
    {
        _viewModel.OpenDefaultAppsSettings();
        UpdateFileAssociationStatusBadge();

        ExplorerActionInfoBar.Severity = InfoBarSeverity.Informational;
        ExplorerActionInfoBar.Message = _viewModel.StatusMessage;
        ExplorerActionInfoBar.IsOpen = true;
    }

    private void OnUnregisterAssociationsClick(object sender, RoutedEventArgs e)
    {
        var ok = _viewModel.UnregisterFileAssociations();
        UpdateFileAssociationStatusBadge();

        ExplorerActionInfoBar.Severity = ok ? InfoBarSeverity.Success : InfoBarSeverity.Error;
        ExplorerActionInfoBar.Message = _viewModel.StatusMessage;
        ExplorerActionInfoBar.IsOpen = true;
    }
}
