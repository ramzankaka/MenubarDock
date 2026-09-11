# MenubarDock Uninstaller
Write-Host "Uninstalling MenubarDock..." -ForegroundColor Yellow

# Stop running process
Get-Process MenubarDock -ErrorAction SilentlyContinue | Stop-Process -Force

# Remove Registry Startup
$runKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
Remove-ItemProperty -Path $runKey -Name "MenubarDock" -ErrorAction SilentlyContinue

# Remove Start Menu shortcut
$startMenuDir = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\MenubarDock"
if (Test-Path $startMenuDir) {
    Remove-Item -Path $startMenuDir -Recurse -Force
}

# Remove app files
$installDir = "$env:LOCALAPPDATA\MenubarDock"
if (Test-Path $installDir) {
    Remove-Item -Path $installDir -Recurse -Force
}

Write-Host "MenubarDock uninstalled cleanly." -ForegroundColor Green
