using System;
using CESDK;
using CeMCP.Models;

namespace CeMCP.Tools
{
    public class MemoryReadTool
    {
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

                var memoryRead = new MemoryRead();
                object value;

                switch (request.DataType.ToLower())
                {
                    case "bytes":
                        if (!request.ByteCount.HasValue || request.ByteCount.Value <= 0)
                        {
                            return new MemoryReadResponse
                            {
                                Value = null,
                                Success = false,
                                Error = "ByteCount parameter is required for bytes and must be greater than 0"
                            };
                        }
                        value = memoryRead.ReadBytes(request.Address, request.ByteCount.Value, request.ReturnAsTable ?? true);
                        break;

                    case "integer":
                    case "int32":
                    case "int":
                        value = memoryRead.ReadInteger(request.Address, request.Signed ?? false);
                        break;

                    case "qword":
                    case "int64":
                    case "long":
                        value = memoryRead.ReadQword(request.Address);
                        break;

                    case "float":
                        value = memoryRead.ReadFloat(request.Address);
                        break;

                    case "string":
                        if (!request.MaxLength.HasValue || request.MaxLength.Value <= 0)
                        {
                            return new MemoryReadResponse
                            {
                                Value = null,
                                Success = false,
                                Error = "MaxLength parameter is required for string and must be greater than 0"
                            };
                        }
                        value = memoryRead.ReadString(request.Address, request.MaxLength.Value, request.WideChar ?? false);
                        break;

                    default:
                        return new MemoryReadResponse
                        {
                            Value = null,
                            Success = false,
                            Error = $"Unsupported data type: {request.DataType}"
                        };
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
    }
}