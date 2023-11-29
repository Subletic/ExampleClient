# Mock Server

Contains an ASP.NET Core project for Mocking a Client using Subletic. It attempts to connect to the Subletic Backend and retries it when the connection is interrupted.

## Usage

| Description | Command |
|---|---|
| Installation of .NET SDK | `winget install Microsoft.DotNet.SDK.7` |
| Check if .NET-SDK installation was successful | `dotnet --version` |
| Load all dependency's | `dotnet restore` |
| Start Backend | `dotnet run` |
| Run UnitTests | `dotnet test` |

## Connection

To start the software a few environment-variables have to be set:

| Variable-Name | Value | Development | Production |
|---|---|---|---|
| BACKEND_WEBSOCKET_URL | ws://d.projekte.swe.htwk-leipzig.de:40114/transcribe | ❌ | ✅ |

## Ports

| Software    | Port  |
|-------------|-------|
| Frontend    | 40110 |
| Backend     | 40114 |
| Mock Server | 40118 |

