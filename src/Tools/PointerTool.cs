using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CESDK.Classes;
using ModelContextProtocol.Server;

namespace Tools
{
    /// <summary>Pointer-chain resolution and bounded direct-reference discovery.</summary>
    [McpServerToolType]
    public class PointerTool
    {
        private const ulong MaximumReferenceScanBytes = 64UL * 1024 * 1024;

        private PointerTool() { }

        [McpServerTool(Name = "read_pointer_chain"), Description(
            "Resolve and validate a pointer chain. Each step dereferences the current address, then applies the corresponding signed offset.")]
        public static object ReadPointerChain(
            [Description("Initial pointer address as hexadecimal")] string baseAddress,
            [Description("Signed offsets applied after each dereference")] IReadOnlyList<long> offsets) =>
            ToolThread.OnMainThread(() =>
            {
                if (!AddressParser.TryParseHexAddress(baseAddress, out ulong parsed))
                    return new { success = false, error = "Invalid base address format" };
                if (offsets is null || offsets.Count == 0)
                    return new { success = false, error = "At least one offset is required" };
                if (offsets.Count > 64)
                    return new { success = false, error = "Pointer chains are limited to 64 offsets" };
                RequireAttachedProcess();

                PointerChainResult result = PointerChains.Resolve(parsed, offsets);
                var steps = result.Steps.Select(step => new
                {
                    readAddress = $"0x{step.ReadAddress:X}",
                    pointerValue = $"0x{step.PointerValue:X}",
                    step.Offset,
                    resultAddress = $"0x{step.ResultAddress:X}",
                    step.Readable
                }).ToList();
                return new
                {
                    success = true,
                    valid = result.Valid,
                    address = $"0x{result.Address:X}",
                    steps
                };
            });

        [McpServerTool(Name = "find_pointer_references"), Description(
            "Find direct pointers to a target within a bounded memory range. This is not CE's Pointer Scanner and does not recurse into pointer paths.")]
        public static object FindPointerReferences(
            [Description("Target address as hexadecimal")] string targetAddress,
            [Description("Start of the scan range as hexadecimal")] string startAddress,
            [Description("End of the scan range as hexadecimal")] string stopAddress,
            [Description("Pointer size in bytes; omit to use the target pointer size")] int? pointerSize = null,
            [Description("Cheat Engine protection flags")] string protectionFlags = "*W*X*C",
            [Description("Maximum returned addresses (1-1000)")] int maxResults = 100) =>
            ToolThread.OnMainThread(() =>
            {
                if (!AddressParser.TryParseHexAddress(targetAddress, out ulong target) ||
                    !AddressParser.TryParseHexAddress(startAddress, out ulong start) ||
                    !AddressParser.TryParseHexAddress(stopAddress, out ulong stop))
                    return new { success = false, error = "Invalid address format" };
                if (stop < start)
                    return new { success = false, error = "Stop address must not precede start address" };
                if (stop - start > MaximumReferenceScanBytes)
                    return new { success = false, error = $"Scan range is limited to {MaximumReferenceScanBytes} bytes" };
                if (maxResults is < 1 or > 1000)
                    return new { success = false, error = "maxResults must be between 1 and 1000" };
                RequireAttachedProcess();

                int size = pointerSize ?? SymbolManager.GetPointerSize();
                List<ulong> addresses = PointerChains.FindDirectReferences(
                    target,
                    start,
                    stop,
                    size,
                    protectionFlags,
                    maxResults);
                return new
                {
                    success = true,
                    count = addresses.Count,
                    addresses = addresses.Select(address => $"0x{address:X}").ToList(),
                    pointerSize = size,
                    truncated = addresses.Count == maxResults
                };
            });
        private static void RequireAttachedProcess()
        {
            if (CESDK.Classes.Process.GetOpenedProcessID() <= 0)
                throw new InvalidOperationException("No process is attached. Open a process first.");
        }

    }
}
