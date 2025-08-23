using System;
using CESDK;
using CeMCP.Models;

namespace CeMCP.Tools
{
    public class MemoryReadTool
    {
        private readonly McpPlugin _plugin;

        public MemoryReadTool(McpPlugin plugin)
        {
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
        }

        public MemoryReadResponse ReadMemory(MemoryReadRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Address))
                {
                    return new MemoryReadResponse
                    {
                        Value = null,
                        Success = false,
                        Error = "Address parameter is required"
                    };
                }

                if (string.IsNullOrWhiteSpace(request.DataType))
                {
                    return new MemoryReadResponse
                    {
                        Value = null,
                        Success = false,
                        Error = "DataType parameter is required"
                    };
                }

                var lua = _plugin.sdk.lua;
                string luaFunction = GetLuaFunction(request.DataType);
                
                if (string.IsNullOrEmpty(luaFunction))
                {
                    return new MemoryReadResponse
                    {
                        Value = null,
                        Success = false,
                        Error = $"Unsupported data type: {request.DataType}"
                    };
                }

                // Build Lua code to read the memory value
                string luaCode;
                if (request.DataType.ToLower() == "bytes" || request.DataType.ToLower() == "byteslocal")
                {
                    // readBytes/readBytesLocal(Address, ByteCount, [ReturnAsTable])
                    if (!request.ByteCount.HasValue || request.ByteCount.Value <= 0)
                    {
                        return new MemoryReadResponse
                        {
                            Value = null,
                            Success = false,
                            Error = $"ByteCount parameter is required for {request.DataType} and must be greater than 0"
                        };
                    }

                    bool returnAsTable = request.ReturnAsTable ?? true;
                    luaCode = $@"
                        local address = {request.Address}
                        local byteCount = {request.ByteCount.Value}
                        local success, result = pcall(function()
                            return {luaFunction}(address, byteCount, {(returnAsTable ? "true" : "false")})
                        end)
                        
                        if success then
                            return result
                        else
                            return nil
                        end
                    ";
                }
                else if ((request.DataType.ToLower() == "smallinteger" || request.DataType.ToLower() == "integer" || request.DataType.ToLower() == "int32" || request.DataType.ToLower() == "int" || request.DataType.ToLower() == "integerlocal" || request.DataType.ToLower() == "int32local" || request.DataType.ToLower() == "intlocal") && request.Signed.HasValue)
                {
                    // readSmallInteger, readInteger, and their Local variants support a signed flag
                    luaCode = $@"
                        local address = {request.Address}
                        local success, result = pcall(function()
                            return {luaFunction}(address, {(request.Signed.Value ? "true" : "false")})
                        end)
                        
                        if success then
                            return result
                        else
                            return nil
                        end
                    ";
                }
                else if (request.DataType.ToLower() == "string" || request.DataType.ToLower() == "stringlocal")
                {
                    // readString/readStringLocal requires MaxLength parameter and optional WideChar
                    if (!request.MaxLength.HasValue || request.MaxLength.Value <= 0)
                    {
                        return new MemoryReadResponse
                        {
                            Value = null,
                            Success = false,
                            Error = $"MaxLength parameter is required for {request.DataType} and must be greater than 0"
                        };
                    }

                    // WideChar parameter is optional, defaults to false
                    string wideCharParam = request.WideChar.HasValue && request.WideChar.Value ? ", true" : "";
                    
                    luaCode = $@"
                        local address = {request.Address}
                        local maxLength = {request.MaxLength.Value}
                        local success, result = pcall(function()
                            return {luaFunction}(address, maxLength{wideCharParam})
                        end)
                        
                        if success then
                            return result
                        else
                            return nil
                        end
                    ";
                }
                else
                {
                    // Standard read functions
                    luaCode = $@"
                        local address = {request.Address}
                        local success, result = pcall(function()
                            return {luaFunction}(address)
                        end)
                        
                        if success then
                            return result
                        else
                            return nil
                        end
                    ";
                }

                int luaResult = lua.DoString(luaCode);
                if (luaResult != 0)
                {
                    string error = lua.ToString(-1);
                    lua.Pop(1);
                    return new MemoryReadResponse
                    {
                        Value = null,
                        Success = false,
                        Error = $"Lua execution failed: {error}"
                    };
                }

                // Get the result
                object value = null;
                
