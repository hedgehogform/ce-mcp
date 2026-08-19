using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CESDK.Classes;
using ModelContextProtocol.Server;
using static CESDK.CESDK;

namespace Tools
{
    /// <summary>
    /// Memory scanning tools: AOB pattern scanning and value scanning.
    /// Supports scanning via the main CE GUI scanner (synced with UI) or independent scanners.
    /// </summary>
    [McpServerToolType]
    public class ScanTool
    {
        private ScanTool() { }

        /// <summary>
        /// Independent scanners keyed by user-chosen name. The main UI scanner is not stored here.
        /// </summary>
        private static readonly Dictionary<string, MemScan> independentScanners = new();
        private const int MaximumIndependentScanners = 32;
        private const int MaximumScannerNameLength = 64;
        private static MemScan? mainScanner;
        private static readonly object ScanWorkflowLock = new();

        /// <summary>
        /// Checks if a process is currently attached in Cheat Engine.
        /// </summary>
        private static bool IsProcessAttached()
        {
            int pid = Process.GetOpenedProcessID();
            return pid > 0;
        }

        /// <summary>
        /// Gets the main CE UI scanner (synced with the GUI).
        /// </summary>
        private static MemScan GetMainScanner() =>
            mainScanner ??= MemScan.GetCurrentMemScan();

        /// <summary>
        /// Gets or creates an independent scanner by name.
        /// </summary>
        private static MemScan GetOrCreateIndependentScanner(string name)
        {
            if (name.Length > MaximumScannerNameLength)
                throw new ArgumentException($"Scanner name is limited to {MaximumScannerNameLength} characters", nameof(name));
            if (independentScanners.TryGetValue(name, out MemScan? scanner))
                return scanner;
            if (independentScanners.Count >= MaximumIndependentScanners)
                throw new InvalidOperationException(
                    $"At most {MaximumIndependentScanners} independent scanners may exist; reset one before creating another");

            scanner = new MemScan();
            independentScanners[name] = scanner;
            return scanner;
        }

        [McpServerTool(Name = "aob_scan"), Description("Scan memory for an Array of Bytes pattern")]
        public static object AobScan(
            [Description("AOB pattern string (e.g. 'AA BB ?? CC DD')")] string pattern,
            [Description("Memory protection flags filter")] string? protectionFlags = null,
            [Description("Alignment type (0=none)")] int? alignmentType = null,
            [Description("Alignment parameter")] string? alignmentParam = null)
        {
            return ToolThread.OnMainThread(() =>
            {
                if (alignmentType is < 0 or > 2)
                    return new { success = false, error = "Alignment type must be 0, 1, or 2" };
                if (string.IsNullOrWhiteSpace(pattern))
                    return new { success = false, error = "AOB pattern is required" };
                if (!IsProcessAttached())
                    return new { success = false, error = "No process is attached. Please open a process first using 'open_process' tool." };

                var result = AobScanner.Scan(
                    pattern,
                    protectionFlags,
                    alignmentType ?? 0,
                    alignmentParam
                );

                var addresses = result.Select(addr => $"0x{addr:X}").ToList();
                return new { success = true, addresses };
            });
        }

        [McpServerTool(Name = "aob_scan_unique"), Description(
            "Return the first address matching an Array of Bytes pattern. The caller must ensure the pattern is unique.")]
        public static object AobScanUnique(
            [Description("AOB pattern string")] string pattern,
            [Description("Memory protection flags filter")] string? protectionFlags = null,
            [Description("Alignment type (0=none)")] int alignmentType = 0,
            [Description("Alignment parameter")] string? alignmentParam = null) =>
            ToolThread.OnMainThread(() =>
            {
                if (alignmentType is < 0 or > 2)
                    return new { success = false, error = "Alignment type must be 0, 1, or 2" };
                if (string.IsNullOrWhiteSpace(pattern))
                    return new { success = false, error = "AOB pattern is required" };
                if (!IsProcessAttached())
                    return new { success = false, error = "No process is attached. Please open a process first using 'open_process' tool." };
                ulong? address = AobScanner.ScanUnique(
                    pattern,
                    protectionFlags,
                    alignmentType,
                    alignmentParam);
                return new
                {
                    success = true,
                    found = address.HasValue,
                    address = address.HasValue ? $"0x{address.Value:X}" : null
                };
            });

