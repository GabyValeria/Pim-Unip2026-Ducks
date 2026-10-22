[Setup]
AppName=Ducks Admin Desktop
AppVersion=1.0.0
AppPublisher=Ecossistema Ducks
DefaultDirName={autopf}\DucksAdmin
DefaultGroupName=Ducks Admin
OutputBaseFilename=DucksAdminSetup_v1.0.0
Compression=lzma2
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "publish\desktop\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\Ducks Admin Desktop"; Filename: "{app}\Ducks.Admin.Desktop.exe"
Name: "{autodesktop}\Ducks Admin Desktop"; Filename: "{app}\Ducks.Admin.Desktop.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Ducks.Admin.Desktop.exe"; Description: "{cm:LaunchProgram,Ducks Admin Desktop}"; Flags: nowait postinstall skipifsilent