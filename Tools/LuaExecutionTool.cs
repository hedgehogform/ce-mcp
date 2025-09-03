using System;
using CESDK;
using CeMCP.Models;

namespace CeMCP.Tools
{
    public class LuaExecutionTool
    {
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

                PluginContext.Lua.DoString(request.Code);

                return new LuaResponse
                {
                    Result = "Lua code executed successfully",
                    Success = true
                };
            }
            catch (InvalidOperationException e)
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