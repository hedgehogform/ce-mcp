using System;
using System.Collections.Generic;
using CESDK;
using CeMCP.Models;

namespace CeMCP.Tools
{
    public class ThreadListTool
    {
        private readonly McpPlugin _plugin;

        public ThreadListTool(McpPlugin plugin)
        {
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
        }

        public ThreadListResponse GetThreadList()
        {
            try
            {
                var lua = _plugin.sdk.lua;
                var threadList = new List<string>();

                // Create a StringList and fill it with thread information
                string luaCode = @"
                    local stringlist = createStringlist()
                    getThreadlist(stringlist)
                    
                    local result = {}
                    for i = 0, stringlist.Count - 1 do
                        table.insert(result, stringlist[i])
                    end
                    
                    stringlist.destroy()
                    return result
                ";

                int luaResult = lua.DoString(luaCode);
                if (luaResult != 0)
                {
                    string error = lua.ToString(-1);
                    lua.Pop(1);
                    return new ThreadListResponse
                    {
                        ThreadList = [],
                        Success = false,
                        Error = $"Lua execution failed: {error}"
                    };
                }

                // Extract the table with thread information
                if (lua.IsTable(-1))
                {
                    int tableLength = lua.ObjLen(-1);

                    for (int i = 1; i <= tableLength; i++)
                    {
                        lua.PushInteger(i);
                        lua.GetTable(-2);

                        if (lua.IsString(-1))
                        {
                            string threadInfo = lua.ToString(-1);
                            if (!string.IsNullOrEmpty(threadInfo))
                            {
                                threadList.Add(threadInfo);
                            }
                        }
                        lua.Pop(1);
                    }
                }

                lua.Pop(1);

                return new ThreadListResponse
                {
                    ThreadList = threadList.ToArray(),
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return new ThreadListResponse
                {
                    ThreadList = null,
                    Success = false,
                    Error = ex.Message
                };
            }
        }
    }
}