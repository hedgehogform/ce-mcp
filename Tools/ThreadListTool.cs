using System;
using CESDK;
using CeMCP.Models;

namespace CeMCP.Tools
{
    public class ThreadListTool
    {
        public ThreadListResponse GetThreadList()
        {
            try
            {
                var threadList = new ThreadList();
                var threads = threadList.GetThreads();

                return new ThreadListResponse
                {
                    ThreadList = threads.ToArray(),
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return new ThreadListResponse
                {
                    ThreadList = null,
                    Success = false,
                    Error = ex.Message
                };
            }
        }
    }
}