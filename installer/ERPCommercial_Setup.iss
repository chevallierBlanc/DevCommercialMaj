; ERPCommercial production installer

[Setup]
AppId={{5B41D4A7-5E6C-4D2B-8E49-5E0C6A5D0C31}}
AppName=ERPCommercial
AppVersion=1.0.0
AppPublisher=NTANTA ANDY
DefaultDirName={pf}\ERPCommercial
DefaultGroupName=ERPCommercial
OutputBaseFilename=ERPCommercial_Setup
OutputDir=.\Output
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
DisableProgramGroupPage=no
AllowNoIcons=no

[Dirs]
Name: "{app}\Logs"; Flags: uninsneveruninstall; Permissions: users-modify
Name: "{app}\Backups"; Flags: uninsneveruninstall; Permissions: users-modify
Name: "{app}\Config"; Flags: uninsneveruninstall; Permissions: users-modify
Name: "{app}\Reports"; Flags: uninsneveruninstall; Permissions: users-modify

[Files]
Source: "..\prototype\WinFormsVB\DevCommerc8ak\bin\Release\DevCommerc8ak.exe"; DestDir: "{app}"; DestName: "ERPCommercial.exe"; Flags: ignoreversion
Source: "..\prototype\WinFormsVB\DevCommerc8ak\bin\Release\DevCommerc8ak.exe.config"; DestDir: "{app}"; DestName: "ERPCommercial.exe.config"; Flags: onlyifdoesntexist
Source: "..\prototype\WinFormsVB\DevCommerc8ak\bin\Release\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "DevCommerc8ak.exe;DevCommerc8ak.exe.config"
Source: "..\prototype\WinFormsVB\DevCommerc8ak\Resources\images\logo.bmp"; DestDir: "{app}\Resources\images"; Flags: ignoreversion
Source: "..\prototype\WinFormsVB\DevCommerc8ak\Resources\images\logo.bmp"; DestDir: "{app}\Config"; DestName: "logo.bmp"; Flags: ignoreversion onlyifdoesntexist
Source: "..\README_PRODUCTION.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\docs\GUIDE_INSTALLATION_INNOSETUP.md"; DestDir: "{app}\Docs"; Flags: ignoreversion

[Icons]
Name: "{group}\ERPCommercial"; Filename: "{app}\ERPCommercial.exe"; WorkingDir: "{app}"
Name: "{commondesktop}\ERPCommercial"; Filename: "{app}\ERPCommercial.exe"; WorkingDir: "{app}"

[Run]
Filename: "{app}\ERPCommercial.exe"; Description: "Lancer ERPCommercial"; Flags: nowait postinstall skipifsilent
