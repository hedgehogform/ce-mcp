using System;
using CESDK;
using CeMCP.Models;

namespace CeMCP.Tools
{
    public class MemoryWriteTool
    {
        public MemoryWriteResponse WriteMemory(MemoryWriteRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Address))
                {
                    return new MemoryWriteResponse
                    {
                        Value = null,
                        Success = false,
                        Error = "Address parameter is required"
                    };
                }

                if (string.IsNullOrWhiteSpace(request.DataType))
                {
                    return new MemoryWriteResponse
                    {
                        Value = null,
                        Success = false,
                        Error = "DataType parameter is required"
                    };
                }

                var memoryWrite = new MemoryWrite();
                object value = null;

                switch (request.DataType.ToLower())
                {
                    case "bytes":
                        if (request.ByteCount == null || request.ByteCount.Value <= 0)
                        {
                            return new MemoryWriteResponse
                            {
                                Value = null,
                                Success = false,
                                Error = "ByteCount parameter is required for bytes and must be greater than 0"
                            };
                        }

                        // Create an int[] of zeros with length ByteCount
                        int[] bytes = new int[request.ByteCount.Value];
                        memoryWrite.WriteBytes(request.Address, bytes);
                        value = bytes;
                        break;

                    case "integer":
                    case "int32":
                    case "int":
                        if (request.Value == null)
                            return new MemoryWriteResponse
                            {
                                Value = null,
                                Success = false,
                                Error = "Value is required for integer write"
                            };

                        if (!int.TryParse(request.Value.ToString(), out int intValue))
                        {
                            return new MemoryWriteResponse
                            {
                                Value = null,
                                Success = false,
                                Error = "Value must be a valid integer"
                            };
                        }

                        memoryWrite.WriteInteger(request.Address, intValue);
                        value = intValue;
                        break;

                    case "qword":
                    case "int64":
                    case "long":
                        if (request.Value == null)
                            return new MemoryWriteResponse
                            {
                                Value = null,
                                Success = false,
                                Error = "Value is required for qword write"
                            };

                        if (!long.TryParse(request.Value.ToString(), out long longValue))
                        {
                            return new MemoryWriteResponse
                            {
                                Value = null,
                                Success = false,
                                Error = "Value must be a valid long"
                            };
                        }

                        memoryWrite.WriteQword(request.Address, longValue);
                        value = longValue;
                        break;

                    case "float":
                        if (request.Value == null)
                            return new MemoryWriteResponse
                            {
                                Value = null,
                                Success = false,
                                Error = "Value is required for float write"
                            };

                        if (!float.TryParse(request.Value.ToString(), out float floatValue))
                        {
                            return new MemoryWriteResponse
                            {
                                Value = null,
                                Success = false,
                                Error = "Value must be a valid float"
                            };
                        }

                        memoryWrite.WriteFloat(request.Address, floatValue);
                        value = floatValue;
                        break;

                    case "string":
                        if (string.IsNullOrEmpty(request.Value?.ToString()))
                        {
                            return new MemoryWriteResponse
                            {
                                Value = null,
                                Success = false,
                                Error = "String value is required"
                            };
                        }

                        // Truncate string if MaxLength is set
                        string text = request.Value.ToString();
                        if (request.MaxLength.HasValue && text.Length > request.MaxLength.Value)
                        {
                            text = text[..request.MaxLength.Value];
                        }

                        memoryWrite.WriteString(request.Address, text, request.WideChar ?? false);
                        value = text;
                        break;


                    default:
                        return new MemoryWriteResponse
                        {
                            Value = null,
                            Success = false,
                            Error = $"Unsupported data type: {request.DataType}"
                        };
                }

                return new MemoryWriteResponse
                {
                    Value = value,
                    Success = true,
                    Error = null
                };
            }
            catch (Exception ex)
            {
                return new MemoryWriteResponse
                {
                    Value = null,
                    Success = false,
                    Error = ex.Message
                };
            }
        }
    }
}
