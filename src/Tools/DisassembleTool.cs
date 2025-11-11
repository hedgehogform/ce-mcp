using System;
using CESDK.Classes;

namespace Tools
{
    public static class DisassembleTool
    {
        /// <summary>
        /// Disassembles instructions or gets instruction size at a memory address.
        /// </summary>
        /// <param name="addressString">Memory address as hex string, e.g., "0x1234ABCD"</param>
        /// <param name="requestType">Optional: "disassemble" or "get-instruction-size"</param>
        /// <returns>Disassembly result or instruction size as string</returns>
        public static string Disassemble(string addressString, string? requestType = null)
        {
            if (string.IsNullOrWhiteSpace(addressString))
                throw new ArgumentException("Address parameter is required.", nameof(addressString));

            if (!ulong.TryParse(addressString.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber, null, out ulong address))
                throw new FormatException("Invalid address format.");

            try
            {
                return requestType?.ToLower() switch
                {
                    "get-instruction-size" => Disassembler.GetInstructionSize(address).ToString(),
                    "disassemble" or null or "" => Disassembler.Disassemble(address)?.ToString() ?? "Failed to disassemble",
                    _ => throw new NotSupportedException($"Unsupported request type: {requestType}")
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Disassembly failed: {ex.Message}", ex);
            }
        }
    }
}
