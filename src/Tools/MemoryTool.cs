using System;
using System.ComponentModel;
using System.Linq;
using CESDK.Classes;
using ModelContextProtocol.Server;

namespace Tools
{
    /// <summary>
    /// Memory read and write tools.
    /// </summary>
    [McpServerToolType]
    public class MemoryTool
    {
        private MemoryTool() { }

        /// <summary>
        /// Checks if a process is currently attached in Cheat Engine.
        /// </summary>
        private static bool IsProcessAttached()
        {
            int pid = Process.GetOpenedProcessID();
            return pid > 0;
        }

        [McpServerTool(Name = "read_memory"), Description("Read memory at the given address with the specified data type")]
        public static object ReadMemory(
            [Description("Memory address as a hex string (e.g. '0x1234ABCD')")] string address,
            [Description("Data type: 'bytes', 'int32', 'int64', 'float', 'string'")] string dataType,
            [Description("Number of bytes to read (required for 'bytes' type)")] int? byteCount = null,
            [Description("Max length for strings (required for 'string' type)")] int? maxLength = null,
            [Description("Whether string is wide char (UTF-16)")] bool wideChar = false)
        {
            return ToolThread.OnMainThread(() =>
            {
                if (string.IsNullOrWhiteSpace(address))
                    return new { success = false, error = "Address parameter is required" };

                if (string.IsNullOrWhiteSpace(dataType))
                    return new { success = false, error = "DataType parameter is required" };

                if (!TryParseAddress(address, out ulong addr))
                    return new { success = false, error = "Invalid address format" };

                string normalizedDataType = dataType.ToLowerInvariant();
                if (normalizedDataType == "bytes" &&
                    (byteCount is null or < 1 or > 1_048_576))
                    return new { success = false, error = "ByteCount must be between 1 and 1048576" };
                if (normalizedDataType == "string" &&
                    (maxLength is null or < 1 or > 1_048_576))
                    return new { success = false, error = "MaxLength must be between 1 and 1048576 for string reads" };
                if (normalizedDataType is not (
                    "bytes" or "integer" or "int32" or "int" or "qword" or "int64" or "long" or
                    "float" or "double" or "byte" or "int16" or "short" or "string"))
                    return new { success = false, error = $"Unsupported data type: {dataType}" };
                if (!IsProcessAttached())
                    return new { success = false, error = "No process is attached. Please open a process first using 'open_process' tool." };

                object value = normalizedDataType switch
                {
                    "bytes" => MemoryAccess.ReadBytes(addr, byteCount!.Value),
                    "integer" or "int32" or "int" => MemoryAccess.ReadInteger(addr),
                    "qword" or "int64" or "long" => MemoryAccess.ReadQword(addr),
                    "float" => MemoryAccess.ReadFloat(addr),
                    "double" => MemoryAccess.ReadDouble(addr),
                    "byte" => MemoryAccess.ReadByte(addr),
                    "int16" or "short" => MemoryAccess.ReadSmallInteger(addr),
                    "string" => MemoryAccess.ReadString(addr, maxLength!.Value, wideChar),
                    _ => throw new InvalidOperationException("Validated memory data type was not handled")
                };

                return new { success = true, value };
            });
        }

