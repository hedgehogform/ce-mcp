# Repository Guidelines

## Project Overview

`ce-mcp` builds `ce-mcp.dll`, an x64 Cheat Engine 7.6.2+ plugin that hosts a stateless MCP server over Streamable HTTP. It exposes process control, memory/pointer/scan, assembly/analysis, symbol/RTTI, Structure Dissect, cheat-table, injection, debugger, optional DBVM, address-list, conversion, and Lua operations. The plugin targets `net10.0-windows`, uses WPF for configuration, and embeds managed dependencies into one DLL.

## Architecture & Data Flow

1. Cheat Engine loads the plugin through `CESDK`, which initializes shared Lua/native state and calls `McpPlugin.OnEnable`.
2. `src/Plugin.cs` installs the `MCP` menu, loads `%APPDATA%\CeMCP\config.json`, applies `MCP_HOST`/`MCP_PORT` overrides, and starts the WPF configuration UI or server.
3. `src/McpServer.cs` builds the ASP.NET Core host, registers tool classes, and maps the stateless Streamable HTTP endpoint.
4. A method in `src/Tools/` validates an MCP request, marshals Cheat Engine work to the main GUI thread, and calls a typed `CESDK` facade.
5. `CESDK/src/Classes/` calls the CE Lua API through `LuaUtils`/`LuaNative`; tools return JSON-visible `{ success = true, ... }` or `{ success = false, error }` objects.

Dependency direction is `MCP tool -> CESDK typed facade -> LuaUtils/LuaNative -> Cheat Engine`. ASP.NET Core may handle concurrent requests, but CE Lua state and engine objects are not thread-safe: serialize CE-facing work with `ToolThread.OnMainThread(...)`. Keep process-attached checks and the subsequent operation in the same main-thread block.

State is deliberately centralized: static `ServerConfig`, shared `PluginContext.Lua`, server fields on `McpPlugin`/`McpServer`, and named scanner state in `ScanTool`. Scanner/found-list lifecycles are order-sensitive: deinitialize old results, run the scan, call `WaitTillDone()`, initialize results, then read them.

## Key Directories

- `src/`: plugin lifecycle, MCP host, schema transforms, configuration, and WPF application code.
- `src/Tools/`: client-facing MCP tool adapters grouped by CE capability.
- `src/Models/`, `src/Views/`: WPF configuration state and UI.
- `CESDK/src/`: submodule-provided native plugin bootstrap, Lua interop, and typed CE wrappers compiled into the plugin.
- `tests/CeMCP.Tests/Unit/`: deterministic tests that do not require Cheat Engine.
- `tests/CeMCP.Tests/Live/`: opt-in tests against a running CE-hosted MCP server.
- `CESDK/tests/`: separate CE-loaded live-test plugin and report-validating MSTest host.
- `skills/ce-mcp/`: distributable AI skill that must track the public MCP surface.
- `.github/workflows/`: Windows build/artifact and SonarCloud pipelines.

## Development Commands

Run from the repository root in PowerShell:

```powershell
git submodule update --init --recursive
dotnet restore
dotnet build
dotnet test --filter "TestCategory!=Live"
dotnet build -c Release
```

CI-equivalent build sequence:

```powershell
dotnet restore
dotnet build -c Debug --no-restore
dotnet test -c Debug --no-restore --no-build --filter "TestCategory!=Live"
dotnet build -c Release --no-restore
```

Debug output is `bin/x64/Debug/net10.0-windows/ce-mcp.dll`; Release uses the corresponding `Release` directory. There is no repository lint or formatter command. SonarCloud is the configured static-analysis gate.

Manual run: copy the built DLL to Cheat Engine's plugins directory, restart CE, enable the plugin, choose `MCP` -> start server, and connect to `http://localhost:6300/` by default. This is a plugin, not a standalone application.

## Code Conventions & Common Patterns

- Use C# with 4-space indentation, nullable reference types, warnings as errors, and the existing brace style. Add XML summaries to public wrapper APIs; keep comments focused on CE/Lua edge cases.
- MCP tool containers are `public class` types with `[McpServerToolType]` and a private constructor. Tool methods are synchronous static `object` methods decorated with `[McpServerTool(Name = "snake_case")]`; describe every public parameter.
- Register every tool class explicitly in `McpServer.Start` with `WithToolsAndSchemaTransform<T>()`. Never use plain `WithTools<T>()`: `SchemaTransform` removes nullable type arrays and oversized numeric schema keywords that break some MCP clients.
- Validate required inputs before native calls. Return anonymous structured objects with `success` and result fields, or `success = false` plus `error`. Catch operational exceptions at the tool boundary; CESDK wrappers translate lower-level failures to domain-specific exceptions.
- Use `ToolThread.OnMainThread(...)` for new CE-facing tool bodies. Follow a subsystem's established pattern when modifying older scan, address-list, Lua, or debugger code; do not casually move CE work onto HTTP worker threads.
- Prefer typed CESDK facades such as `MemoryAccess`, `MemScan`, `Assembler`, and `SymbolManager`. Use `execute_lua` only when no dedicated tool fits, and consult the installed Cheat Engine `celua.txt` before changing Lua bindings or guidance.
- Format addresses as uppercase hexadecimal (`0x{value:X}`). Use strict `AddressParser` where only hex is accepted and `AddressResolver` where symbols are supported.
- Preserve local response-field conventions. Most tools use camelCase; debugger responses intentionally include snake_case fields.
- Server start/stop is asynchronous; tool operations generally are not. The configuration window owns a separate STA thread and WPF `Dispatcher`.
- Configuration precedence is defaults (`127.0.0.1:6300`) < `%APPDATA%\CeMCP\config.json` < `MCP_HOST`/`MCP_PORT`.
- Route CESDK and ASP.NET Core logging through the isolated NLog factory in `CESDK/src/PluginLogger.cs`. The only log path is `%APPDATA%\CeMCP\ce-mcp.log`; do not add direct file loggers or alternate fallback paths.
- Tool-surface, parameter, scan/debugger, Lua, threading, or installed-path changes must also update relevant files under `skills/ce-mcp/`, especially `SKILL.md`, `references/tool-catalog.md`, and `references/lua-execution.md`.
- Memory/physical-memory writes, process control, file operations, injection, assembly/compilation, debugger control, DBVM, and arbitrary Lua can alter a target or host. Keep server defaults loopback-only unless the change explicitly requires otherwise.

