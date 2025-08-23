using System;
using CESDK;
using CeMCP.Models;

namespace CeMCP.Tools
{
    public class ProcessTool
    {
        public ProcessListResponse GetProcessList()
        {
            try
            {
                var process = new Process();
                var processList = process.GetProcessList();

                return new ProcessListResponse
                {
                    ProcessList = processList.ToArray(),
                    Success = true
                };
            }
            catch (Exception e)
            {
                return new ProcessListResponse
                {
                    ProcessList = null,
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

                var processValue = request.Process.Trim();
                var process = new Process();
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

                    success = process.OpenByPid(pid);
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

                    success = process.OpenByName(processValue);
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
                var process = new Process();
                var (processId, isOpen, processName) = process.GetStatus();

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
                    IsOpen = isOpen,
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