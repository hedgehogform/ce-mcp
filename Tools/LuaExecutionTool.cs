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

                var luaExecution = new LuaExecution();
                string result = luaExecution.ExecuteCode(request.Code);

                return new LuaResponse
                {
                    Result = result,
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