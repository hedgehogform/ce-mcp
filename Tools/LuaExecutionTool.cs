using System;
using CESDK;
using CeMCP.Models;

namespace CeMCP.Tools
{
    public class LuaExecutionTool
    {
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

                var lua = PluginContext.Lua;
                var initialStackSize = lua.GetTop();

                // Execute the Lua code
                lua.DoString(request.Code);

                // Check if there are any return values on the stack
                var finalStackSize = lua.GetTop();
                var returnCount = finalStackSize - initialStackSize;

                string result;
                if (returnCount > 0)
                {
                    // Get the top value as the result
                    if (lua.IsString(-1))
                    {
                        result = lua.ToString(-1);
                    }
                    else if (lua.IsNumber(-1))
                    {
                        result = lua.ToNumber(-1).ToString();
                    }
                    else if (lua.IsBoolean(-1))
                    {
                        result = lua.ToBoolean(-1).ToString();
                    }
                    else if (lua.IsNil(-1))
                    {
                        result = "nil";
                    }
                    else
                    {
                        result = $"[{lua.Type(-1)}]";
                    }

                    // Clean up the stack
                    lua.Pop(returnCount);
                }
                else
                {
                    result = "Lua code executed successfully (no return value)";
                }

                return new LuaResponse
                {
                    Result = result,
                    Success = true
                };
            }
            catch (InvalidOperationException e)
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