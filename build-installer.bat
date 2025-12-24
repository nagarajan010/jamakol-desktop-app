@echo off
echo ========================================
echo   Jamakol Astrology Installer Builder
echo ========================================
echo.

echo [1/3] Cleaning previous build...
if exist "publish" rmdir /s /q "publish"

echo [2/3] Publishing application...
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish
if errorlevel 1 (
    echo ERROR: Publish failed!
    pause
    exit /b 1
)

echo [3/3] Building installer...
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer.iss
if errorlevel 1 (
    echo ERROR: Installer build failed!
    pause
    exit /b 1
)

echo.
echo ========================================
echo   SUCCESS! Installer created at:
echo   installer\JamakolAstrology_Setup.exe
echo ========================================
echo.
pause
