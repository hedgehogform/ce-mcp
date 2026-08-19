using System;
using System.ComponentModel;
using CESDK.Classes;
using ModelContextProtocol.Server;

namespace Tools
{
    /// <summary>Target process creation, pause state, and foreground attachment.</summary>
    [McpServerToolType]
    public class ProcessControlTool
    {
        private ProcessControlTool() { }

        [McpServerTool(Name = "create_process"), Description(
            "Create and open a process through Cheat Engine. This launches an external executable.")]
        public static object CreateProcess(
            [Description("Executable path")] string path,
            [Description("Optional command-line parameters")] string? parameters = null,
            [Description("Start under the Cheat Engine debugger")] bool debug = false,
            [Description("Break at the process entry point; requires debug=true")] bool breakOnEntryPoint = false) =>
            ToolThread.OnMainThread(() =>
            {
                if (string.IsNullOrWhiteSpace(path))
                    return new { success = false, error = "Executable path is required" };
                if (breakOnEntryPoint && !debug)
                    return new { success = false, error = "breakOnEntryPoint requires debug=true" };
                ScanTool.ResetForProcessChange();
                ProcessControl.Create(path, parameters, debug, breakOnEntryPoint);
                return new { success = true };
            });

        [McpServerTool(Name = "pause_process"), Description("Pause the currently opened process")]
        public static object PauseProcess() => ToolThread.OnMainThread(() =>
        {
            RequireAttachedProcess();
            ProcessControl.Pause();
            return new { success = true, paused = true };
        });

        [McpServerTool(Name = "resume_process"), Description("Resume the currently opened process")]
        public static object ResumeProcess() => ToolThread.OnMainThread(() =>
        {
            RequireAttachedProcess();
            ProcessControl.Resume();
            return new { success = true, paused = false };
        });

        [McpServerTool(Name = "get_process_state"), Description("Get pause state and age for the currently opened process")]
        public static object GetProcessState() => ToolThread.OnMainThread(() =>
        {
            int processId = Process.GetOpenedProcessID();
            if (processId <= 0)
                return new { success = false, error = "No process is attached. Open a process first." };
            return new
            {
                success = true,
                processId,
                paused = ProcessControl.IsPaused(),
                ageMilliseconds = ProcessControl.GetProcessAge()
            };
        });

        [McpServerTool(Name = "open_foreground_process"), Description(
            "Open the process that owns the current foreground window in Cheat Engine")]
        public static object OpenForegroundProcess() => ToolThread.OnMainThread(() =>
        {
            int processId = ProcessControl.GetForegroundProcessId();
            if (processId <= 0)
                return new { success = false, error = "No foreground process was found" };
            if (Process.GetOpenedProcessID() != processId)
                ScanTool.ResetForProcessChange();
            Process.OpenProcess(processId);
            return new { success = true, processId };
        });
        private static void RequireAttachedProcess()
        {
            if (Process.GetOpenedProcessID() <= 0)
                throw new InvalidOperationException("No process is attached. Open a process first.");
        }

    }
}
