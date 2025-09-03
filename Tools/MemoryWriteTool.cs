using System;
using CESDK.Classes;
using CeMCP.Models;
using System.Linq;

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

                if (!ulong.TryParse(request.Address.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber, null, out ulong address))
                {
                    return new MemoryWriteResponse
                    {
                        Value = null,
                        Success = false,
                        Error = "Invalid address format"
                    };
                }

                object value;
                string dataType = request.DataType.ToLower();
                if (dataType == "bytes")
                    value = WriteBytes(address, request);
                else if (dataType == "integer" || dataType == "int32" || dataType == "int")
                    value = WriteInteger(address, request);
                else if (dataType == "qword" || dataType == "int64" || dataType == "long")
                    value = WriteQword(address, request);
                else if (dataType == "float")
                    value = WriteFloat(address, request);
                else if (dataType == "string")
                    value = WriteString(address, request);
                else
                    value = null;

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

        private static object WriteBytes(ulong address, MemoryWriteRequest request)
        {
            if (request.Value == null)
                throw new ArgumentException("Value is required for bytes write");

            // Parse byte array from various formats
            byte[] bytes;
            if (request.Value is byte[] byteArray)
            {
                bytes = byteArray;
            }
            else if (request.Value is int[] intArray)
            {
                bytes = intArray.Select(i => (byte)i).ToArray();
            }
            else
            {
                throw new ArgumentException("Value must be a byte array or int array for bytes write");
            }

            MemoryAccess.WriteBytes(address, bytes);
            return bytes;
        }

        private static object WriteInteger(ulong address, MemoryWriteRequest request)
        {
            if (request.Value == null)
                throw new ArgumentException("Value is required for integer write");

            if (!int.TryParse(request.Value.ToString(), out int intValue))
                throw new ArgumentException("Value must be a valid integer");

            MemoryAccess.WriteInteger(address, intValue);
            return intValue;
        }

        private static object WriteQword(ulong address, MemoryWriteRequest request)
        {
            if (request.Value == null)
                throw new ArgumentException("Value is required for qword write");

            if (!long.TryParse(request.Value.ToString(), out long longValue))
                throw new ArgumentException("Value must be a valid long");

            MemoryAccess.WriteQword(address, longValue);
            return longValue;
        }

        private static object WriteFloat(ulong address, MemoryWriteRequest request)
        {
            if (request.Value == null)
                throw new ArgumentException("Value is required for float write");

            if (!float.TryParse(request.Value.ToString(), out float floatValue))
                throw new ArgumentException("Value must be a valid float");

            MemoryAccess.WriteFloat(address, floatValue);
            return floatValue;
        }

        private static object WriteString(ulong address, MemoryWriteRequest request)
        {
            string text = request.Value?.ToString() ?? throw new ArgumentException("String value is required");

            if (request.MaxLength.HasValue && text.Length > request.MaxLength.Value)
                text = text.Substring(0, request.MaxLength.Value);

            MemoryAccess.WriteString(address, text, request.WideChar ?? false);
            return text;
        }
    }
}
