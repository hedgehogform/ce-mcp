using System;
using CESDK;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Tools
{
    public static class LuaExecutionTool
    {
        /// <summary>
        /// Maps Lua execution API endpoints
        /// </summary>
        public static void MapLuaApi(this WebApplication app)
        {
            // POST /api/lua/execute - Execute Lua code
            app.MapPost("/api/lua/execute", (LuaExecuteRequest request) =>
            {
                try
                {
                    var result = ExecuteLua(request.Script ?? "");
                    return Results.Ok(new { success = true, result });
                }
                catch (Exception ex)
                {
                    return Results.Ok(new { success = false, error = ex.Message });
                }
            })
            .WithName("ExecuteLua")
            .WithDescription("Execute a Lua script in Cheat Engine")
            .WithOpenApi();
        }

        /// <summary>
        /// Executes Lua code and returns the top return value as a string (if any).
        /// </summary>
        /// <param name="code">The Lua code to execute</param>
        /// <returns>Result of the Lua execution</returns>
        private static string ExecuteLua(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Code parameter is required.", nameof(code));

            try
            {
                var lua = PluginContext.Lua;
                var initialStackSize = lua.GetTop();

                // Execute the Lua code
                lua.DoString(code);

                // Determine how many return values were left on the stack
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

                    // Clean up the stack
                    lua.Pop(returnCount);
                }
                else
                {
                    result = "Lua code executed successfully (no return value)";
                }

                return result;
            }
            catch (InvalidOperationException e)
            {
                throw new InvalidOperationException($"Lua execution failed: {e.Message}", e);
            }
        }
    }

    public record LuaExecuteRequest(string? Script);
}
