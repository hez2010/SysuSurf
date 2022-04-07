# SysuSurf
Cross-platform Implementation for SYSU H3C and Ruijie Authentication.

## Prerequsities
- pcap (libpcap, NPcap, WinPcap and etc.)

## Quick Start
```bash
./SysuSurf help
./SysuSurf auth config.json
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

## Windows Service Integration
SysuSurf supports Windows Service Integration, you can configure it to run with Windows startup automatically.

To create the service, use below command and run it in an elevated shell:
```bash
sc create SysuSurf binPath= "<path/to/SysuSurf.exe> auth <path/to/config.json>"
```

To remove the service, use below command and run it in an elevated shell:
```bash
sc delete SysuSurf
```

## Development Guide
### Prerequsities
- pcap (libpcap, NPcap, WinPcap and etc.)
- .NET 6 SDK

### Build
```bash
dotnet build -c Release
# Or build with Debug mode
dotnet build
```

### Run
```bash
dotnet run -c Release -- auth config.json
# Or run with Debug mode
dotnet run -- auth config.json
```

### Publish
```bash
dotnet publish -c Release -r win-x64
dotnet publish -c Release -r osx-x64
dotnet publish -c Release -r linux-x64
dotnet publish -c Release -r linux-arm64
# Available rids: win-x86, win-x64, win-arm, win-arm64, osx-x64, osx-arm64, linux-x64, linux-arm, linux-arm64, linux-musl-x64, linux-musl-arm64, ...
```
