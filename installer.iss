; Jamakol Astrology Installer Script
; Inno Setup Script

#define MyAppName "Jamakol Astrology"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Jamakol"
#define MyAppExeName "JamakolAstrology.exe"
#define MyAppURL ""

[Setup]
AppId={{B8E5D7C3-A1F2-4E89-B6D4-2C7E8F9A0B1D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=installer
OutputBaseFilename=JamakolAstrology_Setup
SetupIconFile=jamkol-astro.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
CreateUninstallRegKey=yes
UninstallDisplayName={#MyAppName}
SignTool=MySignTool


[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "JamakolCert.cer"; DestDir: "{app}"; Flags: ignoreversion


[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "certutil.exe"; Parameters: "-addstore ""Root"" ""{app}\JamakolCert.cer"""; Description: "Install Trusted Certificate"; Flags: runascurrentuser nowait postinstall; StatusMsg: "Installing Certificate..."

Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
