using System;
using System.ComponentModel;
using CESDK.Classes;
using ModelContextProtocol.Server;

namespace Tools
{
    [McpServerToolType]
    public class ThreadListTool
    {
        private ThreadListTool() { }

        private static readonly ThreadList threadList = new();

        [McpServerTool(Name = "get_thread_list"), Description("Get all threads in the currently opened process")]
        public static object GetThreadList()
        {
            try
            {
                threadList.Refresh();
                var threads = threadList.GetAllThreads();
                return new { success = true, threads };
            }
            catch (Exception ex)
            {
                return new { success = false, error = ex.Message };
            }
        }
    }
}
