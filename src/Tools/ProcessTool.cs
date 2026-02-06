using System;
using System.ComponentModel;
using System.Linq;
using CESDK.Classes;
using ModelContextProtocol.Server;

namespace Tools
{
    [McpServerToolType]
    public class ProcessTool
    {
        private ProcessTool() { }

        [McpServerTool(Name = "get_process_list"), Description("Get the list of all running processes")]
        public static object GetProcessList()
        {
            try
            {
                var processDict = Process.GetProcessList();
                var processes = processDict
                    .Select(kvp => new { processId = kvp.Key, processName = kvp.Value })
                    .OrderBy(p => p.processName)
                    .ToArray();

                return new { success = true, processes };
            }
            catch (Exception ex)
            {
                return new { success = false, error = ex.Message };
            }
        }

        [McpServerTool(Name = "open_process"), Description("Open a process by ID or name")]
        public static object OpenProcess(
            [Description("Process ID (integer) or process name to open")] string process)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(process))
                    return new { success = false, error = "Process parameter is required" };

                var processDict = Process.GetProcessList();

                if (int.TryParse(process, out int pid))
                {
                    if (pid <= 0)
                        return new { success = false, error = "Process ID must be greater than 0" };

                    if (!processDict.ContainsKey(pid))
                        return new { success = false, error = $"Process with ID {pid} not found" };

                    Process.OpenProcess(pid);
                    return new { success = true };
                }

                var target = processDict.FirstOrDefault(p =>
                    string.Equals(p.Value, process, StringComparison.OrdinalIgnoreCase));

                if (target.Key == 0)
                    return new { success = false, error = $"Process with name '{process}' not found" };

                Process.OpenProcess(target.Key);
                return new { success = true };
            }
            catch (Exception ex)
            {
                return new { success = false, error = ex.Message };
            }
        }

        [McpServerTool(Name = "get_current_process"), Description("Get the currently attached process ID and status")]
        public static object GetCurrentProcess()
        {
            try
            {
                int processId = Process.GetOpenedProcessID();

                if (processId == 0)
                    return new { success = true, processId = 0, isOpen = false, processName = "" };

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

                return new { success = true, processId, isOpen = true, processName };
            }
            catch (Exception ex)
            {
                return new { success = false, error = ex.Message };
            }
        }
    }
}
