using System;
using System.ComponentModel;
using CESDK.Classes;
using ModelContextProtocol.Server;

namespace Tools
{
    /// <summary>Cheat Engine table persistence.</summary>
    [McpServerToolType]
    public class CheatTableTool
    {
        private CheatTableTool() { }

        [McpServerTool(Name = "load_cheat_table"), Description(
            "Load a .CT or .CETRAINER file into Cheat Engine; merge=false replaces the current table")]
        public static object LoadCheatTable(
            [Description("Table file path")] string filename,
            [Description("Merge into the current table instead of replacing it")] bool merge = false) =>
            ToolThread.OnMainThread(() =>
            {
                if (string.IsNullOrWhiteSpace(filename))
                    return new { success = false, error = "Filename is required" };
                CheatTable.Load(filename, merge);
                return new { success = true, filename, merge };
            });

        [McpServerTool(Name = "save_cheat_table"), Description(
            "Save the current Cheat Engine table to a .CT or .CETRAINER file")]
        public static object SaveCheatTable(
            [Description("Destination table file path")] string filename,
            [Description("Protect a CETRAINER against normal reading")] bool protect = false,
            [Description("Keep designer forms active while saving")] bool dontDeactivateDesignerForms = false) =>
            ToolThread.OnMainThread(() =>
            {
                if (string.IsNullOrWhiteSpace(filename))
                    return new { success = false, error = "Filename is required" };
                CheatTable.Save(filename, protect, dontDeactivateDesignerForms);
                return new { success = true, filename };
            });
    }
}
