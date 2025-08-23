# Cheat Engine MCP Server

> [!WARNING]
> This project is not feature complete and is under active development.

A Model Context Protocol (MCP) server plugin for Cheat Engine that provides access to Cheat Engine functionality.

## Current Features

- Execute Lua code in Cheat Engine
- Get list of running processes

## Requirements

- Cheat Engine 7.0+
- .NET Framework 4.8.1
- Windows OS

## Installation

1. Build the project: `dotnet build`
2. Copy `ce-mcp.dll` from `bin/` to your Cheat Engine plugins directory
3. Enable the plugin in Cheat Engine

## Development

### Building

```bash
# Build the C# plugin
dotnet build

# Build in Release mode
dotnet build -c Release
```

### Python MCP Client

```bash
# Navigate to mcp-client directory
cd mcp-client

# Install dependencies
uv sync

# Open cheat_engine_mcp_server.py in your AI software.
```

### Testing

1. Build the plugin and copy to Cheat Engine plugins directory
2. Start Cheat Engine and enable the plugin
3. Use MCP menu to start/stop the server
4. Test API endpoints at `http://localhost:6300/swagger`
