using System;
using CESDK.Classes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Tools
{
    public static class ThreadListTool
    {
        private static readonly ThreadList threadList = new();

        /// <summary>
        /// Maps thread list API endpoints
        /// </summary>
        public static void MapThreadListApi(this WebApplication app)
        {
            // GET /api/threads - Get list of all threads
            app.MapGet("/api/threads", () =>
            {
                try
                {
                    var threads = GetThreadList();
                    return Results.Ok(new { success = true, threads });
                }
                catch (Exception ex)
                {
                    return Results.Ok(new { success = false, error = ex.Message });
                }
            })
            .WithName("GetThreadList")
            .WithDescription("Get all threads in the opened process")
            .WithOpenApi();
        }

        /// <summary>
        /// Gets all threads as hex ID strings.
        /// </summary>
        /// <returns>Array of thread IDs in hex format</returns>
        private static string[] GetThreadList()
        {
            try
            {
                threadList.Refresh(); // Refresh to get current threads
                return threadList.GetAllThreads();
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
