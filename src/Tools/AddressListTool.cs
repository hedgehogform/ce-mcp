using System;
using System.Collections.Generic;
using System.ComponentModel;
using CESDK.Classes;
using ModelContextProtocol.Server;
using static CESDK.CESDK;

namespace Tools
{
    [McpServerToolType]
    public class AddressListTool
    {
        private AddressListTool() { }

        [McpServerTool(Name = "get_address_list"), Description("Get all memory records in the cheat table")]
        public static object GetAddressList()
        {
            try
            {
                var records = Synchronize(() =>
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

                return new { success = true, count = records.Count, records };
            }
            catch (Exception ex)
            {
                return new { success = false, error = ex.Message };
            }
        }

        [McpServerTool(Name = "add_memory_record"), Description("Add a new memory record to the cheat table")]
        public static object AddMemoryRecord(
            [Description("Description for the memory record")] string description = "New Entry",
            [Description("Memory address")] string address = "0",
            [Description("Variable type (e.g. vtDword, vtFloat, etc.)")] VariableType varType = VariableType.vtDword,
            [Description("Initial value")] string value = "0")
        {
            try
            {
                var record = Synchronize(() =>
                {
                    var al = new AddressList();
                    var r = al.CreateMemoryRecord();
                    r.Description = description;
                    r.Address = address;
                    r.VarType = varType;
                    r.Value = value;
                    return new
                    {
                        id = r.ID,
                        description = r.Description,
                        address = r.Address,
                        value = r.Value
                    };
                });

                return new { success = true, record };
            }
            catch (Exception ex)
            {
                return new { success = false, error = ex.Message };
            }
        }

#pragma warning disable S107 // Methods should not have too many parameters
        [McpServerTool(Name = "update_memory_record"), Description("Update a memory record (find by id, index, or description)")]
        public static object UpdateMemoryRecord(
            [Description("Record ID to find")] int? id = null,
            [Description("Record index to find")] int? index = null,
            [Description("Record description to find")] string? description = null,
            [Description("New description")] string? newDescription = null,
            [Description("New address")] string? newAddress = null,
            [Description("New variable type")] VariableType? newVarType = null,
            [Description("New value")] string? newValue = null,
            [Description("Set active state")] bool? active = null)
        {
            try
            {
                var result = Synchronize(() =>
                {
                    var al = new AddressList();
                    var r = FindRecord(al, id, index, description);
                    if (r == null)
                        return (object?)null;

                    if (!string.IsNullOrEmpty(newDescription))
                        r.Description = newDescription;
                    if (!string.IsNullOrEmpty(newAddress))
                        r.Address = newAddress;
                    if (newVarType.HasValue)
                        r.VarType = newVarType.Value;
                    if (!string.IsNullOrEmpty(newValue))
                        r.Value = newValue;
                    if (active.HasValue)
                        r.Active = active.Value;

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
                    return new { success = false, error = "Record not found" };

                return new { success = true, record = result };
            }
            catch (Exception ex)
            {
                return new { success = false, error = ex.Message };
            }
        }
#pragma warning restore S107

        [McpServerTool(Name = "delete_memory_record"), Description("Delete a memory record (find by id, index, or description)")]
        public static object DeleteMemoryRecord(
            [Description("Record ID to find")] int? id = null,
            [Description("Record index to find")] int? index = null,
            [Description("Record description to find")] string? description = null)
        {
            try
            {
                var found = Synchronize(() =>
                {
                    var al = new AddressList();
                    var r = FindRecord(al, id, index, description);
                    if (r == null)
                        return false;

                    al.DeleteMemoryRecord(r);
                    return true;
                });

                if (!found)
                    return new { success = false, error = "Record not found" };

                return new { success = true };
            }
            catch (Exception ex)
            {
                return new { success = false, error = ex.Message };
            }
        }

        [McpServerTool(Name = "clear_address_list"), Description("Clear all memory records from the cheat table")]
        public static object ClearAddressList()
        {
            try
            {
                Synchronize(() =>
                {
                    var al = new AddressList();
                    al.Clear();
                });
                return new { success = true };
            }
            catch (Exception ex)
            {
                return new { success = false, error = ex.Message };
            }
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
}
