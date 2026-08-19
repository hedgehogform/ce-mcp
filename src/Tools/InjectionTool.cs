using System;
using System.Collections.Generic;
using System.ComponentModel;
using CESDK.Classes;
using ModelContextProtocol.Server;

namespace Tools
{
    /// <summary>Library injection, remote execution, target C compilation, and script generation.</summary>
    [McpServerToolType]
    public class InjectionTool
    {
        private InjectionTool() { }

        [McpServerTool(Name = "inject_library"), Description("Inject a native library into the attached process")]
        public static object InjectLibrary(
            [Description("Library file path")] string filename,
            [Description("Do not wait for symbol reload after injection")] bool skipSymbolReloadWait = false) =>
            ToolThread.OnMainThread(() =>
            {
                if (string.IsNullOrWhiteSpace(filename))
                    return new { success = false, error = "Filename is required" };
                RequireAttachedProcess();
                bool injected = Injection.InjectLibrary(filename, skipSymbolReloadWait);
                return injected
                    ? new { success = true }
                    : (object)new { success = false, error = "Cheat Engine reported that library injection failed" };
            });

        [McpServerTool(Name = "inject_dotnet_assembly"), Description(
            "Inject a managed assembly and invoke a static method in the attached process")]
        public static object InjectDotNetAssembly(
            [Description("Managed assembly file path")] string dllPath,
            [Description("Fully qualified class name")] string fullClassName,
            [Description("Static method name")] string methodName,
            [Description("String parameter passed to the method")] string parameterString = "",
            [Description("Optional timeout in milliseconds")] int? timeout = null) =>
            ToolThread.OnMainThread(() =>
            {
                if (string.IsNullOrWhiteSpace(dllPath) ||
                    string.IsNullOrWhiteSpace(fullClassName) ||
                    string.IsNullOrWhiteSpace(methodName))
                    return new { success = false, error = "DLL path, class name, and method name are required" };
                if (timeout < 0)
                    return new { success = false, error = "Timeout must not be negative" };
                RequireAttachedProcess();
                Injection.InjectDotNet(dllPath, fullClassName, methodName, parameterString, timeout);
                return new { success = true };
            });

        [McpServerTool(Name = "execute_remote_function"), Description(
            "Execute a one-parameter function in the target process and wait for its result")]
        public static object ExecuteRemoteFunction(
            [Description("Function address or symbol expression")] string address,
            [Description("Integer function parameter")] long parameter = 0,
            [Description("Timeout in milliseconds; -1 waits indefinitely")] int timeout = -1) =>
            ToolThread.OnMainThread(() =>
            {
                if (string.IsNullOrWhiteSpace(address))
                    return new { success = false, error = "Address is required" };
                if (timeout < -1)
                    return new { success = false, error = "Timeout must be -1 or greater" };
                RequireAttachedProcess();
                ulong resolved = AddressResolver.GetAddress(address);
                long result = Injection.Execute(resolved, parameter, timeout);
                return new { success = true, address = $"0x{resolved:X}", result };
            });

        [McpServerTool(Name = "execute_remote_function_ex"), Description(
            "Execute a stdcall or cdecl target function with integer parameters")]
        public static object ExecuteRemoteFunctionExtended(
            [Description("Function address or symbol expression")] string address,
            [Description("Integer parameters in call order")] IReadOnlyList<long>? parameters = null,
            [Description("Calling convention: 0=stdcall, 1=cdecl")] int callMethod = 0,
            [Description("Timeout in milliseconds; -1 waits indefinitely; 0 is fire-and-forget and may leak CE call memory")] int timeout = -1) =>
            ToolThread.OnMainThread(() =>
            {
                if (string.IsNullOrWhiteSpace(address))
                    return new { success = false, error = "Address is required" };
                if (callMethod is not 0 and not 1)
                    return new { success = false, error = "callMethod must be 0 or 1" };
                if (timeout < -1)
                    return new { success = false, error = "Timeout must be -1 or greater" };
                if (parameters?.Count > 32)
                    return new { success = false, error = "At most 32 parameters are supported" };
                RequireAttachedProcess();
                ulong resolved = AddressResolver.GetAddress(address);
                long? result = Injection.ExecuteExtended(callMethod, timeout, resolved, parameters ?? []);
                return new { success = true, address = $"0x{resolved:X}", result };
            });

        [McpServerTool(Name = "generate_api_hook_script"), Description("Generate an Auto Assembler API-hook script without executing it")]
        public static object GenerateApiHookScript(
            [Description("Address or symbol to hook")] string address,
            [Description("Address or symbol the hook should jump to")] string jumpTarget,
            [Description("Optional symbol that receives the new call address")] string? newCallAddress = null,
            [Description("Optional architecture extension")] string? extension = null,
            [Description("Generate for the Cheat Engine process itself")] bool targetSelf = false) =>
            ToolThread.OnMainThread(() =>
            {
                if (string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(jumpTarget))
                    return new { success = false, error = "Address and jump target are required" };
                if (!targetSelf)
                    RequireAttachedProcess();
                string script = Injection.GenerateApiHookScript(
                    address,
                    jumpTarget,
                    newCallAddress,
                    extension,
                    targetSelf);
                return new { success = true, script };
            });

        [McpServerTool(Name = "generate_code_injection_script"), Description(
            "Generate a standard Auto Assembler code-injection script without executing it")]
        public static object GenerateCodeInjectionScript(
            [Description("Address or symbol where code should be injected")] string address,
            [Description("Generate a far-jump variant")] bool farJump = false) =>
            ToolThread.OnMainThread(() =>
            {
                if (string.IsNullOrWhiteSpace(address))
                    return new { success = false, error = "Address is required" };
                RequireAttachedProcess();
                return new { success = true, script = Injection.GenerateCodeInjectionScript(address, farJump) };
            });

        [McpServerTool(Name = "compile_c"), Description(
            "Compile C source with Cheat Engine's target compiler and return exported symbols")]
        public static object CompileC(
            [Description("C source text")] string source,
            [Description("Optional target base address as hexadecimal")] string? address = null,
            [Description("Compile for the Cheat Engine process instead of the target")] bool targetSelf = false,
            [Description("Compile for kernel mode")] bool kernelMode = false,
            [Description("Disable debug information")] bool noDebug = false) =>
            ToolThread.OnMainThread(() =>
            {
                if (string.IsNullOrWhiteSpace(source))
                    return new { success = false, error = "Source is required" };
                if (source.Length > 1_000_000)
                    return new { success = false, error = "Source is limited to 1000000 characters" };
                if (!TryOptionalAddress(address, out ulong? parsed))
                    return new { success = false, error = "Invalid address format" };
                if (!targetSelf)
                    RequireAttachedProcess();
                object? symbols = Injection.CompileC(source, parsed, targetSelf, kernelMode, noDebug);
                return new { success = true, symbols };
            });

        private static void RequireAttachedProcess()
        {
            if (CESDK.Classes.Process.GetOpenedProcessID() <= 0)
                throw new InvalidOperationException("No process is attached. Open a process first.");
        }

        private static bool TryOptionalAddress(string? value, out ulong? address)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                address = null;
                return true;
            }
            if (AddressParser.TryParseHexAddress(value, out ulong parsed))
            {
                address = parsed;
                return true;
            }
            address = null;
            return false;
        }
    }
}