        [McpServerTool(Name = "aob_scan_module_unique"), Description(
            "Return the first address matching an Array of Bytes pattern inside one module")]
        public static object AobScanModuleUnique(
            [Description("Case-sensitive module name")] string moduleName,
            [Description("AOB pattern string")] string pattern,
            [Description("Memory protection flags filter")] string? protectionFlags = null,
            [Description("Alignment type (0=none)")] int alignmentType = 0,
            [Description("Alignment parameter")] string? alignmentParam = null) =>
            ToolThread.OnMainThread(() =>
            {
                if (alignmentType is < 0 or > 2)
                    return new { success = false, error = "Alignment type must be 0, 1, or 2" };
                if (string.IsNullOrWhiteSpace(moduleName) || string.IsNullOrWhiteSpace(pattern))
                    return new { success = false, error = "Module name and AOB pattern are required" };
                if (!IsProcessAttached())
                    return new { success = false, error = "No process is attached. Please open a process first using 'open_process' tool." };
                ulong? address = AobScanner.ScanModuleUnique(
                    moduleName,
                    pattern,
                    protectionFlags,
                    alignmentType,
                    alignmentParam);
                return new
                {
                    success = true,
                    found = address.HasValue,
                    address = address.HasValue ? $"0x{address.Value:X}" : null
                };
            });

