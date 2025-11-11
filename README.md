# Cheat Engine MCP Server

> [!WARNING]
> This project is not feature complete and is under heavy active development.
>
> Things might break while features are added.

A Model Context Protocol (MCP) server plugin for Cheat Engine that provides access to Cheat Engine functionality.

[![FOSSA](https://app.fossa.com/api/projects/git%2Bgithub.com%2Fhedgehogform%2Fce-mcp.svg?type=large&issueType=license)](https://app.fossa.com/projects/git%2Bgithub.com%2Fhedgehogform%2Fce-mcp?ref=badge_large&issueType=license)

## Architecture

This project uses the official [Model Context Protocol C# SDK](https://github.com/modelcontextprotocol/csharp-sdk) to expose Cheat Engine functionality as MCP tools over Server-Sent Events (SSE).

- **MCP SSE Server**: Runs on `http://localhost:6300` with `/sse` and `/messages` endpoints
- **17 MCP Tools**: Lua execution, process management, memory operations, AOB scanning, disassembly, and more
- **Single DLL**: All dependencies embedded using Costura.Fody

## Related Projects

- **Python MCP Client** (Legacy): [hedgehogform/ce-mcp-client](https://github.com/hedgehogform/ce-mcp-client) - Deprecated, connect directly to MCP SSE server instead

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
4. Connect to the MCP SSE server at `http://localhost:6300` using any MCP-compatible client (Claude Desktop, etc.)

### Available MCP Tools

Connect your AI client to `http://localhost:6300` to access these tools:

- `ExecuteLua` - Execute Lua code in Cheat Engine
- `GetProcessList` - Get list of running processes
- `OpenProcess` - Open a process by ID or name
- `GetThreadList` - Get threads in current process
- `GetProcessStatus` - Get currently opened process status
- `ReadMemory` - Read memory from address
- `WriteMemory` - Write value to memory address
- `Convert` - Convert between number formats
- `AobScan` - Search for byte patterns in memory
- `Disassemble` - Disassemble instructions at address
- `MemScan` - Scan memory for specific values
- `MemScanReset` - Reset memory scanner state
- `GetAddressSafe` - Resolve symbolic addresses
- `GetNameFromAddress` - Get symbol name from address
- `InModule` - Check if address is in module
- `InSystemModule` - Check if address is in system module
