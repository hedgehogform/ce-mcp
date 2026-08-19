using System;
using System.Collections.Generic;
using System.ComponentModel;
using CESDK.Classes;
using ModelContextProtocol.Server;

namespace Tools
{
    /// <summary>Bounded disassembly search and control-flow summaries.</summary>
    [McpServerToolType]
    public class AnalysisTool
    {
        private const ulong MaximumRangeBytes = 16UL * 1024 * 1024;

        private AnalysisTool() { }

        [McpServerTool(Name = "analyze_code_range"), Description(
            "Summarize calls, branches, returns, and invalid instructions in a bounded code range")]
        public static object AnalyzeCodeRange(
            [Description("Start address or symbol expression")] string startAddress,
            [Description("Stop address or symbol expression")] string stopAddress,
            [Description("Maximum instructions to inspect (1-10000)")] int maxInstructions = 1000) =>
            ToolThread.OnMainThread(() =>
            {
                if (string.IsNullOrWhiteSpace(startAddress) || string.IsNullOrWhiteSpace(stopAddress))
                    return new { success = false, error = "Start and stop addresses are required" };
                if (maxInstructions is < 1 or > 10_000)
                    return new { success = false, error = "maxInstructions must be between 1 and 10000" };
                RequireAttachedProcess();
                if (!TryResolveRange(startAddress, stopAddress, out ulong start, out ulong stop, out string? error))
                    return new { success = false, error };

                var calls = new List<object>();
                var branches = new List<object>();
                int returns = 0;
                int invalid = 0;
                int instructionCount = 0;
                ulong address = start;

                while (address < stop && instructionCount < maxInstructions)
                {
                    instructionCount++;
                    string? text = Disassembler.Disassemble(address);
                    int size = Disassembler.GetInstructionSize(address);
                    if (string.IsNullOrWhiteSpace(text) || size <= 0)
                    {
                        invalid++;
                        address++;
                        continue;
                    }

                    DisassembledInstruction parsed = Disassembler.SplitDisassembledString(text);
                    string opcode = parsed.Opcode.TrimStart();
                    object item = new { address = $"0x{address:X}", opcode = parsed.Opcode };
                    if (opcode.StartsWith("call", StringComparison.OrdinalIgnoreCase))
                        calls.Add(item);
                    else if (IsBranch(opcode))
                        branches.Add(item);
                    else if (opcode.StartsWith("ret", StringComparison.OrdinalIgnoreCase))
                        returns++;

                    address = (ulong)size >= stop - address ? stop : address + (ulong)size;
                }

                return new
                {
                    success = true,
                    startAddress = $"0x{start:X}",
                    stopAddress = $"0x{stop:X}",
                    instructionCount,
                    callCount = calls.Count,
                    branchCount = branches.Count,
                    returnCount = returns,
                    invalidCount = invalid,
                    truncated = address < stop,
                    calls,
                    branches
                };
            });

        [McpServerTool(Name = "search_disassembly"), Description(
            "Search opcode text in a bounded disassembly range")]
        public static object SearchDisassembly(
            [Description("Start address or symbol expression")] string startAddress,
            [Description("Stop address or symbol expression")] string stopAddress,
            [Description("Case-insensitive opcode substring")] string query,
            [Description("Maximum matches to return (1-1000)")] int maxResults = 100,
            [Description("Maximum instructions to inspect (1-100000)")] int maxInstructions = 10000) =>
            ToolThread.OnMainThread(() =>
            {
                if (string.IsNullOrWhiteSpace(query))
                    return new { success = false, error = "Query is required" };
                if (string.IsNullOrWhiteSpace(startAddress) || string.IsNullOrWhiteSpace(stopAddress))
                    return new { success = false, error = "Start and stop addresses are required" };
                if (maxResults is < 1 or > 1000)
                    return new { success = false, error = "maxResults must be between 1 and 1000" };
                if (maxInstructions is < 1 or > 100_000)
                    return new { success = false, error = "maxInstructions must be between 1 and 100000" };
                RequireAttachedProcess();
                if (!TryResolveRange(startAddress, stopAddress, out ulong start, out ulong stop, out string? error))
                    return new { success = false, error };

                var matches = new List<object>();
                ulong address = start;
                int inspected = 0;
                while (address < stop && inspected < maxInstructions && matches.Count < maxResults)
                {
                    inspected++;
                    string? text = Disassembler.Disassemble(address);
                    int size = Disassembler.GetInstructionSize(address);
                    if (string.IsNullOrWhiteSpace(text) || size <= 0)
                    {
                        address++;
                        continue;
                    }
                    DisassembledInstruction parsed = Disassembler.SplitDisassembledString(text);
                    if (parsed.Opcode.Contains(query, StringComparison.OrdinalIgnoreCase))
                    {
                        matches.Add(new
                        {
                            address = $"0x{address:X}",
                            parsed.Bytes,
                            parsed.Opcode,
                            parsed.Extra
                        });
                    }
                    address = (ulong)size >= stop - address ? stop : address + (ulong)size;
                }

                return new
                {
                    success = true,
                    count = matches.Count,
                    inspected,
                    truncated = address < stop,
                    matches
                };
            });

        private static void RequireAttachedProcess()
        {
            if (CESDK.Classes.Process.GetOpenedProcessID() <= 0)
                throw new InvalidOperationException("No process is attached. Open a process first.");
        }

        private static bool TryResolveRange(
            string startExpression,
            string stopExpression,
            out ulong start,
            out ulong stop,
            out string? error)
        {
            start = 0;
            stop = 0;
            if (string.IsNullOrWhiteSpace(startExpression) || string.IsNullOrWhiteSpace(stopExpression))
            {
                error = "Start and stop addresses are required";
                return false;
            }

            start = AddressResolver.GetAddress(startExpression);
            stop = AddressResolver.GetAddress(stopExpression);
            if (stop <= start)
            {
                error = "Stop address must be greater than start address";
                return false;
            }
            if (stop - start > MaximumRangeBytes)
            {
                error = $"Analysis range is limited to {MaximumRangeBytes} bytes";
                return false;
            }
            error = null;
            return true;
        }

        private static bool IsBranch(string opcode) =>
            opcode.StartsWith('j') ||
            opcode.StartsWith("loop", StringComparison.OrdinalIgnoreCase);
    }
}
