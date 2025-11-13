@echo off
echo Checking .NET installation...
dotnet --info

echo.
echo Checking for SDK...
dotnet --list-sdks

if %errorlevel% neq 0 (
    echo.
    echo ERROR: .NET SDK not found!
    echo You have the .NET Runtime but need the .NET SDK to build projects.
    echo.
    echo Please download and install the .NET SDK from:
    echo https://dotnet.microsoft.com/download/dotnet
    echo.
    echo Choose the SDK version (not just runtime^)
    echo.
    pause
    exit /b 1
)

echo.
echo SDK found! Attempting to run the climate simulation...
echo.
dotnet run

pause