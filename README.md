# Cheat Engine MCP Server

> [!WARNING]
> This project is not feature complete and is under heavy active development.
>
> Things might break while features are added.

A Model Context Protocol (MCP) server plugin for Cheat Engine that provides access to Cheat Engine functionality.

[![FOSSA](https://app.fossa.com/api/projects/git%2Bgithub.com%2Fhedgehogform%2Fce-mcp.svg?type=large&issueType=license)](https://app.fossa.com/projects/git%2Bgithub.com%2Fhedgehogform%2Fce-mcp?ref=badge_large&issueType=license)

## Architecture

This project exposes Cheat Engine functionality through a REST API server built with ASP.NET Core.

- **REST API Server**: Runs on `http://localhost:6300` with OpenAPI documentation at `/scalar/v1`
- **17+ API Endpoints**: Lua execution, process management, memory operations, AOB scanning, disassembly, and more
- **Single DLL Plugin**: All dependencies embedded - just drop into Cheat Engine plugins folder

## Related Projects

- **Python MCP Client**: [hedgehogform/ce-mcp-client](https://github.com/hedgehogform/ce-mcp-client) - Wraps the REST API with MCP SSE server for AI clients like Claude Desktop

<!-- ## Current Features

- Execute Lua code in Cheat Engine
- Get list of running processes -->

## Requirements

- **Cheat Engine 7.6.2+** (minimum version with .NET Core plugin support)
- .NET 9.0 SDK
- Windows OS

> [!IMPORTANT]
> Cheat Engine 7.6.2 or newer is required. Older versions do not support .NET Core plugins.

## Installation

1. Build the project: `dotnet build`
2. Copy `ce-mcp.dll` from `bin/` to your Cheat Engine plugins directory
3. Enable the plugin in Cheat Engine

## Development

### Initial Setup

First, initialize the git submodule (CESDK):

```bash
git submodule update --init --recursive
```

If you cloned the repo without submodules, this command will download the required CESDK dependency.

### Building

```bash
# Build the C# plugin
dotnet build

# Build in Release mode
dotnet build -c Release
```

**Note**: If you encounter a `FodyCommon.dll` access denied error during restore/build, close your IDE and restart it to release the file lock.

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
3. Use "MCP" menu to start the server
4. Access the REST API at `http://localhost:6300` or view documentation at `http://localhost:6300/scalar/v1`
5. For AI integration, use [ce-mcp-client](https://github.com/hedgehogform/ce-mcp-client) to connect Claude Desktop or other MCP clients

### Available API Endpoints

For complete API documentation with interactive examples, visit `http://localhost:6300/scalar/v1` after starting the server.