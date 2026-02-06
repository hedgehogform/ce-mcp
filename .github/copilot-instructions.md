# Project Guidelines

## Project Overview

Cheat Engine MCP Server — a C# plugin that exposes Cheat Engine functionality as MCP tools over SSE (Server-Sent Events) using the official [Model Context Protocol C# SDK](https://github.com/modelcontextprotocol/csharp-sdk).

## Cheat Engine Lua API Reference

The full CE Lua API documentation is at `C:\Program Files\Cheat Engine\celua.txt`. Always consult this file when:
- Adding new CESDK wrapper methods or tools
- Verifying correct Lua function names, parameters, and return types
- Understanding CE object models (MemScan, FoundList, AddressList, MemoryRecord, etc.)

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

- **ProcessTool** — Process and thread management (list/open/get processes, list threads)
- **MemoryTool** — Memory read and write (bytes, int8/16/32/64, float, double, string)
- **ScanTool** — Memory scanning (AOB pattern scan, value scan with first/next, reset)
- **AssemblyTool** — Disassembly and address resolution
- **AddressListTool** — Cheat table CRUD operations (get/add/update/delete/clear records)
- **LuaExecutionTool** — Execute arbitrary Lua scripts in CE with stack management
- **ConversionTool** — String format conversion (MD5, ANSI/UTF8)

### SDK Layer (`CESDK/`)

Git submodule — C# wrapper around Cheat Engine's Lua API. Key classes: `MemoryAccess`, `Process`, `AOBScanner`, `Disassembler`, `AddressResolver`, `MemScan`, `FoundList`, `AddressList`, `ThreadList`, `Converter`, `Speedhack`, `Debugger`, `SymbolWaiter`.

### Views (`src/Views/`)

- **ConfigWindow.xaml/.cs** — WPF config window. Supports dark/light theme via `ThemeHelper`.

## Adding New Tools

1. Create a new file in `src/Tools/` with `[McpServerToolType]` class attribute
2. Add static methods with `[McpServerTool(Name = "tool_name")]` and `[Description("...")]`
3. Use `[Description]` on parameters for schema generation
4. Return anonymous objects: `new { success = true, ... }` or `new { success = false, error = "..." }`
5. Register in `McpServer.cs` via `.WithTools<Tools.YourTool>()`
6. Consult `C:\Program Files\Cheat Engine\celua.txt` for correct Lua function signatures

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
- Target: `net10.0-windows`, WPF enabled, x64 platform
- Tools use static classes/methods, not instance-based
- Wrap CE SDK calls in try-catch, always return structured response objects
- Use proper Lua stack management (`GetTop`, `Pop`) when interacting with Lua

## Important Notes

- **Lua stack**: Always clean up with `lua.Pop()` calls after reading values
- **CE thread safety**: Use `CESDK.Synchronize()` for operations that must run on CE's main thread (e.g., AddressList operations)
- **Memory scanning**: Requires scan → `WaitTillDone()` → `foundList.Initialize()` sequence
- **Dark mode**: UI adapts via Windows registry check (`ThemeHelper.IsInDarkMode()`)
- Default server: `http://127.0.0.1:6300` with MCP SSE at `/sse`
- **CE Lua API docs**: `C:\Program Files\Cheat Engine\celua.txt` — the authoritative reference for all CE Lua functions, objects, and their parameters

## CESDK Submodule

The `CESDK/` directory is a git submodule providing the Cheat Engine SDK wrapper library:

- **Core**: `CESDK.cs`, `CheatEnginePlugin.cs`, `PluginContext.cs`
- **Lua**: `LuaNative.cs` (low-level C API bindings)
- **Classes**: `MemoryAccess`, `Process`, `AOBScanner`, `Disassembler`, `AddressResolver`, `MemScan`, `FoundList`, `AddressList`, `ThreadList`, `Converter`, `Speedhack`, `Debugger`, `SymbolWaiter`, `LuaLogger`
- **Utils**: `LuaUtils.cs` (helper functions for Lua stack operations)

Plugin pattern: inherit `CheatEnginePlugin`, implement `Name`/`OnEnable()`/`OnDisable()`, access Lua via `PluginContext.Lua`.
