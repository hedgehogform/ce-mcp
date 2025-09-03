using System;
using CESDK.Classes;
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

                if (!ulong.TryParse(request.Address.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber, null, out ulong address))
                {
                    return new MemoryReadResponse
                    {
                        Value = null,
                        Success = false,
                        Error = "Invalid address format"
                    };
                }

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
                        value = MemoryAccess.ReadBytes(address, request.ByteCount.Value);
                        break;

                    case "integer":
                    case "int32":
                    case "int":
                        value = MemoryAccess.ReadInteger(address);
                        break;

                    case "qword":
                    case "int64":
                    case "long":
                        value = MemoryAccess.ReadQword(address);
                        break;

                    case "float":
                        value = MemoryAccess.ReadFloat(address);
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
                        value = MemoryAccess.ReadString(address, request.MaxLength.Value, request.WideChar ?? false);
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