using System;
using System.Linq;
using CESDK.Classes;

namespace Tools
{
    public static class ProcessTool
    {
        /// <summary>
        /// Gets the list of running processes.
        /// </summary>
        /// <returns>Array of process info</returns>
        public static (int ProcessId, string ProcessName)[] GetProcessList()
        {
            var processDict = Process.GetProcessList();
            return [.. processDict
                .Select(kvp => (kvp.Key, kvp.Value))
                .OrderBy(p => p.Value)];
        }

        /// <summary>
        /// Opens a process by ID or name.
        /// </summary>
        /// <param name="process">Process ID (int) as string or process name</param>
        public static void OpenProcess(string process)
        {
            if (string.IsNullOrWhiteSpace(process))
                throw new ArgumentException("Process parameter is required", nameof(process));

            string processValue = process.Trim();

            if (int.TryParse(processValue, out int pid))
            {
                if (pid <= 0)
                    throw new ArgumentException("Process ID must be greater than 0", nameof(process));

                Process.OpenProcess(pid);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(processValue))
                    throw new ArgumentException("Process name cannot be empty", nameof(process));

                Process.OpenProcess(processValue);
            }
        }

        /// <summary>
        /// Gets the status of the currently opened process.
        /// </summary>
        /// <returns>Process ID, name, and open status</returns>
        public static (int ProcessId, bool IsOpen, string ProcessName) GetProcessStatus()
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
}