## Important Files

- `CeMCP.sln`: root x64 Debug/Release solution.
- `CeMCP.csproj`: target framework, WPF/ASP.NET references, MCP and Costura packages, CESDK source inclusion, and skill-output copying.
- `global.json`: pins .NET SDK `10.0.102`, Microsoft.Testing.Platform, and MSTest.Sdk `4.2.3`.
- `src/Plugin.cs`: CE lifecycle, menu integration, server ownership, config precedence, and WPF thread startup.
- `src/McpServer.cs`: HTTP/MCP composition root and explicit tool registration.
- `src/SchemaTransform.cs`: required MCP schema compatibility transform.
- `src/ServerConfig.cs`: defaults, persistent config, and environment overrides.
- `src/Tools/ToolThread.cs`: CE GUI-thread boundary and normalized error handling.
- `src/Tools/ScanTool.cs`: stateful scanner/found-list sequencing.
- `CESDK/src/CESDK.cs`: native CE bootstrap and synchronization bridge.
- `CESDK/src/Utils/LuaUtils.cs`: managed-to-Lua calls, stack cleanup, and result extraction.
- `README.md`: installation, runtime prerequisites, and live/manual test setup.
- `.github/workflows/build-dlls.yml`: canonical CI build/test/artifact steps.

## Runtime/Tooling Preferences

- Required platform: Windows x64. The root project targets `net10.0-windows` and uses C# `latest`.
- Required runtimes: .NET 10 SDK for development; .NET 10 Desktop and ASP.NET Core runtimes for Cheat Engine hosting. CE's `ce.runtimeconfig.json` may also need its framework entries updated from .NET 9 to .NET 10.
- Package management uses NuGet `PackageReference` plus the SDK pinned in `global.json`; there is no central package file or lock file.
- Initialize `CESDK/` recursively before restore/build. Treat it as a submodule integration layer, not ordinary generated source.
- Costura.Fody embeds managed dependencies in `ce-mcp.dll`. `CeMCP.csproj` copies `skills/ce-mcp/` beside the DLL; do not edit generated copies under `bin/`.
- Do not commit `skills/ce-mcp/references/local-cheat-engine.md`; it is machine-local. Use the installed `celua.txt` as the source of truth for CE Lua APIs.
- If available, validate skill edits with:

```powershell
python C:\Users\Shadow\.codex\skills\.system\skill-creator\scripts\quick_validate.py skills/ce-mcp
```

## Testing & QA

Tests use MSTest.Sdk on Microsoft.Testing.Platform. Use `[TestClass]`/`[TestMethod]`, behavior-oriented `Subject_Condition_Outcome` names, `[DataRow]` for tables, and deterministic assertions with diagnostic messages. Tests that mutate environment variables or static delegates must be `[DoNotParallelize]` and restore state during cleanup.

- Normal CE-free QA: `dotnet test --filter "TestCategory!=Live"`.
- Bare `dotnet test` is safe without CE because live tests become inconclusive, but CI uses the explicit non-live filter.
- Add unit coverage for deterministic contracts such as schema transforms, config precedence, metadata, validation, result shapes, and skill packaging. Use narrow injectable boundaries rather than mocking CE wholesale.
- `tests/CeMCP.Tests/Support/ToolResultAssert.cs` checks anonymous tool response shapes.
- Live MCP tests require the freshly built plugin loaded and server running, then:

```powershell
$env:CE_MCP_LIVE = "1"
$env:CE_MCP_URL = "http://localhost:6300/" # optional
dotnet test --filter TestCategory=Live
```

Live tests are `[TestCategory("Live")]` and nonparallel. Default coverage is read-only; scan regressions require an already attached readable target or become inconclusive. `NotepadMcpTests` additionally requires `CE_MCP_NOTEPAD_LIVE=1`, launches/discovers a disposable Notepad process, asserts every live tool is attempted or explicitly excluded, and restores owned state. The CESDK harness skips target-dependent checks until a disposable process is attached; run them from `CESDK Tests` -> `Run Tests Against Attached Process`. Set `CESDK_LIVE_MUTATING=1` before launching CE for wrapper mutations; `CESDK_LIVE_TARGET_PID` is optional unattended attachment. Target-specific writes, debugger actions, file operations, or execution changes must remain explicitly opt-in and document setup.
