using System;
using System.Linq;
using CESDK.Classes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Tools
{
    public static class ProcessTool
    {
        /// <summary>
        /// Maps process-related API endpoints
        /// </summary>
        public static void MapProcessApi(this WebApplication app)
        {
            // GET /api/process/list - Get list of all running processes
            app.MapGet("/api/process/list", () =>
            {
                try
                {
                    var processes = GetProcessList();
                    return Results.Ok(new
                    {
                        success = true,
                        processes = processes.Select(p => new
                        {
                            processId = p.ProcessId,
                            processName = p.ProcessName
                        })
                    });
                }
                catch (Exception ex)
                {
                    return Results.Ok(new { success = false, error = ex.Message });
                }
            })
            .WithName("GetProcessList")
            .WithDescription("Get the list of all running processes")
            .WithOpenApi();

            // POST /api/process/open - Open a process by ID or name
            app.MapPost("/api/process/open", (ProcessOpenRequest request) =>
            {
                try
                {
                    OpenProcess(request.Process ?? "");
                    return Results.Ok(new { success = true });
                }
                catch (Exception ex)
                {
                    return Results.Ok(new { success = false, error = ex.Message });
                }
            })
            .WithName("OpenProcess")
            .WithDescription("Open a process by ID or name")
            .WithOpenApi();

            // GET /api/process/current - Get currently opened process status
            app.MapGet("/api/process/current", () =>
            {
                try
                {
                    var status = GetProcessStatus();
                    return Results.Ok(new
                    {
                        success = true,
                        processId = status.ProcessId,
                        isOpen = status.IsOpen,
                        processName = status.ProcessName
                    });
                }
                catch (Exception ex)
                {
                    return Results.Ok(new { success = false, error = ex.Message });
                }
            })
            .WithName("GetProcessStatus")
            .WithDescription("Get the currently attached process ID and status")
            .WithOpenApi();
        }

        /// <summary>
        /// Gets the list of running processes.
        /// </summary>
        /// <returns>Array of process info</returns>
        private static (int ProcessId, string ProcessName)[] GetProcessList()
        {
            var processDict = Process.GetProcessList();
            return [.. processDict
                .Select(kvp => (kvp.Key, kvp.Value))
                .OrderBy(p => p.Value)];
        }

        /// <summary>
        /// Opens a process by ID or name.
        /// Throws if not found.
        /// </summary>
        /// <param name="process">Process ID (int) as string or process name</param>
        private static void OpenProcess(string process)
        {
            if (string.IsNullOrWhiteSpace(process))
                throw new ArgumentException("Process parameter is required", nameof(process));

            var processDict = Process.GetProcessList();

            // Try open by ID
            if (int.TryParse(process, out int pid))
            {
                if (pid <= 0)
                    throw new ArgumentException("Process ID must be greater than 0", nameof(process));

                if (!processDict.ContainsKey(pid))
                    throw new InvalidOperationException($"Process with ID {pid} not found");

                Process.OpenProcess(pid);
                return;
            }

            // Try open by name (case-insensitive)
            var target = processDict.FirstOrDefault(p =>
                string.Equals(p.Value, process, StringComparison.OrdinalIgnoreCase));

            if (target.Key == 0)
                throw new InvalidOperationException($"Process with name '{process}' not found");

            Process.OpenProcess(target.Key);
        }

        /// <summary>
        /// Gets the status of the currently opened process.
        /// </summary>
        /// <returns>Process ID, name, and open status</returns>
        private static (int ProcessId, bool IsOpen, string ProcessName) GetProcessStatus()
        {
            int processId = Process.GetOpenedProcessID();

            if (processId == 0)
                return (0, false, "");

            string processName = "Unknown";
            try
            {
                var processDict = Process.GetProcessList();
                if (processDict.TryGetValue(processId, out var name))
                    processName = name;
            }
            catch
            {
                // If process list fails, just return "Unknown"
                processName = "Unknown";
            }

            return (processId, true, processName);
        }
    }

    public record ProcessOpenRequest(string? Process);
}
