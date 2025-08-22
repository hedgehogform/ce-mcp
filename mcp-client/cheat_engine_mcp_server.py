#!/usr/bin/env python3
"""
Cheat Engine MCP Server
Provides tools to interact with Cheat Engine via REST API
"""

from mcp.server.fastmcp import FastMCP
import httpx
import asyncio
from typing import Dict, Any

mcp = FastMCP(name="Cheat Engine Server")

CHEAT_ENGINE_BASE_URL = "http://localhost:6300"

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
async def get_api_info() -> Dict[str, Any]:
    """
    Get information about the Cheat Engine REST API
    
    Returns:
        API information and available endpoints
    """
    return {
        "base_url": CHEAT_ENGINE_BASE_URL,
        "swagger_ui": f"{CHEAT_ENGINE_BASE_URL}/swagger",
        "endpoints": "execute_lua: POST /api/cheatengine/execute-lua",
        "description": "REST API for Cheat Engine MCP Server"
    }

if __name__ == "__main__":
    import uvicorn
    import asyncio
    
    # Run as MCP server using stdin/stdout
    asyncio.run(mcp.run())