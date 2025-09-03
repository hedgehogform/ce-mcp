using System;
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
                // Process list functionality not implemented in new CESDK yet
                return new ProcessListResponse
                {
                    ProcessList =
                    [
                        new ProcessInfo
                        {
                            ProcessId = 0,
                            ProcessName = "Process list functionality not implemented in new CESDK"
                        }
                    ],
                    Success = true
                };
            }
            catch (Exception e)
            {
                return new ProcessListResponse
                {
                    ProcessList = [],
                    Success = false,
                    Error = e.Message
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

                // Process name functionality not available in new CESDK yet
                var processName = "Unknown";

                // If no process is open, return an error message
                if (processId == 0)
                {
                    return new ProcessStatusResponse
                    {
                        ProcessId = 0,
                        IsOpen = false,
                        ProcessName = "",
                        Success = false,
                        Error = "No process is currently open"
                    };
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