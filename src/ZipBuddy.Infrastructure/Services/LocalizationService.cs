using System.Globalization;
using ZipBuddy.Core.Interfaces;

namespace ZipBuddy.Infrastructure.Services;

/// <summary>
/// Production-ready localization service supporting en-US and pt-BR with graceful fallback.
/// </summary>
public class LocalizationService : ILocalizationService
{
    private string _currentCulture = "System";
    private string _effectiveCulture = "en-US";

    public event EventHandler? CultureChanged;

    public IReadOnlyList<string> SupportedCultures { get; } = ["System", "en-US", "pt-BR"];

    public string CurrentCulture
    {
        get => _currentCulture;
        set
        {
            if (_currentCulture != value)
            {
                _currentCulture = value;
                ResolveEffectiveCulture();
                CultureChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public string EffectiveCulture => _effectiveCulture;

    public LocalizationService(string initialCulture = "System")
    {
        _currentCulture = initialCulture;
        ResolveEffectiveCulture();
    }

    private void ResolveEffectiveCulture()
    {
        if (string.Equals(_currentCulture, "System", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(_currentCulture))
        {
            var systemCulture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            _effectiveCulture = systemCulture.Equals("pt", StringComparison.OrdinalIgnoreCase) ? "pt-BR" : "en-US";
        }
        else if (string.Equals(_currentCulture, "pt-BR", StringComparison.OrdinalIgnoreCase) || _currentCulture.StartsWith("pt", StringComparison.OrdinalIgnoreCase))
        {
            _effectiveCulture = "pt-BR";
        }
        else
        {
            _effectiveCulture = "en-US";
        }
    }

    public string GetString(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return string.Empty;

        // Try effective culture first
        if (StringCatalogs.TryGetValue(_effectiveCulture, out var catalog) && catalog.TryGetValue(key, out var val))
        {
            return val;
        }

        // Fallback to en-US
        if (StringCatalogs["en-US"].TryGetValue(key, out var fallbackVal))
        {
            return fallbackVal;
        }

        // Return key if not found
        return key;
    }

    public string GetString(string key, params object[] args)
    {
        var raw = GetString(key);
        try
        {
            return string.Format(CultureInfo.CurrentCulture, raw, args);
        }
        catch
        {
            return raw;
        }
    }

    public string this[string key] => GetString(key);

    private static readonly Dictionary<string, Dictionary<string, string>> StringCatalogs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en-US"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["AppName"] = "Zip Buddy",
            ["Action_Open"] = "Open",
            ["Action_New"] = "New",
            ["Action_Extract"] = "Extract",
            ["Action_Test"] = "Test",
            ["Action_Delete"] = "Delete",
            ["Action_Refresh"] = "Refresh",
            ["Action_Settings"] = "Settings",
            ["Action_Help"] = "Help",
            ["Action_About"] = "About",
            ["Action_Cancel"] = "Cancel",
            ["Action_Save"] = "Save",
            ["Action_Apply"] = "Apply",
            ["Action_Close"] = "Close",
            ["Action_Exit"] = "Close",
            ["Action_Browse"] = "Browse...",
            ["Action_OK"] = "OK",

            ["Column_Name"] = "Name",
            ["Column_Size"] = "Original Size",
            ["Column_CompressedSize"] = "Compressed Size",
            ["Column_Modified"] = "Date Modified",
            ["Column_Ratio"] = "Ratio",
            ["Column_Encrypted"] = "Encrypted",

            ["Status_Ready"] = "Ready",
            ["Status_ItemsCount"] = "{0} items",
            ["Status_TotalSize"] = "Total: {0}",
            ["Status_Selected"] = "{0} selected",

            ["Progress_Compressing"] = "Compressing...",
            ["Progress_Extracting"] = "Extracting...",
            ["Progress_Testing"] = "Testing archive integrity...",
            ["Progress_Speed"] = "{0:F2} MB/s",
            ["Progress_Elapsed"] = "Elapsed: {0}",
            ["Progress_Remaining"] = "Remaining: {0}",

            ["Settings_Title"] = "Preferences",
            ["Settings_General"] = "General",
            ["Settings_Language"] = "Language",
            ["Settings_Language_System"] = "System default",
            ["Settings_Language_En"] = "English (en-US)",
            ["Settings_Language_Pt"] = "Portuguese (pt-BR)",
            ["Settings_Theme"] = "Theme",
            ["Settings_Theme_System"] = "System",
            ["Settings_Theme_Light"] = "Light",
            ["Settings_Theme_Dark"] = "Dark",
            ["Settings_DefaultExtractFolder"] = "Default extraction folder",
            ["Settings_OpenFolderAfter"] = "Open folder after extraction",
            ["Settings_ConfirmOverwrite"] = "Confirm before overwriting files",
            ["Settings_ConfirmDelete"] = "Confirm before deleting",
            ["Settings_AutoCloseTask"] = "Auto-close progress window when completed",
            ["Settings_NotifyCompletion"] = "Show notification on completion",
            ["Settings_ExplorerIntegration"] = "Windows Explorer Integration",
            ["Settings_EnableContextMenu"] = "Enable right-click context menu",
            ["Settings_CascadingMenu"] = "Use cascading submenu (Zip Buddy >)",
            ["Settings_Updates"] = "Updates",
            ["Settings_AutoCheckUpdates"] = "Check for updates automatically",
            ["Settings_UpdateFrequency"] = "Check frequency",
            ["Settings_CurrentVersion"] = "Current version: v{0}",
            ["Settings_LastCheck"] = "Last check: {0}",
            ["Settings_InstallUpdate"] = "Install Update",

            ["Nav_Extraction"] = "Extraction",
            ["Nav_Compression"] = "Compression",
            ["Nav_Appearance"] = "Appearance",
            ["Nav_FileExplorer"] = "File Explorer",
            ["Nav_Notifications"] = "Notifications",
            ["Nav_Updates"] = "Updates",
            ["Nav_Advanced"] = "Advanced",
            ["Nav_ComingSoon_Title"] = "Under Refactoring",
            ["Nav_ComingSoon_Desc"] = "This category will be updated in the next phase.",
            ["Nav_ComingSoon_Compression_Title"] = "Archive Formats & Compression Levels",
            ["Nav_ComingSoon_Compression_Desc"] = "Configure default compression format (ZIP, 7-Zip, TAR), compression algorithms, and dictionary parameters.",
            ["Nav_ComingSoon_Appearance_Title"] = "Themes & Language",
            ["Nav_ComingSoon_Appearance_Desc"] = "Select application theme (System Default, Light, Dark) and interface language (en-US, pt-BR).",
            ["Nav_ComingSoon_FileExplorer_Title"] = "Shell Integration & File Types",
            ["Nav_ComingSoon_FileExplorer_Desc"] = "Manage Windows Explorer context menu verbs, cascading submenus, and archive file associations.",
            ["Nav_ComingSoon_Notifications_Title"] = "System Alerts & Progress Behavior",
            ["Nav_ComingSoon_Notifications_Desc"] = "Configure task completion toasts and progress window behavior.",
            ["Nav_ComingSoon_Updates_Title"] = "Automatic Update Checks",
            ["Nav_ComingSoon_Updates_Desc"] = "Schedule background update checks, verify digital signatures, and install new versions.",
            ["Nav_ComingSoon_Advanced_Title"] = "Diagnostics & Logging",
            ["Nav_ComingSoon_Advanced_Desc"] = "Configure diagnostic log levels, inspect application logs, and manage performance flags.",

            ["Settings_Extract_Header"] = "Extraction Preferences",
            ["Settings_Extract_Dest_Title"] = "Default Destination",
            ["Settings_Extract_Dest_Desc"] = "Configure where extracted archives and items are saved.",
            ["Settings_Extract_Dest_Placeholder"] = "Leave blank to extract next to archive",
            ["Settings_Extract_Dest_Explanation"] = "When left blank, Zip Buddy extracts into the folder containing the archive by default.",
            ["Settings_Extract_Open_Title"] = "Opening Archives",
            ["Settings_Extract_Open_Desc"] = "Default action when double-clicking supported archives in File Explorer.",
            ["Settings_Extract_DoubleClick_Open"] = "Open in Zip Buddy (Browse archive)",
            ["Settings_Extract_DoubleClick_ArchiveFolder"] = "Extract to folder with archive name [Recommended]",
            ["Settings_Extract_DoubleClick_Predefined"] = "Extract to predefined folder",
            ["Settings_Extract_DoubleClick_Prompt"] = "Prompt for destination folder...",
            ["Settings_Extract_DoubleClick_Here"] = "Extract here (in archive's folder)",
            ["Settings_Extract_After_Title"] = "Post-Extraction Actions",
            ["Settings_Extract_After_OpenFolder"] = "Open destination folder after extraction completes",
            ["Settings_Extract_After_DeleteArchive"] = "Delete source archive after successful extraction",
            ["Settings_Extract_After_DeleteArchive_Title"] = "Caution",
            ["Settings_Extract_After_DeleteArchive_Warn"] = "The source archive will be permanently deleted after extraction completes.",
            ["Settings_Extract_Safety_Title"] = "Safety & Overwrites",
            ["Settings_Extract_Safety_ConfirmOverwrite"] = "Confirm before overwriting existing files",
            ["Settings_Extract_Safety_ConfirmDelete"] = "Confirm before deleting items from archive",
            ["Settings_Extract_Adv_Title"] = "Advanced Extraction Options",
            ["Settings_Extract_Adv_Subtitle"] = "Resource limits and scratch directory for temporary operations",
            ["Settings_Extract_Adv_Threads"] = "Maximum extraction threads",
            ["Settings_Extract_Adv_Threads_Desc"] = "Reserved for upcoming multi-threaded extraction pipeline (current version extracts sequentially).",
            ["Settings_Extract_Adv_Threads_Format"] = "{0} threads",
            ["Settings_Extract_Adv_TempDir"] = "Custom temporary folder",
            ["Settings_Extract_Adv_TempDir_Desc"] = "Used for archive previews and drag-and-drop extraction. Standard extraction writes directly to the destination folder.",
            ["Settings_Extract_Adv_TempDir_Placeholder"] = "Default: Windows %TEMP%",

            ["Settings_Compress_Header"] = "Compression Preferences",
            ["Settings_Compress_Format_Title"] = "Default Archive Format",
            ["Settings_Compress_Format_Desc"] = "Select the default container format used when creating new archives.",
            ["Settings_Compress_Format_Zip"] = "ZIP (.zip) [Recommended for compatibility]",
            ["Settings_Compress_Format_SevenZip"] = "7-Zip (.7z) [Highest compression efficiency]",
            ["Settings_Compress_Format_Tar"] = "TAR (.tar) [Uncompressed packaging]",
            ["Settings_Compress_Format_GZip"] = "GZIP (.tar.gz) [Standard UNIX tarball]",
            ["Settings_Compress_Format_BZip2"] = "BZIP2 (.tar.bz2) [High-ratio UNIX tarball]",
            ["Settings_Compress_Format_Xz"] = "XZ (.tar.xz) [Modern LZMA UNIX tarball]",
            ["Settings_Compress_Format_Zip_Desc"] = "Universally supported across Windows, macOS, and Linux. Supports Deflate compression and AES-256 encryption.",
            ["Settings_Compress_Format_SevenZip_Desc"] = "High-efficiency format utilizing LZMA2 compression. Supports strong encryption, header obfuscation, and multi-volume splits.",
            ["Settings_Compress_Format_Tar_Desc"] = "POSIX container format that stores files and directories without data compression. Ideal for intermediate packaging.",
            ["Settings_Compress_Format_GZip_Desc"] = "UNIX standard compressed tarball (.tar.gz) combining TAR packaging with Deflate stream compression using standard parameters.",
            ["Settings_Compress_Format_BZip2_Desc"] = "UNIX compressed tarball (.tar.bz2) using Burrows-Wheeler block compression for higher ratios on text data using standard parameters.",
            ["Settings_Compress_Format_Xz_Desc"] = "Modern UNIX compressed tarball (.tar.xz) using LZMA stream compression for maximum space savings using standard parameters.",

            ["Settings_Compress_Level_Title"] = "Default Compression Level",
            ["Settings_Compress_Level_Desc"] = "Balance compression ratio against CPU processing time and memory usage.",
            ["Settings_Compress_Level_Store"] = "Store (No compression)",
            ["Settings_Compress_Level_Fast"] = "Fast (Lowest CPU usage)",
            ["Settings_Compress_Level_Normal"] = "Normal (Balanced speed and size) [Recommended]",
            ["Settings_Compress_Level_Maximum"] = "Maximum (High compression)",
            ["Settings_Compress_Level_Ultra"] = "Ultra (Smallest size, slowest)",
            ["Settings_Compress_Level_Store_Desc"] = "Packs files immediately without compression. Fastest operation; ideal for media files already compressed (JPG, MP4, MP3).",
            ["Settings_Compress_Level_Fast_Desc"] = "Applies lightweight compression to save time and reduce CPU workload. Suitable for quick backups and transfers.",
            ["Settings_Compress_Level_Normal_Desc"] = "Delivers the best overall balance of compression efficiency, processing speed, and memory usage. Recommended for general use.",
            ["Settings_Compress_Level_Maximum_Desc"] = "Performs deeper dictionary analysis for improved space savings. Requires more CPU processing time and memory.",
            ["Settings_Compress_Level_Ultra_Desc"] = "Maximizes compression ratio using maximum dictionary depth. Yields the smallest archive size, but uses the most CPU and memory.",

            ["Settings_Compress_Warn_Tar_Title"] = "Uncompressed Archive",
            ["Settings_Compress_Warn_Tar_Message"] = "TAR is an uncompressed container format. Files will be packaged without compression regardless of the selected compression level.",
            ["Settings_Compress_Warn_TarLevel_Title"] = "Compression Level Not Supported",
            ["Settings_Compress_Warn_TarLevel_Message"] = "TAR is an uncompressed container format and does not support compression levels. The selected compression level will be ignored.",
            ["Settings_Compress_Info_FixedLevel_Title"] = "Fixed Compression Parameters",
            ["Settings_Compress_Info_FixedLevel_Message"] = "Tar-based compressed formats (.tar.gz, .tar.bz2) use standardized algorithm parameters. Compression levels cannot be adjusted for this format.",
            ["Settings_Compress_Warn_FixedLevel_Title"] = "Compression Level Not Supported",
            ["Settings_Compress_Warn_FixedLevel_Message"] = "Tar-based compressed formats (.tar.gz, .tar.bz2) operate with fixed algorithm parameters in Zip Buddy. Adjustable levels apply only to ZIP and 7-Zip archives.",
            ["Settings_Compress_Warn_ZipUltra_Title"] = "ZIP Format Notice",
            ["Settings_Compress_Warn_ZipUltra_Message"] = "Ultra compression with ZIP applies maximum Deflate search passes. For significantly higher compression ratios, 7-Zip (.7z) is recommended.",
            ["Settings_Compress_Tar_Reset_Title"] = "TAR Format Notice",
            ["Settings_Compress_Tar_Reset_Message"] = "TAR is an uncompressed container format. The compression level has been set to Normal (standard packaging).",
            ["Settings_Compress_Fixed_Reset_Title"] = "Fixed Compression Parameters",
            ["Settings_Compress_Fixed_Reset_Message"] = "Tar-based compressed formats (.tar.gz, .tar.bz2) use standardized algorithm parameters. The compression level has been set to Normal.",
            ["Settings_Compress_Tar_Level_Desc"] = "TAR packages files without data compression. Compression level adjustment is not applicable.",
            ["Settings_Compress_Fixed_Level_Desc"] = "Operates with fixed, standardized stream compression parameters. Adjustable levels apply only to ZIP and 7-Zip archives.",

            ["Settings_Appearance_Header"] = "Appearance Preferences",
            ["Settings_Appearance_Theme_Title"] = "Application Theme",
            ["Settings_Appearance_Theme_Desc"] = "Choose how Zip Buddy looks on your device.",
            ["Settings_Appearance_Theme_System"] = "System default",
            ["Settings_Appearance_Theme_Light"] = "Light",
            ["Settings_Appearance_Theme_Dark"] = "Dark",
            ["Settings_Appearance_Theme_System_Desc"] = "Automatically adjusts between light and dark modes based on Windows settings.",
            ["Settings_Appearance_Theme_Light_Desc"] = "Always use a bright, high-contrast appearance.",
            ["Settings_Appearance_Theme_Dark_Desc"] = "Always use a dark, low-glare appearance.",
            ["Settings_Appearance_Lang_Title"] = "Display Language",
            ["Settings_Appearance_Lang_Desc"] = "Select your preferred language for menus, dialogs, and messages.",
            ["Settings_Appearance_Lang_System"] = "System default",
            ["Settings_Appearance_Lang_En"] = "English (United States)",
            ["Settings_Appearance_Lang_Pt"] = "Portuguese (Brazil)",
            ["Settings_Appearance_Lang_System_Desc"] = "Uses the language configured in your Windows operating system.",
            ["Settings_Appearance_Lang_En_Desc"] = "Standard American English for all user interface elements.",
            ["Settings_Appearance_Lang_Pt_Desc"] = "Brazilian Portuguese for all user interface elements.",
            ["Settings_Appearance_Restart_Title"] = "Restart Required",
            ["Settings_Appearance_Restart_Desc"] = "Theme and language changes take full effect after restarting the application.",

            ["Settings_Explorer_Header"] = "File Explorer Integration",
            ["Settings_Explorer_ContextMenu_Title"] = "Context Menu",
            ["Settings_Explorer_ContextMenu_Desc"] = "Configure right-click options in Windows File Explorer for archives and folders.",
            ["Settings_Explorer_ContextMenu_Enable"] = "Enable Windows Explorer context menu",
            ["Settings_Explorer_ContextMenu_Cascading"] = "Group commands into a cascading submenu (Zip Buddy >)",
            ["Settings_Explorer_Commands_Title"] = "Available Commands",
            ["Settings_Explorer_Cmd_OpenWith"] = "Open with Zip Buddy",
            ["Settings_Explorer_Cmd_ExtractHere"] = "Extract Here",
            ["Settings_Explorer_Cmd_ExtractToFolder"] = "Extract to archive folder",
            ["Settings_Explorer_Cmd_ExtractTo"] = "Extract to...",
            ["Settings_Explorer_Cmd_CompressZip"] = "Compress to ZIP",
            ["Settings_Explorer_Cmd_Compress7z"] = "Compress to 7Z",
            ["Settings_Explorer_Assoc_Title"] = "File Associations & Default App",
            ["Settings_Explorer_Assoc_Desc"] = "Register Zip Buddy for supported archive formats (.zip, .7z, .rar, .tar, .gz, .bz2, .xz, and more).",
            ["Settings_Explorer_Assoc_Status_Title"] = "Registration status",
            ["Settings_Explorer_Assoc_Status_Desc"] = "User-level registration (HKCU) does not require administrator privileges.",
            ["Settings_Explorer_Assoc_Status_Registered"] = "Registered in Open With",
            ["Settings_Explorer_Assoc_Status_NotRegistered"] = "Not registered in Open With",
            ["Settings_Explorer_Assoc_Btn_Register"] = "Register in Open With",
            ["Settings_Explorer_Assoc_Btn_DefaultApps"] = "Set as Default in Windows",
            ["Settings_Explorer_Assoc_Btn_Unregister"] = "Remove from Open With",
            ["Settings_Explorer_DefaultApps_Note_Title"] = "Windows Default App Assignment",
            ["Settings_Explorer_DefaultApps_Note_Desc"] = "Windows 10 and 11 require setting default file handlers in Windows Settings. Clicking 'Set as Default' opens Windows Default Apps.",
            ["Settings_Explorer_Assoc_Registered_Success"] = "Zip Buddy was successfully registered in 'Open With' for supported archive formats.",
            ["Settings_Explorer_Assoc_Registered_Failed"] = "Could not register file associations in the Windows Registry.",
            ["Settings_Explorer_Assoc_Unregistered_Success"] = "Zip Buddy associations have been removed from the current user account.",
            ["Settings_Explorer_Assoc_Unregistered_Failed"] = "Could not remove file associations from the Windows Registry.",
            ["Settings_Explorer_DefaultApps_Launched"] = "Windows Settings opened. Select Zip Buddy as your default archive application.",

            ["Settings_Save_Failed"] = "Failed to save settings. Please verify permissions and available disk space.",
            ["Settings_Save_IntegrationWarning"] = "Settings saved, but Windows integration could not be updated.",
            ["Settings_Save_Success"] = "Settings saved successfully.",
            ["Settings_Applied_Success"] = "Settings applied successfully.",

            ["Update_UpToDate"] = "You are using the latest version.",
            ["Update_Available"] = "New version available: v{0}",
            ["Update_Checking"] = "Checking for updates...",
            ["Update_Offline"] = "Could not check for updates. The application works normally offline.",
            ["Update_Error"] = "Error checking updates: {0}",
            ["Update_SignatureInvalid"] = "Update rejected: invalid digital signature or tampered package.",
            ["Update_DowngradeRejected"] = "Remote version is older than current version (downgrade rejected).",

            ["Conflict_Title"] = "File already exists",
            ["Conflict_Overwrite"] = "Overwrite",
            ["Conflict_Skip"] = "Skip",
            ["Conflict_Rename"] = "Rename",
            ["Conflict_ApplyToAll"] = "Apply to all remaining items",

            ["Password_Title"] = "Password Required",
            ["Password_Prompt"] = "This archive is encrypted. Please enter the password:",
            ["Password_Show"] = "Show password",
            ["Password_Incorrect"] = "Incorrect password. Please try again.",

            ["Error_PasswordIncorrect"] = "The password may be incorrect, or this archive may use encryption that Zip Buddy cannot read. Check the password and try again.",
            ["Error_SecurityViolation"] = "Zip Buddy blocked an unsafe archive entry to protect files outside the selected destination.",
            ["Error_AccessDenied"] = "Windows denied access to a file or folder. Check its permissions and make sure it is not being used by another app.",
            ["Error_NotFound"] = "A file or folder needed for this operation could not be found. Check the selected location and try again.",
            ["Error_CorruptArchive"] = "Zip Buddy could not read this archive. It may be damaged, incomplete, or in a format this build cannot open.",
            ["Error_SharingViolation"] = "A file needed for this operation is open in another app. Close it and try again.",
            ["Error_IntegrityFailed"] = "Zip Buddy could not verify this archive. It may be damaged or incomplete.",
            ["Error_ExtractFailed"] = "Zip Buddy could not finish extracting the selected files. Check the destination and archive, then try again.",
            ["Error_CompressFailed"] = "Zip Buddy could not create the archive. Check the selected files and destination, then try again.",
            ["Error_Generic"] = "Zip Buddy could not complete this operation. Check the file or folder and try again."
        },
        ["pt-BR"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["AppName"] = "Zip Buddy",
            ["Action_Open"] = "Abrir",
            ["Action_New"] = "Novo",
            ["Action_Extract"] = "Extrair",
            ["Action_Test"] = "Testar",
            ["Action_Delete"] = "Excluir",
            ["Action_Refresh"] = "Atualizar",
            ["Action_Settings"] = "Configurações",
            ["Action_Help"] = "Ajuda",
            ["Action_About"] = "Sobre",
            ["Action_Cancel"] = "Cancelar",
            ["Action_Save"] = "Salvar",
            ["Action_Apply"] = "Aplicar",
            ["Action_Close"] = "Fechar",
            ["Action_Exit"] = "Sair",
            ["Action_Browse"] = "Procurar...",
            ["Action_OK"] = "OK",

            ["Column_Name"] = "Nome",
            ["Column_Size"] = "Tamanho Original",
            ["Column_CompressedSize"] = "Tamanho Compactado",
            ["Column_Modified"] = "Data de Modificação",
            ["Column_Ratio"] = "Taxa",
            ["Column_Encrypted"] = "Criptografado",

            ["Status_Ready"] = "Pronto",
            ["Status_ItemsCount"] = "{0} itens",
            ["Status_TotalSize"] = "Total: {0}",
            ["Status_Selected"] = "{0} selecionados",

            ["Progress_Compressing"] = "Compactando...",
            ["Progress_Extracting"] = "Extraindo...",
            ["Progress_Testing"] = "Testando integridade do arquivo...",
            ["Progress_Speed"] = "{0:F2} MB/s",
            ["Progress_Elapsed"] = "Decorrido: {0}",
            ["Progress_Remaining"] = "Restante: {0}",

            ["Settings_Title"] = "Preferências",
            ["Settings_General"] = "Geral",
            ["Settings_Language"] = "Idioma",
            ["Settings_Language_System"] = "Padrão do sistema",
            ["Settings_Language_En"] = "Inglês (en-US)",
            ["Settings_Language_Pt"] = "Português (pt-BR)",
            ["Settings_Theme"] = "Tema",
            ["Settings_Theme_System"] = "Sistema",
            ["Settings_Theme_Light"] = "Claro",
            ["Settings_Theme_Dark"] = "Escuro",
            ["Settings_DefaultExtractFolder"] = "Pasta de extração padrão",
            ["Settings_OpenFolderAfter"] = "Abrir pasta após extração",
            ["Settings_ConfirmOverwrite"] = "Confirmar antes de sobrescrever arquivos",
            ["Settings_ConfirmDelete"] = "Confirmar antes de excluir",
            ["Settings_AutoCloseTask"] = "Fechar janela de progresso automaticamente ao concluir",
            ["Settings_NotifyCompletion"] = "Notificar ao concluir tarefa",
            ["Settings_ExplorerIntegration"] = "Integração com Windows Explorer",
            ["Settings_EnableContextMenu"] = "Habilitar menu de contexto do Windows Explorer",
            ["Settings_CascadingMenu"] = "Usar submenu em cascata (Zip Buddy >)",
            ["Settings_Updates"] = "Atualizações",
            ["Settings_AutoCheckUpdates"] = "Verificar atualizações automaticamente",
            ["Settings_UpdateFrequency"] = "Frequência de verificação",
            ["Settings_CurrentVersion"] = "Versão atual: v{0}",
            ["Settings_LastCheck"] = "Última verificação: {0}",
            ["Settings_InstallUpdate"] = "Instalar Atualização",

            ["Nav_Extraction"] = "Extração",
            ["Nav_Compression"] = "Compactação",
            ["Nav_Appearance"] = "Aparência",
            ["Nav_FileExplorer"] = "Explorador de Arquivos",
            ["Nav_Notifications"] = "Notificações",
            ["Nav_Updates"] = "Atualizações",
            ["Nav_Advanced"] = "Avançado",
            ["Nav_ComingSoon_Title"] = "Em Refatoração",
            ["Nav_ComingSoon_Desc"] = "Esta categoria será atualizada na próxima fase.",
            ["Nav_ComingSoon_Compression_Title"] = "Formatos de Arquivo e Níveis de Compactação",
            ["Nav_ComingSoon_Compression_Desc"] = "Configure o formato padrão (ZIP, 7-Zip, TAR), algoritmos de compactação e parâmetros de dicionário.",
            ["Nav_ComingSoon_Appearance_Title"] = "Temas e Idioma",
            ["Nav_ComingSoon_Appearance_Desc"] = "Selecione o tema do aplicativo (Padrão do Sistema, Claro, Escuro) e o idioma da interface (en-US, pt-BR).",
            ["Nav_ComingSoon_FileExplorer_Title"] = "Integração com Shell e Tipos de Arquivo",
            ["Nav_ComingSoon_FileExplorer_Desc"] = "Gerencie itens do menu de contexto do Windows Explorer, submenus em cascata e associações de arquivos.",
            ["Nav_ComingSoon_Notifications_Title"] = "Alertas do Sistema e Comportamento de Progresso",
            ["Nav_ComingSoon_Notifications_Desc"] = "Configure notificações de conclusão de tarefas e o comportamento da janela de progresso.",
            ["Nav_ComingSoon_Updates_Title"] = "Verificação Automática de Atualizações",
            ["Nav_ComingSoon_Updates_Desc"] = "Agende verificações em segundo plano, confira assinaturas digitais e instale novas versões.",
            ["Nav_ComingSoon_Advanced_Title"] = "Diagnósticos e Logs",
            ["Nav_ComingSoon_Advanced_Desc"] = "Configure níveis de log de diagnóstico, inspecione logs do aplicativo e ajuste sinalizadores de desempenho.",

            ["Settings_Extract_Header"] = "Preferências de Extração",
            ["Settings_Extract_Dest_Title"] = "Destino Padrão",
            ["Settings_Extract_Dest_Desc"] = "Configure o local onde arquivos e pastas extraídos serão salvos.",
            ["Settings_Extract_Dest_Placeholder"] = "Deixe em branco para usar a pasta do arquivo",
            ["Settings_Extract_Dest_Explanation"] = "Quando não definido, o Zip Buddy extrai os arquivos na mesma pasta onde o arquivo compactado está localizado.",
            ["Settings_Extract_Open_Title"] = "Abertura de Arquivos",
            ["Settings_Extract_Open_Desc"] = "Ação padrão ao dar duplo clique em arquivos suportados no Explorer.",
            ["Settings_Extract_DoubleClick_Open"] = "Abrir no Zip Buddy (Navegar no arquivo)",
            ["Settings_Extract_DoubleClick_ArchiveFolder"] = "Extrair para pasta com nome do arquivo [Recomendado]",
            ["Settings_Extract_DoubleClick_Predefined"] = "Extrair para pasta pré-definida",
            ["Settings_Extract_DoubleClick_Prompt"] = "Selecionar pasta de destino...",
            ["Settings_Extract_DoubleClick_Here"] = "Extrair aqui (na mesma pasta do arquivo)",
            ["Settings_Extract_After_Title"] = "Ações Pós-Extração",
            ["Settings_Extract_After_OpenFolder"] = "Abrir pasta de destino após concluir a extração",
            ["Settings_Extract_After_DeleteArchive"] = "Excluir arquivo compactado original após extrair com sucesso",
            ["Settings_Extract_After_DeleteArchive_Title"] = "Atenção",
            ["Settings_Extract_After_DeleteArchive_Warn"] = "O arquivo compactado original será excluído permanentemente após a extração ser concluída com sucesso.",
            ["Settings_Extract_Safety_Title"] = "Segurança e Confirmações",
            ["Settings_Extract_Safety_ConfirmOverwrite"] = "Confirmar antes de sobrescrever arquivos existentes",
            ["Settings_Extract_Safety_ConfirmDelete"] = "Confirmar antes de excluir itens do arquivo",
            ["Settings_Extract_Adv_Title"] = "Opções Avançadas de Extração",
            ["Settings_Extract_Adv_Subtitle"] = "Limites de recursos e pasta temporária para operações transitórias",
            ["Settings_Extract_Adv_Threads"] = "Máximo de threads de extração",
            ["Settings_Extract_Adv_Threads_Desc"] = "Reservado para o pipeline de extração multithread (a versão atual extrai sequencialmente).",
            ["Settings_Extract_Adv_Threads_Format"] = "{0} threads",
            ["Settings_Extract_Adv_TempDir"] = "Pasta temporária personalizada",
            ["Settings_Extract_Adv_TempDir_Desc"] = "Usada para pré-visualização e operações de arrastar e soltar. A extração padrão grava diretamente na pasta de destino.",
            ["Settings_Extract_Adv_TempDir_Placeholder"] = "Padrão: %TEMP% do Windows",

            ["Settings_Compress_Header"] = "Preferências de Compactação",
            ["Settings_Compress_Format_Title"] = "Formato de Arquivo Padrão",
            ["Settings_Compress_Format_Desc"] = "Selecione o formato de arquivo padrão ao criar novos arquivos compactados.",
            ["Settings_Compress_Format_Zip"] = "ZIP (.zip) [Recomendado para compatibilidade]",
            ["Settings_Compress_Format_SevenZip"] = "7-Zip (.7z) [Maior eficiência de compactação]",
            ["Settings_Compress_Format_Tar"] = "TAR (.tar) [Empacotamento sem compactação]",
            ["Settings_Compress_Format_GZip"] = "GZIP (.tar.gz) [Tarball UNIX padrão]",
            ["Settings_Compress_Format_BZip2"] = "BZIP2 (.tar.bz2) [Tarball UNIX de alta taxa]",
            ["Settings_Compress_Format_Xz"] = "XZ (.tar.xz) [Tarball UNIX LZMA moderno]",
            ["Settings_Compress_Format_Zip_Desc"] = "Suportado universalmente no Windows, macOS e Linux. Compatível com compactação Deflate e criptografia AES-256.",
            ["Settings_Compress_Format_SevenZip_Desc"] = "Formato de alta eficiência utilizando compactação LZMA2. Suporta criptografia robusta, ofuscação de nomes e divisão em volumes.",
            ["Settings_Compress_Format_Tar_Desc"] = "Formato POSIX que armazena arquivos e diretórios sem compactação de dados. Ideal para empacotamento intermediário.",
            ["Settings_Compress_Format_GZip_Desc"] = "Tarball UNIX padrão (.tar.gz) combinando empacotamento TAR com fluxo de compactação Deflate sob parâmetros padrão.",
            ["Settings_Compress_Format_BZip2_Desc"] = "Tarball UNIX (.tar.bz2) com algoritmo Burrows-Wheeler para maiores taxas em arquivos de texto sob parâmetros padrão.",
            ["Settings_Compress_Format_Xz_Desc"] = "Tarball UNIX moderno (.tar.xz) com fluxo de compactação LZMA para máxima economia de espaço sob parâmetros padrão.",

            ["Settings_Compress_Level_Title"] = "Nível de Compactação Padrão",
            ["Settings_Compress_Level_Desc"] = "Equilibre a taxa de compactação com o uso de CPU e memória.",
            ["Settings_Compress_Level_Store"] = "Armazenar (Sem compactação)",
            ["Settings_Compress_Level_Fast"] = "Rápido (Menor uso de CPU)",
            ["Settings_Compress_Level_Normal"] = "Normal (Equilíbrio entre velocidade e tamanho) [Recomendado]",
            ["Settings_Compress_Level_Maximum"] = "Máximo (Alta compactação)",
            ["Settings_Compress_Level_Ultra"] = "Ultra (Menor tamanho, mais lento)",
            ["Settings_Compress_Level_Store_Desc"] = "Empacota arquivos imediatamente sem compactação. Operação mais rápida; ideal para arquivos já compactados (JPG, MP4, MP3).",
            ["Settings_Compress_Level_Fast_Desc"] = "Aplica compactação leve para economizar tempo e reduzir a carga de CPU. Adequado para backups rápidos e transferências.",
            ["Settings_Compress_Level_Normal_Desc"] = "Oferece o melhor equilíbrio geral entre taxa de compactação, velocidade e consumo de memória. Recomendado para uso geral.",
            ["Settings_Compress_Level_Maximum_Desc"] = "Realiza análise de dicionário mais aprofundada para maior economia de espaço. Requer mais tempo de CPU e memória.",
            ["Settings_Compress_Level_Ultra_Desc"] = "Maximiza a taxa de compactação utilizando profundidade máxima de dicionário. Produz o menor arquivo, com maior uso de CPU e memória.",

            ["Settings_Compress_Warn_Tar_Title"] = "Arquivo Sem Compactação",
            ["Settings_Compress_Warn_Tar_Message"] = "O formato TAR apenas empacota os arquivos sem compactação. Os dados não serão reduzidos independentemente do nível selecionado.",
            ["Settings_Compress_Warn_TarLevel_Title"] = "Nível de Compactação Não Suportado",
            ["Settings_Compress_Warn_TarLevel_Message"] = "O formato TAR é um container não compactado e não suporta níveis de compactação. O nível selecionado será ignorado.",
            ["Settings_Compress_Info_FixedLevel_Title"] = "Parâmetros Fixos de Compactação",
            ["Settings_Compress_Info_FixedLevel_Message"] = "Formatos tar compactados (.tar.gz, .tar.bz2) utilizam parâmetros padronizados de algoritmo. Os níveis de compactação não são ajustáveis para este formato.",
            ["Settings_Compress_Warn_FixedLevel_Title"] = "Nível de Compactação Não Suportado",
            ["Settings_Compress_Warn_FixedLevel_Message"] = "Formatos tar compactados (.tar.gz, .tar.bz2) operam com parâmetros fixos no Zip Buddy. Níveis ajustáveis aplicam-se apenas a arquivos ZIP e 7-Zip.",
            ["Settings_Compress_Warn_ZipUltra_Title"] = "Observação sobre o Formato ZIP",
            ["Settings_Compress_Warn_ZipUltra_Message"] = "A compactação Ultra no formato ZIP utiliza busca máxima no Deflate. Para taxas significativamente superiores, recomenda-se o formato 7-Zip (.7z).",
            ["Settings_Compress_Tar_Reset_Title"] = "Aviso sobre o Formato TAR",
            ["Settings_Compress_Tar_Reset_Message"] = "O formato TAR apenas empacota os arquivos sem compactação. O nível de compactação foi definido como Normal (empacotamento padrão).",
            ["Settings_Compress_Fixed_Reset_Title"] = "Parâmetros Fixos de Compactação",
            ["Settings_Compress_Fixed_Reset_Message"] = "Formatos tar compactados (.tar.gz, .tar.bz2) utilizam parâmetros padronizados de algoritmo. O nível de compactação foi definido como Normal.",
            ["Settings_Compress_Tar_Level_Desc"] = "O formato TAR armazena arquivos sem compactação de dados. Ajuste de nível de compactação não é aplicável.",
            ["Settings_Compress_Fixed_Level_Desc"] = "Opera com parâmetros de compactação padronizados e fixos. Níveis ajustáveis aplicam-se apenas a arquivos ZIP e 7-Zip.",

            ["Settings_Appearance_Header"] = "Preferências de Aparência",
            ["Settings_Appearance_Theme_Title"] = "Tema do Aplicativo",
            ["Settings_Appearance_Theme_Desc"] = "Escolha a aparência do Zip Buddy no seu dispositivo.",
            ["Settings_Appearance_Theme_System"] = "Padrão do sistema",
            ["Settings_Appearance_Theme_Light"] = "Claro",
            ["Settings_Appearance_Theme_Dark"] = "Escuro",
            ["Settings_Appearance_Theme_System_Desc"] = "Ajusta automaticamente entre os modos claro e escuro com base nas configurações do Windows.",
            ["Settings_Appearance_Theme_Light_Desc"] = "Sempre usa uma aparência clara e de alto contraste.",
            ["Settings_Appearance_Theme_Dark_Desc"] = "Sempre usa uma aparência escura com menos brilho.",
            ["Settings_Appearance_Lang_Title"] = "Idioma de Exibição",
            ["Settings_Appearance_Lang_Desc"] = "Selecione o idioma de sua preferência para menus, diálogos e mensagens.",
            ["Settings_Appearance_Lang_System"] = "Padrão do sistema",
            ["Settings_Appearance_Lang_En"] = "Inglês (Estados Unidos)",
            ["Settings_Appearance_Lang_Pt"] = "Português (Brasil)",
            ["Settings_Appearance_Lang_System_Desc"] = "Usa o idioma configurado no sistema operacional Windows.",
            ["Settings_Appearance_Lang_En_Desc"] = "Inglês padrão (Estados Unidos) para todos os elementos da interface.",
            ["Settings_Appearance_Lang_Pt_Desc"] = "Português do Brasil para todos os elementos da interface.",
            ["Settings_Appearance_Restart_Title"] = "Reinicialização Necessária",
            ["Settings_Appearance_Restart_Desc"] = "As alterações de tema e idioma terão efeito completo após reiniciar o aplicativo.",

            ["Settings_Explorer_Header"] = "Integração com o Explorador de Arquivos",
            ["Settings_Explorer_ContextMenu_Title"] = "Menu de Contexto",
            ["Settings_Explorer_ContextMenu_Desc"] = "Configure as opções de clique com botão direito no Windows Explorer para arquivos e pastas.",
            ["Settings_Explorer_ContextMenu_Enable"] = "Habilitar menu de contexto do Windows Explorer",
            ["Settings_Explorer_ContextMenu_Cascading"] = "Agrupar comandos em um submenu em cascata (Zip Buddy >)",
            ["Settings_Explorer_Commands_Title"] = "Comandos Disponíveis",
            ["Settings_Explorer_Cmd_OpenWith"] = "Abrir com o Zip Buddy",
            ["Settings_Explorer_Cmd_ExtractHere"] = "Extrair Aqui",
            ["Settings_Explorer_Cmd_ExtractToFolder"] = "Extrair para pasta com nome do arquivo",
            ["Settings_Explorer_Cmd_ExtractTo"] = "Extrair para...",
            ["Settings_Explorer_Cmd_CompressZip"] = "Compactar para ZIP",
            ["Settings_Explorer_Cmd_Compress7z"] = "Compactar para 7Z",
            ["Settings_Explorer_Assoc_Title"] = "Associações de Arquivos e Aplicativo Padrão",
            ["Settings_Explorer_Assoc_Desc"] = "Registra o Zip Buddy para formatos de arquivo suportados (.zip, .7z, .rar, .tar, .gz, .bz2, .xz e outros).",
            ["Settings_Explorer_Assoc_Status_Title"] = "Status do registro",
            ["Settings_Explorer_Assoc_Status_Desc"] = "O registro no nível do usuário (HKCU) não requer privilégios de administrador.",
            ["Settings_Explorer_Assoc_Status_Registered"] = "Registrado em Abrir com",
            ["Settings_Explorer_Assoc_Status_NotRegistered"] = "Não registrado em Abrir com",
            ["Settings_Explorer_Assoc_Btn_Register"] = "Registrar em Abrir com",
            ["Settings_Explorer_Assoc_Btn_DefaultApps"] = "Definir como Padrão no Windows",
            ["Settings_Explorer_Assoc_Btn_Unregister"] = "Remover de Abrir com",
            ["Settings_Explorer_DefaultApps_Note_Title"] = "Definição de Padrão no Windows",
            ["Settings_Explorer_DefaultApps_Note_Desc"] = "O Windows 10 e 11 exigem que os aplicativos padrão sejam definidos nas Configurações do Windows. Ao clicar em 'Definir como Padrão', a tela do sistema será aberta.",
            ["Settings_Explorer_Assoc_Registered_Success"] = "Zip Buddy registrado com sucesso no menu 'Abrir com' para formatos de arquivo suportados.",
            ["Settings_Explorer_Assoc_Registered_Failed"] = "Não foi possível registrar as associações de arquivos no Registro do Windows.",
            ["Settings_Explorer_Assoc_Unregistered_Success"] = "As associações do Zip Buddy foram removidas da conta do usuário atual.",
            ["Settings_Explorer_Assoc_Unregistered_Failed"] = "Não foi possível remover as associações de arquivos do Registro do Windows.",
            ["Settings_Explorer_DefaultApps_Launched"] = "Configurações do Windows abertas. Selecione o Zip Buddy como aplicativo padrão.",

            ["Settings_Save_Failed"] = "Falha ao salvar as configurações. Verifique as permissões de acesso e o espaço em disco.",
            ["Settings_Save_IntegrationWarning"] = "Configurações salvas, mas a integração com o Windows não pôde ser atualizada.",
            ["Settings_Save_Success"] = "Configurações salvas com sucesso.",
            ["Settings_Applied_Success"] = "Configurações aplicadas com sucesso.",

            ["Update_UpToDate"] = "Você está usando a versão mais recente.",
            ["Update_Available"] = "Nova versão disponível: v{0}",
            ["Update_Checking"] = "Verificando atualizações...",
            ["Update_Offline"] = "Não foi possível verificar atualizações. O aplicativo funciona normalmente offline.",
            ["Update_Error"] = "Erro ao verificar: {0}",
            ["Update_SignatureInvalid"] = "Atualização recusada: assinatura digital inválida ou pacote adulterado.",
            ["Update_DowngradeRejected"] = "Versão remota inferior à versão atual (downgrade recusado).",

            ["Conflict_Title"] = "O arquivo já existe",
            ["Conflict_Overwrite"] = "Sobrescrever",
            ["Conflict_Skip"] = "Pular",
            ["Conflict_Rename"] = "Renomear",
            ["Conflict_ApplyToAll"] = "Aplicar a todos os itens restantes",

            ["Password_Title"] = "Senha Necessária",
            ["Password_Prompt"] = "Este arquivo está criptografado. Digite a senha:",
            ["Password_Show"] = "Mostrar senha",
            ["Password_Incorrect"] = "Senha incorreta. Tente novamente.",

            ["Error_PasswordIncorrect"] = "A senha pode estar incorreta, ou este arquivo pode usar criptografia incompatível com o Zip Buddy. Verifique a senha e tente novamente.",
            ["Error_SecurityViolation"] = "O Zip Buddy bloqueou uma entrada insegura do arquivo para proteger pastas fora do destino selecionado.",
            ["Error_AccessDenied"] = "O Windows negou acesso a um arquivo ou pasta. Verifique as permissões e certifique-se de que não está em uso por outro aplicativo.",
            ["Error_NotFound"] = "Um arquivo ou pasta necessário para esta operação não foi encontrado. Verifique o local selecionado e tente novamente.",
            ["Error_CorruptArchive"] = "O Zip Buddy não conseguiu ler este arquivo. Ele pode estar danificado, incompleto ou em um formato incompatível.",
            ["Error_SharingViolation"] = "Um arquivo necessário para esta operação está aberto em outro aplicativo. Feche-o e tente novamente.",
            ["Error_IntegrityFailed"] = "O Zip Buddy não conseguiu verificar este arquivo. Ele pode estar danificado ou incompleto.",
            ["Error_ExtractFailed"] = "O Zip Buddy não conseguiu concluir a extração dos arquivos. Verifique o destino e o arquivo e tente novamente.",
            ["Error_CompressFailed"] = "O Zip Buddy não conseguiu criar o arquivo. Verifique os arquivos selecionados e o destino e tente novamente.",
            ["Error_Generic"] = "O Zip Buddy não conseguiu concluir esta operação. Verifique o arquivo ou pasta e tente novamente."
        }
    };
}
