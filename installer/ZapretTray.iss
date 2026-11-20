#define AppName "Zapret Tray"
#define AppVersion "0.2.0"
#define AppPublisher "vitalyabsolute000-beep"
#define AppExeName "ZapretTrayApp.exe"
#ifndef AppSource
  #define AppSource "..\dist\ZapretTrayApp.exe"
#endif

[Setup]
AppId={{C3B28881-4320-4C5C-BE20-9C48A61B1F68}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={localappdata}\Programs\Zapret Tray
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\dist
OutputBaseFilename=ZapretTraySetup-{#AppVersion}
SetupIconFile=..\ZapretTrayApp\app.ico
UninstallDisplayIcon={app}\{#AppExeName}
WizardStyle=modern
Compression=lzma2/ultra64
SolidCompression=yes
CloseApplications=yes
RestartApplications=no
Uninstallable=yes

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "desktopicon"; Description: "Создать ярлык на рабочем столе"; GroupDescription: "Дополнительные ярлыки:"

[Files]
Source: "{#AppSource}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Запустить {#AppName}"; Flags: postinstall nowait skipifsilent
