# MironovLab.OvpnMonitor

MironovLab.OvpnMonitor is an OpenVPN connection monitoring service built on top
of the [OpenVPN Management Interface](https://openvpn.net/community-resources/management-interface/).
It tracks connected clients and traffic counters and stores user and session
information in MySQL.

The solution also includes a reusable client library for connecting to the
Management Interface, executing commands, reading status information, and
processing real-time OpenVPN messages.

## Features

- Monitors multiple OpenVPN servers concurrently.
- Automatically reconnects when the Management Interface connection is lost.
- Reads connected clients and incoming/outgoing traffic counters.
- Stores users, sessions, and daily intermediate statistics in MySQL.
- Supports password-protected and unprotected Management Interfaces.
- Handles `CLIENT`, `BYTECOUNT_CLI`, `STATE`, `LOG`, `PASSWORD`, `NEED-OK`,
  `NEED-STR`, and other OpenVPN messages.
- Parses OpenVPN packet filter configurations.
- Optionally replaces NAT or forwarded connection addresses with the client's
  real IP address by using conntrack events or socat logs.
- Runs as either a console application or a systemd service.

## Solution structure

| Project | Purpose |
| --- | --- |
| `MironovLab.OvpnMonitor.Service` | .NET Worker Service that monitors OpenVPN and writes data to MySQL |
| `MironovLab.OpenVPN.Management` | OpenVPN Management Interface client library targeting `netstandard2.0` and `net8` |
| `MironovLab.OvpnMonitor.Tests` | Parser unit tests and integration tests that connect to OpenVPN |

## Requirements

- .NET 8 SDK
- MySQL 8 or a compatible server
- OpenVPN with the Management Interface enabled
- Linux and the `conntrack` utility when using the `Conntrack` translation mode
- A socat systemd service and `journalctl` when using the `Socat` translation mode

## Database setup

The repository includes
[`openvpn.sql`](openvpn.sql), which
can be used to reproduce the required database structure quickly. The script
creates:

- the `openvpn` database if it does not already exist;
- the `users`, `sessions`, and `intermediate_data` tables;
- primary, unique, and lookup indexes;
- foreign keys between users, sessions, and intermediate data;
- the `active_sessions` view, which returns recently updated sessions.

Import the complete database schema from the repository root with:

```bash
mysql -u root -p < openvpn.sql
```

The script creates the `openvpn` database with `utf8mb4` encoding and selects it
before creating the remaining database objects. Running it requires an account
with permission to create databases and schemas.

The schema uses the MySQL 8 `utf8mb4_0900_ai_ci` collation. The
`active_sessions` view is declared with `DEFINER=root@localhost`; change or
remove that clause if the schema is imported under a different administrative
account.

The account configured for the service must have permission to read and modify
these tables.

## Configuration

The main settings are stored in the `AppConfiguration` section of
`MironovLab.OvpnMonitor.Service/appsettings.json`:

```json
{
  "AppConfiguration": {
    "OpenVPNConfigurations": [
      {
        "HostAddress": "127.0.0.1",
        "Port": 5555,
        "Password": "management-password"
      }
    ],
    "MySqlConfiguration": {
      "Server": "localhost",
      "Port": 3306,
      "User": "openvpn-monitor",
      "Password": "database-password",
      "DataBase": "openvpn"
    },
    "AddressTranslatorConfiguration": {
      "Method": "None",
      "Proto": "udp",
      "LocalPort": 1195,
      "TargetIPAddress": "127.0.0.1",
      "TargetPort": 1194,
      "SourceIPAddress": "192.168.1.1",
      "WaitingTimeout": "00:00:03",
      "SocatModuleName": "socat"
    }
  }
}
```

`OpenVPNConfigurations` may contain more than one server. A separate worker is
created for each entry. Leave `Password` empty when the Management Interface is
not password-protected.

Address translation is optional and is intended for deployments where the
machine running this service also performs NAT or forwards connections to the
OpenVPN server. In that setup, OpenVPN may report the address of an intermediate
endpoint instead of the client's real IP address. The translation mechanism
replaces that address with the original client address.

Two translation methods are available:

- `Conntrack` — resolves the client's real IP address from conntrack events;
- `Socat` — resolves the client's real IP address from the socat systemd
  journal.

Set `Method` to `None` when address translation is not required.

Do not store production passwords in `appsettings.json`. Use .NET User Secrets
for local development and environment variables or a dedicated secret store in
production. For example:

```text
AppConfiguration__MySqlConfiguration__Password
AppConfiguration__OpenVPNConfigurations__0__Password
```

## Build and run

Restore dependencies and build the solution:

```bash
dotnet restore MironovLab.OvpnMonitor.sln
dotnet build MironovLab.OvpnMonitor.sln
```

Run the service from the repository root:

```bash
dotnet run --project MironovLab.OvpnMonitor.Service
```

The service uses the standard .NET Generic Host configuration system. Select an
environment with `DOTNET_ENVIRONMENT` and override configuration values with
environment variables when needed.

## Publish for Linux

To publish a self-contained Linux x64 application:

```bash
dotnet publish MironovLab.OvpnMonitor.Service \
  --configuration Release \
  --runtime linux-x64 \
  --self-contained true
```

The application calls `UseSystemd()`, so the published executable can be hosted
as a regular systemd service. Its working directory must contain a valid
`appsettings.json`, unless all required settings are supplied through the
environment.

## Tests

Run the entire test project with:

```bash
dotnet test MironovLab.OvpnMonitor.Tests
```

The parser tests do not require an external OpenVPN server:

```bash
dotnet test MironovLab.OvpnMonitor.Tests \
  --filter "FullyQualifiedName~ParsersTests"
```

`OvpnPasswordProtectedTests` and `OvpnNoPasswordProtectedTests` are integration
tests. They currently use Management Interface addresses and ports defined in
the source code, so a matching OpenVPN server must be available. Some of these
tests also wait for events for several minutes.

## Using the Management library

The following example connects to a password-protected Management Interface and
reads the current status:

```csharp
using MironovLab.OpenVPN.Management;

using var manager = new OvpnManager();
manager.Connect("127.0.0.1", 5555, "management-password");

var status = manager.Status();
foreach (var client in status.Clients)
{
    Console.WriteLine($"{client.CommonName}: {client.RealAddress}");
}
```

Use the `Connect(host, port)` overload for a Management Interface without a
password.

## Security

The Management Interface provides administrative access to OpenVPN. Do not
expose it to an untrusted network. Bind it to a loopback address or a protected
internal network and use password authentication where possible.
