using System;
using System.ComponentModel;
using CESDK;
using ModelContextProtocol.Server;

namespace Tools
{
    [McpServerToolType]
    public class LuaExecutionTool
    {
        private LuaExecutionTool() { }

        [McpServerTool(Name = "execute_lua"), Description("Execute a Lua script in Cheat Engine and return the result")]
        public static object ExecuteLua(
            [Description("The Lua code to execute")] string script)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(script))
                    return new { success = false, error = "Script parameter is required" };

                var lua = PluginContext.Lua;
                var initialStackSize = lua.GetTop();

                lua.DoString(script);

                var finalStackSize = lua.GetTop();
                var returnCount = finalStackSize - initialStackSize;

                string result;

                if (returnCount > 0)
                {
                    if (lua.IsString(-1))
                        result = lua.ToString(-1);
                    else if (lua.IsNumber(-1))
                        result = lua.ToNumber(-1).ToString();
                    else if (lua.IsBoolean(-1))
                        result = lua.ToBoolean(-1).ToString();
                    else if (lua.IsNil(-1))
                        result = "nil";
                    else
                        result = $"[{lua.Type(-1)}]";

                    lua.Pop(returnCount);
                }
                else
                {
                    result = "Lua code executed successfully (no return value)";
                }

                return new { success = true, result };
            }
            catch (Exception ex)
            {
                return new { success = false, error = $"Lua execution failed: {ex.Message}" };
            }
        }
    }
}
