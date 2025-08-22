using System;
using CESDK;
using CeMCP.Models;

namespace CeMCP.Tools
{
    public class LuaExecutionTool
    {
        private readonly McpPlugin _plugin;

        public LuaExecutionTool(McpPlugin plugin)
        {
            _plugin = plugin;
        }

        public LuaResponse ExecuteLua(LuaRequest request)
        {
            try
            {
                if (request?.Code == null)
                {
                    return new LuaResponse
                    {
                        Result = "Code parameter is required",
                        Success = false
                    };
                }

                var lua = _plugin.sdk.lua;
                
                // Execute the Lua code
                var executionResult = lua.DoString(request.Code);
                
                // Check if there are any return values on the stack
                int stackSize = lua.GetTop();
                string result;
                
                if (stackSize > 0)
                {
                    // Get the top value from the stack as a string
                    result = lua.ToString(-1);
                    
                    // Clear the stack
                    lua.SetTop(0);
                }
                else
                {
                    // No return value, just indicate success
                    result = executionResult.ToString();
                }

                return new LuaResponse
                {
                    Result = result,
                    Success = true
                };
            }
            catch (Exception e)
            {
                return new LuaResponse
                {
                    Result = e.Message,
                    Success = false
                };
            }
        }
    }
}