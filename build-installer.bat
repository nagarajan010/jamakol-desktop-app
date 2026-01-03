@echo off
setlocal
echo ========================================
echo   Jamakol Astrology Installer Builder
echo ========================================
echo.

set CERT_NAME=JamakolCert.pfx
set CERT_PASS=jamakol123
set "SIGNTOOL=C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\signtool.exe"

echo [1/4] Checking for Signing Certificate...
if not exist "%CERT_NAME%" (
    echo     Certificate not found. Generating Self-Signed Certificate...
    powershell -Command "$cert = New-SelfSignedCertificate -CertStoreLocation Cert:\CurrentUser\My -Subject 'CN=Jamakol Astrology' -Type CodeSigningCert; Export-PfxCertificate -Cert $cert -FilePath '%CERT_NAME%' -Password (ConvertTo-SecureString -String '%CERT_PASS%' -Force -AsPlainText)"
    if errorlevel 1 (
        echo ERROR: Failed to generate certificate!
        pause
        exit /b 1
    )
    echo     Certificate generated: %CERT_NAME%
    echo     IMPORTANT: You must install this certificate to Trusted Root Authorities to trust it locally.
) else (
    echo     Using existing certificate: %CERT_NAME%
)

echo [1.5/4] Exporting Public Certificate...
if not exist "JamakolCert.cer" (
    echo     Exporting public certificate for installer inclusion...
    powershell -Command "$cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2('%CERT_NAME%', '%CERT_PASS%'); [System.IO.File]::WriteAllBytes('JamakolCert.cer', $cert.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Cert))"
    if errorlevel 1 (
         echo ERROR: Failed to export public certificate!
         pause
         exit /b 1
    )
)

echo.
echo [2/4] Cleaning previous build...
if exist "publish" rmdir /s /q "publish"

echo.
echo [3/4] Publishing application...
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish
if errorlevel 1 (
    echo ERROR: Publish failed!
    pause
    exit /b 1
)

echo.
echo [3.5/4] Signing application executable...
"%SIGNTOOL%" sign /f "%CERT_NAME%" /p "%CERT_PASS%" /t http://timestamp.digicert.com /fd SHA256 "publish\JamakolAstrology.exe"
if errorlevel 1 (
    echo WARNING: Failed to sign executable. Is 'signtool' in your PATH?
    echo Attempting to verify 'signtool' presence...
    where signtool
) else (
    echo     Executable signed successfully.
)

echo.
echo [4/4] Building installer with signature...
rem Resolve absolute path for certificate to ensure ISCC finds it
set "ABS_CERT_PATH=%~dp0%CERT_NAME%"

rem Verify certificate exists at absolute path
if not exist "%ABS_CERT_PATH%" (
    echo ERROR: Certificate not found at %ABS_CERT_PATH%
    pause
    exit /b 1
)

"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" /S"MySignTool=""%SIGNTOOL%"" sign /f ""%ABS_CERT_PATH%"" /p ""%CERT_PASS%"" /t http://timestamp.digicert.com /fd SHA256 $f" installer.iss
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