        [McpServerTool(Name = "write_memory"), Description("Write a value to memory at the given address")]
        public static object WriteMemory(
            [Description("Memory address as hex string (e.g. '0x1234ABCD')")] string address,
            [Description("Data type: 'bytes', 'int32', 'int64', 'float', 'string'")] string dataType,
            [Description("Value to write (format depends on dataType)")] string value,
            [Description("Max length for strings")] int? maxLength = null,
            [Description("Whether string is wide char (UTF-16)")] bool wideChar = false)
        {
            return ToolThread.OnMainThread(() =>
            {
                if (string.IsNullOrWhiteSpace(address))
                    return new { success = false, error = "Address parameter is required" };

                if (string.IsNullOrWhiteSpace(dataType))
                    return new { success = false, error = "DataType parameter is required" };

                if (value is null)
                    return new { success = false, error = "Value parameter is required" };

                if (!TryParseAddress(address, out ulong addr))
                    return new { success = false, error = "Invalid address format" };
                string normalizedDataType = dataType.ToLowerInvariant();
                if (normalizedDataType is not (
                    "bytes" or "integer" or "int32" or "int" or "qword" or "int64" or "long" or
                    "float" or "double" or "byte" or "int16" or "short" or "string"))
                    return new { success = false, error = $"Unsupported data type: {dataType}" };
                if (normalizedDataType == "bytes" && value.Length > 3_145_728)
                    return new { success = false, error = "Value is limited to 1048576 hexadecimal bytes" };
                if (normalizedDataType == "string" && maxLength is < 0 or > 1_048_576)
                    return new { success = false, error = "MaxLength must be between 0 and 1048576" };
                if (normalizedDataType == "string" && value.Length > 1_048_576)
                    return new { success = false, error = "String value is limited to 1048576 characters" };
                if (!IsProcessAttached())
                    return new { success = false, error = "No process is attached. Please open a process first using 'open_process' tool." };

                object written = normalizedDataType switch
                {
                    "bytes" => WriteBytes(addr, value),
                    "integer" or "int32" or "int" => WriteInt32(addr, value),
                    "qword" or "int64" or "long" => WriteInt64(addr, value),
                    "float" => WriteFloat(addr, value),
                    "double" => WriteDouble(addr, value),
                    "byte" => WriteByte(addr, value),
                    "int16" or "short" => WriteInt16(addr, value),
                    "string" => WriteString(addr, value, maxLength, wideChar),
                    _ => throw new NotSupportedException($"Unsupported data type: {dataType}")
                };

                return new { success = true, value = written };
            });
        }

        private static bool TryParseAddress(string address, out ulong result) =>
            AddressParser.TryParseHexAddress(address, out result);

        private static object WriteBytes(ulong address, string value)
        {
            if (value.Length > 3_145_728)
                throw new ArgumentException("Value is limited to 1048576 hexadecimal bytes");
            byte[] bytes = value.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries)
                .Select(item => Convert.ToByte(item, 16))
                .ToArray();
            if (bytes.Length == 0)
            {
                throw new ArgumentException("Value must contain at least one hexadecimal byte");
            }
            if (bytes.Length > 1_048_576)
            {
                throw new ArgumentException("Value is limited to 1048576 hexadecimal bytes");
            }
            EnsureWritten(MemoryAccess.WriteBytes(address, bytes));
            return bytes;
        }

        private static object WriteByte(ulong address, string value)
        {
            byte result = Convert.ToByte(value, System.Globalization.CultureInfo.InvariantCulture);
            EnsureWritten(MemoryAccess.WriteByte(address, result));
            return result;
        }

        private static object WriteInt16(ulong address, string value)
        {
            if (!short.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out short result))
                throw new ArgumentException("Value must be a valid int16");
            EnsureWritten(MemoryAccess.WriteSmallInteger(address, result));
            return result;
        }

        private static object WriteInt32(ulong address, string value)
        {
            if (!int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int result))
                throw new ArgumentException("Value must be a valid integer");
            EnsureWritten(MemoryAccess.WriteInteger(address, result));
            return result;
        }

        private static object WriteInt64(ulong address, string value)
        {
            if (!long.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out long result))
                throw new ArgumentException("Value must be a valid long");
            EnsureWritten(MemoryAccess.WriteQword(address, result));
            return result;
        }

        private static object WriteFloat(ulong address, string value)
        {
            if (!float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float result))
                throw new ArgumentException("Value must be a valid float");
            EnsureWritten(MemoryAccess.WriteFloat(address, result));
            return result;
        }

        private static object WriteDouble(ulong address, string value)
        {
            if (!double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double result))
                throw new ArgumentException("Value must be a valid double");
            EnsureWritten(MemoryAccess.WriteDouble(address, result));
            return result;
        }

        private static object WriteString(ulong address, string value, int? maxLength, bool wideChar)
        {
            if (maxLength is < 0 or > 1_048_576)
                throw new ArgumentException("MaxLength must be between 0 and 1048576");
            if (value.Length > 1_048_576)
                throw new ArgumentException("String value is limited to 1048576 characters");

            string text = maxLength.HasValue && value.Length > maxLength.Value
                ? value[..maxLength.Value]
                : value;
            EnsureWritten(MemoryAccess.WriteString(address, text, wideChar));
            return text;
        }

        private static void EnsureWritten(bool written)
        {
            if (!written)
                throw new MemoryAccessException("Cheat Engine could not write the requested memory");
        }
    }
}
