using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CESDK.Classes;
using ModelContextProtocol.Server;

namespace Tools
{
    [McpServerToolType]
    public class AobScanTool
    {
        private AobScanTool() { }

        [McpServerTool(Name = "aob_scan"), Description("Scan memory for an Array of Bytes pattern")]
        public static object AobScan(
            [Description("AOB pattern string (e.g. 'AA BB ?? CC DD')")] string pattern,
            [Description("Memory protection flags filter")] string? protectionFlags = null,
            [Description("Alignment type (0=none)")] int? alignmentType = null,
            [Description("Alignment parameter")] string? alignmentParam = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(pattern))
                    return new { success = false, error = "AOB pattern is required" };

                var result = AOBScanner.Scan(
                    pattern,
                    protectionFlags,
                    alignmentType ?? 0,
                    alignmentParam
                );

                var addresses = result.Select(addr => $"0x{addr:X}").ToList();
                return new { success = true, addresses };
            }
            catch (Exception ex)
            {
                return new { success = false, error = ex.Message };
            }
        }
    }
}
