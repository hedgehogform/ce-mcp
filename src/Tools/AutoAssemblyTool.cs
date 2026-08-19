using System;
using System.ComponentModel;
using System.Linq;
using CESDK.Classes;
using ModelContextProtocol.Server;

namespace Tools
{
    /// <summary>
    /// Assembly and auto-assembly tools for code injection and modification.
    /// </summary>
    [McpServerToolType]
    public class AutoAssemblyTool
    {
        private AutoAssemblyTool() { }

        [McpServerTool(Name = "assemble"), Description("Assemble a single instruction into bytes (e.g. 'nop', 'mov eax,ebx', 'jmp 0x12345')")]
        public static object Assemble(
            [Description("Assembly instruction to assemble (e.g. 'nop', 'mov eax,ebx')")] string instruction,
            [Description("Address to assemble at (affects relative addressing). Hex string e.g. '0x401000'")] string? address = null,
            [Description("Preference: 0=none, 1=short, 2=long, 3=far")] int assemblePreference = 0,
            [Description("Skip relative branch range checks")] bool skipRangeCheck = false)
        {
            return ToolThread.OnMainThread(() =>
            {
                if (string.IsNullOrWhiteSpace(instruction))
                    return new { success = false, error = "Instruction is required" };

                if (assemblePreference is < 0 or > 3)
                    return new { success = false, error = "Assemble preference must be between 0 and 3" };

                ulong addr = 0;
                if (!string.IsNullOrEmpty(address) && !TryParseAddress(address, out addr))
                    return new { success = false, error = "Invalid address format" };

                var bytes = Assembler.Assemble(instruction, addr, assemblePreference, skipRangeCheck);
                return new
                {
                    success = true,
                    bytes = bytes.Select(b => $"{b:X2}").ToArray(),
                    hex = string.Join(" ", bytes.Select(b => $"{b:X2}")),
                    size = bytes.Length
                };
            });
        }

        [McpServerTool(Name = "auto_assemble"), Description(
            "Enable or disable a Cheat Engine Auto Assembler script. " +
            "Omit disableId to execute [ENABLE]; retain the returned disableId and pass it with the same script to execute [DISABLE].")]
        public static object AutoAssemble(
            [Description("Auto Assembler script text with [ENABLE]/[DISABLE] sections")] string script,
            [Description("If true, assemble into Cheat Engine instead of the target; used only while enabling")] bool targetSelf = false,
            [Description("Disable ID returned by the matching enable call; omit to enable")] string? disableId = null)
        {
            return ToolThread.OnMainThread(() =>
            {
                if (string.IsNullOrWhiteSpace(script))
                    return new { success = false, error = "Script is required" };

                if (!string.IsNullOrWhiteSpace(disableId))
                {
                    Assembler.AutoAssembleDisable(script, disableId);
                    return new { success = true, mode = "disabled", disableId, warnings = Array.Empty<string>() };
                }
                if (!targetSelf)
                    RequireAttachedProcess();

                AutoAssembleResult result = Assembler.AutoAssemble(script, targetSelf);
                return new { success = true, mode = "enabled", disableId = result.DisableId, warnings = result.Warnings.ToArray() };
            });
        }

        [McpServerTool(Name = "auto_assemble_check"), Description("Check an Auto Assembler script for syntax errors without executing it")]
        public static object AutoAssembleCheck(
            [Description("Auto assembler script text to check")] string script,
            [Description("Check in enable mode (true) or disable mode (false)")] bool enable = true,
            [Description("If true, check against CE process instead of target")] bool targetSelf = false)
        {
            return ToolThread.OnMainThread(() =>
            {
                if (string.IsNullOrWhiteSpace(script))
                    return new { success = false, error = "Script is required" };
                if (!targetSelf)
                    RequireAttachedProcess();

                var (syntaxOk, errorMessage) = Assembler.AutoAssembleCheck(script, enable, targetSelf);
                if (syntaxOk)
                    return new { success = true, syntaxValid = true };
                else
                    return new { success = true, syntaxValid = false, error = errorMessage ?? "Unknown syntax error" };
            });
        }

        private static void RequireAttachedProcess()
        {
            if (CESDK.Classes.Process.GetOpenedProcessID() <= 0)
                throw new InvalidOperationException("No process is attached. Open a process first.");
        }

        private static bool TryParseAddress(string address, out ulong result) =>
            AddressParser.TryParseHexAddress(address, out result);
    }
}
