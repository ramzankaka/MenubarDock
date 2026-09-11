# MenubarDock Installer
$installDir = "$env:LOCALAPPDATA\MenubarDock"
$binDir = "$installDir\bin"
New-Item -ItemType Directory -Path $binDir -Force | Out-Null

$srcBin = "$PSScriptRoot\self_contained_dist"
if (-not (Test-Path "$srcBin\MenubarDock.exe")) {
    $srcBin = "$PSScriptRoot\bin\Release\net8.0-windows"
}

Copy-Item -Path "$srcBin\*" -Destination $binDir -Recurse -Force

# Create Start Menu Shortcut
$wshShell = New-Object -ComObject WScript.Shell
$startMenuDir = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\MenubarDock"
New-Item -ItemType Directory -Path $startMenuDir -Force | Out-Null
$shortcut = $wshShell.CreateShortcut("$startMenuDir\MenubarDock.lnk")
$shortcut.TargetPath = "$binDir\MenubarDock.exe"
$shortcut.Description = "macOS Top Menu Bar for Windows 11"
$shortcut.WorkingDirectory = $binDir
$shortcut.Save()

Write-Host "MenubarDock installed successfully to $installDir" -ForegroundColor Green
