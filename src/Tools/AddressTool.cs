using System;
using System.ComponentModel;
using CESDK.Classes;
using ModelContextProtocol.Server;

namespace Tools
{
    [McpServerToolType]
    public class AddressTool
    {
        private AddressTool() { }

        [McpServerTool(Name = "resolve_address"), Description("Resolve an address from a string expression (supports symbols, module+offset, etc.)")]
        public static object ResolveAddress(
            [Description("Address string expression to resolve")] string addressString,
            [Description("Whether to resolve as local address")] bool local = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(addressString))
                    return new { success = false, error = "Address string is required" };

                var address = AddressResolver.GetAddressSafe(addressString, local);
                return new { success = true, address = address.HasValue ? $"0x{address.Value:X}" : "0" };
            }
            catch (Exception ex)
            {
                return new { success = false, error = ex.Message };
            }
        }
    }
}
