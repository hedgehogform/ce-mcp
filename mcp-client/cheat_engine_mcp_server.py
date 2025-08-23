#!/usr/bin/env python3
"""
Cheat Engine MCP Server
Provides tools to interact with Cheat Engine via REST API
"""

from mcp.server.fastmcp import FastMCP
import httpx
import os
from typing import Dict, Any

mcp = FastMCP(name="Cheat Engine Server")

CHEAT_ENGINE_HOST = os.getenv("MCP_HOST", "localhost")
CHEAT_ENGINE_PORT = int(os.getenv("MCP_PORT", "6300"))
CHEAT_ENGINE_BASE_URL = f"http://{CHEAT_ENGINE_HOST}:{CHEAT_ENGINE_PORT}"

@mcp.tool()
async def execute_lua(code: str) -> Dict[str, Any]:
    """
    Execute Lua code in Cheat Engine
    
    Args:
        code: The Lua code to execute
        
    Returns:
        Dictionary with result and success status
    """
    async with httpx.AsyncClient() as client:
        try:
            response = await client.post(
                f"{CHEAT_ENGINE_BASE_URL}/api/cheatengine/execute-lua",
                json={"code": code},
                timeout=30.0
            )
            response.raise_for_status()
            return response.json()
        except httpx.RequestError as e:
            return {"result": f"Request failed: {e}", "success": False}
        except httpx.HTTPStatusError as e:
            return {"result": f"HTTP error: {e.response.status_code}", "success": False}

@mcp.tool()
async def get_process_list() -> Dict[str, Any]:
    """
    Get list of running processes from Cheat Engine
    
    Returns:
        Dictionary with process list and success status
    """
    async with httpx.AsyncClient() as client:
        try:
            response = await client.get(
                f"{CHEAT_ENGINE_BASE_URL}/api/cheatengine/process-list",
                timeout=30.0
            )
            response.raise_for_status()
            return response.json()
        except httpx.RequestError as e:
            return {"process_list": None, "success": False, "error": f"Request failed: {e}"}
        except httpx.HTTPStatusError as e:
            return {"process_list": None, "success": False, "error": f"HTTP error: {e.response.status_code}"}

@mcp.tool()
async def open_process(process: str) -> Dict[str, Any]:
    """
    Open a process in Cheat Engine by process ID or name
    
    Args:
        process: Process ID (as string) or process name to open
        
    Returns:
        Dictionary with success status and error message if any
    """
    async with httpx.AsyncClient() as client:
        try:
            response = await client.post(
                f"{CHEAT_ENGINE_BASE_URL}/api/cheatengine/open-process",
                json={"process": process},
                timeout=30.0
            )
            response.raise_for_status()
            return response.json()
        except httpx.RequestError as e:
            return {"success": False, "error": f"Request failed: {e}"}
        except httpx.HTTPStatusError as e:
            return {"success": False, "error": f"HTTP error: {e.response.status_code}"}

@mcp.tool()
async def get_thread_list() -> Dict[str, Any]:
    """
    Get list of threads for the currently opened process in Cheat Engine
    
    Returns:
        Dictionary with thread list and success status
    """
    async with httpx.AsyncClient() as client:
        try:
            response = await client.get(
                f"{CHEAT_ENGINE_BASE_URL}/api/cheatengine/thread-list",
                timeout=30.0
            )
            response.raise_for_status()
            return response.json()
        except httpx.RequestError as e:
            return {"thread_list": None, "success": False, "error": f"Request failed: {e}"}
        except httpx.HTTPStatusError as e:
            return {"thread_list": None, "success": False, "error": f"HTTP error: {e.response.status_code}"}

@mcp.tool()
async def get_process_status() -> Dict[str, Any]:
    """
    Get status of the currently opened process in Cheat Engine
    
    Returns:
        Dictionary with process ID, open status, process name, and success status
    """
    async with httpx.AsyncClient() as client:
        try:
            response = await client.get(
                f"{CHEAT_ENGINE_BASE_URL}/api/cheatengine/process-status",
                timeout=30.0
            )
            response.raise_for_status()
            return response.json()
        except httpx.RequestError as e:
            return {"process_id": 0, "is_open": False, "process_name": "", "success": False, "error": f"Request failed: {e}"}
        except httpx.HTTPStatusError as e:
            return {"process_id": 0, "is_open": False, "process_name": "", "success": False, "error": f"HTTP error: {e.response.status_code}"}

@mcp.tool() 
async def get_api_info() -> Dict[str, Any]:
    """
    Get information about the Cheat Engine REST API
    
    Returns:
        API information and available endpoints
    """
    return {
        "base_url": CHEAT_ENGINE_BASE_URL,
        "swagger_ui": f"{CHEAT_ENGINE_BASE_URL}/swagger",
        "description": "REST API for Cheat Engine MCP Server"
    }

if __name__ == "__main__":
    # Run as MCP server using stdin/stdout
    mcp.run()