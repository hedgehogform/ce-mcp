using System;
using System.Collections.Generic;
using System.Text.Json;
using CESDK;
using CeMCP.Models;

namespace CeMCP.Tools
{
    public class ProcessListTool
    {
        private readonly McpPlugin _plugin;

        public ProcessListTool(McpPlugin plugin)
        {
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
        }

        public ProcessListResponse GetProcessList()
        {
            try
            {
                var lua = _plugin.sdk.lua;
                var processList = new List<ProcessInfo>();

                // Use the correct approach: iterate through key-value pairs where key=pid, value=name
                string luaCode = @"
                    local plist = getProcessList()
                    local result = {}
                    
                    for pid, name in pairs(plist) do
                        table.insert(result, {ProcessId = tonumber(pid), ProcessName = name})
                    end
                    
                    return result
                ";

                int luaResult = lua.DoString(luaCode);
                if (luaResult != 0)
                {
                    string error = lua.ToString(-1);
                    lua.Pop(1);
                    return new ProcessListResponse
                    {
                        ProcessList = [],
                        Success = false,
                        Error = $"Lua execution failed: {error}"
                    };
                }

                // Now extract the table with the C# Lua API
                if (lua.IsTable(-1))
                {
                    int tableLength = lua.ObjLen(-1);

                    for (int i = 1; i <= tableLength; i++)
                    {
                        lua.PushInteger(i);
                        lua.GetTable(-2);

                        if (lua.IsTable(-1))
                        {
                            // Get ProcessId
                            lua.GetField(-1, "ProcessId");
                            int processId = 0;
                            if (lua.IsNumber(-1))
                            {
                                processId = (int)lua.ToNumber(-1);
                            }
                            lua.Pop(1);

                            // Get ProcessName
                            lua.GetField(-1, "ProcessName");
                            string processName = "";
                            if (lua.IsString(-1))
                            {
                                processName = lua.ToString(-1);
                            }
                            lua.Pop(1);

                            if (processId > 0 && !string.IsNullOrEmpty(processName))
                            {
                                processList.Add(new ProcessInfo
                                {
                                    ProcessId = processId,
                                    ProcessName = processName
                                });
                            }
                        }
                        lua.Pop(1);
                    }
                }

                lua.Pop(1);

                return new ProcessListResponse
                {
                    ProcessList = processList.ToArray(),
                    Success = true
                };
            }
            catch (Exception e)
            {
                return new ProcessListResponse
                {
                    ProcessList = null,
                    Success = false,
                    Error = e.Message
                };
            }
        }
    }
}