# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Cheat Engine MCP (Model Context Protocol) Server that provides MCP tool access to Cheat Engine functionality through a C# plugin using the official MCP C# SDK. The project enables AI tools to interact with Cheat Engine for memory analysis, process manipulation, and debugging tasks via Server-Sent Events (SSE).

## Build Commands

### C# Plugin

```bash
# Build the plugin
dotnet build

# Build in Release mode
dotnet build -c Release
```

### Python MCP Client

```bash
# Navigate to the client directory
cd ce-mcp-client

# If it doesn't exist you will need to git clone https://github.com/hedgehogform/ce-mcp-client.git inside root folder.

# Install dependencies using UV package manager
uv sync
```

## Architecture

### Core Components

- **Plugin.cs**: Main Cheat Engine plugin entry point that implements `CESDKPluginClass`
  - Manages MCP server lifecycle (start/stop)
  - Registers Lua functions for CE integration
  - Provides configuration UI through WPF window

- **McpServer.cs**: MCP SSE server using the official Model Context Protocol C# SDK
  - Exposes all Cheat Engine functionality as MCP tools
  - Runs in SSE (Server-Sent Events) mode for compatibility with CE plugin environment
  - Configurable host/port via environment variables or config file

- **CheatEngineTools.cs**: Service layer that delegates to specialized tools in `/Tools` directory
  - All tools are exposed via MCP protocol
  - Includes: Lua execution, process management, memory operations, AOB scanning, disassembly, memory scanning

### SDK Layer (`/SDK`)

Wrapper classes around Cheat Engine SDK functionality:

- **CESDKLua.cs**: Lua script execution
- **MemoryRead.cs/MemoryWrite.cs**: Memory operations
- **Process.cs**: Process management
- **AOB.cs**: Array of Bytes scanning
- **Disassembler.cs**: Assembly instruction analysis
- **MemScan.cs**: Memory value scanning

### Configuration

- **ServerConfig.cs**: Manages server configuration with defaults:
  - Host: 127.0.0.1
  - Port: 6300
  - Configuration file: `%APPDATA%\CeMCP\config.json`
  - Environment variable overrides: `MCP_HOST`, `MCP_PORT`

### Python MCP Client (Legacy)

Note: The Python MCP client is now deprecated as the C# plugin directly implements the MCP protocol using the official SDK. You can connect directly to the MCP SSE server from any MCP-compatible client.

## Development Workflow

1. Build the C# plugin with `dotnet build`
2. Copy `ce-mcp.dll` from `bin/` to Cheat Engine plugins directory
3. Enable the plugin in Cheat Engine
4. Use "MCP" menu to start/configure the server
5. Connect to the MCP SSE server at `http://localhost:6300` using any MCP-compatible client

## Adding New Tools

To add a new MCP tool to the server:

**Important**: Always examine existing implementations first. Look at similar tools in `/Tools`, models in `/Models`, and SDK wrappers in `/SDK` to understand patterns and conventions.

### 1. Create SDK Wrapper (if needed)
If your tool requires new Cheat Engine functionality not already wrapped, create an SDK wrapper in `/SDK` directory (e.g., `NewSDKFeature.cs`):
```csharp
public class NewSDKFeature : CEObjectWrapper
{
    public string DoCheatEngineOperation(string parameter)
    {
        // Use sdk.lua or other CE SDK calls
        return sdk.lua.DoString($"return someOperation('{parameter}')");
    }
}
```

### 2. Create the Tool Implementation
Add a new tool class in `/Tools` directory (e.g., `NewFeatureTool.cs`):
```csharp
public class NewFeatureTool
{
    private readonly NewSDKFeature _sdkFeature = new NewSDKFeature();

    public NewFeatureResponse DoSomething(NewFeatureRequest request)
    {
        try
        {
            var result = _sdkFeature.DoCheatEngineOperation(request.Parameter);
            return new NewFeatureResponse { Success = true, Result = result };
        }
        catch (Exception ex)
        {
            return new NewFeatureResponse { Success = false, ErrorMessage = ex.Message };
        }
    }
}
```

### 3. Add Models
Create request/response models in `/Models` directory:
```csharp
public class NewFeatureRequest
{
    public string Parameter { get; set; }
}

public class NewFeatureResponse : BaseResponse
{
    public string Result { get; set; }
}
```

### 4. Register in CheatEngineTools
Add the tool to `CheatEngineTools.cs`:
```csharp
private readonly NewFeatureTool _newFeatureTool = new NewFeatureTool();

public NewFeatureResponse DoSomething(NewFeatureRequest request)
{
    return _newFeatureTool.DoSomething(request);
}
```

### 5. Add MCP Tool in McpServer.cs
Add the MCP tool definition in the `ConfigureTools` method in `McpServer.cs`:
```csharp
builder.AddTool("do_something", "Description of what this tool does",
    async (arguments, cancellationToken) =>
    {
        var request = new NewFeatureRequest
        {
            Parameter = arguments.GetValueOrDefault("parameter")?.ToString() ?? ""
        };
        var response = _tools.DoSomething(request);
        return new
        {
            success = response.Success,
            result = response.Result,
            error = response.Error
        };
    },
    new Dictionary<string, object>
    {
        ["type"] = "object",
        ["properties"] = new Dictionary<string, object>
        {
            ["parameter"] = new Dictionary<string, object>
            {
                ["type"] = "string",
                ["description"] = "Description of the parameter"
            }
        },
        ["required"] = new[] { "parameter" }
    });
```

## Project Structure

```
├── SDK/                    # Cheat Engine SDK wrappers
├── Tools/                  # Specialized functionality tools
├── Models/                 # Data transfer objects
├── McpServer.cs           # MCP SSE server implementation
├── CheatEngineTools.cs    # Service layer for tools
└── Plugin.cs              # Main plugin entry point
```

## Configuration

The server supports configuration through:

1. Environment variables (`MCP_HOST`, `MCP_PORT`)
2. Configuration file at `%APPDATA%\CeMCP\config.json`
3. Runtime configuration through WPF configuration window

Default server runs on `http://127.0.0.1:6300` as an MCP SSE server.

## Important Implementation Notes

### Lua Integration
- Always use proper Lua stack management with `lua.Pop()` calls
- Use `celua.txt` documentation as the definitive reference for Cheat Engine Lua API
- Create proper CE objects (e.g., `createStringlist()`) rather than Lua tables when required by CE functions
- Memory scanning requires: scan → `waitTillDone()` → `getAttachedFoundlist()` → `foundList.initialize()`

### Error Handling
- All tool methods should return response objects with `Success` boolean and optional `Error` message
- Wrap Lua calls in try-catch blocks and provide meaningful error messages
- Clean up Lua stack on exceptions to prevent memory leaks

### Dark Mode Support
- UI components support automatic dark/light theme detection via Windows registry
- Use dynamic colors in WPF code that adapt to system theme
- Status text colors should be theme-aware for visibility