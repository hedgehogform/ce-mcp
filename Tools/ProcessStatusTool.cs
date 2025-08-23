using System;
using System.Diagnostics;
using CESDK;
using CeMCP.Models;

namespace CeMCP.Tools
{
    public class ProcessStatusTool
    {
        private readonly McpPlugin _plugin;

        public ProcessStatusTool(McpPlugin plugin)
        {
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
        }

        public ProcessStatusResponse GetProcessStatus()
        {
            try
            {
                var lua = _plugin.sdk.lua;

                // Get the opened process ID and check if it's still valid
                string luaCode = @"
                    local pid = getOpenedProcessID()
                    local isOpen = false
                    local processName = ''
                    
                    if pid ~= 0 then
                        -- Check if we can access the process by trying to read from it
                        local success, result = pcall(function()
                            -- Try to read a safe memory address (like the process handle)
                            return readInteger(process) ~= nil
                        end)
                        
                        -- If pcall succeeded and we could read something, process is accessible
                        if success and result then
                            isOpen = true
                        else
                            -- Alternative check: see if process still exists in system
                            local sysProcesses = getProcessList()
                            if sysProcesses[tostring(pid)] then
                                isOpen = true
                            end
                        end
                        
                        -- Get process name if available
                        local plist = getProcessList()
                        if plist[tostring(pid)] then
                            processName = plist[tostring(pid)]
                        end
                    end
                    
                    return {
                        ProcessId = pid,
                        IsOpen = isOpen,
                        ProcessName = processName
                    }
                ";

                int luaResult = lua.DoString(luaCode);
                if (luaResult != 0)
                {
                    string error = lua.ToString(-1);
                    lua.Pop(1);
                    return new ProcessStatusResponse
                    {
                        ProcessId = 0,
                        IsOpen = false,
                        ProcessName = "",
                        Success = false,
                        Error = $"Lua execution failed: {error}"
                    };
                }

                // Extract the result table
                int processId = 0;
                bool isOpen = false;
                string processName = "";

                if (lua.IsTable(-1))
                {
                    // Get ProcessId
                    lua.GetField(-1, "ProcessId");
                    if (lua.IsNumber(-1))
                    {
                        processId = (int)lua.ToNumber(-1);
                    }
                    lua.Pop(1);

                    // Get IsOpen
                    lua.GetField(-1, "IsOpen");
                    if (lua.IsBoolean(-1))
                    {
                        isOpen = lua.ToBoolean(-1);
                    }
                    lua.Pop(1);

                    // Get ProcessName
                    lua.GetField(-1, "ProcessName");
                    if (lua.IsString(-1))
                    {
                        processName = lua.ToString(-1) ?? "";
                    }
                    lua.Pop(1);
                }

                lua.Pop(1);

                // If we have a process ID but no name from Lua, try to get it from C#
                if (processId > 0 && string.IsNullOrEmpty(processName))
                {
                    processName = GetProcessNameFromPid(processId);
                }

                return new ProcessStatusResponse
                {
                    ProcessId = processId,
                    IsOpen = isOpen,
                    ProcessName = processName,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return new ProcessStatusResponse
                {
                    ProcessId = 0,
                    IsOpen = false,
                    ProcessName = "",
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        private string GetProcessNameFromPid(int processId)
        {
            try
            {
                using (var process = Process.GetProcessById(processId))
                {
                    return process.ProcessName + ".exe";
                }
            }
            catch (ArgumentException)
            {
                // Process not found or access denied
                return "";
            }
            catch (Exception)
            {
                // Other exceptions (access denied, etc.)
                return "";
            }
        }
    }
}