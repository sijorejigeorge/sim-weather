# Setup Instructions for MonoGame Climate Simulation

## Current Issue
You have the .NET Runtime installed but need the .NET **SDK** to build and run this project from source code.

## Solution: Install .NET SDK

### Step 1: Download the SDK
1. Go to: https://dotnet.microsoft.com/download/dotnet
2. Choose either:
   - **.NET 8** (Latest) - Recommended
   - **.NET 6** (LTS - Long Term Support)

### Step 2: Download the Correct Package
**IMPORTANT**: Download the **SDK** not just the runtime!
- Look for "SDK x64" (not "Runtime x64")
- The SDK includes everything needed to build and run projects

### Step 3: Install
1. Run the downloaded installer
2. Follow the installation wizard
3. Restart your command prompt/PowerShell

### Step 4: Verify Installation
Open a new command prompt and run:
```cmd
dotnet --info
```

You should now see something like:
```
.NET SDKs installed:
  8.0.xxx [C:\Program Files\dotnet\sdk\8.0.xxx]
```

### Step 5: Run the Game
Once the SDK is installed, you can run the climate simulation with:
```cmd
dotnet run
```

Or simply double-click the `run.bat` file I created for you.

## What You Currently Have vs What You Need

**You have**: .NET Runtime 10.0.0-rc.2.25502.107
- This allows you to RUN compiled .NET applications
- Cannot build or compile source code

**You need**: .NET SDK
- This includes the runtime PLUS development tools
- Allows you to build, compile, and run projects from source

## Alternative: Pre-compiled Version
If you prefer not to install the SDK, I could help you set up the project differently, but the SDK approach is much simpler and gives you the full development experience.