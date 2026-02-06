using System;
using System.ComponentModel;
using System.Linq;
using CESDK.Classes;
using ModelContextProtocol.Server;

namespace Tools
{
    [McpServerToolType]
    public class MemoryWriteTool
    {
        private MemoryWriteTool() { }

        [McpServerTool(Name = "write_memory"), Description("Write a value to memory at the given address")]
        public static object WriteMemory(
            [Description("Memory address as hex string (e.g. '0x1234ABCD')")] string address,
            [Description("Data type: 'bytes', 'int32', 'int64', 'float', 'string'")] string dataType,
            [Description("Value to write (format depends on dataType)")] string value,
            [Description("Max length for strings")] int? maxLength = null,
            [Description("Whether string is wide char (UTF-16)")] bool wideChar = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(address))
                    return new { success = false, error = "Address parameter is required" };

                if (string.IsNullOrWhiteSpace(dataType))
                    return new { success = false, error = "DataType parameter is required" };

                if (string.IsNullOrWhiteSpace(value))
                    return new { success = false, error = "Value parameter is required" };

                if (!ulong.TryParse(address.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber, null, out ulong addr))
                    return new { success = false, error = "Invalid address format" };

                object written = dataType.ToLower() switch
                {
                    "bytes" => WriteBytes(addr, value),
                    "integer" or "int32" or "int" => WriteInteger(addr, value),
                    "qword" or "int64" or "long" => WriteQword(addr, value),
                    "float" => WriteFloat(addr, value),
                    "string" => WriteString(addr, value, maxLength, wideChar),
                    _ => throw new NotSupportedException($"Unsupported data type: {dataType}")
                };

                return new { success = true, value = written };
            }
            catch (Exception ex)
            {
                return new { success = false, error = ex.Message };
            }
        }

        private static object WriteBytes(ulong address, string value)
        {
            var bytes = value.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries)
                .Select(b => Convert.ToByte(b, 16))
                .ToArray();
            MemoryAccess.WriteBytes(address, bytes);
            return bytes;
        }

        private static object WriteInteger(ulong address, string value)
        {
            if (!int.TryParse(value, out int intValue))
                throw new ArgumentException("Value must be a valid integer");
            MemoryAccess.WriteInteger(address, intValue);
            return intValue;
        }

        private static object WriteQword(ulong address, string value)
        {
            if (!long.TryParse(value, out long longValue))
                throw new ArgumentException("Value must be a valid long");
            MemoryAccess.WriteQword(address, longValue);
            return longValue;
        }

        private static object WriteFloat(ulong address, string value)
        {
            if (!float.TryParse(value, out float floatValue))
                throw new ArgumentException("Value must be a valid float");
            MemoryAccess.WriteFloat(address, floatValue);
            return floatValue;
        }

        private static object WriteString(ulong address, string value, int? maxLength, bool wideChar)
        {
            string text = value;
            if (maxLength.HasValue && text.Length > maxLength.Value)
                text = text[..maxLength.Value];
            MemoryAccess.WriteString(address, text, wideChar);
            return text;
        }
    }
}
