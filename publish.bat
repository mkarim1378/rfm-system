@echo off
echo Publishing RFM System as standalone executable...
dotnet publish RFMSystem/RFMSystem.csproj --configuration Release --self-contained true --runtime win-x64 --output ./publish
if %ERRORLEVEL% EQU 0 (
    echo.
    echo Publish successful!
    echo Executable location: publish\RFMSystem.exe
    echo.
    echo You can now run the application from the publish folder without .NET SDK installed.
) else (
    echo Publish failed!
    pause
)

