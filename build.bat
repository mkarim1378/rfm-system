@echo off
echo Building RFM System...
dotnet build RFMSystem/RFMSystem.csproj --configuration Release
if %ERRORLEVEL% EQU 0 (
    echo Build successful!
    echo Executable location: RFMSystem\bin\Release\net8.0-windows\RFMSystem.exe
) else (
    echo Build failed!
    pause
)

