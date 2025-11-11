using System;
using System.ComponentModel;
using System.Linq;
using CESDK.Classes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Tools
{
    public static class MemScanTool
    {
        private static MemScan? memoryScanner = null;
        private static FoundList? foundList = null;

        /// <summary>
        /// Maps memory scan API endpoints
        /// </summary>
        public static void MapMemScanApi(this WebApplication app)
        {
            // POST /api/memscan/scan - Perform memory scan
            app.MapPost("/api/memscan/scan", (MemScanRequest request) =>
            {
                try
                {
                    var results = Scan(
                        request.ScanOption,
                        request.VarType,
                        request.Input1,
                        request.Input2,
                        request.StartAddress,
                        request.StopAddress,
                        request.ProtectionFlags,
                        request.AlignmentType,
                        request.AlignmentParam,
                        request.IsHexadecimalInput,
                        request.IsUnicodeScan,
                        request.IsCaseSensitive,
                        request.IsPercentageScan);

                    return Results.Ok(new
                    {
                        success = true,
                        count = results.Length,
                        results = results.Select(r => new { address = $"0x{r.Address:X}", value = r.Value })
                    });
                }
                catch (Exception ex)
                {
                    return Results.Ok(new { success = false, error = ex.Message });
                }
            })
            .WithName("MemScan")
            .WithDescription("Perform a memory scan for values")
            .WithOpenApi();

            // POST /api/memscan/reset - Reset memory scan state
            app.MapPost("/api/memscan/reset", () =>
            {
                try
                {
                    ResetScan();
                    return Results.Ok(new { success = true });
                }
                catch (Exception ex)
                {
                    return Results.Ok(new { success = false, error = ex.Message });
                }
            })
            .WithName("ResetMemScan")
            .WithDescription("Reset the memory scan state")
            .WithOpenApi();
        }

        /// <summary>
        /// Performs a memory scan.
        /// </summary>
        /// <param name="scanOption">Scan option</param>
        /// <param name="varType">Variable type</param>
        /// <param name="input1">First input value</param>
        /// <param name="input2">Second input value (for range scans)</param>
        /// <param name="startAddress">Start address for scan</param>
        /// <param name="stopAddress">Stop address for scan</param>
        /// <param name="protectionFlags">Memory protection flags</param>
        /// <param name="alignmentType">Alignment type</param>
        /// <param name="alignmentParam">Alignment parameter</param>
        /// <param name="isHexadecimalInput">Whether input is hexadecimal</param>
        /// <param name="isUnicodeScan">Whether to scan as Unicode</param>
        /// <param name="isCaseSensitive">Case-sensitive for strings</param>
        /// <param name="isPercentageScan">Whether to scan as percentage</param>
        /// <returns>Array of found addresses and values</returns>
        private static (ulong Address, object Value)[] Scan(
            ScanOption scanOption,
            VariableType varType,
            string input1,
            string? input2 = null,
            ulong startAddress = 0,
            ulong stopAddress = ulong.MaxValue,
            string protectionFlags = "+W-C",
            AlignmentType alignmentType = AlignmentType.fsmAligned,
            string alignmentParam = "4",
            bool isHexadecimalInput = false,
            bool isUnicodeScan = false,
            bool isCaseSensitive = false,
            bool isPercentageScan = false)
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
                return Array.Empty<(ulong, object)>();

            var maxResults = Math.Min(count, 1000);
            var results = new (ulong Address, object Value)[maxResults];

            for (int i = 0; i < maxResults; i++)
            {
                string addrStr = foundList.GetAddress(i);
                if (!ulong.TryParse(addrStr.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber, null, out ulong address))
                    throw new FormatException($"Invalid address format from scan result: {addrStr}");

                object value = foundList.GetValue(i);
                results[i] = (address, value);
            }

            return results;
        }

        /// <summary>
        /// Resets the memory scan state.
        /// </summary>
        private static void ResetScan()
        {
            memoryScanner = new MemScan();
            foundList = new FoundList(memoryScanner);
        }
    }

    public record MemScanRequest(
         [property: DefaultValue(ScanOption.soExactValue)]
        ScanOption ScanOption = ScanOption.soExactValue,

         [property: DefaultValue(VariableType.vtDword)]
        VariableType VarType = VariableType.vtDword,

         [property: DefaultValue("")]
        string Input1 = "",

         [property: DefaultValue(null)]
        string? Input2 = null,

         [property: DefaultValue((ulong)0)]
        ulong StartAddress = 0,

         [property: DefaultValue(ulong.MaxValue)]
        ulong StopAddress = ulong.MaxValue,

         [property: DefaultValue("+W-C")]
        string ProtectionFlags = "+W-C",

         [property: DefaultValue(AlignmentType.fsmAligned)]
        AlignmentType AlignmentType = AlignmentType.fsmAligned,

         [property: DefaultValue("4")]
        string AlignmentParam = "4",

         [property: DefaultValue(false)]
        bool IsHexadecimalInput = false,

         [property: DefaultValue(false)]
        bool IsUnicodeScan = false,

         [property: DefaultValue(false)]
        bool IsCaseSensitive = false,

         [property: DefaultValue(false)]
        bool IsPercentageScan = false
     );
}
