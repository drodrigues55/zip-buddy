; Zip Buddy Inno Setup Script
; Generates silent-install capable, Winget-ready native Windows installer

#define MyAppName "Zip Buddy"
#define MyAppVersion "1.0.1"
#define MyAppPublisher "Zip Buddy Contributors"
#define MyAppURL "https://github.com/drodrigues55/zip-buddy"
#define MyAppExeName "ZipBuddy.UI.exe"

[Setup]
AppId={{8B036367-AE8C-4D88-B1F3-E18F2E23A534}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\..\dist
OutputBaseFilename=ZipBuddySetup-x64-v{#MyAppVersion}
SetupIconFile=..\..\src\ZipBuddy.UI\Assets\AppIcon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
LZMAUseSeparateProcess=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
ChangesAssociations=yes
PrivilegesRequiredOverridesAllowed=commandline dialog
UsedUserAreasWarning=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "autoupdate"; Description: "Verificar atualizações automaticamente"; GroupDescription: "Atualizações / Updates:"; Flags: checkedonce
Name: "assoc_zip"; Description: "Associate with .zip files"; GroupDescription: "File Associations:"
Name: "assoc_7z"; Description: "Associate with .7z files"; GroupDescription: "File Associations:"
Name: "assoc_rar"; Description: "Associate with .rar files"; GroupDescription: "File Associations:"
Name: "assoc_tar"; Description: "Associate with .tar, .gz, .bz2 files"; GroupDescription: "File Associations:"

[Files]
Source: "..\..\ZipBuddy.UI.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\ZipBuddyUpdater.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\MyUnzip.exe"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "..\..\MyUnzipUpdater.exe"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
; File type registration
Root: HKA; Subkey: "Software\Classes\ZipBuddy.Archive"; ValueType: string; ValueData: "Zip Buddy Archive"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\ZipBuddy.Archive\DefaultIcon"; ValueType: string; ValueData: "{app}\{#MyAppExeName},0"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\ZipBuddy.Archive\shell\open"; ValueType: string; ValueData: "Open with Zip Buddy"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\ZipBuddy.Archive\shell\open"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\{#MyAppExeName},0"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\ZipBuddy.Archive\shell\open\command"; ValueType: string; ValueData: """{app}\{#MyAppExeName}"" ""%1"""; Flags: uninsdeletekey

; Dedicated task verbs for archive files
Root: HKA; Subkey: "Software\Classes\ZipBuddy.Archive\shell\ExtractHere"; ValueType: string; ValueData: "Extract Here"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\ZipBuddy.Archive\shell\ExtractHere"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\{#MyAppExeName},0"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\ZipBuddy.Archive\shell\ExtractHere\command"; ValueType: string; ValueData: """{app}\{#MyAppExeName}"" --extract-here ""%1"""; Flags: uninsdeletekey

Root: HKA; Subkey: "Software\Classes\ZipBuddy.Archive\shell\ExtractToFolder"; ValueType: string; ValueData: "Extract to Folder"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\ZipBuddy.Archive\shell\ExtractToFolder"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\{#MyAppExeName},0"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\ZipBuddy.Archive\shell\ExtractToFolder\command"; ValueType: string; ValueData: """{app}\{#MyAppExeName}"" --extract-to-folder ""%1"""; Flags: uninsdeletekey

Root: HKA; Subkey: "Software\Classes\ZipBuddy.Archive\shell\ExtractTo"; ValueType: string; ValueData: "Extract to..."; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\ZipBuddy.Archive\shell\ExtractTo"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\{#MyAppExeName},0"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\ZipBuddy.Archive\shell\ExtractTo\command"; ValueType: string; ValueData: """{app}\{#MyAppExeName}"" --extract-to ""%1"""; Flags: uninsdeletekey

; File associations in HKA (machine or per-user depending on install scope)
Root: HKA; Subkey: "Software\Classes\.zip"; ValueType: string; ValueData: "ZipBuddy.Archive"; Tasks: assoc_zip; Flags: uninsdeletevalue
Root: HKA; Subkey: "Software\Classes\.7z"; ValueType: string; ValueData: "ZipBuddy.Archive"; Tasks: assoc_7z; Flags: uninsdeletevalue
Root: HKA; Subkey: "Software\Classes\.rar"; ValueType: string; ValueData: "ZipBuddy.Archive"; Tasks: assoc_rar; Flags: uninsdeletevalue
Root: HKA; Subkey: "Software\Classes\.tar"; ValueType: string; ValueData: "ZipBuddy.Archive"; Tasks: assoc_tar; Flags: uninsdeletevalue
Root: HKA; Subkey: "Software\Classes\.gz"; ValueType: string; ValueData: "ZipBuddy.Archive"; Tasks: assoc_tar; Flags: uninsdeletevalue
Root: HKA; Subkey: "Software\Classes\.bz2"; ValueType: string; ValueData: "ZipBuddy.Archive"; Tasks: assoc_tar; Flags: uninsdeletevalue

; OpenWithProgids so Explorer lists Zip Buddy in Open With
Root: HKA; Subkey: "Software\Classes\.zip\OpenWithProgids"; ValueType: none; ValueName: "ZipBuddy.Archive"; Tasks: assoc_zip; Flags: uninsdeletevalue
Root: HKA; Subkey: "Software\Classes\.7z\OpenWithProgids"; ValueType: none; ValueName: "ZipBuddy.Archive"; Tasks: assoc_7z; Flags: uninsdeletevalue
Root: HKA; Subkey: "Software\Classes\.rar\OpenWithProgids"; ValueType: none; ValueName: "ZipBuddy.Archive"; Tasks: assoc_rar; Flags: uninsdeletevalue
Root: HKA; Subkey: "Software\Classes\.tar\OpenWithProgids"; ValueType: none; ValueName: "ZipBuddy.Archive"; Tasks: assoc_tar; Flags: uninsdeletevalue
Root: HKA; Subkey: "Software\Classes\.gz\OpenWithProgids"; ValueType: none; ValueName: "ZipBuddy.Archive"; Tasks: assoc_tar; Flags: uninsdeletevalue
Root: HKA; Subkey: "Software\Classes\.bz2\OpenWithProgids"; ValueType: none; ValueName: "ZipBuddy.Archive"; Tasks: assoc_tar; Flags: uninsdeletevalue

; Also write to HKCU so current user profile receives file association immediately
Root: HKCU; Subkey: "Software\Classes\.zip"; ValueType: string; ValueData: "ZipBuddy.Archive"; Tasks: assoc_zip; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.7z"; ValueType: string; ValueData: "ZipBuddy.Archive"; Tasks: assoc_7z; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.rar"; ValueType: string; ValueData: "ZipBuddy.Archive"; Tasks: assoc_rar; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.tar"; ValueType: string; ValueData: "ZipBuddy.Archive"; Tasks: assoc_tar; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.gz"; ValueType: string; ValueData: "ZipBuddy.Archive"; Tasks: assoc_tar; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.bz2"; ValueType: string; ValueData: "ZipBuddy.Archive"; Tasks: assoc_tar; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.zip\OpenWithProgids"; ValueType: none; ValueName: "ZipBuddy.Archive"; Tasks: assoc_zip; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.7z\OpenWithProgids"; ValueType: none; ValueName: "ZipBuddy.Archive"; Tasks: assoc_7z; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.rar\OpenWithProgids"; ValueType: none; ValueName: "ZipBuddy.Archive"; Tasks: assoc_rar; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.tar\OpenWithProgids"; ValueType: none; ValueName: "ZipBuddy.Archive"; Tasks: assoc_tar; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.gz\OpenWithProgids"; ValueType: none; ValueName: "ZipBuddy.Archive"; Tasks: assoc_tar; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.bz2\OpenWithProgids"; ValueType: none; ValueName: "ZipBuddy.Archive"; Tasks: assoc_tar; Flags: uninsdeletevalue

; Windows Default Programs Capabilities (enables Zip Buddy in Windows 10/11 Default Apps list)
Root: HKA; Subkey: "Software\Zip Buddy\Capabilities"; ValueType: string; ValueName: "ApplicationName"; ValueData: "{#MyAppName}"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Zip Buddy\Capabilities"; ValueType: string; ValueName: "ApplicationDescription"; ValueData: "Zip Buddy Archive Manager"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Zip Buddy\Capabilities\FileAssociations"; ValueType: string; ValueName: ".zip"; ValueData: "ZipBuddy.Archive"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Zip Buddy\Capabilities\FileAssociations"; ValueType: string; ValueName: ".7z"; ValueData: "ZipBuddy.Archive"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Zip Buddy\Capabilities\FileAssociations"; ValueType: string; ValueName: ".rar"; ValueData: "ZipBuddy.Archive"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Zip Buddy\Capabilities\FileAssociations"; ValueType: string; ValueName: ".tar"; ValueData: "ZipBuddy.Archive"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Zip Buddy\Capabilities\FileAssociations"; ValueType: string; ValueName: ".gz"; ValueData: "ZipBuddy.Archive"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Zip Buddy\Capabilities\FileAssociations"; ValueType: string; ValueName: ".bz2"; ValueData: "ZipBuddy.Archive"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\RegisteredApplications"; ValueType: string; ValueName: "Zip Buddy"; ValueData: "Software\Zip Buddy\Capabilities"; Flags: uninsdeletevalue

; Applications subkey for Windows Open With integration
Root: HKA; Subkey: "Software\Classes\Applications\{#MyAppExeName}"; ValueType: string; ValueName: "FriendlyAppName"; ValueData: "{#MyAppName}"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\Applications\{#MyAppExeName}\DefaultIcon"; ValueType: string; ValueData: "{app}\{#MyAppExeName},0"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".zip"; ValueData: ""; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".7z"; ValueData: ""; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".rar"; ValueData: ""; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".tar"; ValueData: ""; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".gz"; ValueData: ""; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".bz2"; ValueData: ""; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\Applications\{#MyAppExeName}\shell\open\command"; ValueType: string; ValueData: """{app}\{#MyAppExeName}"" ""%1"""; Flags: uninsdeletekey

[Run]
Filename: "{sys}\schtasks.exe"; Parameters: "/Create /TN ""Zip Buddy\ZipBuddyUpdateTask"" /TR """"{app}\ZipBuddyUpdater.exe"""" --auto --silent"" /SC DAILY /F"; Flags: runhidden; Tasks: autoupdate
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
Filename: "ms-settings:defaultapps"; Description: "Definir Zip Buddy como aplicativo padrão nas Configurações do Windows"; Flags: postinstall shellexec skipifsilent unchecked

[UninstallRun]
Filename: "{sys}\schtasks.exe"; Parameters: "/Delete /TN ""Zip Buddy\ZipBuddyUpdateTask"" /F"; Flags: runhidden
