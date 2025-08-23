using System;
using System.Collections.Generic;
using CESDK;
using CeMCP.Models;

namespace CeMCP.Tools
{
    public class MemoryWriteTool
    {
        private readonly McpPlugin _plugin;

        public MemoryWriteTool(McpPlugin plugin)
        {
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
        }

        public BaseResponse WriteMemory(MemoryWriteRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Address))
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Error = "Address parameter is required"
                    };
                }

                if (string.IsNullOrWhiteSpace(request.DataType))
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Error = "DataType parameter is required"
                    };
                }

                var lua = _plugin.sdk.lua;
                string luaFunction = GetLuaFunction(request.DataType);
                
                if (string.IsNullOrEmpty(luaFunction))
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Error = $"Unsupported data type: {request.DataType}"
                    };
                }

                // Build Lua code to write the memory value
                string luaCode;
                if (request.DataType.ToLower() == "bytes" || request.DataType.ToLower() == "byteslocal")
                {
                    // writeBytes/writeBytesLocal(Address, Byte1, [Byte2, ...]) or writeBytes/writeBytesLocal(Address, Table)
                    if (request.ByteValues == null || request.ByteValues.Length == 0)
                    {
                        return new BaseResponse
                        {
                            Success = false,
                            Error = $"ByteValues parameter is required for {request.DataType} and must not be empty"
                        };
                    }

                    // Use table format for writeBytes/writeBytesLocal
                    string bytesTable = "{" + string.Join(", ", request.ByteValues) + "}";
                    luaCode = $@"
                        local address = {request.Address}
                        local bytesTable = {bytesTable}
                        local success, result = pcall(function()
                            return {luaFunction}(address, bytesTable)
                        end)
                        
                        if success then
                            return true
                        else
                            return false
                        end
                    ";
                }
                else if (request.DataType.ToLower() == "string" || request.DataType.ToLower() == "stringlocal")
                {
                    // writeString/writeStringLocal requires Text parameter and optional WideChar
                    if (request.Value == null || string.IsNullOrWhiteSpace(request.Value.ToString()))
                    {
                        return new BaseResponse
                        {
                            Success = false,
                            Error = $"Value parameter is required for {request.DataType} and must not be empty"
                        };
                    }

                    // WideChar parameter is optional, defaults to false
                    string wideCharParam = request.WideChar.HasValue && request.WideChar.Value ? ", true" : "";
                    string textValue = request.Value.ToString().Replace("\"", "\\\"");
                    
                    luaCode = $@"
                        local address = {request.Address}
                        local text = ""{textValue}""
                        local success, result = pcall(function()
                            return {luaFunction}(address, text{wideCharParam})
                        end)
                        
                        if success then
                            return true
                        else
                            return false
                        end
                    ";
                }
                else
                {
                    // Standard write functions
                    if (request.Value == null)
                    {
                        return new BaseResponse
                        {
                            Success = false,
                            Error = $"Value parameter is required for {request.DataType}"
                        };
                    }

                    luaCode = $@"
                        local address = {request.Address}
                        local value = {request.Value}
                        local success, result = pcall(function()
                            return {luaFunction}(address, value)
                        end)
                        
                        if success then
                            return true
                        else
                            return false
                        end
                    ";
                }

                int luaResult = lua.DoString(luaCode);
                if (luaResult != 0)
                {
                    string error = lua.ToString(-1);
                    lua.Pop(1);
                    return new BaseResponse
                    {
                        Success = false,
                        Error = $"Lua execution failed: {error}"
                    };
                }

                // Get the result
                bool success = false;
                if (lua.IsBoolean(-1))
                {
                    success = lua.ToBoolean(-1);
                }
                else if (lua.IsNumber(-1))
                {
                    success = Math.Abs(lua.ToNumber(-1)) > double.Epsilon;
                }

                lua.Pop(1);

                return new BaseResponse
                {
                    Success = success
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        private string GetLuaFunction(string dataType)
        {
            return dataType.ToLower() switch
            {
                "bytes" => "writeBytes",
                "byteslocal" => "writeBytesLocal",
                "smallinteger" or "int16" or "short" => "writeSmallInteger",
                "smallintegerlocal" or "int16local" or "shortlocal" => "writeSmallIntegerLocal",
                "integer" or "int32" or "int" => "writeInteger",
                "integerlocal" or "int32local" or "intlocal" => "writeIntegerLocal",
                "qword" or "int64" or "long" => "writeQword",
                "qwordlocal" or "int64local" or "longlocal" => "writeQwordLocal",
                "float" => "writeFloat",
                "floatlocal" => "writeFloatLocal",
                "double" => "writeDouble",
                "doublelocal" => "writeDoubleLocal",
                "string" => "writeString",
                "stringlocal" => "writeStringLocal",
                _ => null
            };
        }
    }
}