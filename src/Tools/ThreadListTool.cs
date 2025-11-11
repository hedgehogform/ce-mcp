using System;
using System.Linq;
using CESDK.Classes;

namespace Tools
{
    public static class ThreadListTool
    {
        private static readonly ThreadList threadList = new();

        /// <summary>
        /// Gets all threads as string representations.
        /// </summary>
        /// <returns>Array of thread strings</returns>
        public static string[] GetThreadList()
        {
            try
            {
                var threads = threadList.GetAllThreads();
                return threads.Select(t => t.ToString()).ToArray();
            }
            catch (SystemException ex)
            {
                throw new InvalidOperationException("Failed to get thread list.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("An unexpected error occurred while getting thread list.", ex);
            }
        }
    }
}
