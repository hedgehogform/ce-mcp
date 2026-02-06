using System;
using System.ComponentModel;
using System.Linq;
using CESDK.Classes;
using ModelContextProtocol.Server;

namespace Tools
{
    [McpServerToolType]
    public class MemScanTool
    {
        private MemScanTool() { }

        private static MemScan? memoryScanner = null;
        private static FoundList? foundList = null;

#pragma warning disable S107 // Methods should not have too many parameters
        [McpServerTool(Name = "memory_scan"), Description("Perform a memory scan for values in the opened process")]
        public static object Scan(
            [Description("Scan option (e.g. soExactValue, soValueBetween, etc.)")] ScanOption scanOption = ScanOption.soExactValue,
            [Description("Variable type (e.g. vtDword, vtFloat, vtString, etc.)")] VariableType varType = VariableType.vtDword,
            [Description("Primary scan input value")] string input1 = "",
            [Description("Secondary scan input (for between scans)")] string? input2 = null,
            [Description("Start address for scan range")] ulong startAddress = 0,
            [Description("Stop address for scan range")] ulong stopAddress = ulong.MaxValue,
            [Description("Memory protection flags (e.g. '+W-C')")] string protectionFlags = "+W-C",
            [Description("Alignment type")] AlignmentType alignmentType = AlignmentType.fsmAligned,
            [Description("Alignment parameter")] string alignmentParam = "4",
            [Description("Whether input is hexadecimal")] bool isHexadecimalInput = false,
            [Description("Whether to scan for unicode strings")] bool isUnicodeScan = false,
            [Description("Whether string scan is case sensitive")] bool isCaseSensitive = false,
            [Description("Whether to use percentage-based scanning")] bool isPercentageScan = false)
        {
            try
            {
                if (memoryScanner == null)
                {
                    memoryScanner = new MemScan();
                    foundList = new FoundList(memoryScanner);
                }

                var parameters = new ScanParameters
                {
                    ScanOption = scanOption,
                    VarType = varType,
                    Input1 = input1,
                    Input2 = input2 ?? string.Empty,
                    StartAddress = startAddress,
                    StopAddress = stopAddress,
                    ProtectionFlags = protectionFlags,
                    AlignmentType = alignmentType,
                    AlignmentParam = alignmentParam,
                    IsHexadecimalInput = isHexadecimalInput,
                    IsUnicodeScan = isUnicodeScan,
                    IsCaseSensitive = isCaseSensitive,
                    IsPercentageScan = isPercentageScan
                };

                bool isFirstScan = foundList == null || foundList.Count <= 0;

                if (isFirstScan)
                    memoryScanner.FirstScan(parameters);
                else
                    memoryScanner.NextScan(parameters);

                memoryScanner.WaitTillDone();
                foundList!.Initialize();

                int count = foundList.Count;
                if (count == 0)
                    return new { success = true, count = 0, results = Array.Empty<object>() };

                var maxResults = Math.Min(count, 1000);
                var results = new object[maxResults];

                for (int i = 0; i < maxResults; i++)
                {
                    string addrStr = foundList.GetAddress(i);
                    if (!ulong.TryParse(addrStr.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber, null, out ulong address))
                        throw new FormatException($"Invalid address format from scan result: {addrStr}");

                    object value = foundList.GetValue(i);
                    results[i] = new { address = $"0x{address:X}", value };
                }

                return new { success = true, count, results };
            }
            catch (Exception ex)
            {
                return new { success = false, error = ex.Message };
            }
        }
#pragma warning restore S107

        [McpServerTool(Name = "reset_memory_scan"), Description("Reset the memory scan state to start a fresh scan")]
        public static object ResetScan()
        {
            try
            {
                memoryScanner = new MemScan();
                foundList = new FoundList(memoryScanner);
                return new { success = true };
            }
            catch (Exception ex)
            {
                return new { success = false, error = ex.Message };
            }
        }
    }
}
