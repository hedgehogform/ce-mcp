# Cheat Engine MCP Server

`ce-mcp` is an x64 Cheat Engine plugin that exposes Cheat Engine workflows as a stateless Model Context Protocol server over Streamable HTTP. It builds as one `ce-mcp.dll` with managed dependencies embedded and includes a distributable AI skill beside the DLL.

[![FOSSA](https://app.fossa.com/api/projects/git%2Bgithub.com%2FShadowNineX%2Fce-mcp.svg?type=large&issueType=license)](https://app.fossa.com/projects/git%2Bgithub.com%2FShadowNineX%2Fce-mcp?ref=badge_large&issueType=license)

> [!WARNING]
> This project is under active development. Memory writes, process control, file operations, injection, compilation, debugger actions, DBVM, Auto Assembler, and arbitrary Lua can alter or crash a target or host. Use a disposable process while testing.

## Capabilities

| Area | Examples |
| --- | --- |
| Processes | List/open/create processes, pause/resume, inspect threads and state |
| Memory | Typed reads/writes, regions, protection, allocation, copy/compare, hashes, dump/load |
| Pointers and scans | Pointer chains, direct-reference scans, AOB, string, first/next value scans |
| Symbols and structures | Modules, symbols, RTTI, registered symbols, Structure Dissect CRUD and comparison |
| Code | Assembly, disassembly, bounded analysis, Auto Assembler, target C compilation |
| Cheat tables | Address-list records and `.CT` load/save |
| Debugger | Interfaces, breakpoints, hit tracking, threads, registers, XMM, stepping, stack traces, LBR |
| Injection and execution | Script generation, native/.NET injection, remote function calls |
| Optional DBVM | Availability, physical-memory access, and watches |
| Lua and conversion | Structured Lua execution, MD5, ANSI/UTF-8 conversion |

The maintained tool inventory, enum values, and recommended workflows are in [`skills/ce-mcp/references/tool-catalog.md`](skills/ce-mcp/references/tool-catalog.md). Prefer the live MCP schemas for exact parameter names and defaults.

## Quick Start

### 1. Install prerequisites

- Windows x64.
- Cheat Engine 7.6.2 or newer.
- [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0).
- [ASP.NET Core 10 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0).

For development, also install the .NET 10 SDK:

```powershell
winget install Microsoft.DotNet.SDK.10
winget install Microsoft.DotNet.DesktopRuntime.10
winget install Microsoft.DotNet.AspNetCore.10
```

### 2. Get the plugin

Download the latest successful artifact from the [build workflow](https://github.com/ShadowNineX/ce-mcp/actions/workflows/build-dlls.yml):

- `ce-mcp-release`: optimized build for normal use.
- `ce-mcp-debug`: build with debugging information.

Each bundle contains:

```text
ce-mcp.dll
skills/ce-mcp/
```

Copy `ce-mcp.dll` into Cheat Engine's `plugins` directory. Keep `skills/ce-mcp/` with the distributed bundle for AI clients that consume repository skills.

### 3. Start the server

1. Restart Cheat Engine.
2. Enable the `ce-mcp` plugin in Cheat Engine's plugin settings.
3. Choose **MCP** -> **Start MCP Server**.
4. Connect an MCP client to `http://127.0.0.1:6300/`.
5. Call `get_plugin_version` to confirm which DLL is loaded.

This is a Cheat Engine plugin, not a standalone executable.

## MCP Client Configuration

For clients that accept a Streamable HTTP endpoint:

```json
{
  "mcpServers": {
    "cheat-engine": {
      "url": "http://127.0.0.1:6300/"
    }
  }
}
```

The endpoint is stateless and mapped at `/`.

## Configuration

Choose **MCP** -> **Configure** to edit the host, port, and server name. Settings are persisted to:

```text
%APPDATA%\CeMCP\config.json
```

Example:

```json
{
  "Host": "127.0.0.1",
  "Port": 6300,
  "ServerName": "Cheat Engine MCP Server"
}
```

Configuration precedence is:

```text
defaults < config.json < MCP_HOST / MCP_PORT
```

Environment override example:

```powershell
$env:MCP_HOST = "127.0.0.1"
$env:MCP_PORT = "6300"
```

> [!IMPORTANT]
> The default loopback host is intentional. Binding to a non-loopback interface exposes powerful target and host operations to the network. Add authentication and network controls before doing so.

## Runtime Compatibility

Some Cheat Engine 7.6.x installations still declare .NET 9 frameworks in `ce.runtimeconfig.json`. This plugin targets .NET 10 and requires these shared frameworks:

```json
{
  "runtimeOptions": {
    "tfm": "net10.0",
    "frameworks": [
      {
        "name": "Microsoft.NETCore.App",
        "version": "10.0.0",
        "rollForward": "latestMinor"
      },
      {
        "name": "Microsoft.WindowsDesktop.App",
        "version": "10.0.0",
        "rollForward": "latestMinor"
      },
      {
        "name": "Microsoft.AspNetCore.App",
        "version": "10.0.0",
        "rollForward": "latestMinor"
      }
    ]
  }
}
```

If Cheat Engine reports `CEPluginInitialize (Result=80070002)`, install the Desktop and ASP.NET Core 10 runtimes and verify this file.

## Architecture

```text
Cheat Engine
  -> CESDK plugin bootstrap and shared Lua state
  -> ce-mcp tool adapter
  -> typed CESDK facade
  -> LuaUtils / LuaNative
  -> Cheat Engine API and target process
```

`src/McpServer.cs` builds the ASP.NET Core host and explicitly registers every tool class with the required schema transform. HTTP requests may be concurrent, but Cheat Engine Lua state and engine objects are not thread-safe. CE-facing work is serialized onto Cheat Engine's GUI thread.

Stateful scanner order matters:

```text
deinitialize old results -> scan -> WaitTillDone -> initialize results -> read -> deinitialize
```

`CESDK/` is a git submodule and is compiled into the plugin assembly. See [`CESDK/README.md`](CESDK/README.md) for wrapper and plugin-bootstrap guidance.

## Build from Source

Clone with the CESDK submodule, then build from the repository root:

```powershell
git submodule update --init --recursive
dotnet restore
dotnet build
dotnet test --filter "TestCategory!=Live"
dotnet build -c Release
```

Outputs:

```text
bin/x64/Debug/net10.0-windows/ce-mcp.dll
bin/x64/Release/net10.0-windows/ce-mcp.dll
```

CI-equivalent sequence:

```powershell
dotnet restore
dotnet build -c Debug --no-restore
dotnet test -c Debug --no-restore --no-build --filter "TestCategory!=Live"
dotnet build -c Release --no-restore
```

There is no repository formatter or lint command. SonarCloud is the configured static-analysis gate.

## Testing

### Normal tests

The normal suite is deterministic and does not require Cheat Engine:

```powershell
dotnet test --filter "TestCategory!=Live"
```

Test layout:

- `tests/CeMCP.Tests/Unit/`: validation, schemas, configuration, metadata, result shapes, logging, and skill packaging.
- `tests/CeMCP.Tests/Live/`: opt-in MCP tests against a CE-loaded plugin.
- `tests/CeMCP.Tests/Support/`: shared result assertions.
- `CESDK/tests/`: separate CE-loaded wrapper harness and JSON report validator.

### Safe live MCP tests

Build and install the fresh Debug DLL, restart CE, start the MCP server, then run:

```powershell
$env:CE_MCP_LIVE = "1"
$env:CE_MCP_URL = "http://127.0.0.1:6300/"
dotnet test --filter TestCategory=Live
```

The default live suite uses inspection calls. Scan regressions execute only when CE already has a readable target; otherwise they are inconclusive.

### Dedicated Notepad suite

The opt-in Notepad suite launches a disposable Notepad process through MCP, discovers the real Windows 11 Notepad PID, exercises every process-scoped tool with a safe scenario, restores CE table state, frees allocations, deletes owned temporary files, detaches the debugger, and terminates only that PID.

```powershell
$env:CE_MCP_LIVE = "1"
$env:CE_MCP_NOTEPAD_LIVE = "1"
$env:CE_MCP_URL = "http://127.0.0.1:6300/"
dotnet test tests/CeMCP.Tests/CeMCP.Tests.csproj `
  -p:Platform=x64 `
  --filter TestCategory=NotepadLive
```

The suite fails if any live tool has neither a test scenario nor a documented exclusion. Exclusions are limited to operations that are not safely scoped to Notepad: foreground retargeting, symbol downloads/kernel symbols, native/.NET payload injection, and DBVM initialization/physical-memory/watch operations.

## Logging and Troubleshooting

CESDK and the ASP.NET Core host use one isolated NLog factory and one log file:

```text
%APPDATA%\CeMCP\ce-mcp.log
```

The file rolls at 10 MiB and retains five archives.

Common checks:

1. Call `get_plugin_version` and verify the reported DLL path/version.
2. Confirm CE was restarted after replacing the DLL.
3. Verify the Desktop and ASP.NET Core 10 runtimes are installed.
4. Verify `ce.runtimeconfig.json` declares the .NET 10 frameworks.
5. Inspect `%APPDATA%\CeMCP\ce-mcp.log`.
6. Keep the server on loopback while debugging connectivity.

If a build reports a locked Fody or DLL file, close Cheat Engine and any process loading the build output, then rebuild.

## Contributors

<a href="https://github.com/ShadowNineX/ce-mcp/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=ShadowNineX/ce-mcp" alt="Contributors" />
</a>

Made with [contrib.rocks](https://contrib.rocks).
