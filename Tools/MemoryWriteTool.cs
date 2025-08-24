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
                // Basic validation
                var validation = ValidateRequest(request);
                if (!validation.Success)
                    return validation;

                var memoryWrite = new MemoryWrite();
                object value = request.DataType.ToLower() switch
                {
                    "bytes" => WriteBytes(memoryWrite, request),
                    "integer" or "int32" or "int" => WriteInteger(memoryWrite, request),
                    "qword" or "int64" or "long" => WriteQword(memoryWrite, request),
                    "float" => WriteFloat(memoryWrite, request),
                    "string" => WriteString(memoryWrite, request),
                    _ => null
                };

                if (value == null && !string.Equals(request.DataType, "bytes", StringComparison.OrdinalIgnoreCase))
                {
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

        private MemoryWriteResponse ValidateRequest(MemoryWriteRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Address))
                return Error("Address parameter is required");

            if (string.IsNullOrWhiteSpace(request.DataType))
                return Error("DataType parameter is required");

            return new MemoryWriteResponse { Success = true };
        }

        private MemoryWriteResponse Error(string message) =>
            new()
            { Value = null, Success = false, Error = message };

        private static object WriteBytes(MemoryWrite memoryWrite, MemoryWriteRequest request)
        {
            if (request.ByteCount == null || request.ByteCount.Value <= 0)
                throw new ArgumentException("ByteCount parameter is required for bytes and must be greater than 0");

            int[] bytes = new int[request.ByteCount.Value];
            memoryWrite.WriteBytes(request.Address, bytes);
            return bytes;
        }

        private static object WriteInteger(MemoryWrite memoryWrite, MemoryWriteRequest request)
        {
            if (request.Value == null)
                throw new ArgumentException("Value is required for integer write");

            if (!int.TryParse(request.Value.ToString(), out int intValue))
                throw new ArgumentException("Value must be a valid integer");

            memoryWrite.WriteInteger(request.Address, intValue);
            return intValue;
        }

        private static object WriteQword(MemoryWrite memoryWrite, MemoryWriteRequest request)
        {
            if (request.Value == null)
                throw new ArgumentException("Value is required for qword write");

            if (!long.TryParse(request.Value.ToString(), out long longValue))
                throw new ArgumentException("Value must be a valid long");

            memoryWrite.WriteQword(request.Address, longValue);
            return longValue;
        }

        private static object WriteFloat(MemoryWrite memoryWrite, MemoryWriteRequest request)
        {
            if (request.Value == null)
                throw new ArgumentException("Value is required for float write");

            if (!float.TryParse(request.Value.ToString(), out float floatValue))
                throw new ArgumentException("Value must be a valid float");

            memoryWrite.WriteFloat(request.Address, floatValue);
            return floatValue;
        }

        private static object WriteString(MemoryWrite memoryWrite, MemoryWriteRequest request)
        {
            string text = request.Value?.ToString() ?? throw new ArgumentException("String value is required");

            if (request.MaxLength.HasValue && text.Length > request.MaxLength.Value)
                text = text[..request.MaxLength.Value];

            memoryWrite.WriteString(request.Address, text, request.WideChar ?? false);
            return text;
        }
    }
}
