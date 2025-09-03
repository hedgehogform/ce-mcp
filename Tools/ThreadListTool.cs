using System;
using System.Linq;
using CESDK.Classes;
using CeMCP.Models;

namespace CeMCP.Tools
{
    public class ThreadListTool
    {
        private readonly ThreadList threadList = new();

        public ThreadListResponse GetThreadList()
        {
            try
            {
                var threads = threadList.GetAllThreads();
                var threadStrings = threads.Select(t => t.ToString()).ToArray();

                return new ThreadListResponse
                {
                    ThreadList = threadStrings,
                    Success = true
                };
            }
            catch (SystemException ex)
            {
                return new ThreadListResponse
                {
                    ThreadList = null,
                    Success = false,
                    Error = ex.Message
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