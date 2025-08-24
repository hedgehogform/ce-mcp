# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Cheat Engine MCP (Model Context Protocol) Server that provides REST API access to Cheat Engine functionality through a C# plugin and Python MCP client. The project enables AI tools to interact with Cheat Engine for memory analysis, process manipulation, and debugging tasks.

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

- **McpServer.cs**: OWIN-based web server that hosts the REST API

  - Uses Web API with Swagger documentation
  - Configurable host/port via environment variables or config file

- **CheatEngineController.cs**: Main API controller with endpoints for:

  - Lua execution
  - Process management
  - Memory reading/writing
  - AOB scanning
  - Disassembly
  - Memory scanning

- **CheatEngineTools.cs**: Service layer that delegates to specialized tools in `/Tools` directory

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

### Python MCP Client

- **cheat_engine_mcp_server.py**: FastMCP-based server that exposes tools for AI interaction
- Provides async wrappers for all REST API endpoints
- Uses httpx for HTTP communication with configurable timeout (600s)

## Development Workflow

1. Build the C# plugin with `dotnet build`
2. Copy `ce-mcp.dll` from `bin/` to Cheat Engine plugins directory
3. Enable the plugin in Cheat Engine
4. Use "MCP" menu to start/configure the server
5. Test endpoints at `http://localhost:6300/swagger`

## Adding New Tools

To add a new tool/endpoint to the MCP server:

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

### 5. Add Controller Endpoint
Add endpoint to `CheatEngineController.cs`:
```csharp
[HttpPost]
[Route("new-feature")]
public NewFeatureResponse DoSomething([FromBody] NewFeatureRequest request)
{
    return tools.DoSomething(request);
}
```

### 6. Add Python MCP Tool
Add corresponding tool in `ce-mcp-client/src/cheat_engine_mcp_server.py`:
```python
@mcp.tool()
async def do_something(parameter: str) -> Dict[str, Any]:
    """
    Description of what this tool does
    
    Args:
        parameter: Description of the parameter
        
    Returns:
        Dictionary with result and success status
    """
    return await make_request("new-feature", "POST", {"Parameter": parameter})
```

## Project Structure

```
├── SDK/                    # Cheat Engine SDK wrappers
├── Tools/                  # Specialized functionality tools
├── Controllers/            # Web API controllers
├── Models/                 # Data transfer objects
├── ce-mcp-client/         # Python MCP client
│   └── src/cheat_engine_mcp_server.py
└── Plugin.cs              # Main plugin entry point
```

## Configuration

The server supports configuration through:

1. Environment variables (`MCP_HOST`, `MCP_PORT`)
2. Configuration file at `%APPDATA%\CeMCP\config.json`
3. Runtime configuration through WPF configuration window

Default server runs on `http://127.0.0.1:6300` with Swagger UI available for API testing.
