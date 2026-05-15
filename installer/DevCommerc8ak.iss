[Setup]
AppName=DevCommerc8ak
AppVersion=1.0.0
DefaultDirName={pf}\DevCommerc8ak
DefaultGroupName=DevCommerc8ak
OutputBaseFilename=DevCommerc8ak-Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Tasks]
Name: "desktopicon"; Description: "Creer un raccourci sur le Bureau"; GroupDescription: "Raccourcis"

[Files]
Source: "..\prototype\WinFormsVB\DevCommerc8ak\bin\Release\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\prototype\WinFormsVB\DevCommerc8ak\Resources\*"; DestDir: "{app}\Resources"; Flags: recursesubdirs createallsubdirs

[Icons]
Name: "{group}\DevCommerc8ak"; Filename: "{app}\DevCommerc8ak.exe"
Name: "{commondesktop}\DevCommerc8ak"; Filename: "{app}\DevCommerc8ak.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\DevCommerc8ak.exe"; Description: "Lancer DevCommerc8ak"; Flags: nowait postinstall skipifsilent