        [McpServerTool(Name = "string_scan"), Description(
            "Run a fresh exact string scan with a named independent scanner")]
        public static object StringScan(
            [Description("String value to find")] string value,
            [Description("Start address for the scan range")] ulong startAddress = 0,
            [Description("Stop address for the scan range")] ulong stopAddress = 0x7FFFFFFFFFFFFFFF,
            [Description("Memory protection flags")] string protectionFlags = "+W-C",
            [Description("Scan UTF-16 strings")] bool unicode = false,
            [Description("Use case-sensitive matching")] bool caseSensitive = false,
            [Description("Independent scanner name")] string scannerName = "string-scan")
        {
            if (string.IsNullOrEmpty(value))
                return new { success = false, error = "String value is required" };
            if (string.IsNullOrWhiteSpace(scannerName))
                return new { success = false, error = "scannerName is required" };
            if (scannerName.Length > MaximumScannerNameLength)
                return new { success = false, error = $"scannerName is limited to {MaximumScannerNameLength} characters" };
            if (startAddress > stopAddress)
                return new { success = false, error = "Start address must not exceed stop address" };
            using ScanWorkflowScope scanWorkflow = EnterScanWorkflow();

            ResetMemoryScan(scannerName);
            return MemoryScan(
                ScanOption.soExactValue,
                VariableType.vtString,
                value,
                string.Empty,
                startAddress,
                stopAddress,
                protectionFlags,
                AlignmentType.fsmNotAligned,
                string.Empty,
                false,
                unicode,
                caseSensitive,
                false,
                scannerName);
        }

#pragma warning disable S107 // Methods should not have too many parameters
        [McpServerTool(Name = "memory_scan"), Description(
            "Perform a memory scan for values in the opened process. " +
            "By default uses the main CE GUI scanner which syncs results with the Cheat Engine UI. " +
            "Automatically detects first scan vs next scan. Use reset_memory_scan to start fresh.")]
        public static object MemoryScan(
            [Description("Scan option (e.g. soExactValue, soValueBetween, etc.)")] ScanOption scanOption = ScanOption.soExactValue,
            [Description("Variable type (e.g. vtDword, vtFloat, vtString, etc.)")] VariableType varType = VariableType.vtDword,
            [Description("Primary scan input value")] string input1 = "",
            [Description("Secondary scan input (for between scans)")] string? input2 = null,
            [Description("Start address for scan range")] ulong startAddress = 0,
            [Description("Stop address for scan range")] ulong stopAddress = 0x7FFFFFFFFFFFFFFF,
            [Description("Memory protection flags (e.g. '+W-C')")] string protectionFlags = "+W-C",
            [Description("Alignment type")] AlignmentType alignmentType = AlignmentType.fsmAligned,
            [Description("Alignment parameter")] string alignmentParam = "4",
            [Description("Whether input is hexadecimal")] bool isHexadecimalInput = false,
            [Description("Whether to scan for unicode strings")] bool isUnicodeScan = false,
            [Description("Whether string scan is case sensitive")] bool isCaseSensitive = false,
            [Description("Whether to use percentage-based scanning")] bool isPercentageScan = false,
            [Description("Optional scanner name. If omitted, uses the main CE GUI scanner (synced with UI). " +
                         "Provide a name to use an independent scanner that won't affect the CE GUI.")] string? scannerName = null)
        {
            using ScanWorkflowScope scanWorkflow = EnterScanWorkflow();
            try
            {
                bool isMainScanner = string.IsNullOrWhiteSpace(scannerName);
                // Run all scanner work on CE's main GUI thread. The scan engine
                // and found list are not thread-safe; running Scan/InitializeResults
                // over a large result set on the MCP worker thread races CE's main
                // thread and crashes the process (notably nextScan over big found
                // lists). Synchronize marshals onto the main thread, matching
                // reset_memory_scan.
                return Synchronize<object>(() =>
                {
                    if (!IsProcessAttached())
                        return new { success = false, error = "No process is attached. Please open a process first using 'open_process' tool." };
                    if (startAddress > stopAddress)
                        return new { success = false, error = "Start address must not exceed stop address" };
                    if (!Enum.IsDefined(scanOption) || !Enum.IsDefined(varType) || !Enum.IsDefined(alignmentType))
                        return new { success = false, error = "Scan option, variable type, or alignment type is invalid" };
                    if (alignmentType != AlignmentType.fsmNotAligned && string.IsNullOrWhiteSpace(alignmentParam))
                        return new { success = false, error = "Alignment parameter is required for aligned scans" };
                    if (scannerName?.Length > MaximumScannerNameLength)
                        return new { success = false, error = $"scannerName is limited to {MaximumScannerNameLength} characters" };
                    MemScan scanner = isMainScanner ? GetMainScanner() : GetOrCreateIndependentScanner(scannerName!);

                    var parameters = new ScanParameters
                    {
                        ScanOption = scanOption,
                        VarType = varType,
                        Input1 = input1 ?? string.Empty,
                        Input2 = input2 ?? string.Empty,
                        StartAddress = startAddress,
                        StopAddress = stopAddress,
                        ProtectionFlags = protectionFlags ?? string.Empty,
                        AlignmentType = alignmentType,
                        IsHexadecimalInput = isHexadecimalInput,
                        AlignmentParam = alignmentParam ?? string.Empty,
                        IsUnicodeScan = isUnicodeScan,
                        IsCaseSensitive = isCaseSensitive,
                        IsPercentageScan = isPercentageScan
                    };

                    // Release any FoundList still initialized from a previous
                    // scan BEFORE scanning again. A foundlist left attached to
                    // this memscan holds pointers into the prior result set; the
                    // next scan frees/reallocates those results, so CE would
                    // write through stale pointers and crash. This is the root
                    // cause of the nextScan-over-a-large-set crashes.
                    scanner.DeinitializeResults();

                    // Use the high-level Scan() method which auto-detects first vs next scan
                    scanner.Scan(parameters);
                    scanner.WaitTillDone();

                    // Check if this was a region scan (unknown initial value)
                    // Region scans mark memory regions but don't have individual addressable results
                    bool isRegionScan = scanner.LastScanWasRegionScan;

                    if (isRegionScan)
                    {
                        // For region scans, we can't read individual addresses (could be billions of bytes)
                        // Just report success - the next scan will narrow it down
                        return new
                        {
                            success = true,
                            isRegionScan = true,
                            message = "Region scan completed. Memory regions marked for next scan. Perform a next scan with a specific condition (e.g., decreased value, exact value) to narrow down results.",
                            syncedWithUI = isMainScanner
                        };
                    }

                    // Initialize results using the high-level API
                    scanner.InitializeResults();

                    int count = scanner.GetResultCount();
                    if (count == 0)
                        return new { success = true, count = 0, results = Array.Empty<object>(), syncedWithUI = isMainScanner };

                    var maxResults = Math.Min(count, 1000);
                    var results = new object[maxResults];

                    for (int i = 0; i < maxResults; i++)
                    {
                        string addrStr = scanner.GetResultAddress(i);
                        if (!ulong.TryParse(addrStr.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber, null, out ulong address))
                            throw new FormatException($"Invalid address format from scan result: {addrStr}");

                        object value = scanner.GetResultValue(i);
                        results[i] = new { address = $"0x{address:X}", value };
                    }

                    return new { success = true, count, results, syncedWithUI = isMainScanner };
                });
            }
            catch (Exception ex)
            {
                return new { success = false, error = ex.Message };
            }
        }
#pragma warning restore S107

