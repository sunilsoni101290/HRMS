# BiometricAgent

Small Windows Service that runs **on a machine at the client's site** (same
local network as the biometric device) and forwards attendance punches to
the central ERP. It exists because the ERP API generally cannot reach a
device sitting behind the client's own router/firewall - something local has
to read the device and push out over the internet instead.

```
Biometric device (LAN)  <--SDK/TCP-->  BiometricAgent (client PC/mini-PC)
                                              |
                                              | HTTPS POST /api/BiometricSync/ingest
                                              v
                                     Central ERP API/Database
```

## What it does each cycle (default: every 60s, configurable)

1. Retries any punches queued from a previous failed push (offline queue).
2. Connects to the device, reads punches since the last successful sync.
3. Posts them to `POST /api/BiometricSync/ingest` with the device's
   `DeviceCode` + `DeviceKey`.
4. On success, advances its local "last synced" marker. On failure (API/
   internet down), writes the punches to a local queue file and retries
   next cycle - nothing is lost.

## One-time setup on the client machine

1. **Create the device record** in the ERP (Biometric Devices screen or
   `POST /api/BiometricDevice`), or note the `DeviceCode` and `DeviceKey`
   of an existing one. `DeviceKey` is auto-generated on create - copy it
   now, it's only easily visible right after creation.
2. **Install the vendor SDK** that shipped with the biometric device
   (eSSL's install disc/download, e.g. "eTimeTrackLite" bundles it). Note
   where the COM DLL lands.
3. **Register the COM DLL** from an elevated, 32-bit command prompt:
   ```
   C:\Windows\SysWOW64\regsvr32.exe "C:\path\to\zkemkeeper.dll"
   ```
4. **Confirm the ProgID** the DLL registered under (usually
   `zkemkeeper.CZKEM` for ZK-family SDKs, which most eSSL devices use
   under the hood - check `HKEY_CLASSES_ROOT` if unsure) and put it in
   `appsettings.json` under `Agent:Device:ProgId`.
5. **Edit `appsettings.json`**: `TenantId`, `ApiBaseUrl`, `Device.DeviceCode`,
   `Device.DeviceKey`, `Device.IPAddress`, `Device.Port`.
6. **Verify method names/parameter mapping** in
   `Drivers/EsslDeviceDriver.cs` against the SDK's actual documentation -
   `Connect_Net`, `ReadGeneralLogData`, and `SSR_GetGeneralLogData` are the
   standard ZK-family names, but confirm the in/out-mode mapping matches
   how the device is configured (see comments in that file).

Until steps 2-4 are done, set `Agent:Device:DriverType` to `"Mock"` - the
agent will generate fake punches every cycle so you can prove out the
Windows Service install, logging, offline queue, and API push end-to-end
without hardware.

## Publish and install as a Windows Service

```powershell
dotnet publish -c Release -r win-x86 --self-contained false -o C:\Apps\BiometricAgent

sc create "ERP Biometric Agent" binPath= "C:\Apps\BiometricAgent\BiometricAgent.exe"
sc start "ERP Biometric Agent"
```

Logs go to the Windows Event Log (source: the service name) by default when
running as a service; when run interactively (`dotnet run` / the .exe
directly) they print to the console.

## Notes / things to validate before go-live

- **Bitness**: most vendor SDKs are 32-bit COM DLLs - the project is
  already pinned to `x86` for this reason. If the client's SDK is 64-bit,
  change `PlatformTarget` in `BiometricAgent.csproj`.
- **One agent per device** (or one agent handling multiple devices if you
  extend `Worker`/`AgentOptions` to loop a device list) - the current
  version is wired for a single device per installed service instance,
  which is the simplest setup for a single-branch office.
- **Network**: the client machine needs LAN access to the device's IP/port
  (commonly 4370) and outbound HTTPS access to the ERP's `ApiBaseUrl`.
- **Security**: `DeviceKey` is a shared secret - treat `appsettings.json`
  like a credential file (NTFS permissions restricting who can read it).
