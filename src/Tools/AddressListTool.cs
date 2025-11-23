using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json;
using CESDK;
using CESDK.Classes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Tools
{
    public static class AddressListTool
    {
        /// <summary>
        /// Executes Lua code wrapped in synchronize() for GUI thread safety
        /// </summary>
        private static string RunSync(string luaCode)
        {
            var lua = PluginContext.Lua;
            var wrappedCode = $"return synchronize(function() {luaCode} end)";

            lua.DoString(wrappedCode);

            string result = "";
            if (lua.IsString(-1))
                result = lua.ToString(-1) ?? "";
            else if (lua.IsNumber(-1))
                result = lua.ToNumber(-1).ToString();

            lua.SetTop(0);
            return result;
        }

        private static string Escape(string s) =>
            s.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\"", "\\\"");

        public static void MapAddressListApi(this WebApplication app)
        {
            // GET /api/addresslist - Get all memory records
            app.MapGet("/api/addresslist", () =>
            {
                try
                {
                    var json = RunSync(@"
                        local al = getAddressList()
                        local t = {}
                        for i = 0, al.Count - 1 do
                            local r = al.getMemoryRecord(i)
                            table.insert(t, string.format(
                                '{""id"":%d,""index"":%d,""description"":""%s"",""address"":""%s"",""value"":""%s"",""active"":%s}',
                                r.ID, r.Index,
                                (r.Description or ''):gsub('""', '\\""'),
                                (r.Address or ''):gsub('""', '\\""'),
                                tostring(r.Value or ''):gsub('""', '\\""'),
                                r.Active and 'true' or 'false'
                            ))
                        end
                        return '[' .. table.concat(t, ',') .. ']'
                    ");

                    var records = JsonSerializer.Deserialize<JsonElement>(string.IsNullOrEmpty(json) ? "[]" : json);
                    return Results.Ok(new { success = true, count = records.GetArrayLength(), records });
                }
                catch (Exception ex)
                {
                    return Results.Ok(new { success = false, error = ex.Message });
                }
            })
            .WithName("GetAddressList")
            .WithDescription("Get all memory records in the cheat table")
            .WithOpenApi();

            // POST /api/addresslist/add - Add a new memory record
            app.MapPost("/api/addresslist/add", (AddRecordRequest request) =>
            {
                try
                {
                    var desc = Escape(request.Description);
                    var addr = Escape(request.Address);
                    var varType = (int)request.VarType;
                    var value = Escape(request.Value);

                    var json = RunSync($@"
                        local al = getAddressList()
                        local r = al.createMemoryRecord()
                        r.Description = '{desc}'
                        r.Address = '{addr}'
                        r.Type = {varType}
                        r.Value = '{value}'
                        return string.format(
                            '{{""id"":%d,""description"":""%s"",""address"":""%s"",""value"":""%s""}}',
                            r.ID, (r.Description or ''):gsub('""', '\\""'), (r.Address or ''):gsub('""', '\\""'), tostring(r.Value or ''):gsub('""', '\\""')
                        )
                    ");

                    var record = JsonSerializer.Deserialize<JsonElement>(json);
                    return Results.Ok(new { success = true, record });
                }
                catch (Exception ex)
                {
                    return Results.Ok(new { success = false, error = ex.Message });
                }
            })
            .WithName("AddMemoryRecord")
            .WithDescription("Add a new memory record to the cheat table")
            .WithOpenApi();

            // POST /api/addresslist/update - Update a memory record
            app.MapPost("/api/addresslist/update", (UpdateRecordRequest request) =>
            {
                try
                {
                    var findCode = GetFindCode(request.Id, request.Index, request.Description);
                    var updates = new List<string>();

                    if (!string.IsNullOrEmpty(request.NewDescription))
                        updates.Add($"r.Description = '{Escape(request.NewDescription)}'");
                    if (!string.IsNullOrEmpty(request.NewAddress))
                        updates.Add($"r.Address = '{Escape(request.NewAddress)}'");
                    if (request.NewVarType.HasValue)
                        updates.Add($"r.Type = {(int)request.NewVarType.Value}");
                    if (!string.IsNullOrEmpty(request.NewValue))
                        updates.Add($"r.Value = '{Escape(request.NewValue)}'");
                    if (request.Active.HasValue)
                        updates.Add($"r.Active = {(request.Active.Value ? "true" : "false")}");

                    var updateCode = string.Join("\n", updates);

                    var json = RunSync($@"
                        local al = getAddressList()
                        local r = {findCode}
                        if r == nil then return '{{""error"":""not found""}}' end
                        {updateCode}
                        return string.format(
                            '{{""id"":%d,""description"":""%s"",""address"":""%s"",""value"":""%s"",""active"":%s}}',
                            r.ID, (r.Description or ''):gsub('""', '\\""'), (r.Address or ''):gsub('""', '\\""'), tostring(r.Value or ''):gsub('""', '\\""'), r.Active and 'true' or 'false'
                        )
                    ");

                    if (json.Contains("\"error\""))
                        return Results.Ok(new { success = false, error = "Record not found" });

                    var record = JsonSerializer.Deserialize<JsonElement>(json);
                    return Results.Ok(new { success = true, record });
                }
                catch (Exception ex)
                {
                    return Results.Ok(new { success = false, error = ex.Message });
                }
            })
            .WithName("UpdateMemoryRecord")
            .WithDescription("Update a memory record (find by id, index, or description)")
            .WithOpenApi();

            // POST /api/addresslist/delete - Delete a memory record
            app.MapPost("/api/addresslist/delete", (DeleteRecordRequest request) =>
            {
                try
                {
                    var findCode = GetFindCode(request.Id, request.Index, request.Description);

                    var result = RunSync($@"
                        local al = getAddressList()
                        local r = {findCode}
                        if r == nil then return 'notfound' end
                        r.destroy()
                        return 'ok'
                    ");

                    if (result == "notfound")
                        return Results.Ok(new { success = false, error = "Record not found" });

                    return Results.Ok(new { success = true });
                }
                catch (Exception ex)
                {
                    return Results.Ok(new { success = false, error = ex.Message });
                }
            })
            .WithName("DeleteMemoryRecord")
            .WithDescription("Delete a memory record (find by id, index, or description)")
            .WithOpenApi();

            // POST /api/addresslist/clear - Clear all records
            app.MapPost("/api/addresslist/clear", () =>
            {
                try
                {
                    RunSync(@"
                        local al = getAddressList()
                        for i = al.Count - 1, 0, -1 do
                            al.getMemoryRecord(i).destroy()
                        end
                        return 'ok'
                    ");
                    return Results.Ok(new { success = true });
                }
                catch (Exception ex)
                {
                    return Results.Ok(new { success = false, error = ex.Message });
                }
            })
            .WithName("ClearAddressList")
            .WithDescription("Clear all memory records from the cheat table")
            .WithOpenApi();
        }

        private static string GetFindCode(int? id, int? index, string? description)
        {
            if (id.HasValue)
                return $"al.getMemoryRecordByID({id.Value})";
            if (index.HasValue)
                return $"al.getMemoryRecord({index.Value})";
            if (!string.IsNullOrEmpty(description))
                return $"al.getMemoryRecordByDescription('{Escape(description)}')";

            throw new ArgumentException("Provide id, index, or description to find the record");
        }
    }

    public record AddRecordRequest(
        [property: DefaultValue("New Entry")]
        string Description = "New Entry",

        [property: DefaultValue("0")]
        string Address = "0",

        [property: DefaultValue(VariableType.vtDword)]
        VariableType VarType = VariableType.vtDword,

        [property: DefaultValue("0")]
        string Value = "0"
    );

    public record UpdateRecordRequest(
        [property: DefaultValue(null)]
        int? Id = null,

        [property: DefaultValue(null)]
        int? Index = null,

        [property: DefaultValue(null)]
        string? Description = null,

        [property: DefaultValue(null)]
        string? NewDescription = null,

        [property: DefaultValue(null)]
        string? NewAddress = null,

        [property: DefaultValue(null)]
        VariableType? NewVarType = null,

        [property: DefaultValue(null)]
        string? NewValue = null,

        [property: DefaultValue(null)]
        bool? Active = null
    );

    public record DeleteRecordRequest(
        [property: DefaultValue(null)]
        int? Id = null,

        [property: DefaultValue(null)]
        int? Index = null,

        [property: DefaultValue(null)]
        string? Description = null
    );
}