        [McpServerTool(Name = "reset_memory_scan"), Description(
            "Reset the memory scan state to start a fresh scan. " +
            "If no scannerName is provided, resets the main CE GUI scanner. " +
            "Provide a scannerName to reset a specific independent scanner.")]
        public static object ResetMemoryScan(
            [Description("Optional scanner name. If omitted, resets the main CE GUI scanner.")] string? scannerName = null)
        {
            using ScanWorkflowScope scanWorkflow = EnterScanWorkflow();
            try
            {
                if (scannerName?.Length > MaximumScannerNameLength)
                    return new { success = false, error = $"scannerName is limited to {MaximumScannerNameLength} characters" };
                if (string.IsNullOrWhiteSpace(scannerName))
                {
                    // Reset the main CE GUI scanner
                    // Synchronize to ensure UI updates properly on CE's main thread
                    Synchronize(() =>
                    {
                        var scanner = GetMainScanner();
                        scanner.DeinitializeResults();
                        // First deinitialize the foundlist to clear the UI panel and results
                        scanner.DeinitializeFoundList();
                        // Then call newScan to reset the scan state and clear results
                        scanner.NewScan();
                        // Do NOT reinitialize - let CE manage the UI state naturally

                        // Clear the UI counter label
                        LuaExecutor.Execute(@"
                            local mainForm = getMainForm()
                            if mainForm then
                                local foundLabel = mainForm.findComponentByName('foundcountlabel')
                                if foundLabel then
                                    foundLabel.Caption = '0'
                                end
                            end
                        ");
                    });
                }
                else
                {
                    Synchronize(() =>
                    {
                        if (independentScanners.Remove(scannerName, out MemScan? scanner))
                            scanner.Dispose();
                    });
                }

                return new { success = true };
            }
            catch (Exception ex)
            {
                return new { success = false, error = ex.Message };
            }
        }
        internal static void ResetForProcessChange()
        {
            using ScanWorkflowScope scanWorkflow = EnterScanWorkflow();
            Exception? cleanupError = null;
            MemScan? scannerForMainUi = mainScanner;
            mainScanner = null;
            if (scannerForMainUi != null)
            {
                try
                {
                    scannerForMainUi.DeinitializeResults();
                    scannerForMainUi.DeinitializeFoundList();
                    scannerForMainUi.NewScan();
                }
                catch (Exception ex)
                {
                    cleanupError = ex;
                }
            }

            MemScan[] scanners = independentScanners.Values.ToArray();
            independentScanners.Clear();
            foreach (MemScan scanner in scanners)
            {
                try
                {
                    scanner.Dispose();
                }
                catch (Exception ex)
                {
                    cleanupError ??= ex;
                }
            }

            if (cleanupError != null)
                throw new InvalidOperationException("Failed to reset scan state before changing processes", cleanupError);
        }

        private static ScanWorkflowScope EnterScanWorkflow() =>
            new(ScanWorkflowLock);

        private readonly struct ScanWorkflowScope : IDisposable
        {
            private readonly object syncRoot;

            public ScanWorkflowScope(object syncRoot)
            {
                this.syncRoot = syncRoot;
                System.Threading.Monitor.Enter(syncRoot);
            }

            public void Dispose() =>
                System.Threading.Monitor.Exit(syncRoot);
        }

    }
}
