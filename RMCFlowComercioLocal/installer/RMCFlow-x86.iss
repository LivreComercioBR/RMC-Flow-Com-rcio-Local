#define MyAppName "RMC Flow Comércio Local"
#define MyAppVersion "3.9.0"
#define MyAppExeName "RMCFlowComercioLocal.exe"
[Setup]
AppId={{73892053-7B29-4A4C-B8F1-D9E112344AAA}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
DefaultDirName={autopf}\RMC Sites\RMC Flow Comércio Local
DefaultGroupName=RMC Flow Comércio Local
OutputBaseFilename=RMC-Flow-Comercio-Local-Setup-x86
ArchitecturesAllowed=x86compatible
PrivilegesRequired=lowest
Compression=lzma2
SolidCompression=yes
[Files]
Source: "..\src\RMCFlowComercioLocal\bin\x86\Release\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs
[Icons]
Name: "{group}\RMC Flow Comércio Local"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\RMC Flow Comércio Local"; Filename: "{app}\{#MyAppExeName}"
[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Abrir RMC Flow Comércio Local"; Flags: nowait postinstall skipifsilent

[Code]
function InitializeSetup(): Boolean;
var
  Release: Cardinal;
begin
  Result := RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release) and (Release >= 528040);
  if not Result then
    MsgBox('O RMC Flow Comércio Local requer o Microsoft .NET Framework 4.8. Instale-o antes de continuar a instalação.', mbError, MB_OK);
end;
