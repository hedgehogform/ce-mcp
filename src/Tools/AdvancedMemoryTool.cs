using System;
using System.ComponentModel;
using CESDK.Classes;
using ModelContextProtocol.Server;

namespace Tools
{
    /// <summary>Advanced allocation, protection, transfer, comparison, and persistence operations.</summary>
    [McpServerToolType]
    public class AdvancedMemoryTool
    {
        private AdvancedMemoryTool() { }

        [McpServerTool(Name = "allocate_memory"), Description("Allocate memory in the attached process")]
        public static object AllocateMemory(
            [Description("Allocation size in bytes")] ulong size,
            [Description("Optional preferred base address as hexadecimal")] string? preferredAddress = null,
            [Description("Optional Cheat Engine protection constant")] int? protection = null) =>
            ToolThread.OnMainThread(() =>
            {
                if (size == 0)
                    return new { success = false, error = "Size must be greater than zero" };
                if (!TryOptionalAddress(preferredAddress, out ulong? preferred, out string? error))
                    return new { success = false, error };
                RequireAttachedProcess();
                ulong address = AdvancedMemory.Allocate(size, preferred, protection);
                return new { success = true, address = $"0x{address:X}", size };
            });

        [McpServerTool(Name = "free_memory"), Description("Free memory previously allocated in the attached process")]
        public static object FreeMemory(
            [Description("Allocation base address as hexadecimal")] string address,
            [Description("Optional allocation size")] ulong? size = null) =>
            ToolThread.OnMainThread(() =>
            {
                if (!AddressParser.TryParseHexAddress(address, out ulong parsed))
                    return new { success = false, error = "Invalid address format" };
                RequireAttachedProcess();
                AdvancedMemory.Free(parsed, size);
                return new { success = true };
            });

        [McpServerTool(Name = "allocate_shared_memory"), Description("Map a named shared-memory region into the attached process")]
        public static object AllocateSharedMemory(
            [Description("Shared-memory name")] string name,
            [Description("Optional size in bytes; Cheat Engine defaults to 4096 for a new mapping")] ulong? size = null) =>
            ToolThread.OnMainThread(() =>
            {
                if (string.IsNullOrWhiteSpace(name))
                    return new { success = false, error = "Name is required" };
                if (size == 0)
                    return new { success = false, error = "Size must be greater than zero" };
                RequireAttachedProcess();
                ulong address = AdvancedMemory.AllocateShared(name, size);
                return new { success = true, address = $"0x{address:X}", size };
            });

        [McpServerTool(Name = "set_memory_protection"), Description("Set read, write, and execute protection on a target memory range")]
        public static object SetMemoryProtection(
            [Description("Range base address as hexadecimal")] string address,
            [Description("Range size in bytes")] ulong size,
            [Description("Allow reads")] bool read = true,
            [Description("Allow writes")] bool write = false,
            [Description("Allow execution")] bool execute = false) =>
            ToolThread.OnMainThread(() =>
            {
                if (!AddressParser.TryParseHexAddress(address, out ulong parsed))
                    return new { success = false, error = "Invalid address format" };
                if (size == 0)
                    return new { success = false, error = "Size must be greater than zero" };
                RequireAttachedProcess();
                AdvancedMemory.SetProtection(parsed, size, read, write, execute);
                return new { success = true, address = $"0x{parsed:X}", size, read, write, execute };
            });

        [McpServerTool(Name = "copy_memory"), Description("Copy target memory, optionally allocating the destination")]
        public static object CopyMemory(
            [Description("Source address as hexadecimal")] string sourceAddress,
            [Description("Number of bytes to copy")] ulong size,
            [Description("Optional destination address; omit to let Cheat Engine allocate it")] string? destinationAddress = null,
            [Description("Cheat Engine copy method: 0=target-to-target, 1=target-to-CE, 2=CE-to-target, 3=CE-to-CE")]
            int method = 0) =>
            ToolThread.OnMainThread(() =>
            {
                if (method is < 0 or > 3)
                    return new { success = false, error = "Method must be between 0 and 3" };
                if (!AddressParser.TryParseHexAddress(sourceAddress, out ulong source))
                    return new { success = false, error = "Invalid source address format" };
                if (!TryOptionalAddress(destinationAddress, out ulong? destination, out string? error))
                    return new { success = false, error };
                if (size == 0)
                    return new { success = false, error = "Size must be greater than zero" };
                RequireAttachedProcess();
                ulong result = AdvancedMemory.Copy(source, size, destination, method);
                return new { success = true, destinationAddress = $"0x{result:X}", allocated = !destination.HasValue, size };
            });

        [McpServerTool(Name = "compare_memory"), Description("Compare two target memory ranges")]
        public static object CompareMemory(
            [Description("First address as hexadecimal")] string firstAddress,
            [Description("Second address as hexadecimal")] string secondAddress,
            [Description("Number of bytes to compare")] ulong size,
            [Description("Cheat Engine comparison method")] int method = 0) =>
            ToolThread.OnMainThread(() =>
            {
                if (method is < 0 or > 2)
                    return new { success = false, error = "Method must be between 0 and 2" };
                if (!AddressParser.TryParseHexAddress(firstAddress, out ulong first) ||
                    !AddressParser.TryParseHexAddress(secondAddress, out ulong second))
                    return new { success = false, error = "Invalid address format" };
                if (size == 0)
                    return new { success = false, error = "Size must be greater than zero" };
                RequireAttachedProcess();
                MemoryComparison comparison = AdvancedMemory.Compare(first, second, size, method);
                return new
                {
                    success = true,
                    equal = comparison.Equal,
                    firstDifferenceOffset = comparison.FirstDifferenceOffset,
                    firstDifferenceAddress = comparison.FirstDifferenceOffset.HasValue
                        ? $"0x{first + comparison.FirstDifferenceOffset.Value:X}"
                        : null
                };
            });

        [McpServerTool(Name = "hash_memory"), Description("Compute an MD5 hash for a target memory range")]
        public static object HashMemory(
            [Description("Range base address as hexadecimal")] string address,
            [Description("Range size in bytes")] ulong size) =>
            ToolThread.OnMainThread(() =>
            {
                if (!AddressParser.TryParseHexAddress(address, out ulong parsed))
                    return new { success = false, error = "Invalid address format" };
                if (size == 0)
                    return new { success = false, error = "Size must be greater than zero" };
                RequireAttachedProcess();
                return new { success = true, md5 = AdvancedMemory.Md5(parsed, size) };
            });

        [McpServerTool(Name = "dump_memory"), Description("Write a target memory range to a file; this creates or overwrites the requested file")]
        public static object DumpMemory(
            [Description("Destination file path")] string filename,
            [Description("Range base address as hexadecimal")] string address,
            [Description("Range size in bytes")] ulong size) =>
            ToolThread.OnMainThread(() =>
            {
                if (string.IsNullOrWhiteSpace(filename))
                    return new { success = false, error = "Filename is required" };
                if (!AddressParser.TryParseHexAddress(address, out ulong parsed))
                    return new { success = false, error = "Invalid address format" };
                if (size == 0)
                    return new { success = false, error = "Size must be greater than zero" };
                RequireAttachedProcess();
                long bytesWritten = AdvancedMemory.DumpToFile(filename, parsed, size);
                return new { success = true, filename, bytesWritten };
            });

        [McpServerTool(Name = "load_memory"), Description("Load file bytes into an existing target memory region")]
        public static object LoadMemory(
            [Description("Source file path")] string filename,
            [Description("Destination address as hexadecimal")] string destinationAddress) =>
            ToolThread.OnMainThread(() =>
            {
                if (string.IsNullOrWhiteSpace(filename))
                    return new { success = false, error = "Filename is required" };
                if (!AddressParser.TryParseHexAddress(destinationAddress, out ulong destination))
                    return new { success = false, error = "Invalid destination address format" };
                RequireAttachedProcess();
                AdvancedMemory.LoadFromFile(filename, destination);
                return new { success = true, filename, destinationAddress = $"0x{destination:X}" };
            });

        private static void RequireAttachedProcess()
        {
            if (CESDK.Classes.Process.GetOpenedProcessID() <= 0)
                throw new InvalidOperationException("No process is attached. Open a process first.");
        }

        private static bool TryOptionalAddress(string? value, out ulong? address, out string? error)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                address = null;
                error = null;
                return true;
            }
            if (AddressParser.TryParseHexAddress(value, out ulong parsed))
            {
                address = parsed;
                error = null;
                return true;
            }
            address = null;
            error = "Invalid optional address format";
            return false;
        }
    }
}
