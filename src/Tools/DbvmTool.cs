using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using CESDK.Classes;
using ModelContextProtocol.Server;

namespace Tools
{
    /// <summary>Optional DBVM physical-memory and hardware watch operations.</summary>
    [McpServerToolType]
    public class DbvmTool
    {
        private DbvmTool() { }

        [McpServerTool(Name = "dbvm_status"), Description("Check whether DBVM is loaded and available")]
        public static object Status() =>
            ToolThread.OnMainThread(() => new { success = true, available = Dbvm.IsAvailable() });

        [McpServerTool(Name = "dbvm_initialize"), Description(
            "Initialize DBVM. offloadOperatingSystem=true is advanced and can destabilize the host.")]
        public static object Initialize(
            [Description("Offload the operating system onto DBVM when possible")] bool offloadOperatingSystem = false,
            [Description("Optional reason shown by Cheat Engine")] string? reason = null) =>
            ToolThread.OnMainThread(() =>
            {
                bool initialized = Dbvm.Initialize(offloadOperatingSystem, reason);
                return initialized
                    ? new { success = true, available = true }
                    : (object)new { success = false, error = "DBVM initialization failed or DBVM is unavailable" };
            });

        [McpServerTool(Name = "dbvm_read_physical"), Description("Read physical memory through DBVM")]
        public static object ReadPhysical(
            [Description("Physical address as hexadecimal")] string address,
            [Description("Byte count (1-1048576)")] int size) =>
            ToolThread.OnMainThread(() =>
            {
                if (!AddressParser.TryParseHexAddress(address, out ulong parsed))
                    return new { success = false, error = "Invalid address format" };
                if (size is < 1 or > 1_048_576)
                    return new { success = false, error = "Size must be between 1 and 1048576" };
                return new { success = true, address = $"0x{parsed:X}", bytes = Dbvm.ReadPhysical(parsed, size) };
            });

        [McpServerTool(Name = "dbvm_write_physical"), Description("Write physical memory through DBVM")]
        public static object WritePhysical(
            [Description("Physical address as hexadecimal")] string address,
            [Description("Hex bytes separated by spaces or commas")] string bytes) =>
            ToolThread.OnMainThread(() =>
            {
                if (!AddressParser.TryParseHexAddress(address, out ulong parsed))
                    return new { success = false, error = "Invalid address format" };
                if (!TryParseBytes(bytes, out byte[] parsedBytes))
                    return new { success = false, error = "Bytes must be valid hexadecimal" };
                if (parsedBytes.Length is < 1 or > 1_048_576)
                    return new { success = false, error = "Byte count must be between 1 and 1048576" };
                Dbvm.WritePhysical(parsed, parsedBytes);
                return new { success = true, bytesWritten = parsedBytes.Length };
            });

        [McpServerTool(Name = "dbvm_watch"), Description(
            "Start a DBVM physical-memory read, write, or execute watch")]
        public static object StartWatch(
            [Description("Access type: read, write, or execute")] string access,
            [Description("Physical address as hexadecimal")] string physicalAddress,
            [Description("Watched byte size")] int byteSize = 1,
            [Description("DBVM watch option bit field")] int options = 0,
            [Description("Internal event capacity (1-65536)")] int internalEntryCount = 8192) =>
            ToolThread.OnMainThread(() =>
            {
                if (!AddressParser.TryParseHexAddress(physicalAddress, out ulong parsed))
                    return new { success = false, error = "Invalid physical address format" };
                if (byteSize < 1)
                    return new { success = false, error = "byteSize must be greater than zero" };
                if (internalEntryCount is < 1 or > 65_536)
                    return new { success = false, error = "internalEntryCount must be between 1 and 65536" };
                long id = Dbvm.StartWatch(access, parsed, byteSize, options, internalEntryCount);
                return new { success = true, id };
            });

        [McpServerTool(Name = "dbvm_watch_log"), Description("Retrieve the bounded event log for a DBVM watch")]
        public static object GetWatchLog(
            [Description("Watch identifier returned by dbvm_watch")] long id) =>
            ToolThread.OnMainThread(() => new { success = true, events = Dbvm.GetWatchLog(id) });

        [McpServerTool(Name = "dbvm_watch_stop"), Description("Stop a DBVM watch")]
        public static object StopWatch(
            [Description("Watch identifier returned by dbvm_watch")] long id) =>
            ToolThread.OnMainThread(() =>
            {
                Dbvm.StopWatch(id);
                return new { success = true };
            });

        private static bool TryParseBytes(string text, out byte[] bytes)
        {
            try
            {
                bytes = text
                    .Split([' ', ',', '-'], StringSplitOptions.RemoveEmptyEntries)
                    .Select(value => byte.Parse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture))
                    .ToArray();
                return bytes.Length > 0;
            }
            catch (FormatException)
            {
                bytes = [];
                return false;
            }
            catch (OverflowException)
            {
                bytes = [];
                return false;
            }
        }
    }
}
