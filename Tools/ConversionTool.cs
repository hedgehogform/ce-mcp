using System;
using CESDK;
using CeMCP.Models;

namespace CeMCP.Tools
{
    public class ConversionTool
    {
        private readonly McpPlugin _plugin;

        public ConversionTool(McpPlugin plugin)
        {
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
        }

        public ConversionResponse Convert(ConversionRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Input))
                {
                    return new ConversionResponse
                    {
                        Success = false,
                        Error = "Input parameter is required"
                    };
                }

                if (string.IsNullOrWhiteSpace(request.ConversionType))
                {
                    return new ConversionResponse
                    {
                        Success = false,
                        Error = "ConversionType parameter is required"
                    };
                }

                var lua = _plugin.sdk.lua;
                string luaFunction = GetLuaFunction(request.ConversionType);
                
                if (string.IsNullOrEmpty(luaFunction))
                {
                    return new ConversionResponse
                    {
                        Success = false,
                        Error = $"Unsupported conversion type: {request.ConversionType}"
                    };
                }

                // Build Lua code to perform the conversion
                string inputValue = request.Input.Replace("\"", "\\\"");
                string luaCode = $@"
                    local input = ""{inputValue}""
                    local success, result = pcall(function()
                        return {luaFunction}(input)
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
                    return new ConversionResponse
                    {
                        Success = false,
                        Error = $"Lua execution failed: {error}"
                    };
                }

                // Get the result
                string output = null;
                if (lua.IsString(-1))
                {
                    output = lua.ToString(-1);
                }
                else if (lua.IsNil(-1))
                {
                    lua.Pop(1);
                    return new ConversionResponse
                    {
                        Success = false,
                        Error = "Conversion failed"
                    };
                }

                lua.Pop(1);

                return new ConversionResponse
                {
                    Success = true,
                    Output = output
                };
            }
            catch (Exception ex)
            {
                return new ConversionResponse
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        private string GetLuaFunction(string conversionType)
        {
            return conversionType.ToLower() switch
            {
                "ansitoutf8" => "ansiToUtf8",
                "utf8toansi" => "utf8ToAnsi",
                "stringtomd5string" or "stringtomd5" or "md5" => "stringToMD5String",
                _ => null
            };
        }
    }
}