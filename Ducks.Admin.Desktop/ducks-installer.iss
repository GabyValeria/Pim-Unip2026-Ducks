; Script de Instalação do Ducks Admin Desktop
; Plataforma Educacional Gamificada - Ducks

#define MyAppName "Ducks Admin Desktop"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Equipe Ducks"
#define MyAppURL "https://github.com/GabyValeria/Pim-Unip2026-Ducks.git"
#define MyAppExeName "Ducks.Admin.Desktop.exe"

[Setup]
; Identificador único da aplicação (GUID)
AppId={{A8B5F3C2-1234-4567-89AB-CDEF01234567}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\DucksAdminDesktop
DisableProgramGroupPage=yes
; Pasta onde o instalador final será salvo
OutputDir=.\OutputInstalador
OutputBaseFilename=DucksAdminDesktop_Setup_v1.0.0
SetupIconFile=.\duck-icon.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "portuguese"; MessagesFile: "compiler:Languages\\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Copia o executável publicado pelo .NET para o diretório de destino do usuário
Source: "src\Ducks.Admin.Desktop\bin\Release\net10.0-windows\win-x64\publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "src\Ducks.Admin.Desktop\bin\Release\net10.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; Cria os atalhos no Menu Iniciar e na Área de Trabalho com o nosso ícone oficial
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\duck-icon.ico"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\duck-icon.ico"; Tasks: desktopicon

[Run]
; Opção para abrir o aplicativo logo após a conclusão da instalação
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent