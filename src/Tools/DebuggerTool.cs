using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.ComponentModel;
using CESDK.Classes;
using ModelContextProtocol.Server;

namespace Tools
{
    /// <summary>
    /// Debugger tools: attach/detach, breakpoints, registers, stepping.
    /// Supports "find what writes/accesses an address" via tracked write/access breakpoints.
    /// </summary>
    [McpServerToolType]
    public class DebuggerTool
    {
        private DebuggerTool() { }

        private const string InvalidAddressMsg = "Invalid address format";
        private const string DefaultDataType = "int32";
        private static readonly HashSet<string> ValidRegisterNames =
        [
            "EAX", "EBX", "ECX", "EDX", "ESI", "EDI", "EBP", "ESP", "EIP",
            "RAX", "RBX", "RCX", "RDX", "RSI", "RDI", "RBP", "RSP", "RIP",
            "R8", "R9", "R10", "R11", "R12", "R13", "R14", "R15", "EFLAGS"
        ];

        // Lua IIFE: builds a register table from the current debug context.
        // Caller must already have invoked debug_getContext(false).
        private const string LuaBuildRegTable = @"(function()
    if targetIs64Bit() then
        return {
            ip    = string.format('%016X', RIP),
            ax    = string.format('%016X', RAX),
            bx    = string.format('%016X', RBX),
            cx    = string.format('%016X', RCX),
            dx    = string.format('%016X', RDX),
            si    = string.format('%016X', RSI),
            di    = string.format('%016X', RDI),
            bp    = string.format('%016X', RBP),
            sp    = string.format('%016X', RSP),
            r8    = string.format('%016X', R8),
            r9    = string.format('%016X', R9),
            r10   = string.format('%016X', R10),
            r11   = string.format('%016X', R11),
            r12   = string.format('%016X', R12),
            r13   = string.format('%016X', R13),
            r14   = string.format('%016X', R14),
            r15   = string.format('%016X', R15),
            flags = string.format('%08X', EFLAGS)
        }
    else
        return {
            ip    = string.format('%08X', EIP),
            ax    = string.format('%08X', EAX),
            bx    = string.format('%08X', EBX),
            cx    = string.format('%08X', ECX),
            dx    = string.format('%08X', EDX),
            si    = string.format('%08X', ESI),
            di    = string.format('%08X', EDI),
            bp    = string.format('%08X', EBP),
            sp    = string.format('%08X', ESP),
            flags = string.format('%08X', EFLAGS)
        }
    end
end)()";

        // ── Attach / Status ──────────────────────────────────────────────────

        [McpServerTool(Name = "dbg_start"), Description(
            "Attach the Cheat Engine debugger to the currently opened process. " +
            "The operation validates that the PID is still live, preserves CE's same-process handle, and is idempotent for an active interface or known CE fallback. " +
            "debugInterface: 0=default, 1=windows, 2=VEH, 3=kernel. The response reports the actual interface selected by CE.")]
        public static object DbgStart(
            [Description("Debugger interface to request: 0=default, 1=windows, 2=VEH, 3=kernel")] int debugInterface = 0)
            => SafeRun(() =>
            {
                if (debugInterface is < 0 or > 3)
                    return new { success = false, error = "debugInterface must be between 0 and 3" };

                DebuggerAttachResult result = Debugger.DebugProcess(debugInterface);
                return new
                {
                    success = true,
                    message = result.UsedFallback
                        ? "Debugger attached using Cheat Engine's fallback interface"
                        : "Debugger attached",
                    processId = result.ProcessId,
                    requestedDebuggerInterface = result.RequestedInterface,
                    debuggerInterface = result.ActualInterface,
                    usedFallback = result.UsedFallback,
                    alreadyAttached = result.AlreadyAttached
                };
            });

        [McpServerTool(Name = "dbg_exit"), Description(
            "Unpause and detach the Cheat Engine debugger while preserving the same live target process handle for a later reattach.")]
        public static object DbgExit()
            => SafeRun(() =>
            {
                Debugger.DetachIfPossible();
                return new
                {
                    success = true,
                    message = "Debugger detached; target handle preserved",
                    processId = Process.GetOpenedProcessID(),
                    isDebugging = Debugger.IsDebugging()
                };
            });

        [McpServerTool(Name = "dbg_is_debugging"), Description(
            "Check whether the CE debugger is currently attached to the process.")]
        public static object DbgIsDebugging()
            => SafeRun(() =>
            {
                bool active = Debugger.IsDebugging();
                bool broken = active && Debugger.IsBroken();
                bool paused = Debugger.IsPaused();
                int? iface = active ? Debugger.GetCurrentDebuggerInterface() : null;
                return new
                {
                    success = true,
                    is_debugging = active,
                    is_broken = broken,
                    is_paused = paused,
                    debugger_interface = iface
                };
            });

        [McpServerTool(Name = "dbg_is_broken"), Description(
            "Returns true if the debugger is currently halted on a breakpoint or single step.")]
        public static object DbgIsBroken()
            => SafeRun(() => new { success = true, is_broken = Debugger.IsBroken() });

        // ── Breakpoints ──────────────────────────────────────────────────────

        [McpServerTool(Name = "dbg_add_bp"), Description(
            "Set a breakpoint at the given address. " +
            "trigger: 'execute' (breaks execution), 'write' (fires when address is written), 'access' (fires on read or write). " +
            "size: watch size in bytes for write/access breakpoints (1, 2, 4 or 8). Ignored for execute BPs. " +
            "track_hits: when true (recommended for write/access BPs), installs a Lua callback that records each hit " +
            "(registers + instruction) and auto-continues — use dbg_get_bp_hits to retrieve results. " +
            "When false, the breakpoint will pause execution (use dbg_gpregs / dbg_continue after it fires). " +
            "Call dbg_start first to attach the debugger. " +
            "Example for 'find what writes to address': dbg_add_bp('0x1234ABCD', 4, 'write', true)")]
        public static object DbgAddBp(
            [Description("Memory address as hex string (e.g. '0x1234ABCD')")] string address,
            [Description("Watch size in bytes for write/access BPs (1, 2, 4, 8). Ignored for execute BPs.")] int size = 4,
            [Description("Breakpoint trigger: 'execute', 'write', or 'access'")] string trigger = "execute",
            [Description("When true, installs a hit-tracking callback that auto-continues and records instruction+registers per hit")] bool trackHits = false)
            => SafeRunWithAddress(address, addr =>
            {
                string ceTrigger = trigger.ToLowerInvariant() switch
                {
                    "write" => "bptWrite",
                    "access" => "bptAccess",
                    "execute" or "exec" => "bptExecute",
                    _ => throw new ArgumentException($"Unknown trigger '{trigger}'. Use 'execute', 'write', or 'access'")
                };
                if (size is not 1 and not 2 and not 4 and not 8)
                    throw new ArgumentException("Size must be 1, 2, 4, or 8");

                if (trackHits)
                {
                    // Set up a Lua callback that records hits and auto-continues.
                    // The callback captures registers + disassembly of the instruction at EIP/RIP.
                    string script = $@"
if __mcp_bp_hits == nil then __mcp_bp_hits = {{}} end
local addrHex = string.format('%016X', {addr})
if __mcp_bp_hits[addrHex] == nil then __mcp_bp_hits[addrHex] = {{}} end
debug_setBreakpoint({addr}, {size}, {ceTrigger}, function()
    local hit = {{}}
    debug_getContext(false)
    local regs = {LuaBuildRegTable}
    for k, v in pairs(regs) do hit[k] = v end
    local ipval = tonumber(regs.ip, 16)
    if ipval and ipval ~= 0 then
        hit.instruction = disassemble(ipval)
    end
    hit.tick = getTickCount()
    table.insert(__mcp_bp_hits[addrHex], hit)
    debug_continueFromBreakpoint(co_run)
    return 1
end)
return true";
                    LuaExecutor.Execute(script);
                    return new
                    {
                        success = true,
                        address = $"0x{addr:X}",
                        trigger = ceTrigger,
                        track_hits = true,
                        message = $"Tracking breakpoint set. Trigger the write/access, then call dbg_get_bp_hits('{address}')."
                    };
                }

                Debugger.SetBreakpoint(addr, size, ceTrigger);
                return new
                {
                    success = true,
                    address = $"0x{addr:X}",
                    trigger = ceTrigger,
                    track_hits = false,
                    message = "Breaking breakpoint set. When it fires use dbg_gpregs to read registers, then dbg_continue to resume."
                };
            });

        [McpServerTool(Name = "dbg_toggle_bp"), Description(
            "Toggle a breakpoint at the given address: remove it if it exists, add a new execute breakpoint if it does not.")]
        public static object DbgToggleBp(
            [Description("Memory address as hex string (e.g. '0x1234ABCD')")] string address)
            => SafeRunWithAddress(address, addr =>
            {
                var list = Debugger.GetBreakpointList();
                if (list.Contains(addr))
                {
                    Debugger.RemoveBreakpoint(addr);
                    return new { success = true, action = "removed", address = $"0x{addr:X}" };
                }
                Debugger.SetBreakpoint(addr, 1, "bptExecute");
                return new { success = true, action = "added", address = $"0x{addr:X}" };
            });

        [McpServerTool(Name = "dbg_delete_bp"), Description(
            "Remove the breakpoint at the given address (execute, write, or access).")]
        public static object DbgDeleteBp(
            [Description("Memory address of the breakpoint to remove, as hex string (e.g. '0x1234ABCD')")] string address)
            => SafeRunWithAddress(address, addr =>
            {
                Debugger.RemoveBreakpoint(addr);
                return new { success = true, address = $"0x{addr:X}", message = "Breakpoint removed" };
            });

        [McpServerTool(Name = "dbg_bps"), Description(
            "List all currently active breakpoint addresses.")]
        public static object DbgBps()
            => SafeRun(() =>
            {
                var list = Debugger.GetBreakpointList();
                var formatted = new List<string>(list.Count);
                foreach (var bp in list)
                    formatted.Add($"0x{bp:X}");
                return new { success = true, count = list.Count, breakpoints = formatted };
            });

        // ── Hit Tracking ─────────────────────────────────────────────────────

        [McpServerTool(Name = "dbg_get_bp_hits"), Description(
            "Retrieve the recorded hits for a tracked write/access breakpoint set with dbg_add_bp(track_hits=true). " +
            "Each hit contains the instruction pointer, GP registers, and disassembly of the instruction that fired the BP. " +
            "This is the core of the 'find what writes to address' / Code Finder workflow.")]
        public static object DbgGetBpHits(
            [Description("The breakpoint address to get hits for, as hex string (e.g. '0x1234ABCD')")] string address,
            [Description("Maximum number of hits to return (newest first). 0 = return all.")] int maxHits = 0)
            => SafeRunWithAddress(address, addr =>
            {
                if (maxHits < 0)
                    return new { success = false, error = "maxHits must not be negative" };
                string script = $@"
if __mcp_bp_hits == nil then return {{}} end
local addrHex = string.format('%016X', {addr})
local hits = __mcp_bp_hits[addrHex]
if hits == nil then return {{}} end
return hits";
                var result = LuaExecutor.Execute(script);

                // Parse the returned table into structured hit list
                var hits = ParseHitsFromResult(result);

                // Apply maxHits limit (newest = last inserted, so take from end)
                if (maxHits > 0 && hits.Count > maxHits)
                    hits = hits.GetRange(hits.Count - maxHits, maxHits);

                return new { success = true, address = $"0x{addr:X}", hit_count = hits.Count, hits };
            });

        [McpServerTool(Name = "dbg_clear_bp_hits"), Description(
            "Clear the recorded hit history for a tracked breakpoint address.")]
        public static object DbgClearBpHits(
            [Description("The breakpoint address to clear hits for, as hex string")] string address)
            => SafeRunWithAddress(address, addr =>
            {
                string script = $@"
if __mcp_bp_hits ~= nil then
    local addrHex = string.format('%016X', {addr})
    __mcp_bp_hits[addrHex] = {{}}
end
return 'ok'";
                LuaExecutor.Execute(script);
                return new { success = true, address = $"0x{addr:X}", message = "Hit records cleared" };
            });

        // ── Registers ────────────────────────────────────────────────────────

        [McpServerTool(Name = "dbg_gpregs"), Description(
            "Read general-purpose registers from the broken thread's context. " +
            "The debugger must be broken (dbg_is_broken = true) for this to return meaningful values. " +
            "Returns instruction pointer (RIP/EIP), all GPRs, stack pointer, and base pointer. " +
            "For 64-bit targets returns RAX–R15, RIP, RSP, RBP. For 32-bit returns EAX–EDI, EIP, ESP, EBP.")]
        public static object DbgGpregs() =>
            SafeRun(() => ReadRegisters(extraRegisters: false));

        [McpServerTool(Name = "dbg_gpregs_remote"), Description(
            "Read general-purpose registers via a Lua script call to debug_getContext. " +
            "Alternative to dbg_gpregs when the standard context read is unavailable. " +
            "Only meaningful when the debugger is broken on a thread.")]
        public static object DbgGpregsRemote()
            => SafeRun(() =>
            {
                string script = $@"
debug_getContext(false)
return {LuaBuildRegTable}";
                var result = LuaExecutor.Execute(script);
                return new { success = true, registers = result.Value };
            });

        [McpServerTool(Name = "dbg_regs"), Description(
            "Read all standard registers (GP + segment + flags) from the broken thread's context.")]
        public static object DbgRegs() => DbgGpregs();

        [McpServerTool(Name = "dbg_regs_all"), Description(
            "Read all registers including FP0–FP7 and XMM0–XMM15 from the broken thread's context.")]
        public static object DbgRegsAll() =>
            SafeRun(() => ReadRegisters(extraRegisters: true));

        [McpServerTool(Name = "dbg_regs_named"), Description(
            "Read specific named registers from the broken thread's context. " +
            "Provide a comma-separated list of register names, e.g. 'RAX,RBX,RIP' or 'EAX,EIP'.")]
        public static object DbgRegsNamed(
            [Description("Comma-separated register names (e.g. 'RAX,RBX,RIP' or 'EAX,EBX,EIP')")] string registerNames) =>
            SafeRun(() =>
            {
                Dictionary<string, object?> context = GetContextDictionary(extraRegisters: true);
                string[] names = ParseRegisterNames(registerNames);
                var registers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (string name in names)
                {
                    if (!context.TryGetValue(name, out object? value))
                        throw new InvalidOperationException($"Register '{name}' is not available in the broken thread context");
                    registers[name] = $"0x{ToUInt64(value):X}";
                }
                return new { success = true, registers };
            });

        [McpServerTool(Name = "dbg_regs_named_remote"), Description(
            "Read specific named registers via Lua from the broken thread's context. " +
            "Alternative to dbg_regs_named. Provide a comma-separated list of register names.")]
        public static object DbgRegsNamedRemote(
            [Description("Comma-separated register names (e.g. 'RAX,RBX,RIP')")] string registerNames)
            => SafeRun(() =>
            {
                string[] names = ParseRegisterNames(registerNames);
                var builder = new System.Text.StringBuilder();
                builder.AppendLine("debug_getContext(false)");
                builder.AppendLine("local result = {}");
                foreach (string name in names)
                    builder.AppendLine($"if {name} ~= nil then result['{name}'] = string.format('%016X', {name}) end");
                builder.AppendLine("return result");

                var result = LuaExecutor.Execute(builder.ToString());
                return new { success = true, registers = result.Value };
            });

        [McpServerTool(Name = "dbg_regs_remote"), Description(
            "Read all registers from the broken thread's context via Lua. " +
            "Returns both 32-bit and 64-bit register names for the current target architecture.")]
        public static object DbgRegsRemote() => DbgGpregsRemote();

        // ── Continue / Step ──────────────────────────────────────────────────

        [McpServerTool(Name = "dbg_continue"), Description(
            "Continue execution from the current breakpoint. " +
            "method: 'run' = resume normally (default), 'stepinto' = step into next instruction (follows calls), " +
            "'stepover' = step over next instruction (skips calls).")]
        public static object DbgContinue(
            [Description("Continue method: 'run' (default), 'stepinto', or 'stepover'")] string method = "run")
            => SafeRun(() =>
            {
                string ceMethod = method.ToLowerInvariant() switch
                {
                    "run" or "continue" or "co_run" => "co_run",
                    "stepinto" or "step_into" or "co_stepinto" => "co_stepinto",
                    "stepover" or "step_over" or "co_stepover" => "co_stepover",
                    _ => throw new ArgumentException($"Unknown method '{method}'. Use 'run', 'stepinto', or 'stepover'")
                };
                Debugger.ContinueFromBreakpoint(ceMethod);
                return new { success = true, method = ceMethod };
            });

        [McpServerTool(Name = "dbg_step_into"), Description(
            "Single-step into the next instruction (follows CALL instructions into called functions). " +
            "Debugger must be broken. After stepping, use dbg_gpregs to read the new instruction pointer.")]
        public static object DbgStepInto()
            => SafeRun(() =>
            {
                Debugger.ContinueFromBreakpoint("co_stepinto");
                return new { success = true, message = "Step-into issued. Wait for next break, then call dbg_gpregs." };
            });

        [McpServerTool(Name = "dbg_step_over"), Description(
            "Single-step over the next instruction (skips over CALL instructions without entering them). " +
            "Debugger must be broken. After stepping, use dbg_gpregs to read the new instruction pointer.")]
        public static object DbgStepOver()
            => SafeRun(() =>
            {
                Debugger.ContinueFromBreakpoint("co_stepover");
                return new { success = true, message = "Step-over issued. Wait for next break, then call dbg_gpregs." };
            });

        [McpServerTool(Name = "dbg_run_to"), Description(
            "Set a one-shot execute breakpoint at the target address and continue execution. " +
            "Useful for running to a known return address or label without manually managing execute BPs. " +
            "The breakpoint is left in place after firing — remove it with dbg_delete_bp if needed.")]
        public static object DbgRunTo(
            [Description("Target address to run to, as hex string (e.g. '0x1234ABCD')")] string address)
            => SafeRunWithAddress(address, addr =>
            {
                // Set execute BP at target, then continue
                Debugger.SetBreakpoint(addr, 1, "bptExecute");
                Debugger.ContinueFromBreakpoint("co_run");
                return new
                {
                    success = true,
                    target = $"0x{addr:X}",
                    message = "Execute BP set and continued. Execution will break when the address is reached."
                };
            });

        // ── Stack Trace ──────────────────────────────────────────────────────

        [McpServerTool(Name = "dbg_stacktrace"), Description(
            "Read potential return addresses from the call stack when the debugger is broken. " +
            "Reads 'depth' pointer-sized values from the stack pointer (RSP/ESP) upwards and returns " +
            "those that appear to be code addresses (preceded by a CALL instruction). " +
            "Use disassemble_range or get_name_from_address to identify the callers.")]
        public static object DbgStacktrace(
            [Description("Number of stack entries to inspect (default 32, max 128)")] int depth = 32)
            => SafeRun(() =>
            {
                if (depth < 1) depth = 1;
                if (depth > 128) depth = 128;

                string script = $@"
debug_getContext(false)
local is64 = targetIs64Bit()
local sp = is64 and RSP or ESP
local ptrSize = is64 and 8 or 4
local frames = {{}}
for i = 0, {depth - 1} do
    local addr = sp + i * ptrSize
    local val
    if is64 then
        val = readQword(addr)
    else
        val = readInteger(addr)
    end
    if val and val ~= 0 then
        -- Check if the bytes before this address look like a CALL instruction
        local prevBytes = readBytes(val - 6, 6, true)
        local isReturn = false
        if prevBytes then
            -- E8 xx xx xx xx  (CALL rel32 near)
            -- FF D? or FF 1? or FF 5?  (CALL indirect)
            -- 9A (CALL far, rare)
            local b1 = prevBytes[6] -- byte immediately before 'val'
            local b2 = prevBytes[5]
            local b3 = prevBytes[3]
            if b2 == 0xE8 then isReturn = true end
            if b1 ~= nil and (b2 == 0xFF) and (b1 == 0xD0 or b1 == 0xD1 or b1 == 0xD2 or b1 == 0xD3 or b1 == 0xD4 or b1 == 0xD5 or b1 == 0xD6 or b1 == 0xD7) then isReturn = true end
            if b3 ~= nil and b3 == 0xFF then isReturn = true end
        end
        table.insert(frames, {{
            stack_offset = i * ptrSize,
            value = is64 and string.format('%016X', val) or string.format('%08X', val),
            likely_return = isReturn
        }})
    end
end
return frames";
                var result = LuaExecutor.Execute(script);
                return new { success = true, frames = result.Value };
            });

        // ── Read / Write memory while broken ─────────────────────────────────

        [McpServerTool(Name = "dbg_read"), Description(
            "Read memory from the target process (convenience wrapper around read_memory for use while debugging). " +
            "Supports data types: byte, int16, int32, int64, float, double, string, bytes. " +
            "Works regardless of whether the debugger is broken.")]
        public static object DbgRead(
            [Description("Memory address as hex string")] string address,
            [Description("Data type: byte, int16, int32, int64, float, double, string, bytes")] string dataType = DefaultDataType,
            [Description("Length for 'bytes' or 'string' types")] int length = 16)
            => SafeRunWithAddress(address, addr =>
            {
                if (length <= 0)
                    return new { success = false, error = "Length must be greater than zero" };
                object val = dataType.ToLowerInvariant() switch
                {
                    "byte" => MemoryAccess.ReadByte(addr),
                    "int16" or "short" => MemoryAccess.ReadSmallInteger(addr),
                    DefaultDataType or "int" => MemoryAccess.ReadInteger(addr),
                    "int64" or "qword" or "long" => MemoryAccess.ReadQword(addr),
                    "float" => MemoryAccess.ReadFloat(addr),
                    "double" => MemoryAccess.ReadDouble(addr),
                    "string" => MemoryAccess.ReadString(addr, length),
                    "bytes" => BitConverter.ToString(MemoryAccess.ReadBytes(addr, length)).Replace("-", " "),
                    _ => throw new ArgumentException($"Unknown data type '{dataType}'")
                };

                return new { success = true, address = $"0x{addr:X}", dataType, value = val };
            });

        [McpServerTool(Name = "dbg_write"), Description(
            "Write a value to the target process memory (convenience wrapper for use while debugging). " +
            "Supports data types: byte, int16, int32, int64, float, double, string. " +
            "Works regardless of whether the debugger is broken.")]
        public static object DbgWrite(
            [Description("Memory address as hex string")] string address,
            [Description("Value to write as string (e.g. '42', '3.14', 'hello')")] string value,
            [Description("Data type: byte, int16, int32, int64, float, double, string")] string dataType = DefaultDataType)
            => SafeRunWithAddress(address, addr =>
            {
                bool written = dataType.ToLowerInvariant() switch
                {
                    "byte" => MemoryAccess.WriteByte(addr, byte.Parse(value, System.Globalization.CultureInfo.InvariantCulture)),
                    "int16" or "short" => MemoryAccess.WriteSmallInteger(addr, short.Parse(value, System.Globalization.CultureInfo.InvariantCulture)),
                    DefaultDataType or "int" => MemoryAccess.WriteInteger(addr, int.Parse(value, System.Globalization.CultureInfo.InvariantCulture)),
                    "int64" or "qword" or "long" => MemoryAccess.WriteQword(addr, long.Parse(value, System.Globalization.CultureInfo.InvariantCulture)),
                    "float" => MemoryAccess.WriteFloat(addr, float.Parse(value, System.Globalization.CultureInfo.InvariantCulture)),
                    "double" => MemoryAccess.WriteDouble(addr, double.Parse(value, System.Globalization.CultureInfo.InvariantCulture)),
                    "string" => MemoryAccess.WriteString(addr, value),
                    _ => throw new ArgumentException($"Unknown data type '{dataType}'")
                };
                if (!written)
                    throw new MemoryAccessException("Cheat Engine could not write the requested memory");

                return new { success = true, address = $"0x{addr:X}", dataType, value };
            });

        [McpServerTool(Name = "dbg_break_thread"), Description(
            "Request that the debugger break a specific target thread")]
        public static object DbgBreakThread(
            [Description("Target thread identifier")] int threadId) =>
            ToolThread.OnMainThread(() =>
            {
                if (threadId <= 0)
                    return new { success = false, error = "threadId must be greater than zero" };
                Debugger.BreakThread(threadId);
                return new { success = true, threadId };
            });

        [McpServerTool(Name = "dbg_exclude_thread"), Description(
            "Ignore breakpoint hits from a specific target thread")]
        public static object DbgExcludeThread(
            [Description("Target thread identifier")] int threadId) =>
            ToolThread.OnMainThread(() =>
            {
                if (threadId <= 0)
                    return new { success = false, error = "threadId must be greater than zero" };
                Debugger.AddThreadToNoBreakList(threadId);
                return new { success = true, threadId };
            });

        [McpServerTool(Name = "dbg_include_thread"), Description(
            "Remove a target thread from the breakpoint exclusion list")]
        public static object DbgIncludeThread(
            [Description("Target thread identifier")] int threadId) =>
            ToolThread.OnMainThread(() =>
            {
                if (threadId <= 0)
                    return new { success = false, error = "threadId must be greater than zero" };
                Debugger.RemoveThreadFromNoBreakList(threadId);
                return new { success = true, threadId };
            });

        [McpServerTool(Name = "dbg_add_thread_bp"), Description(
            "Set a breakpoint that only applies to one target thread")]
        public static object DbgAddThreadBreakpoint(
            [Description("Target thread identifier")] int threadId,
            [Description("Breakpoint address as hexadecimal")] string address,
            [Description("Watch size in bytes")] int size = 1,
            [Description("Trigger: execute, write, or access")] string trigger = "execute") =>
            ToolThread.OnMainThread(() =>
            {
                if (threadId <= 0)
                    return new { success = false, error = "threadId must be greater than zero" };
                if (!TryParseAddress(address, out ulong parsed))
                    return new { success = false, error = InvalidAddressMsg };
                string ceTrigger = trigger.ToLowerInvariant() switch
                {
                    "execute" => "bptExecute",
                    "write" => "bptWrite",
                    "access" => "bptAccess",
                    _ => ""
                };
                if (ceTrigger.Length == 0)
                    return new { success = false, error = "trigger must be execute, write, or access" };
                if (size is not 1 and not 2 and not 4 and not 8)
                    return new { success = false, error = "size must be 1, 2, 4, or 8" };
                Debugger.SetBreakpointForThread(threadId, parsed, size, ceTrigger);
                return new { success = true, threadId, address = $"0x{parsed:X}", trigger };
            });

        [McpServerTool(Name = "dbg_context_table"), Description(
            "Return the current broken-thread context as reported by Cheat Engine")]
        public static object DbgContextTable(
            [Description("Include floating-point and XMM state")] bool extraRegisters = false) =>
            ToolThread.OnMainThread(() => new
            {
                success = true,
                context = Debugger.GetCurrentContextTable(extraRegisters)
            });

        [McpServerTool(Name = "dbg_read_xmm"), Description(
            "Read the raw 16-byte value of an XMM register from the broken thread")]
        public static object DbgReadXmm(
            [Description("XMM register index (0-15)")] int register) =>
            ToolThread.OnMainThread(() =>
            {
                if (register is < 0 or > 15)
                    return new { success = false, error = "register must be between 0 and 15" };
                ulong pointer = Debugger.GetXmmPointer(register);
                byte[] bytes = MemoryAccess.ReadBytesLocal(pointer, 16);
                return new
                {
                    success = true,
                    register = $"XMM{register}",
                    bytes = BitConverter.ToString(bytes).Replace("-", " ")
                };
            });

        [McpServerTool(Name = "dbg_lbr_enable"), Description(
            "Enable or disable CPU last-branch recording for the kernel debugger")]
        public static object DbgLastBranchEnable(
            [Description("Enable last-branch recording")] bool enabled) =>
            ToolThread.OnMainThread(() =>
            {
                Debugger.SetLastBranchRecording(enabled);
                return new { success = true, enabled };
            });

        [McpServerTool(Name = "dbg_lbr_records"), Description(
            "Read available CPU last-branch records from the current breakpoint")]
        public static object DbgLastBranchRecords(
            [Description("Maximum records to return (1-256)")] int maxRecords = 64) =>
            ToolThread.OnMainThread(() =>
            {
                if (maxRecords is < 1 or > 256)
                    return new { success = false, error = "maxRecords must be between 1 and 256" };
                int available = Debugger.GetMaxLastBranchRecord();
                var records = new List<string>();
                for (int index = 0; index < Math.Min(available, maxRecords); index++)
                    records.Add($"0x{Debugger.GetLastBranchRecord(index):X}");
                return new { success = true, available, records };
            });

        // ── Private helpers ──────────────────────────────────────────────────

        private static object SafeRun(Func<object> action) =>
            ToolThread.OnMainThread(action);

        private static object SafeRunWithAddress(string address, Func<ulong, object> action)
            => SafeRun(() =>
            {
                if (!TryParseAddress(address, out ulong addr))
                    return new { success = false, error = InvalidAddressMsg };
                return action(addr);
            });

        private static string[] ParseRegisterNames(string registerNames)
        {
            string[] names = registerNames.Split(
                    ',',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(name => name.ToUpperInvariant())
                .ToArray();
            if (names.Length == 0)
                throw new ArgumentException("At least one register name is required");

            foreach (string name in names)
            {
                if (!ValidRegisterNames.Contains(name))
                    throw new ArgumentException($"Unknown register name '{name}'");
            }
            return names;
        }

        private static bool TryParseAddress(string address, out ulong result)
        {
            result = 0;
            if (string.IsNullOrWhiteSpace(address)) return false;
            var s = address.Trim();
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ||
                s.StartsWith("0X", StringComparison.OrdinalIgnoreCase))
                s = s[2..];
            return ulong.TryParse(s, System.Globalization.NumberStyles.HexNumber, null, out result);
        }

        private static object ReadRegisters(bool extraRegisters)
        {
            Dictionary<string, object?> context = GetContextDictionary(extraRegisters);
            bool is64 = context.ContainsKey("RIP");
            string prefix = is64 ? "R" : "E";

            string Format(string name)
            {
                if (!context.TryGetValue(name, out object? value))
                    throw new InvalidOperationException($"Register '{name}' is not available in the broken thread context");
                return string.Format(
                    CultureInfo.InvariantCulture,
                    is64 ? "0x{0:X16}" : "0x{0:X8}",
                    ToUInt64(value));
            }

            var result = new Dictionary<string, object>
            {
                ["success"] = true,
                ["bits"] = is64 ? 64 : 32,
                ["ip"] = Format(prefix + "IP"),
                ["ax"] = Format(prefix + "AX"),
                ["bx"] = Format(prefix + "BX"),
                ["cx"] = Format(prefix + "CX"),
                ["dx"] = Format(prefix + "DX"),
                ["si"] = Format(prefix + "SI"),
                ["di"] = Format(prefix + "DI"),
                ["bp"] = Format(prefix + "BP"),
                ["sp"] = Format(prefix + "SP"),
            };

            if (is64)
            {
                for (int index = 8; index <= 15; index++)
                    result["r" + index] = Format("R" + index);
            }

            result["flags"] = context.TryGetValue("EFLAGS", out object? flags)
                ? $"0x{ToUInt64(flags):X8}"
                : "0x00000000";
            return result;
        }

        private static Dictionary<string, object?> GetContextDictionary(bool extraRegisters)
        {
            object? value = Debugger.GetCurrentContextTable(extraRegisters);
            if (value is not Dictionary<string, object?> source)
                throw new InvalidOperationException("Cheat Engine did not return a broken-thread context table");

            return source.ToDictionary(
                item => item.Key.ToUpperInvariant(),
                item => item.Value,
                StringComparer.OrdinalIgnoreCase);
        }

        private static ulong ToUInt64(object? value) => value switch
        {
            long signed => unchecked((ulong)signed),
            int signed => unchecked((ulong)signed),
            ulong unsigned => unsigned,
            double number => checked((ulong)number),
            string text when ulong.TryParse(
                text.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? text[2..] : text,
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture,
                out ulong parsed) => parsed,
            _ => throw new InvalidOperationException($"Unsupported register value '{value}'")
        };

        private static List<Dictionary<string, object>> ParseHitsFromResult(CESDK.Classes.LuaResult result)
        {
            var hits = new List<Dictionary<string, object>>();
            if (result?.Value == null) return hits;

            // LuaExecutor returns array tables as List<object?>, each element is a Dictionary<string, object?>
            if (result.Value is System.Collections.Generic.List<object?> list)
            {
                foreach (var item in list)
                {
                    var hit = new Dictionary<string, object>();
                    if (item is System.Collections.Generic.Dictionary<string, object?> dict)
                    {
                        foreach (var kvp in dict)
                            hit[kvp.Key] = kvp.Value?.ToString() ?? "";
                    }
                    hits.Add(hit);
                }
            }

            return hits;
        }
    }
}
