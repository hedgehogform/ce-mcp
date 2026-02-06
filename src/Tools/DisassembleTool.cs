using System;
using System.ComponentModel;
using CESDK.Classes;
using ModelContextProtocol.Server;

namespace Tools
{
    [McpServerToolType]
    public class DisassembleTool
    {
        private DisassembleTool() { }

        [McpServerTool(Name = "disassemble"), Description("Disassemble instructions or get instruction size at a memory address")]
        public static object Disassemble(
            [Description("Memory address as hex string (e.g. '0x1234ABCD')")] string address,
            [Description("Request type: 'disassemble' or 'get-instruction-size' (default: disassemble)")] string? requestType = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(address))
                    return new { success = false, error = "Address parameter is required" };

                if (!ulong.TryParse(address.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber, null, out ulong addr))
                    return new { success = false, error = "Invalid address format" };

                string result = requestType?.ToLower() switch
                {
                    "get-instruction-size" => Disassembler.GetInstructionSize(addr).ToString(),
                    "disassemble" or null or "" => Disassembler.Disassemble(addr)?.ToString() ?? "Failed to disassemble",
                    _ => throw new NotSupportedException($"Unsupported request type: {requestType}")
                };

                return new { success = true, result };
            }
            catch (Exception ex)
            {
                return new { success = false, error = ex.Message };
            }
        }
    }
}
