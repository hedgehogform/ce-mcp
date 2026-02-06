using System;
using System.ComponentModel;
using CESDK.Classes;
using ModelContextProtocol.Server;

namespace Tools
{
    [McpServerToolType]
    public class LuaExecutionTool
    {
        private LuaExecutionTool() { }

        [McpServerTool(Name = "execute_lua"), Description(
            "Execute a Lua script in Cheat Engine's Lua environment and return the result. " +
            "Supports all CE Lua API functions. Returns are automatically serialized including tables. " +
            "Use 'return <value>' to get values back. Multiple return values are supported. " +
            "IMPORTANT: Prefer using dedicated non-script tools (e.g. memory_scan, aob_scan, open_process, " +
            "read_memory, write_memory, add_memory_record, etc.) when they can accomplish the task. " +
            "Only use this Lua execution tool when no other available tool can perform the required action.")]
        public static object ExecuteLua(
            [Description("The Lua code to execute. Use 'return' to get values back.")] string script)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(script))
                    return new { success = false, error = "Script parameter is required" };

                var result = LuaExecutor.Execute(script);

                if (!result.HasValue)
                    return new { success = true, result = (object?)null, message = "Executed successfully (no return value)" };

                if (result.ReturnCount == 1)
                    return new { success = true, result = result.Value };

                return new { success = true, results = result.Values };
            }
            catch (Exception ex)
            {
                return new { success = false, error = ex.Message };
            }
        }
    }
}
