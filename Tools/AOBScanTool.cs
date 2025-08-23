using System;
using System.Collections.Generic;
using CESDK;
using CeMCP.Models;

namespace CeMCP.Tools
{
    public class AOBScanTool
    {
        private readonly McpPlugin _plugin;

        public AOBScanTool(McpPlugin plugin)
        {
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
        }

        public AOBScanResponse AOBScan(AOBScanRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.AOBString))
                {
                    return new AOBScanResponse
                    {
                        Success = false,
                        Error = "AOBString parameter is required"
                    };
                }

                var lua = _plugin.sdk.lua;
                
                // Build Lua code for AOB scanning
                string luaCode;
                
                // Build parameter list for AOBScan function
                var parameters = new List<string> { $"\"{request.AOBString}\"" };
                
                if (!string.IsNullOrWhiteSpace(request.ProtectionFlags))
                {
                    parameters.Add($"\"{request.ProtectionFlags}\"");
                }
                
                if (request.AlignmentType.HasValue)
                {
                    parameters.Add(request.AlignmentType.Value.ToString());
                    
                    if (!string.IsNullOrWhiteSpace(request.AlignmentParam))
                    {
                        parameters.Add($"\"{request.AlignmentParam}\"");
                    }
                }

                string parameterString = string.Join(", ", parameters);

                luaCode = $@"
                    local success, result = pcall(function()
                        return AOBScan({parameterString})
                    end)
                    
                    if success and result then
                        -- Convert StringList to table for easier handling
                        local addresses = {{}}
                        if result.Count then
                            for i = 0, result.Count - 1 do
                                addresses[i + 1] = result[i]
                            end
                        end
                        result.destroy()
                        return addresses
                    else
                        return {{}}
                    end
                ";

                int luaResult = lua.DoString(luaCode);
                if (luaResult != 0)
                {
                    string error = lua.ToString(-1);
                    lua.Pop(1);
                    return new AOBScanResponse
                    {
                        Success = false,
                        Error = $"Lua execution failed: {error}"
                    };
                }

                // Get the result
                var addresses = new List<string>();
                
                if (lua.IsTable(-1))
                {
                    int tableLength = lua.ObjLen(-1);

                    for (int i = 1; i <= tableLength; i++)
                    {
                        lua.PushInteger(i);
                        lua.GetTable(-2);

                        if (lua.IsString(-1))
                        {
                            string address = lua.ToString(-1);
                            addresses.Add(address);
                        }
                        lua.Pop(1);
                    }
                }
                else if (lua.IsNil(-1))
                {
                    // No results found, which is valid
                }

                lua.Pop(1);

                return new AOBScanResponse
                {
                    Success = true,
                    Addresses = addresses.ToArray()
                };
            }
            catch (Exception ex)
            {
                return new AOBScanResponse
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }
    }
}