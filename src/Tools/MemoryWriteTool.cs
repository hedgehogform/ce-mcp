using System;
using System.Linq;
using CESDK.Classes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Tools
{
    public static class MemoryWriteTool
    {
        /// <summary>
        /// Maps memory write API endpoints
        /// </summary>
        public static void MapMemoryWriteApi(this WebApplication app)
        {
            // POST /api/memory/write - Write memory at address
            app.MapPost("/api/memory/write", (MemoryWriteRequest request) =>
            {
                try
                {
                    var value = WriteMemory(
                        request.Address ?? "",
                        request.DataType ?? "",
                        request.Value!,
                        request.MaxLength,
                        request.WideChar ?? false);
                    return Results.Ok(new { success = true, value });
                }
                catch (Exception ex)
                {
                    return Results.Ok(new { success = false, error = ex.Message });
                }
            })
            .WithName("WriteMemory")
            .WithDescription("Write a value to memory at the given address")
            .WithOpenApi();
        }


        /// <summary>
        /// Writes a value to memory at the given address.
        /// </summary>
        /// <param name="addressString">Memory address as hex string, e.g., "0x1234ABCD"</param>
        /// <param name="dataType">Data type: "bytes", "int32", "int64", "float", "string"</param>
        /// <param name="value">Value to write (type depends on dataType)</param>
        /// <param name="maxLength">Max length for strings</param>
        /// <param name="wideChar">Whether string is wide char (UTF-16)</param>
        /// <returns>The value that was written</returns>
        public static object WriteMemory(
            string addressString,
            string dataType,
            object value,
            int? maxLength = null,
            bool wideChar = false)
        {
            if (string.IsNullOrWhiteSpace(addressString))
                throw new ArgumentException("Address parameter is required.", nameof(addressString));

            if (string.IsNullOrWhiteSpace(dataType))
                throw new ArgumentException("DataType parameter is required.", nameof(dataType));

            if (!ulong.TryParse(addressString.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber, null, out ulong address))
                throw new FormatException("Invalid address format.");

            try
            {
                return dataType.ToLower() switch
                {
                    "bytes" => WriteBytes(address, value),
                    "integer" or "int32" or "int" => WriteInteger(address, value),
                    "qword" or "int64" or "long" => WriteQword(address, value),
                    "float" => WriteFloat(address, value),
                    "string" => WriteString(address, value, maxLength, wideChar),
                    _ => throw new NotSupportedException($"Unsupported data type: {dataType}")
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Memory write failed: {ex.Message}", ex);
            }
        }

        private static object WriteBytes(ulong address, object value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value), "Value is required for bytes write");

            byte[] bytes = value switch
            {
                byte[] b => b,
                int[] i => i.Select(x => (byte)x).ToArray(),
                _ => throw new ArgumentException("Value must be a byte array or int array for bytes write", nameof(value))
            };

            MemoryAccess.WriteBytes(address, bytes);
            return bytes;
        }

        private static object WriteInteger(ulong address, object value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value), "Value is required for integer write");

            if (!int.TryParse(value.ToString(), out int intValue))
                throw new ArgumentException("Value must be a valid integer", nameof(value));

            MemoryAccess.WriteInteger(address, intValue);
            return intValue;
        }

        private static object WriteQword(ulong address, object value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value), "Value is required for qword write");

            if (!long.TryParse(value.ToString(), out long longValue))
                throw new ArgumentException("Value must be a valid long", nameof(value));

            MemoryAccess.WriteQword(address, longValue);
            return longValue;
        }

        private static object WriteFloat(ulong address, object value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value), "Value is required for float write");

            if (!float.TryParse(value.ToString(), out float floatValue))
                throw new ArgumentException("Value must be a valid float", nameof(value));

            MemoryAccess.WriteFloat(address, floatValue);
            return floatValue;
        }

        private static object WriteString(ulong address, object value, int? maxLength, bool wideChar)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value), "Value is required for string write");

            string text = value.ToString() ?? throw new ArgumentException("String value is required", nameof(value));

            if (maxLength.HasValue && text.Length > maxLength.Value)
                text = text.Substring(0, maxLength.Value);

            MemoryAccess.WriteString(address, text, wideChar);
            return text;
        }
    }

    public record MemoryWriteRequest(
        string? Address,
        string? DataType,
        object? Value,
        int? MaxLength = null,
        bool? WideChar = false);
}
