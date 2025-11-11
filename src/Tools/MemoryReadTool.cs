using System;
using CESDK.Classes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Tools
{
    public static class MemoryReadTool
    {
        /// <summary>
        /// Maps memory read API endpoints
        /// </summary>
        public static void MapMemoryReadApi(this WebApplication app)
        {
            // POST /api/memory/read - Read memory at address
            app.MapPost("/api/memory/read", (MemoryReadRequest request) =>
            {
                try
                {
                    var value = ReadMemory(
                        request.Address ?? "",
                        request.DataType ?? "",
                        request.ByteCount,
                        request.MaxLength,
                        request.WideChar ?? false);
                    return Results.Ok(new { success = true, value });
                }
                catch (Exception ex)
                {
                    return Results.Ok(new { success = false, error = ex.Message });
                }
            })
            .WithName("ReadMemory")
            .WithDescription("Read memory at the given address with the specified data type")
            .WithOpenApi();
        }

        /// <summary>
        /// Reads memory at the given address with the specified data type.
        /// </summary>
        /// <param name="addressString">Memory address as a hex string (e.g., "0x1234ABCD")</param>
        /// <param name="dataType">Data type: "bytes", "int32", "int64", "float", "string"</param>
        /// <param name="byteCount">Number of bytes to read (required for "bytes")</param>
        /// <param name="maxLength">Max length for strings (required for "string")</param>
        /// <param name="wideChar">Whether string is wide char (UTF-16)</param>
        /// <returns>Read value as object</returns>
        private static object ReadMemory(
            string addressString,
            string dataType,
            int? byteCount = null,
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
                    "bytes" => byteCount.HasValue && byteCount.Value > 0
                        ? MemoryAccess.ReadBytes(address, byteCount.Value)
                        : throw new ArgumentException("ByteCount parameter is required for bytes and must be greater than 0.", nameof(byteCount)),

                    "integer" or "int32" or "int" => MemoryAccess.ReadInteger(address),

                    "qword" or "int64" or "long" => MemoryAccess.ReadQword(address),

                    "float" => MemoryAccess.ReadFloat(address),

                    "string" => maxLength.HasValue && maxLength.Value > 0
                        ? MemoryAccess.ReadString(address, maxLength.Value, wideChar)
                        : throw new ArgumentException("MaxLength parameter is required for string and must be greater than 0.", nameof(maxLength)),

                    _ => throw new NotSupportedException($"Unsupported data type: {dataType}")
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Memory read failed: {ex.Message}", ex);
            }
        }
    }

    public record MemoryReadRequest(
        string? Address,
        string? DataType,
        int? ByteCount = null,
        int? MaxLength = null,
        bool? WideChar = false);
}
