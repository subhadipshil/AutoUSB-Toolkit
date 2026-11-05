@echo off

echo ===================================================================
echo   AutoUSB Toolkit - Car Stereo USB Optimizer ^& Formatter
echo   Building Standalone Native Windows Executable...
echo ===================================================================

set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe

if not exist "%CSC%" (
    echo [ERROR] C# compiler csc.exe not found at: %CSC%
    exit /b 1
)

if not exist "bin" mkdir bin

echo Compiling C# source files with embedded icon...
"%CSC%" /nologo /target:winexe /optimize+ /platform:anycpu ^
    /win32manifest:App.manifest ^
    /win32icon:icons\app.ico ^
    /resource:icons\app.png,AutoUSBToolkit.icons.app.png ^
    /out:bin\AutoUSB-Toolkit.exe ^
    /r:System.dll,System.Core.dll,System.Data.dll,System.Drawing.dll,System.Windows.Forms.dll,System.Xml.dll,System.Xaml.dll,"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF\PresentationCore.dll","C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF\PresentationFramework.dll","C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF\WindowsBase.dll" ^
    /recurse:src\*.cs

if errorlevel 1 goto :fail

echo.
echo ===================================================================
echo   [SUCCESS] Build completed successfully.
echo   Executable: bin\AutoUSB-Toolkit.exe
echo   App Icon:   Embedded (Win32 icon + High-Res PNG Resource)
echo ===================================================================
exit /b 0

:fail
echo.
echo [ERROR] Build failed with exit code %ERRORLEVEL%.
exit /b %ERRORLEVEL%
