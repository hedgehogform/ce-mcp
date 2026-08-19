using System;
using System.ComponentModel;
using CESDK.Classes;
using ModelContextProtocol.Server;

namespace Tools
{
    /// <summary>RTTI lookup and user-defined symbol management.</summary>
    [McpServerToolType]
    public class SymbolRegistryTool
    {
        private SymbolRegistryTool() { }

        [McpServerTool(Name = "get_rtti_class_name"), Description("Resolve a likely class name for an address using RTTI")]
        public static object GetRttiClassName(
            [Description("Target address as hexadecimal")] string address) =>
            ToolThread.OnMainThread(() =>
            {
                if (!AddressParser.TryParseHexAddress(address, out ulong parsed))
                    return new { success = false, error = "Invalid address format" };
                string? className = SymbolRegistry.GetRttiClassName(parsed);
                return new { success = true, address = $"0x{parsed:X}", className, found = className is not null };
            });

        [McpServerTool(Name = "register_symbol"), Description("Register a user-defined Cheat Engine symbol")]
        public static object RegisterSymbol(
            [Description("Symbol name")] string name,
            [Description("Address as hexadecimal")] string address,
            [Description("Do not persist this symbol in saved cheat tables")] bool doNotSave = false) =>
            ToolThread.OnMainThread(() =>
            {
                if (string.IsNullOrWhiteSpace(name))
                    return new { success = false, error = "Name is required" };
                if (!AddressParser.TryParseHexAddress(address, out ulong parsed))
                    return new { success = false, error = "Invalid address format" };
                SymbolRegistry.Register(name, parsed, doNotSave);
                return new { success = true, name, address = $"0x{parsed:X}" };
            });

        [McpServerTool(Name = "unregister_symbol"), Description("Unregister a user-defined Cheat Engine symbol")]
        public static object UnregisterSymbol(
            [Description("Symbol name")] string name) =>
            ToolThread.OnMainThread(() =>
            {
                if (string.IsNullOrWhiteSpace(name))
                    return new { success = false, error = "Name is required" };
                SymbolRegistry.Unregister(name);
                return new { success = true };
            });

        [McpServerTool(Name = "enum_registered_symbols"), Description("List user-defined Cheat Engine symbols")]
        public static object EnumerateRegisteredSymbols() =>
            ToolThread.OnMainThread(() => new { success = true, symbols = SymbolRegistry.Enumerate() });
    }
}
