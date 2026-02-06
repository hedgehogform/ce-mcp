using System;
using System.ComponentModel;
using CESDK.Classes;
using ModelContextProtocol.Server;

namespace Tools
{
    [McpServerToolType]
    public class MemoryReadTool
    {
        private MemoryReadTool() { }

        [McpServerTool(Name = "read_memory"), Description("Read memory at the given address with the specified data type")]
        public static object ReadMemory(
            [Description("Memory address as a hex string (e.g. '0x1234ABCD')")] string address,
            [Description("Data type: 'bytes', 'int32', 'int64', 'float', 'string'")] string dataType,
            [Description("Number of bytes to read (required for 'bytes' type)")] int? byteCount = null,
            [Description("Max length for strings (required for 'string' type)")] int? maxLength = null,
            [Description("Whether string is wide char (UTF-16)")] bool wideChar = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(address))
                    return new { success = false, error = "Address parameter is required" };

                if (string.IsNullOrWhiteSpace(dataType))
                    return new { success = false, error = "DataType parameter is required" };

                if (!ulong.TryParse(address.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber, null, out ulong addr))
                    return new { success = false, error = "Invalid address format" };

                object value = dataType.ToLower() switch
                {
                    "bytes" => byteCount.HasValue && byteCount.Value > 0
                        ? MemoryAccess.ReadBytes(addr, byteCount.Value)
                        : throw new ArgumentException("ByteCount is required for bytes and must be > 0"),

                    "integer" or "int32" or "int" => MemoryAccess.ReadInteger(addr),

                    "qword" or "int64" or "long" => MemoryAccess.ReadQword(addr),

                    "float" => MemoryAccess.ReadFloat(addr),

                    "string" => maxLength.HasValue && maxLength.Value > 0
                        ? MemoryAccess.ReadString(addr, maxLength.Value, wideChar)
                        : throw new ArgumentException("MaxLength is required for string and must be > 0"),

                    _ => throw new NotSupportedException($"Unsupported data type: {dataType}")
                };

                return new { success = true, value };
            }
            catch (Exception ex)
            {
                return new { success = false, error = ex.Message };
            }
        }
    }
}