                if (request.DataType.ToLower() == "bytes" || request.DataType.ToLower() == "byteslocal")
                {
                    // Handle readBytes/readBytesLocal result (table or multiple values)
                    bool returnAsTable = request.ReturnAsTable ?? true;
                    
                    if (returnAsTable && lua.IsTable(-1))
                    {
                        // Extract bytes from table
                        var bytesList = new List<int>();
                        int tableLength = lua.ObjLen(-1);

                        for (int i = 1; i <= tableLength; i++)
                        {
                            lua.PushInteger(i);
                            lua.GetTable(-2);

                            if (lua.IsNumber(-1))
                            {
                                int byteValue = (int)lua.ToNumber(-1);
                                bytesList.Add(byteValue);
                            }
                            lua.Pop(1);
                        }
                        value = bytesList.ToArray();
                    }
                    else if (!returnAsTable)
                    {
                        // Handle multiple return values - collect all values on stack
                        var bytesList = new List<int>();
                        int stackSize = lua.GetTop();
                        
                        for (int i = 1; i <= stackSize; i++)
                        {
                            if (lua.IsNumber(i))
                            {
                                int byteValue = (int)lua.ToNumber(i);
                                bytesList.Add(byteValue);
                            }
                        }
                        value = bytesList.ToArray();
                        lua.SetTop(0); // Clear entire stack
                    }
                    else if (lua.IsNil(-1))
                    {
                        lua.Pop(1);
                        return new MemoryReadResponse
                        {
                            Value = null,
                            Success = false,
                            Error = "Failed to read bytes at specified address"
                        };
                    }
                }
                else if (lua.IsNumber(-1))
                {
                    double numValue = lua.ToNumber(-1);
                    // Convert to appropriate type based on data type
                    value = ConvertToAppropriateType(numValue, request.DataType);
                }
                else if (lua.IsString(-1))
                {
                    value = lua.ToString(-1);
                }
                else if (lua.IsNil(-1))
                {
                    lua.Pop(1);
                    return new MemoryReadResponse
                    {
                        Value = null,
                        Success = false,
                        Error = "Failed to read memory at specified address"
                    };
                }

                // Only pop if we haven't cleared the stack already
                if ((request.DataType.ToLower() == "bytes" || request.DataType.ToLower() == "byteslocal") && !(request.ReturnAsTable ?? true))
                {
                    // Stack already cleared above
                }
                else
                {
                    lua.Pop(1);
                }

                return new MemoryReadResponse
                {
                    Value = value,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return new MemoryReadResponse
                {
                    Value = null,
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        private string GetLuaFunction(string dataType)
        {
            return dataType.ToLower() switch
            {
                "bytes" => "readBytes",
                "byteslocal" => "readBytesLocal",
                "smallinteger" or "int16" or "short" => "readSmallInteger",
                "smallintegerlocal" or "int16local" or "shortlocal" => "readSmallIntegerLocal",
                "integer" or "int32" or "int" => "readInteger",
                "integerlocal" or "int32local" or "intlocal" => "readIntegerLocal",
                "qword" or "int64" or "long" => "readQword",
                "qwordlocal" or "int64local" or "longlocal" => "readQwordLocal",
                "pointer" => "readPointer",
                "pointerlocal" => "readPointerLocal",
                "float" => "readFloat",
                "floatlocal" => "readFloatLocal",
                "double" => "readDouble",
                "doublelocal" => "readDoubleLocal",
                "string" => "readString",
                "stringlocal" => "readStringLocal",
                _ => null
            };
        }

        private object ConvertToAppropriateType(double value, string dataType)
        {
            return dataType.ToLower() switch
            {
                "byte" => (byte)value,
                "smallinteger" or "int16" or "short" => (short)value,
                "smallintegerlocal" or "int16local" or "shortlocal" => (short)value,
                "integer" or "int32" or "int" => (int)value,
                "integerlocal" or "int32local" or "intlocal" => (int)value,
                "qword" or "int64" or "long" => (long)value,
                "qwordlocal" or "int64local" or "longlocal" => (long)value,
                "pointer" => (long)value,
                "pointerlocal" => (long)value,
                "float" => (float)value,
                "floatlocal" => (float)value,
                "double" => value,
                "doublelocal" => value,
                _ => value
            };
        }
    }
}