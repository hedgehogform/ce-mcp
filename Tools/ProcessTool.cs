using System;
using System.Linq;
using CESDK.Classes;
using CeMCP.Models;

namespace CeMCP.Tools
{
    public class ProcessTool
    {
        public ProcessListResponse GetProcessList()
        {
            try
            {
                // Get process list using CESDK
                var processDict = Process.GetProcessList();

                // Convert Dictionary to ProcessInfo array
                var processList = processDict
                    .Select(kvp => new ProcessInfo
                    {
                        ProcessId = kvp.Key,
                        ProcessName = kvp.Value
                    })
                    .OrderBy(p => p.ProcessName) // Sort by name for better usability
                    .ToArray();

                return new ProcessListResponse
                {
                    ProcessList = processList,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return new ProcessListResponse
                {
                    ProcessList = [],
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        public BaseResponse OpenProcess(OpenProcessRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Process))
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Error = "Process parameter is required"
                    };
                }

                var processValue = request.Process!.Trim();
                bool success;

                if (int.TryParse(processValue, out int pid))
                {
                    if (pid <= 0)
                    {
                        return new BaseResponse
                        {
                            Success = false,
                            Error = "Process ID must be greater than 0"
                        };
                    }

                    Process.OpenProcess(pid);
                    success = true; // Assume success if no exception thrown
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(processValue))
                    {
                        return new BaseResponse
                        {
                            Success = false,
                            Error = "Process name cannot be empty"
                        };
                    }

                    Process.OpenProcess(processValue);
                    success = true; // Assume success if no exception thrown
                }

                return new BaseResponse { Success = success };
            }
            catch (Exception ex)
            {
                return new BaseResponse
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        public ProcessStatusResponse GetProcessStatus()
        {
            try
            {
                // Get process ID using CESDK
                var processId = Process.GetOpenedProcessID();

                // If no process is open, return closed status
                if (processId == 0)
                {
                    return new ProcessStatusResponse
                    {
                        ProcessId = 0,
                        IsOpen = false,
                        ProcessName = "",
                        Success = true
                    };
                }

                // Try to get the process name from the process list
                var processName = "Unknown";
                try
                {
                    var processDict = Process.GetProcessList();
                    if (processDict.TryGetValue(processId, out var name))
                    {
                        processName = name;
                    }
                }
                catch
                {
                    // If we can't get the process list, just use "Unknown"
                    processName = "Unknown";
                }

                return new ProcessStatusResponse
                {
                    ProcessId = processId,
                    IsOpen = true,
                    ProcessName = processName,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return new ProcessStatusResponse
                {
                    ProcessId = 0,
                    IsOpen = false,
                    ProcessName = "",
                    Success = false,
                    Error = ex.Message
                };
            }
        }
    }
}