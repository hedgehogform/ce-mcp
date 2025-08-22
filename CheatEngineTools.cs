using System;
using CESDK;

namespace CeMCP
{
    public class LuaRequest
    {
        public string Code { get; set; }
    }

    public class LuaResponse
    {
        public string Result { get; set; }
        public bool Success { get; set; }
    }

    public class CheatEngineTools
    {
        private readonly McpPlugin _plugin;

        public CheatEngineTools(McpPlugin plugin)
        {
            _plugin = plugin;
        }

        public LuaResponse ExecuteLua(LuaRequest request)
        {
            try
            {
                if (request?.Code == null)
                {
                    return new LuaResponse
                    {
                        Result = "Code parameter is required",
                        Success = false
                    };
                }

                var result = _plugin.sdk.lua.DoString(request.Code);

                return new LuaResponse
                {
                    Result = result.ToString(),
                    Success = true
                };
            }
            catch (Exception e)
            {
                return new LuaResponse
                {
                    Result = e.Message,
                    Success = false
                };
            }
        }
    }
}