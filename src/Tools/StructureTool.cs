using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CESDK.Classes;
using ModelContextProtocol.Server;

namespace Tools
{
    /// <summary>Input for a Cheat Engine structure element.</summary>
    public sealed class StructureElementInput
    {
        public long Offset { get; set; }
        public string Name { get; set; } = string.Empty;
        public int VariableType { get; set; }
        public int ByteSize { get; set; }
        public string ChildClassName { get; set; } = string.Empty;
    }

    /// <summary>Global Structure Dissect definitions and comparisons.</summary>
    [McpServerToolType]
    public class StructureTool
    {
        private StructureTool() { }

        [McpServerTool(Name = "list_structures"), Description("List global Cheat Engine structures and their elements")]
        public static object ListStructures() =>
            ToolThread.OnMainThread(() => new { success = true, structures = StructureManager.List() });

        [McpServerTool(Name = "get_structure"), Description("Get one Cheat Engine structure by its case-sensitive name")]
        public static object GetStructure(
            [Description("Structure name")] string name) =>
            ToolThread.OnMainThread(() =>
            {
                if (string.IsNullOrWhiteSpace(name))
                    return new { success = false, error = "Name is required" };
                StructureInfo? structure = StructureManager.Get(name);
                return structure is null
                    ? new { success = false, error = $"Structure '{name}' was not found" }
                    : (object)new { success = true, structure };
            });

        [McpServerTool(Name = "create_structure"), Description("Create and register a global Cheat Engine structure")]
        public static object CreateStructure(
            [Description("Unique structure name")] string name,
            [Description("Initial structure elements")] IReadOnlyList<StructureElementInput>? elements = null,
            [Description("Keep the structure internal and hidden from the global dropdown")] bool isInternal = false) =>
            ToolThread.OnMainThread(() =>
            {
                if (string.IsNullOrWhiteSpace(name))
                    return new { success = false, error = "Name is required" };
                if (elements?.Count > 512)
                    return new { success = false, error = "A structure is limited to 512 input elements per call" };
                StructureInfo structure = StructureManager.Create(
                    name,
                    elements?.Select(ToDefinition).ToList() ?? [],
                    isInternal);
                return new { success = true, structure };
            });

        [McpServerTool(Name = "delete_structure"), Description("Remove and destroy a global Cheat Engine structure")]
        public static object DeleteStructure(
            [Description("Structure name")] string name) =>
            ToolThread.OnMainThread(() =>
            {
                if (string.IsNullOrWhiteSpace(name))
                    return new { success = false, error = "Name is required" };
                StructureManager.Remove(name);
                return new { success = true };
            });

        [McpServerTool(Name = "add_structure_element"), Description("Add an element to an existing Cheat Engine structure")]
        public static object AddStructureElement(
            [Description("Structure name")] string structureName,
            [Description("Element definition")] StructureElementInput element) =>
            ToolThread.OnMainThread(() =>
            {
                if (string.IsNullOrWhiteSpace(structureName))
                    return new { success = false, error = "Structure name is required" };
                if (element is null)
                    return new { success = false, error = "Element is required" };
                StructureInfo structure = StructureManager.AddElement(structureName, ToDefinition(element));
                return new { success = true, structure };
            });

        [McpServerTool(Name = "remove_structure_element"), Description("Remove a structure element by zero-based index")]
        public static object RemoveStructureElement(
            [Description("Structure name")] string structureName,
            [Description("Zero-based element index")] int index) =>
            ToolThread.OnMainThread(() =>
            {
                if (string.IsNullOrWhiteSpace(structureName))
                    return new { success = false, error = "Structure name is required" };
                if (index < 0)
                    return new { success = false, error = "Index must not be negative" };
                StructureInfo structure = StructureManager.RemoveElement(structureName, index);
                return new { success = true, structure };
            });

        [McpServerTool(Name = "autoguess_structure"), Description(
            "Fill an existing structure by asking Cheat Engine to infer fields from a target address")]
        public static object AutoGuessStructure(
            [Description("Structure name")] string structureName,
            [Description("Target base address as hexadecimal")] string baseAddress,
            [Description("Starting offset within the structure")] int offset = 0,
            [Description("Maximum bytes to infer (1-1048576)")] int size = 4096) =>
            ToolThread.OnMainThread(() =>
            {
                if (string.IsNullOrWhiteSpace(structureName))
                    return new { success = false, error = "Structure name is required" };
                if (!AddressParser.TryParseHexAddress(baseAddress, out ulong address))
                    return new { success = false, error = "Invalid base address format" };
                if (size is < 1 or > 1_048_576)
                    return new { success = false, error = "Size must be between 1 and 1048576" };
                StructureInfo structure = StructureManager.AutoGuess(structureName, address, offset, size);
                return new { success = true, structure };
            });

        [McpServerTool(Name = "compare_structures"), Description(
            "Compare interpreted fields of one structure at two target addresses")]
        public static object CompareStructures(
            [Description("Structure name")] string structureName,
            [Description("First base address as hexadecimal")] string firstAddress,
            [Description("Second base address as hexadecimal")] string secondAddress,
            [Description("Maximum differences to return (1-1000)")] int maxDifferences = 100) =>
            ToolThread.OnMainThread(() =>
            {
                if (string.IsNullOrWhiteSpace(structureName))
                    return new { success = false, error = "Structure name is required" };
                if (!AddressParser.TryParseHexAddress(firstAddress, out ulong first) ||
                    !AddressParser.TryParseHexAddress(secondAddress, out ulong second))
                    return new { success = false, error = "Invalid address format" };
                if (maxDifferences is < 1 or > 1000)
                    return new { success = false, error = "maxDifferences must be between 1 and 1000" };
                List<StructureDifference> differences = StructureManager.Compare(
                    structureName,
                    first,
                    second,
                    maxDifferences);
                return new { success = true, count = differences.Count, differences };
            });

        private static StructureElementDefinition ToDefinition(StructureElementInput input) =>
            new(input.Offset, input.Name, input.VariableType, input.ByteSize, input.ChildClassName);
    }
}
