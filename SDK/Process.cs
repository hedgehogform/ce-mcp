using System;
using System.Collections.Generic;
using System.Diagnostics;
using CeMCP.Models;

namespace CESDK
{
    class Process : CEObjectWrapper
    {
        /// <summary>
        /// Get list of running processes
        /// </summary>
        /// <returns>List of process information</returns>
        public List<ProcessInfo> GetProcessList()
        {
            var processList = new List<ProcessInfo>();

            try
            {
                // Call getProcessList() directly using C# Lua API
                lua.GetGlobal("getProcessList");
                if (!lua.IsFunction(-1))
                {
                    lua.SetTop(0);
                    throw new InvalidOperationException("getProcessList function not found");
                }

                // Call the function
                int result = lua.PCall(0, 1);
                if (result != 0)
                {
                    string error = lua.ToString(-1);
                    lua.SetTop(0);
                    throw new InvalidOperationException($"getProcessList call failed: {error}");
                }

                // The result should be a table where keys are PIDs and values are process names
                if (lua.IsTable(-1))
                {
                    // Iterate through the table using lua_next
                    lua.PushNil(); // First key
                    while (lua.Next(-2) != 0)
                    {
                        // Key is at -2, value is at -1
                        if (lua.IsNumber(-2) && lua.IsString(-1))
                        {
                            int processId = (int)lua.ToInteger(-2);
                            string processName = lua.ToString(-1);
                            
                            if (processId > 0 && !string.IsNullOrEmpty(processName))
                            {
                                processList.Add(new ProcessInfo
                                {
                                    ProcessId = processId,
                                    ProcessName = processName
                                });
                            }
                        }
                        
                        // Remove value, keep key for next iteration
                        lua.Pop(1);
                    }
                }

                lua.SetTop(0);
                return processList;
            }
            catch (Exception ex)
            {
                lua.SetTop(0);
                throw new InvalidOperationException($"GetProcessList error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Open a process by PID
        /// </summary>
        /// <param name="pid">Process ID</param>
        /// <returns>True if successful</returns>
        public bool OpenByPid(int pid)
        {
            try
            {
                lua.GetGlobal("openProcess");
                if (!lua.IsFunction(-1))
                {
                    lua.SetTop(0);
                    throw new InvalidOperationException("openProcess function not found");
                }

                lua.PushInteger(pid);

                int result = lua.PCall(1, 1);
                if (result != 0)
                {
                    string error = lua.ToString(-1);
                    lua.SetTop(0);
                    throw new InvalidOperationException($"openProcess call failed: {error}");
                }

                bool success = lua.ToBoolean(-1);
                lua.SetTop(0);
                return success;
            }
            catch (Exception ex)
            {
                lua.SetTop(0);
                throw new InvalidOperationException($"OpenByPid error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Open a process by name
        /// </summary>
        /// <param name="processName">Process name</param>
        /// <returns>True if successful</returns>
        public bool OpenByName(string processName)
        {
            try
            {
                lua.GetGlobal("openProcess");
                if (!lua.IsFunction(-1))
                {
                    lua.SetTop(0);
                    throw new InvalidOperationException("openProcess function not found");
                }

                lua.PushString(processName);

                int result = lua.PCall(1, 1);
                if (result != 0)
                {
                    string error = lua.ToString(-1);
                    lua.SetTop(0);
                    throw new InvalidOperationException($"openProcess call failed: {error}");
                }

                bool success = lua.ToBoolean(-1);
                lua.SetTop(0);
                return success;
            }
            catch (Exception ex)
            {
                lua.SetTop(0);
                throw new InvalidOperationException($"OpenByName error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get the status of the currently opened process
        /// </summary>
        /// <returns>Process status information</returns>
        public (int processId, bool isOpen, string processName) GetStatus()
        {
            try
            {
                // Get the opened process ID using direct Lua API
                lua.GetGlobal("getOpenedProcessID");
                if (!lua.IsFunction(-1))
                {
                    lua.SetTop(0);
                    throw new InvalidOperationException("getOpenedProcessID function not found");
                }

                int result = lua.PCall(0, 1);
                if (result != 0)
                {
                    string error = lua.ToString(-1);
                    lua.SetTop(0);
                    throw new InvalidOperationException($"getOpenedProcessID call failed: {error}");
                }

                int processId = 0;
                if (lua.IsNumber(-1))
                {
                    processId = (int)lua.ToInteger(-1);
                }
                lua.Pop(1);

                bool isOpen = false;
                string processName = "";

                if (processId != 0)
                {
                    // Check if process is still accessible by getting process list
                    lua.GetGlobal("getProcessList");
                    if (lua.IsFunction(-1))
                    {
                        result = lua.PCall(0, 1);
                        if (result == 0 && lua.IsTable(-1))
                        {
                            // getProcessList() returns keys as numbers, not strings
                            lua.PushInteger(processId);
                            lua.GetTable(-2);
                            
                            if (!lua.IsNil(-1))
                            {
                                isOpen = true;
                                if (lua.IsString(-1))
                                {
                                    processName = lua.ToString(-1) ?? "";
                                }
                            }
                            lua.Pop(1); // Remove the process name/nil
                        }
                        lua.Pop(1); // Remove process list table
                    }
                    else
                    {
                        lua.SetTop(0);
                    }
                }

                // If we have a process ID but no name from Lua, try to get it from C#
                if (processId > 0 && string.IsNullOrEmpty(processName))
                {
                    processName = GetProcessNameFromPid(processId);
                }

                lua.SetTop(0);
                return (processId, isOpen, processName);
            }
            catch (Exception ex)
            {
                lua.SetTop(0);
                throw new InvalidOperationException($"GetStatus error: {ex.Message}", ex);
            }
        }

        private static string GetProcessNameFromPid(int processId)
        {
            try
            {
                using (var process = System.Diagnostics.Process.GetProcessById(processId))
                {
                    return process.ProcessName + ".exe";
                }
            }
            catch (ArgumentException)
            {
                // Process not found or access denied
                return "";
            }
            catch (Exception)
            {
                // Other exceptions (access denied, etc.)
                return "";
            }
        }
    }
}