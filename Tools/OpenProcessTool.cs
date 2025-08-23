using CeMCP.Models;
using CESDK;
using System;

namespace CeMCP.Tools
{
    public class OpenProcessTool
    {
        private readonly McpPlugin _plugin;

        public OpenProcessTool(McpPlugin plugin)
        {
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
        }

        public BaseResponse OpenProcess(OpenProcessRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Process))
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Error = "Process parameter is required"
                    };
                }

                var processValue = request.Process.Trim();
                string luaCode;

                if (int.TryParse(processValue, out int pid))
                {
                    if (pid <= 0)
                    {
                        return new BaseResponse
                        {
                            Success = false,
                            Error = "Process ID must be greater than 0"
                        };
                    }

                    // For PID, we can directly try to open it
                    luaCode = $@"
                        openProcess({pid})
                        return true
                    ";
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(processValue))
                    {
                        return new BaseResponse
                        {
                            Success = false,
                            Error = "Process name cannot be empty"
                        };
                    }

                    var escapedProcessName = processValue.Replace("'", "\\'").Replace("\"", "\\\"");

                    // For process name, validate it exists first using getProcessIDFromProcessName
                    luaCode = $@"
                        local pid = getProcessIDFromProcessName('{escapedProcessName}')
                        if pid then
                            openProcess('{escapedProcessName}')
                            return true
                        else
                            return false
                        end
                    ";
                }

                var lua = _plugin.sdk.lua;

                // Execute the Lua code
                var executionResult = lua.DoString(luaCode);

                // Check if Lua execution had an error (non-zero return means error)
                if (executionResult != 0)
                {
                    string error = lua.ToString(-1);
                    lua.Pop(1);
                    return new BaseResponse
                    {
                        Success = false,
                        Error = $"Lua execution failed: {error}"
                    };
                }

                // Get the return value
                bool success = false;
                if (lua.IsBoolean(-1))
                {
                    success = lua.ToBoolean(-1);
                }
                else if (lua.IsNumber(-1))
                {
                    success = Math.Abs(lua.ToNumber(-1)) > double.Epsilon;
                }

                // Clear the stack
                lua.SetTop(0);

                if (success)
                {
                    return new BaseResponse
                    {
                        Success = true
                    };
                }
                else
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Error = $"Process not found or failed to open: {processValue}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }
    }
}