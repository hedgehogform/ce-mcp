using System;
using System.Collections.Generic;
using System.ComponentModel;
using CESDK.Classes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Tools
{
    public static class AddressListTool
    {
        public static void MapAddressListApi(this WebApplication app)
        {
            // GET /api/addresslist - Get all memory records
            app.MapGet("/api/addresslist", () =>
            {
                try
                {
                    var records = CESDK.CESDK.Synchronize(() =>
                    {
                        var al = new AddressList();
                        var result = new List<object>();
                        for (int i = 0; i < al.Count; i++)
                        {
                            var r = al.GetMemoryRecord(i);
                            result.Add(new
                            {
                                id = r.ID,
                                index = r.Index,
                                description = r.Description,
                                address = r.Address,
                                value = r.Value,
                                active = r.Active
                            });
                        }
                        return result;
                    });

                    return Results.Ok(new { success = true, count = records.Count, records });
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
                    var record = CESDK.CESDK.Synchronize(() =>
                    {
                        var al = new AddressList();
                        var r = al.CreateMemoryRecord();
                        r.Description = request.Description;
                        r.Address = request.Address;
                        r.VarType = request.VarType;
                        r.Value = request.Value;
                        return new
                        {
                            id = r.ID,
                            description = r.Description,
                            address = r.Address,
                            value = r.Value
                        };
                    });

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
                    var result = CESDK.CESDK.Synchronize(() =>
                    {
                        var al = new AddressList();
                        var r = FindRecord(al, request.Id, request.Index, request.Description);
                        if (r == null)
                            return (object?)null;

                        if (!string.IsNullOrEmpty(request.NewDescription))
                            r.Description = request.NewDescription;
                        if (!string.IsNullOrEmpty(request.NewAddress))
                            r.Address = request.NewAddress;
                        if (request.NewVarType.HasValue)
                            r.VarType = request.NewVarType.Value;
                        if (!string.IsNullOrEmpty(request.NewValue))
                            r.Value = request.NewValue;
                        if (request.Active.HasValue)
                            r.Active = request.Active.Value;

                        return new
                        {
                            id = r.ID,
                            description = r.Description,
                            address = r.Address,
                            value = r.Value,
                            active = r.Active
                        };
                    });

                    if (result == null)
                        return Results.Ok(new { success = false, error = "Record not found" });

                    return Results.Ok(new { success = true, record = result });
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
                    var found = CESDK.CESDK.Synchronize(() =>
                    {
                        var al = new AddressList();
                        var r = FindRecord(al, request.Id, request.Index, request.Description);
                        if (r == null)
                            return false;

                        al.DeleteMemoryRecord(r);
                        return true;
                    });

                    if (!found)
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
                    CESDK.CESDK.Synchronize(() =>
                    {
                        var al = new AddressList();
                        al.Clear();
                    });
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

        private static MemoryRecord? FindRecord(AddressList al, int? id, int? index, string? description)
        {
            if (id.HasValue)
                return al.GetMemoryRecordByID(id.Value);
            if (index.HasValue)
                return al.GetMemoryRecord(index.Value);
            if (!string.IsNullOrEmpty(description))
                return al.GetMemoryRecordByDescription(description);

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
