[Setup]
AppName=FuelMeter
AppVersion=1.0
AppPublisher=FuelMeter
AppPublisherURL=https://fuelmeter.com
DefaultDirName={autopf}\FuelMeter
DefaultGroupName=FuelMeter
OutputDir=Installer
OutputBaseFilename=FuelMeterSetup
SetupIconFile=Resources\AppIcon\appicon.ico
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
; All published output — adjust path if you changed -o above
Source: "publish\windows-exe\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\FuelMeter";       Filename: "{app}\FuelMeter.exe"
Name: "{group}\Uninstall FuelMeter"; Filename: "{uninstallexe}"
Name: "{commondesktop}\FuelMeter"; Filename: "{app}\FuelMeter.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\FuelMeter.exe"; Description: "{cm:LaunchProgram,FuelMeter}"; Flags: nowait postinstall skipifsilent
