using System;
using CESDK;
using CeMCP.Models;

namespace CeMCP.Tools
{
    public class GetInstructionSizeTool
    {
        private readonly McpPlugin _plugin;

        public GetInstructionSizeTool(McpPlugin plugin)
        {
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
        }

        public GetInstructionSizeResponse GetInstructionSize(GetInstructionSizeRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Address))
                {
                    return new GetInstructionSizeResponse
                    {
                        Success = false,
                        Error = "Address parameter is required"
                    };
                }

                var lua = _plugin.sdk.lua;

                // Escape address string safely for Lua
                string safeAddress = request.Address.Replace("\\", "\\\\").Replace("\"", "\\\"");

                // Build Lua code to resolve both CEAddressString and integer addresses
                string luaCode = $@"
                    local success, result = pcall(function()
                        local address = getAddress(""{safeAddress}"")
                        return getInstructionSize(address)
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
                    return new GetInstructionSizeResponse
                    {
                        Success = false,
                        Error = $"Lua execution failed: {error}"
                    };
                }

                int instructionSize = 0;
                if (lua.IsNumber(-1))
                {
                    instructionSize = (int)lua.ToNumber(-1);
                }
                else
                {
                    lua.Pop(1);
                    return new GetInstructionSizeResponse
                    {
                        Success = false,
                        Error = "Failed to get instruction size at specified address"
                    };
                }

                lua.Pop(1);

                return new GetInstructionSizeResponse
                {
                    Success = true,
                    Size = instructionSize
                };
            }
            catch (Exception ex)
            {
                return new GetInstructionSizeResponse
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }
    }
}
