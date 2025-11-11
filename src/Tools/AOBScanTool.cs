using System;
using System.Linq;
using System.Collections.Generic;
using CESDK.Classes;


namespace Tools
{
    public static class AobScanTool
    {
        public static List<string> AobScan(
            string aobString,
            string? protectionFlags = null,
            int? alignmentType = null,
            string? alignmentParam = null)
        {
            if (string.IsNullOrWhiteSpace(aobString))
                throw new ArgumentException("AOB string is required.", nameof(aobString));

            try
            {
                var result = AOBScanner.Scan(
                    aobString,
                    protectionFlags,
                    alignmentType ?? 0, // ✅ Use 0 if null
                    alignmentParam
                );

                return [.. result.Select(addr => $"0x{addr:X}")];
            }
            catch (AOBScanException ex)
            {
                throw new InvalidOperationException($"AOB scan failed: {ex.Message}", ex);
            }
        }
    }
}