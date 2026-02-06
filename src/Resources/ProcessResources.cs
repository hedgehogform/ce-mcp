using System;
using System.ComponentModel;
using CESDK.Classes;
using ModelContextProtocol.Server;

namespace Resources
{
    /// <summary>
    /// Process-related MCP resources. Resources represent readable state/data, unlike tools which perform actions.
    /// </summary>
    [McpServerResourceType]
    public class ProcessResources
    {
        private ProcessResources() { }

        [McpServerResource(
            UriTemplate = "process://current",
            Title = "Current Process"),
         Description("Get information about the currently attached Cheat Engine process")]
        public static string GetCurrentProcess()
        {
            try
            {
                int processId = Process.GetOpenedProcessID();

                if (processId == 0)
                {
                    return System.Text.Json.JsonSerializer.Serialize(new
                    {
                        processId = 0,
                        isOpen = false,
                        processName = "",
                        message = "No process is currently attached to Cheat Engine"
                    });
                }

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
                }

                return System.Text.Json.JsonSerializer.Serialize(new
                {
                    processId,
                    isOpen = true,
                    processName,
                    message = $"Attached to process: {processName} (PID: {processId})"
                });
            }
            catch (Exception ex)
            {
                return System.Text.Json.JsonSerializer.Serialize(new
                {
                    error = ex.Message
                });
            }
        }
        [McpServerResource(
            UriTemplate = "process://threads",
            Title = "Thread List"),
         Description("Get all threads in the currently opened process")]
        public static string GetThreadList()
        {
            try
            {
                var threadList = new ThreadList();
                threadList.Refresh();
                var threads = threadList.GetAllThreadIds();
                return System.Text.Json.JsonSerializer.Serialize(new
                {
                    success = true,
                    threads
                });
            }
            catch (Exception ex)
            {
                return System.Text.Json.JsonSerializer.Serialize(new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }
    }
}
