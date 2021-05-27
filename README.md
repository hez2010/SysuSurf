# SysuH3C
Cross-platform Implementation for SYSU H3C Authentication.

## Prerequsities
- NpCap

## Quick Start
```bash
SysuH3C config.json
```

## Config Schema
```json
{
    "UserName": "your netid",
    "Password": "your password",
    "DeviceName": "your ethernet inteface id",
    "Md5Method": 0 // 0: xor, 1: md5
}
```

## Development Guide
### Prerequsities
- NpCap
- .NET 6 SDK

### Build
```bash
dotnet build -c Release
# Or build with Debug mode
dotnet build
```

### Run
```bash
dotnet run -c Release config.json
# Or run with Debug mode
dotnet run config.json
```

### Publish
```bash
dotnet publish -c Release -r win-x64
dotnet publish -c Release -r osx-x64
dotnet publish -c Release -r linux-x64
dotnet publish -c Release -r linux-arm64
# Available rids: win-x86, win-x64, win-arm, win-arm64, osx-x64, osx-arm64, linux-x64, linux-arm, linux-arm64
```