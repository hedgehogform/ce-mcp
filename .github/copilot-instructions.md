# Project Guidelines

## Project Overview

Cheat Engine MCP Server — a C# plugin that exposes Cheat Engine functionality as MCP tools over SSE (Server-Sent Events) using the official [Model Context Protocol C# SDK](https://github.com/modelcontextprotocol/csharp-sdk).

## Build and Test

```bash
# Initialize submodule (first time)
git submodule update --init --recursive

# Build
dotnet build

# Build Release
dotnet build -c Release
```

Deploy: copy `ce-mcp.dll` from `bin/` to Cheat Engine plugins directory, enable in CE, use "MCP" menu.

## Architecture

### Core Components

- **Plugin.cs** — Main CE plugin entry point (`CESDKPluginClass`). Manages MCP server lifecycle, registers Lua functions for CE menu integration, provides WPF config UI.
- **McpServer.cs** — MCP SSE server using `ModelContextProtocol.AspNetCore`. Registers all tools via `WithTools<T>()` and maps endpoints with `MapMcp()`.
- **ServerConfig.cs** — Configuration management (host/port/name). Loads from `%APPDATA%\CeMCP\config.json` with env var overrides (`MCP_HOST`, `MCP_PORT`).

### Tools (`src/Tools/`)

All tools use `[McpServerToolType]` on the class and `[McpServerTool]` + `[Description]` on methods. Tools are static classes with static methods. Each returns anonymous objects with `success` boolean and either result data or `error` message.

- **ProcessTool** — List processes, open by ID/name, get current process
- **LuaExecutionTool** — Execute Lua scripts in CE with stack management
- **MemoryReadTool** — Read memory (bytes, int32, int64, float, string)
- **MemoryWriteTool** — Write memory values
- **AOBScanTool** — Array of Bytes pattern scanning
- **DisassembleTool** — Disassemble instructions at address
- **AddressTool** — Resolve address expressions
- **ConversionTool** — String format conversion (MD5, ANSI/UTF8)
- **ThreadListTool** — List process threads
- **MemScanTool** — Memory value scanning with first/next scan pattern
- **AddressListTool** — Cheat table CRUD operations

### SDK Layer (`CESDK/`)

Git submodule — C# wrapper around Cheat Engine's Lua API. Key classes: `LuaEngine`, `MemoryAccess`, `Process`, `AOBScanner`, `Disassembler`, `MemScan`, `AddressList`.

### Views (`src/Views/`)

- **ConfigWindow.cs** — WPF config window (code-only, no XAML). Supports dark/light theme via `ThemeHelper`.

## Adding New Tools

1. Create a new file in `src/Tools/` with `[McpServerToolType]` class attribute
2. Add static methods with `[McpServerTool(Name = "tool_name")]` and `[Description("...")]`
3. Use `[Description]` on parameters for schema generation
4. Return anonymous objects: `new { success = true, ... }` or `new { success = false, error = "..." }`
5. Register in `McpServer.cs` via `.WithTools<Tools.YourTool>()`

Example:
```csharp
[McpServerToolType]
public static class MyTool
{
    [McpServerTool(Name = "my_action"), Description("Does something useful")]
    public static object MyAction([Description("Input param")] string input)
    {
        try {
            // ... CE SDK calls ...
            return new { success = true, result = "done" };
        } catch (Exception ex) {
            return new { success = false, error = ex.Message };
        }
    }
}
```

## Code Style

- C# with nullable reference types enabled, `TreatWarningsAsErrors`
- Target: `net9.0-windows`, WPF enabled, x64 platform
- Tools use static classes/methods, not instance-based
- Wrap CE SDK calls in try-catch, always return structured response objects
- Use proper Lua stack management (`GetTop`, `Pop`) when interacting with Lua

## Important Notes

- **Lua stack**: Always clean up with `lua.Pop()` calls after reading values
- **CE thread safety**: Use `CESDK.CESDK.Synchronize()` for operations that must run on CE's main thread (e.g., AddressList operations)
- **Memory scanning**: Requires scan → `WaitTillDone()` → `foundList.Initialize()` sequence
- **Dark mode**: UI adapts via Windows registry check (`ThemeHelper.IsInDarkMode()`)
- Default server: `http://127.0.0.1:6300` with MCP SSE at `/sse`

## CESDK Submodule

The `CESDK/` directory is a git submodule providing the Cheat Engine SDK wrapper library:

- **Core**: `CESDK.cs`, `CheatEnginePlugin.cs`, `PluginContext.cs`
- **Lua**: `LuaEngine.cs` (high-level), `LuaNative.cs` (low-level C API)
- **Memory**: `MemoryScanner.cs`, `ScanConfiguration.cs`, `ScanResults.cs`
- **System**: `SystemInfo.cs`, `CEInterop.cs`

Plugin pattern: inherit `CheatEnginePlugin`, implement `Name`/`OnEnable()`/`OnDisable()`, access Lua via `PluginContext.Lua`.
