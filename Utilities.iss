#define MyAppName "Utilities"
#define MyAppDisplayName "Утилиты"
#define MyAppVersion "1.1.2"
#define MyAppPublisher "andrey1b"
#define MyAppURL "https://github.com/andrey1b/Utilities"
#define MyAppExeName "Utilities.exe"
#define MyAppSourceDir "bin\Release\net9.0-windows10.0.19041.0\win-x64\publish"

[Setup]
AppId={{F1E2D3C4-B5A6-4978-8A1B-2C3D4E5F6071}
AppName={#MyAppDisplayName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppDisplayName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppDisplayName}
DisableProgramGroupPage=yes
SetupIconFile=app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
OutputDir=Distributions
OutputBaseFilename=Utilities_Setup_v{#MyAppVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
; Многофайловая self-contained сборка — все файлы из publish (без single-file,
; чтобы не срабатывала эвристика «упаковщика» антивирусов).
Source: "{#MyAppSourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "app.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "snapshot.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppDisplayName}";        Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\app.ico"
Name: "{autodesktop}\{#MyAppDisplayName}";  Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\app.ico"; Tasks: desktopicon

; Отдельный ярлык второй утилиты «Снимок → текст» (запускает захват напрямую через --snapshot)
Name: "{group}\Снимок → текст";       Filename: "{app}\{#MyAppExeName}"; Parameters: "--snapshot"; WorkingDir: "{app}"; IconFilename: "{app}\snapshot.ico"
Name: "{autodesktop}\Снимок → текст"; Filename: "{app}\{#MyAppExeName}"; Parameters: "--snapshot"; WorkingDir: "{app}"; IconFilename: "{app}\snapshot.ico"

[Run]
Filename: "{app}\{#MyAppExeName}"; \
  Description: "{cm:LaunchProgram,{#StringChange(MyAppDisplayName, '&', '&&')}}"; \
  Flags: nowait postinstall skipifsilent
