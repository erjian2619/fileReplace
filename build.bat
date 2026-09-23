@echo off
setlocal
cd /d "%~dp0"

set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"

if not exist "%CSC%" (
  echo [ERROR] Windows C# compiler was not found.
  if /i not "%~1"=="--no-pause" pause
  exit /b 1
)

echo Building FileReplaceTool.exe...
"%CSC%" /nologo /target:winexe /optimize+ /platform:anycpu ^
  /win32manifest:FileReplaceTool.manifest ^
  /reference:System.dll ^
  /reference:System.Drawing.dll ^
  /reference:System.Windows.Forms.dll ^
  /out:FileReplaceTool.exe ^
  FileReplaceTool.cs ModernMainForm.cs

if errorlevel 1 (
  echo.
  echo [FAILED] See the compiler output above.
  if /i not "%~1"=="--no-pause" pause
  exit /b 1
)

echo.
echo [DONE] Created: %CD%\FileReplaceTool.exe
if /i not "%~1"=="--no-pause" pause
