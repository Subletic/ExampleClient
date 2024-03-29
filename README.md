# Example-Client

Example how to connect to Subletic. It attempts to connect to the Backend and retries it when the connection is interrupted.

## Usage

| Description | Command |
|---|---|
| Installation of .NET SDK | `winget install Microsoft.DotNet.SDK.7` |
| Check if .NET-SDK installation was successful | `dotnet --version` |
| Load all dependency's | `dotnet restore` |
| Start Backend | `dotnet run` |
| Run UnitTests | `dotnet test` |

## Connection

To start the software a few **environment-variables** have to be set. When the software is run for development purpose a **`launchSettings.json`** can be used to set these values. Also note the port **`40118`** the Mock-Server is started on.

| Variable-Name | Value |
|---|---|
| BACKEND_WEBSOCKET_URL | ws://d.projekte.swe.htwk-leipzig.de:40114/transcribe |

**`Properties/launchSettings.json`:**
```json
{
    "$schema": "https://json.schemastore.org/launchsettings.json",
    "iisSettings": {
        "windowsAuthentication": false,
        "anonymousAuthentication": true,
        "iisExpress": {
            "applicationUrl": "http://localhost:61006",
            "sslPort": 44387
        }
    },
    "profiles": {
        "http": {
            "commandName": "Project",
            "dotnetRunMessages": true,
            "launchBrowser": true,
            "launchUrl": "swagger",
            "applicationUrl": "http://localhost:40118",
            "environmentVariables": {
                "BACKEND_WEBSOCKET_URL": "ws://localhost:40114/transcribe"
            }
        },
        "IIS Express": {
            "commandName": "IISExpress",
            "launchBrowser": true,
            "launchUrl": "swagger",
        }
    }
}
```

## Ports

| Software    | HTTP Port  | HTTPS Port |
|-------------|------------|------------|
| Backend     | 40114      | 40115      |
| Mock Server | 40118      | 40119      |

