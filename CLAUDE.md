# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Model Context Protocol (MCP) server plugin for Cheat Engine that exposes Cheat Engine functionality via HTTP REST API. The project consists of two main components:

1. **C# Plugin** (`ce-mcp.dll`) - A Cheat Engine plugin that hosts an HTTP server
2. **Python MCP Client** (`mcp-client/`) - MCP server that communicates with the HTTP API

## Build Commands

### C# Plugin

```bash
# Build the plugin DLL
dotnet build

# Build in Release mode
dotnet build -c Release
```

### Python MCP Client

```bash
# Navigate to mcp-client directory
cd mcp-client

# Install dependencies with uv
uv sync

# Run the MCP server
uv run python cheat_engine_mcp_server.py
```

## Architecture

### C# Plugin Architecture

The plugin follows a layered architecture:

- **Plugin.cs** (`McpPlugin`) - Entry point that integrates with Cheat Engine SDK, manages server lifecycle
- **MCPServerWrapper.cs** - Wraps OWIN HTTP server hosting
- **WebApiStartup.cs** - Configures Web API routing, dependency injection, and Swagger documentation
- **Controllers/** - REST API endpoints
- **Tools/** - Business logic for Cheat Engine operations (Lua execution, process list, etc.)
- **Models/** - Data transfer objects for API requests/responses
- **SDK/** - Cheat Engine SDK wrapper classes

### HTTP Server Details

- Uses Microsoft.Owin.Hosting for self-hosting
- Default endpoint: `http://localhost:6300`
- Swagger UI available at `/swagger`
- Environment variables: `MCP_HOST` and `MCP_PORT` for configuration

### Tool Architecture Pattern

When adding new Cheat Engine functionality:

1. Create a tool class in `Tools/` (e.g., `ProcessListTool.cs`) - Study existing tools like `ProcessListTool.cs` to understand patterns for Lua execution, error handling, and stack management
2. Add corresponding model classes in `Models/LuaModels.cs`
3. Add method to `CheatEngineTools.cs`
4. Add REST endpoint in `Controllers/CheatEngineController.cs`
5. Add corresponding MCP tool function in `mcp-client/cheat_engine_mcp_server.py`

**Important**: When implementing Lua functionality, examine existing tool implementations to understand:
- Proper Lua error checking (check `DoString()` return value)
- Stack management (`lua.SetTop(0)`, `lua.Pop()`)
- How to handle different Lua return types (boolean, number, string, table)
- When to use `return` in Lua code vs. when not to

### Python MCP Client

- Built with FastMCP framework
- Communicates with C# plugin via HTTP requests
- Tools are auto-discovered and exposed to MCP clients
- Uses httpx for async HTTP communication

## Configuration

### Server Configuration

The HTTP server configuration is managed by `ServerConfig.cs`:

- Default: `localhost:6300`
- Configurable via `MCP_HOST` and `MCP_PORT` environment variables

### Plugin Integration

- Menu integration: Adds "MCP" menu to Cheat Engine with Start/Stop server options
- Lua integration: Registers custom Lua functions for server control
- SDK dependency: Requires Cheat Engine SDK for plugin interface

## Development Workflow

1. **C# Plugin Development**: Make changes to C# code, build with `dotnet build`
2. **Plugin Testing**: Copy `ce-mcp.dll` from `bin/` to Cheat Engine plugins directory
3. **Python Client Development**: Modify `cheat_engine_mcp_server.py` and test with MCP clients
4. **API Testing**: Use Swagger UI at `http://localhost:6300/swagger` for endpoint testing
5. **Git Workflow**: Always use `git add .` to stage all changes instead of individual file names

## Dependencies

### C# (.NET 4.8.1)

- Microsoft.Owin.Hosting - HTTP server hosting
- Microsoft.AspNet.WebApi.Owin - Web API framework
- Swashbuckle.Core - API documentation
- ModelContextProtocol - MCP .NET library
- Costura.Fody - Assembly embedding

### Python (>=3.13)

- mcp[cli] - Model Context Protocol framework
- httpx - Async HTTP client

## Key Files

- `Plugin.cs` - Main plugin entry point and Cheat Engine integration
- `CheatEngineTools.cs` - Central hub for all Cheat Engine tool functionality
- `mcp-client/cheat_engine_mcp_server.py` - MCP server exposing tools to MCP clients
- `CeMCP.csproj` - C# project configuration with all package dependencies
