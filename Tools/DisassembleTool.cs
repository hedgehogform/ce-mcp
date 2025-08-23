using System;
using CESDK;
using CeMCP.Models;

namespace CeMCP.Tools
{
    public class DisassembleTool
    {
        private readonly McpPlugin _plugin;

        public DisassembleTool(McpPlugin plugin)
        {
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
        }

        public DisassembleResponse Disassemble(DisassembleRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Address))
                {
                    return new DisassembleResponse
                    {
                        Success = false,
                        Error = "Address parameter is required"
                    };
                }

                var lua = _plugin.sdk.lua;

                // Build Lua code to disassemble the address
                string luaCode = $@"
                    local address = {request.Address}
                    local success, result = pcall(function()
                        return disassemble(address)
                    end)
                    
                    if success then
                        return result
                    else
                        return nil
                    end
                ";

                int luaResult = lua.DoString(luaCode);
                if (luaResult != 0)
                {
                    string error = lua.ToString(-1);
                    lua.Pop(1);
                    return new DisassembleResponse
                    {
                        Success = false,
                        Error = $"Lua execution failed: {error}"
                    };
                }

                // Get the result
                string disassembly = null;
                if (lua.IsString(-1))
                {
                    disassembly = lua.ToString(-1);
                }
                else if (lua.IsNil(-1))
                {
                    lua.Pop(1);
                    return new DisassembleResponse
                    {
                        Success = false,
                        Error = "Failed to disassemble address"
                    };
                }

                lua.Pop(1);

                return new DisassembleResponse
                {
                    Success = true,
                    Disassembly = disassembly
                };
            }
            catch (Exception ex)
            {
                return new DisassembleResponse
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }
    }
}