# Cheat Engine MCP Server

This project is not ready yet.

<!--
A Model Context Protocol (MCP) server plugin for Cheat Engine that provides HTTP access to Cheat Engine functionality.

## Features

- HTTP-based MCP server running on `http://localhost:6300`
- Integration with Cheat Engine via plugin interface
- Basic tools for getting server information and status
- CORS support for web-based MCP clients

## Installation

1. Build the project to generate `ce-mcp.dll`
2. Copy the DLL to your Cheat Engine plugins directory
3. Enable the plugin in Cheat Engine

## Usage

1. **Enable Plugin**: Load the plugin in Cheat Engine
2. **Start Server**: Use the menu `MCP → Start MCP Server`
3. **Connect**: Point your MCP client to `http://localhost:6300`
4. **Stop Server**: Use the menu `MCP → Stop MCP Server`

## Available Tools

- `get_info` - Get information about the Cheat Engine MCP Server
- `echo` - Echo a message back to the client
- `get_status` - Get current status of Cheat Engine

## Technical Details

- Built with .NET Framework 4.8.1
- Uses Microsoft.Owin.Hosting for HTTP server
- Implements Model Context Protocol v2024-11-05
- Auto-discovers tools using MCP SDK attributes

## Requirements
- Cheat Engine 7.0+
- .NET Framework 4.8.1
- Windows OS -->
