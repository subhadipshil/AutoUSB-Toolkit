# AutoUSB Toolkit PowerShell Build Script
Write-Host "===================================================================" -ForegroundColor Cyan
Write-Host "  AutoUSB Toolkit - Car Stereo USB Optimizer & Formatter" -ForegroundColor Cyan
Write-Host "  Building Standalone Native Windows Executable..." -ForegroundColor Cyan
Write-Host "===================================================================" -ForegroundColor Cyan

$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) {
    Write-Error "C# compiler not found at $csc"
    exit 1
}

if (-not (Test-Path "bin")) {
    New-Item -ItemType Directory -Path "bin" | Out-Null
}

$refs = @(
    "System.dll",
    "System.Core.dll",
    "System.Data.dll",
    "System.Drawing.dll",
    "System.Windows.Forms.dll",
    "System.Xml.dll",
    "System.Xaml.dll",
    "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF\PresentationCore.dll",
    "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF\PresentationFramework.dll",
    "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF\WindowsBase.dll"
) -join ","

$argsList = @(
    "/nologo",
    "/target:winexe",
    "/optimize+",
    "/platform:anycpu",
    "/win32manifest:App.manifest",
    "/win32icon:icons\app.ico",
    "/resource:icons\app.png,AutoUSBToolkit.icons.app.png",
    "/out:bin\AutoUSB-Toolkit.exe",
    "/r:$refs",
    "/recurse:src\*.cs"
)

Write-Host "Compiling source files with embedded icon..." -ForegroundColor Yellow
$proc = Start-Process -FilePath $csc -ArgumentList $argsList -NoNewWindow -Wait -PassThru

if ($proc.ExitCode -eq 0) {
    Write-Host ""
    Write-Host "===================================================================" -ForegroundColor Green
    Write-Host "  [SUCCESS] Build completed successfully!" -ForegroundColor Green
    Write-Host "  Executable: bin\AutoUSB-Toolkit.exe" -ForegroundColor Green
    Write-Host "  App Icon:   Embedded (Win32 .ico + High-Res PNG Resource)" -ForegroundColor Green
    Write-Host "===================================================================" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "[ERROR] Compilation failed with exit code $($proc.ExitCode)" -ForegroundColor Red
    exit $proc.ExitCode
}
