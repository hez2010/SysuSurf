# SysuSurf
Cross-platform Implementation for SYSU H3C and Ruijie Authentication.

## Prerequsities
- NpCap

## Quick Start
```bash
SysuSurf config.json
```

## Config Schema
```json5
{
    "Type": 0, // 0: H3C, 1: Ruijie
    "UserName": "your netid",
    "Password": "your password",
    "DeviceName": "your ethernet inteface id",
    "Md5Method": 0, // H3C optional. 0: xor, 1: md5
    "GroupcastMode": 0, // Ruijie optional. 0: Standard, 1: Ruijie Private, 2: Saier
    "DhcpMode": 0, // Ruijie optional. 0: None, 1: Second Auth, 2: After Auth, 3: Before Auth
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